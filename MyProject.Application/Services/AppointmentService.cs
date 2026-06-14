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

    public AppointmentService(
        IAppointmentRepository repo,
        IPatientRepository patientRepo,
        IDoctorRepository doctorRepo)
    {
        _repo = repo;
        _patientRepo = patientRepo;
        _doctorRepo = doctorRepo;
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

        // Check for double booking
        var existingAppointments = await _repo.GetAllAsync();
        var isDoctorBusy = existingAppointments.Any(a => 
            a.DoctorId == req.DoctorId && 
            a.AppointmentDate == req.AppointmentDate && 
            a.AppointmentTime == req.AppointmentTime && 
            !a.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase));
            
        if (isDoctorBusy)
        {
            throw new ArgumentException("Bác sĩ đã có lịch hẹn vào ngày và giờ này. Vui lòng chọn khung giờ khác.");
        }

        var isPatientBusy = existingAppointments.Any(a => 
            a.PatientId == req.PatientId && 
            a.AppointmentDate == req.AppointmentDate && 
            a.AppointmentTime == req.AppointmentTime && 
            !a.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase));

        if (isPatientBusy)
        {
            throw new ArgumentException("Bạn đã có lịch hẹn khác vào ngày và giờ này. Vui lòng chọn khung giờ khác.");
        }

        var appointment = new Appointment
        {
            PatientId = req.PatientId,
            DoctorId = req.DoctorId,
            AppointmentDate = req.AppointmentDate,
            AppointmentTime = req.AppointmentTime,
            Status = string.IsNullOrWhiteSpace(req.Status) ? "Pending" : req.Status.Trim(),
            Reason = req.Reason?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(appointment);
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

        // Check for double booking
        var existingAppointments = await _repo.GetAllAsync();
        var isDoctorBusy = existingAppointments.Any(a => 
            a.AppointmentId != id &&
            a.DoctorId == req.DoctorId && 
            a.AppointmentDate == req.AppointmentDate && 
            a.AppointmentTime == req.AppointmentTime && 
            !a.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase));
            
        if (isDoctorBusy)
        {
            throw new ArgumentException("Bác sĩ đã có lịch hẹn vào ngày và giờ này. Vui lòng chọn khung giờ khác.");
        }

        var isPatientBusy = existingAppointments.Any(a => 
            a.AppointmentId != id &&
            a.PatientId == req.PatientId && 
            a.AppointmentDate == req.AppointmentDate && 
            a.AppointmentTime == req.AppointmentTime && 
            !a.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase));

        if (isPatientBusy)
        {
            throw new ArgumentException("Bạn đã có lịch hẹn khác vào ngày và giờ này. Vui lòng chọn khung giờ khác.");
        }

        appointment.PatientId = req.PatientId;
        appointment.DoctorId = req.DoctorId;
        appointment.AppointmentDate = req.AppointmentDate;
        appointment.AppointmentTime = req.AppointmentTime;
        appointment.Status = string.IsNullOrWhiteSpace(req.Status) ? "Pending" : req.Status.Trim();
        appointment.Reason = req.Reason?.Trim();

        await _repo.UpdateAsync(appointment);
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

        appointment.Status = "Confirmed";
        await _repo.UpdateAsync(appointment);
    }

    public async Task ConfirmAsync(int id)
    {
        var appointment = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Appointment with ID {id} not found");

        if (appointment.Status != "Pending")
        {
            throw new InvalidOperationException($"Cannot confirm. Current status is '{appointment.Status}' but must be 'Pending'.");
        }

        appointment.Status = "Confirmed";
        await _repo.UpdateAsync(appointment);
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
        var list = await _repo.GetAllAsync();
        return list
            .Where(a => a.Patient != null && a.Patient.UserId == userId)
            .Select(MapToDto);
    }

    public async Task<IEnumerable<AppointmentDto>> GetByPatientIdAsync(int patientId)
    {
        var list = await _repo.GetAllAsync();
        return list
            .Where(a => a.PatientId == patientId)
            .Select(MapToDto);
    }

    private AppointmentDto MapToDto(Appointment a)
    {
        return new AppointmentDto(
            a.AppointmentId,
            a.PatientId,
            a.Patient != null && a.Patient.User != null ? a.Patient.User.FullName : "Unknown Patient",
            a.DoctorId,
            a.Doctor != null && a.Doctor.User != null ? a.Doctor.User.FullName : "Unknown Doctor",
            a.AppointmentDate,
            a.AppointmentTime,
            a.Status,
            a.Reason,
            a.CreatedAt
        );
    }
}
