using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public AppointmentService(
        IAppointmentRepository repo,
        IPatientRepository patientRepo,
        IDoctorRepository doctorRepo,
        IUserRepository userRepo)
    {
        _repo = repo;
        _patientRepo = patientRepo;
        _doctorRepo = doctorRepo;
        _userRepo = userRepo;
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

        // Ensure slot has availability (maximum 2 confirmed/in-progress/completed appointments)
        await ValidateAppointmentLimitAsync(req.DoctorId, req.AppointmentDate, req.AppointmentTime);

        var status = string.IsNullOrWhiteSpace(req.Status) ? "Pending" : req.Status.Trim();

        var appointment = new Appointment
        {
            PatientId = req.PatientId,
            DoctorId = req.DoctorId,
            AppointmentDate = req.AppointmentDate,
            AppointmentTime = req.AppointmentTime,
            Status = status,
            Reason = req.Reason?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(appointment);

        if (status == "Confirmed" || status == "InProgress" || status == "Completed")
        {
            await CancelOtherPendingAppointmentsIfLimitReachedAsync(req.DoctorId, req.AppointmentDate, req.AppointmentTime);
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
        await ValidateAppointmentLimitAsync(req.DoctorId, req.AppointmentDate, req.AppointmentTime, id);

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
            await CancelOtherPendingAppointmentsIfLimitReachedAsync(req.DoctorId, req.AppointmentDate, req.AppointmentTime);
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

        if (appointment.Status != "Pending")
        {
            throw new InvalidOperationException($"Cannot check-in. Current appointment status is '{appointment.Status}' but must be 'Pending'.");
        }

        await ValidateAppointmentLimitAsync(appointment.DoctorId, appointment.AppointmentDate, appointment.AppointmentTime, id);

        appointment.Status = "Confirmed";
        await _repo.UpdateAsync(appointment);

        await CancelOtherPendingAppointmentsIfLimitReachedAsync(appointment.DoctorId, appointment.AppointmentDate, appointment.AppointmentTime);
    }

    public async Task ConfirmAsync(int id)
    {
        var appointment = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Appointment with ID {id} not found");

        if (appointment.Status != "Pending")
        {
            throw new InvalidOperationException($"Cannot confirm. Current status is '{appointment.Status}' but must be 'Pending'.");
        }

        await ValidateAppointmentLimitAsync(appointment.DoctorId, appointment.AppointmentDate, appointment.AppointmentTime, id);

        appointment.Status = "Confirmed";
        await _repo.UpdateAsync(appointment);

        await CancelOtherPendingAppointmentsIfLimitReachedAsync(appointment.DoctorId, appointment.AppointmentDate, appointment.AppointmentTime);
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

        if (appointment.Status != "InProgress")
        {
            throw new InvalidOperationException($"Cannot complete appointment. Current status is '{appointment.Status}' but must be 'InProgress'.");
        }

        appointment.Status = "Completed";
        await _repo.UpdateAsync(appointment);
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

    private async Task ValidateAppointmentLimitAsync(int doctorId, DateOnly date, TimeSpan time, int? excludingAppointmentId = null)
    {
        var list = await _repo.GetAllAsync();
        var count = list.Count(a => 
            a.DoctorId == doctorId && 
            a.AppointmentDate == date && 
            a.AppointmentTime == time && 
            (excludingAppointmentId == null || a.AppointmentId != excludingAppointmentId.Value) &&
            (a.Status == "Confirmed" || a.Status == "InProgress" || a.Status == "Completed"));

        if (count >= 2)
        {
            throw new ArgumentException("Ca này đã được đặt full, vui lòng chọn ca khác.");
        }
    }

    private async Task CancelOtherPendingAppointmentsIfLimitReachedAsync(int doctorId, DateOnly date, TimeSpan time)
    {
        var list = await _repo.GetAllAsync();
        var confirmedCount = list.Count(a => 
            a.DoctorId == doctorId && 
            a.AppointmentDate == date && 
            a.AppointmentTime == time && 
            (a.Status == "Confirmed" || a.Status == "InProgress" || a.Status == "Completed"));

        if (confirmedCount >= 2)
        {
            var pendingAppointments = list.Where(a => 
                a.DoctorId == doctorId && 
                a.AppointmentDate == date && 
                a.AppointmentTime == time && 
                a.Status == "Pending")
                .ToList();

            foreach (var pending in pendingAppointments)
            {
                pending.Status = "Cancelled";
                await _repo.UpdateAsync(pending);
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
            a.AppointmentDate,
            a.AppointmentTime,
            a.Status,
            a.Reason,
            a.CreatedAt
        );
    }
}
