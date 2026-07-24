using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MyProject.Application.Configuration;
using MyProject.Application.DTOs;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;

namespace MyProject.Application.Services;

public class AppointmentService
{
    private readonly IAppointmentRepository _repo;
    private readonly IPatientRepository _patientRepo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly IUserRepository _userRepo;
    private readonly NotificationService _notificationService;
    private readonly QueueSettings _queueSettings;

    public AppointmentService(
        IAppointmentRepository repo,
        IPatientRepository patientRepo,
        IDoctorRepository doctorRepo,
        IUserRepository userRepo,
        NotificationService notificationService,
        IOptions<QueueSettings> queueSettings)
    {
        _repo = repo;
        _patientRepo = patientRepo;
        _doctorRepo = doctorRepo;
        _userRepo = userRepo;
        _notificationService = notificationService;
        _queueSettings = queueSettings.Value;
    }

    /// <summary>
    /// Resolves the UserId that corresponds to a patient's login account.
    /// The legacy schema has no PatientId-&gt;UserId FK, so accounts are linked
    /// by convention: User.Username == Patient.Phone.
    /// </summary>
    private async Task<int?> ResolvePatientUserIdAsync(int patientId)
    {
        var patient = await _patientRepo.GetByIdAsync(patientId);
        if (patient == null || string.IsNullOrWhiteSpace(patient.Phone)) return null;
        var user = await _userRepo.GetByUsernameAsync(patient.Phone);
        return user?.UserId;
    }

    public async Task<IEnumerable<AppointmentDto>> GetAllAsync()
    {
        var list = await _repo.GetAllAsync();
        return list.Select(MapToDto);
    }

    public async Task<AppointmentDto?> GetByIdAsync(int id)
    {
        var a = await _repo.GetByIdAsync(id);
        return a is null ? null : MapToDto(a);
    }

    public async Task CreateAsync(CreateAppointmentRequest req)
    {
        // Check patient exists
        var patient = await _patientRepo.GetByIdAsync(req.PatientId)
            ?? throw new KeyNotFoundException($"Patient with ID {req.PatientId} not found");

        // Check doctor exists
        var doctor = await _doctorRepo.GetByIdAsync(req.DoctorId)
            ?? throw new KeyNotFoundException($"Doctor with ID {req.DoctorId} not found");

        // Ensure slot has availability
        await ValidateAppointmentLimitAsync(req.DoctorId, req.PatientId, req.AppointmentDate, req.AppointmentTime);

        var status = string.IsNullOrWhiteSpace(req.Status) ? "Pending" : req.Status.Trim();

        var appointment = new Appointment
        {
            PatientId = req.PatientId,
            DoctorId = req.DoctorId,
            StaffId = req.StaffId,
            AppointmentDate = req.AppointmentDate,
            AppointmentTime = req.AppointmentTime,
            Status = status,
            Reason = req.Reason?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(appointment);

        if (status == "Confirmed" || status == "InProgress" || status == "Completed")
        {
            await CancelOtherPendingAppointmentsIfLimitReachedAsync(req.DoctorId, req.AppointmentDate, req.AppointmentTime, appointment.AppointmentId);
        }
    }

    public async Task UpdateAsync(int id, UpdateAppointmentRequest req)
    {
        var appointment = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Appointment with ID {id} not found");

        // Check patient exists
        _ = await _patientRepo.GetByIdAsync(req.PatientId)
            ?? throw new KeyNotFoundException($"Patient with ID {req.PatientId} not found");

        // Check doctor exists
        _ = await _doctorRepo.GetByIdAsync(req.DoctorId)
            ?? throw new KeyNotFoundException($"Doctor with ID {req.DoctorId} not found");

        // Validate the slot availability excluding the current appointment
        await ValidateAppointmentLimitAsync(req.DoctorId, req.PatientId, req.AppointmentDate, req.AppointmentTime, id);

        var status = string.IsNullOrWhiteSpace(req.Status) ? "Pending" : req.Status.Trim();

        appointment.PatientId = req.PatientId;
        appointment.DoctorId = req.DoctorId;
        appointment.AppointmentDate = req.AppointmentDate;
        appointment.AppointmentTime = req.AppointmentTime;
        appointment.Status = status;
        appointment.Reason = req.Reason?.Trim();

        await _repo.UpdateAsync(appointment);

        if (status == "Confirmed" || status == "InProgress" || status == "Completed")
        {
            await CancelOtherPendingAppointmentsIfLimitReachedAsync(req.DoctorId, req.AppointmentDate, req.AppointmentTime, id);
        }
    }

    public async Task DeleteAsync(int id)
    {
        await _repo.DeleteAsync(id);
    }

    public async Task CheckInAsync(int id)
    {
        var appointment = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Appointment with ID {id} not found");

        var previousStatus = appointment.Status;
        if (previousStatus != "Pending" && previousStatus != "Late")
        {
            throw new InvalidOperationException($"Cannot check-in. Current appointment status is '{appointment.Status}' but must be 'Pending' or 'Late'.");
        }

        var now = DateTime.Now;
        var scheduledStart = appointment.AppointmentDate.ToDateTime(TimeOnly.FromTimeSpan(appointment.AppointmentTime));
        var graceDeadline = scheduledStart.AddMinutes(_queueSettings.GracePeriodMinutes);
        var withinGrace = previousStatus == "Pending" && now <= graceDeadline;

        // Only enforce the slot limit while the patient is still within their booked grace window.
        // Late arrivals lose their reserved slot and rejoin the queue by real arrival time (Cách A).
        if (withinGrace)
        {
            await ValidateAppointmentLimitAsync(appointment.DoctorId, appointment.PatientId, appointment.AppointmentDate, appointment.AppointmentTime, id);
        }

        appointment.CheckInTime = now;
        appointment.QueuePriorityTime = withinGrace ? scheduledStart : now;
        appointment.Status = "Confirmed";
        await _repo.UpdateAsync(appointment);

        if (withinGrace)
        {
            await CancelOtherPendingAppointmentsIfLimitReachedAsync(appointment.DoctorId, appointment.AppointmentDate, appointment.AppointmentTime, id);
        }
    }

    public async Task CreateWalkInAsync(CreateWalkInRequest req, int? staffId = null)
    {
        var patient = await _patientRepo.GetByIdAsync(req.PatientId)
            ?? throw new KeyNotFoundException($"Patient with ID {req.PatientId} not found");

        var doctor = await _doctorRepo.GetByIdAsync(req.DoctorId)
            ?? throw new KeyNotFoundException($"Doctor with ID {req.DoctorId} not found");

        var now = DateTime.Now;

        // Walk-in ca không chiếm slot cố định nên bỏ qua ValidateAppointmentLimitAsync.
        var appointment = new Appointment
        {
            PatientId = req.PatientId,
            DoctorId = req.DoctorId,
            StaffId = staffId,
            AppointmentDate = DateOnly.FromDateTime(now),
            AppointmentTime = now.TimeOfDay,
            Reason = req.Reason?.Trim(),
            Status = "Confirmed",
            IsWalkIn = true,
            CheckInTime = now,
            QueuePriorityTime = now,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(appointment);
    }

    /// <summary>
    /// Quét các lịch Pending hôm nay chưa check-in đã quá grace period, chuyển sang 'Late'
    /// và bắn thông báo cho bệnh nhân. Gọi định kỳ từ background service.
    /// </summary>
    public async Task<int> MarkOverdueAppointmentsAsLateAsync()
    {
        var now = DateTime.Now;
        var today = DateOnly.FromDateTime(now);
        var list = await _repo.GetAllAsync();

        var overdue = list.Where(a =>
            a.Status == "Pending" &&
            a.AppointmentDate == today &&
            a.CheckInTime == null &&
            now > a.AppointmentDate.ToDateTime(TimeOnly.FromTimeSpan(a.AppointmentTime)).AddMinutes(_queueSettings.GracePeriodMinutes))
            .ToList();

        foreach (var appt in overdue)
        {
            appt.Status = "Late";
            await _repo.UpdateAsync(appt);

            var patientUserId = await ResolvePatientUserIdAsync(appt.PatientId);
            if (patientUserId.HasValue)
            {
                await _notificationService.NotifyAsync(
                    patientUserId.Value,
                    "Quá giờ hẹn",
                    "Bạn đã quá giờ hẹn, vị trí hàng chờ của bạn đã được xếp lại theo thời gian bạn check-in",
                    "Appointment",
                    appt.AppointmentId);
            }
        }

        return overdue.Count;
    }

    public async Task ConfirmAsync(int id)
    {
        var appointment = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Appointment with ID {id} not found");

        if (appointment.Status != "Pending")
        {
            throw new InvalidOperationException($"Cannot confirm. Current status is '{appointment.Status}' but must be 'Pending'.");
        }

        await ValidateAppointmentLimitAsync(appointment.DoctorId, appointment.PatientId, appointment.AppointmentDate, appointment.AppointmentTime, id);

        appointment.Status = "Confirmed";
        await _repo.UpdateAsync(appointment);

        await CancelOtherPendingAppointmentsIfLimitReachedAsync(appointment.DoctorId, appointment.AppointmentDate, appointment.AppointmentTime, id);

        var patientUserId = await ResolvePatientUserIdAsync(appointment.PatientId);
        if (patientUserId.HasValue)
        {
            await _notificationService.NotifyAsync(
                patientUserId.Value,
                "Lịch hẹn đã được xác nhận",
                $"Lịch hẹn ngày {appointment.AppointmentDate:dd/MM/yyyy} lúc {appointment.AppointmentTime:hh\\:mm} đã được xác nhận.",
                "Appointment",
                appointment.AppointmentId);
        }
    }

    public async Task StartExaminationAsync(int id)
    {
        var appointment = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Appointment with ID {id} not found");

        if (appointment.Status != "Confirmed")
        {
            throw new InvalidOperationException($"Cannot start examination. Current status is '{appointment.Status}' but must be 'Confirmed'.");
        }

        appointment.Status = "InProgress";
        await _repo.UpdateAsync(appointment);
    }

    public async Task CompleteAsync(int id)
    {
        var appointment = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Appointment with ID {id} not found");

        if (appointment.Status != "Confirmed" && appointment.Status != "InProgress")
        {
            throw new InvalidOperationException($"Cannot complete appointment. Current status is '{appointment.Status}' but must be 'Confirmed' or 'InProgress'.");
        }

        appointment.Status = "Completed";
        await _repo.UpdateAsync(appointment);

        var patientUserId = await ResolvePatientUserIdAsync(appointment.PatientId);
        if (patientUserId.HasValue)
        {
            await _notificationService.NotifyAsync(
                patientUserId.Value,
                "Khám bệnh hoàn tất",
                $"Lịch hẹn ngày {appointment.AppointmentDate:dd/MM/yyyy} đã hoàn tất. Vui lòng xem hồ sơ bệnh án và đơn thuốc.",
                "MedicalRecord",
                appointment.AppointmentId);
        }
    }

    public async Task<IEnumerable<AppointmentDto>> GetByPatientUserIdAsync(int userId)
    {
        var user = await _userRepo.GetByIdAsync(userId);
        if (user == null) return Enumerable.Empty<AppointmentDto>();

        var list = await _repo.GetAllAsync();
        return list
            .Where(a => a.Patient != null && a.Patient.Phone == user.Username)
            .Select(MapToDto);
    }

    public async Task<IEnumerable<AppointmentDto>> GetByPatientIdAsync(int patientId)
    {
        var list = await _repo.GetAllAsync();
        return list
            .Where(a => a.PatientId == patientId)
            .Select(MapToDto);
    }

    private async Task ValidateAppointmentLimitAsync(int doctorId, int patientId, DateOnly date, TimeSpan time, int? excludingAppointmentId = null)
    {
        var list = await _repo.GetAllAsync();
        var newStart = time;
        var newEnd = newStart.Add(TimeSpan.FromMinutes(30));

        // 1. Check doctor overlap
        var doctorOverlap = list.FirstOrDefault(a => 
            a.DoctorId == doctorId && 
            a.AppointmentDate == date && 
            (excludingAppointmentId == null || a.AppointmentId != excludingAppointmentId.Value) &&
            a.Status != "Cancelled" &&
            (a.AppointmentTime < newEnd && newStart < a.AppointmentTime.Add(TimeSpan.FromMinutes(30))));

        if (doctorOverlap != null)
        {
            throw new ArgumentException($"Bác sĩ đã có lịch hẹn khác từ {doctorOverlap.AppointmentTime:hh\\:mm} đến {doctorOverlap.AppointmentTime.Add(TimeSpan.FromMinutes(30)):hh\\:mm}. Vui lòng chọn giờ khác.");
        }

        // 2. Check patient overlap
        var patientOverlap = list.FirstOrDefault(a => 
            a.PatientId == patientId && 
            a.AppointmentDate == date && 
            (excludingAppointmentId == null || a.AppointmentId != excludingAppointmentId.Value) &&
            a.Status != "Cancelled" &&
            (a.AppointmentTime < newEnd && newStart < a.AppointmentTime.Add(TimeSpan.FromMinutes(30))));

        if (patientOverlap != null)
        {
            throw new ArgumentException($"Bệnh nhân đã có lịch hẹn khác từ {patientOverlap.AppointmentTime:hh\\:mm} đến {patientOverlap.AppointmentTime.Add(TimeSpan.FromMinutes(30)):hh\\:mm} trùng với ca này.");
        }
    }

    private async Task CancelOtherPendingAppointmentsIfLimitReachedAsync(int doctorId, DateOnly date, TimeSpan time, int excludingAppointmentId)
    {
        var list = await _repo.GetAllAsync();
        var start = time;
        var end = start.Add(TimeSpan.FromMinutes(30));

        var pendingAppointments = list.Where(a => 
            a.DoctorId == doctorId && 
            a.AppointmentDate == date && 
            a.Status == "Pending" &&
            a.AppointmentId != excludingAppointmentId &&
            (a.AppointmentTime < end && start < a.AppointmentTime.Add(TimeSpan.FromMinutes(30))))
            .ToList();

        foreach (var pending in pendingAppointments)
        {
            pending.Status = "Cancelled";
            await _repo.UpdateAsync(pending);

            var patientUserId = await ResolvePatientUserIdAsync(pending.PatientId);
            if (patientUserId.HasValue)
            {
                await _notificationService.NotifyAsync(
                    patientUserId.Value,
                    "Lịch hẹn đã bị hủy",
                    $"Lịch hẹn ngày {pending.AppointmentDate:dd/MM/yyyy} lúc {pending.AppointmentTime:hh\\:mm} đã bị hủy do trùng khung giờ với lịch hẹn khác đã được xác nhận.",
                    "Appointment",
                    pending.AppointmentId);
            }
        }
    }

    private AppointmentDto MapToDto(Appointment a)
    {
        return new AppointmentDto(
            a.AppointmentId,
            a.PatientId,
            a.Patient != null ? a.Patient.FullName : "Unknown Patient",
            a.DoctorId,
            a.Doctor != null ? a.Doctor.FullName : "Unknown Doctor",
            a.StaffId,
            a.Staff != null ? a.Staff.FullName : null,
            a.AppointmentDate,
            a.AppointmentTime,
            a.Status,
            a.Reason,
            a.CreatedAt,
            a.CheckInTime,
            a.QueuePriorityTime,
            a.IsWalkIn
        );
    }
}
