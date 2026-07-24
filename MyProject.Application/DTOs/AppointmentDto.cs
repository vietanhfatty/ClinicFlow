using System;
using System.ComponentModel.DataAnnotations;

namespace MyProject.Application.DTOs;

public record AppointmentDto(
    int AppointmentId,
    int PatientId,
    string PatientName,
    int DoctorId,
    string DoctorName,
    int? StaffId,
    string? StaffName,
    DateOnly AppointmentDate,
    TimeSpan AppointmentTime,
    string Status,
    string? Reason,
    DateTime CreatedAt,
    DateTime? CheckInTime = null,
    DateTime? QueuePriorityTime = null,
    bool IsWalkIn = false
);

public record CreateWalkInRequest(
    [Required(ErrorMessage = "Patient is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid patient.")]
    int PatientId,

    [Required(ErrorMessage = "Doctor is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid doctor.")]
    int DoctorId,

    [StringLength(500, ErrorMessage = "Reason must not exceed 500 characters.")]
    string? Reason,

    int? StaffId = null
);

public record CreateAppointmentRequest(
    [Required(ErrorMessage = "Patient is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid patient.")]
    int PatientId,

    [Required(ErrorMessage = "Doctor is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid doctor.")]
    int DoctorId,

    [Required(ErrorMessage = "Appointment date is required.")]
    DateOnly AppointmentDate,

    [Required(ErrorMessage = "Appointment time is required.")]
    TimeSpan AppointmentTime,

    string? Status,

    [StringLength(500, ErrorMessage = "Reason must not exceed 500 characters.")]
    string? Reason,

    int? StaffId = null
);

public record UpdateAppointmentRequest(
    [Required(ErrorMessage = "Patient is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid patient.")]
    int PatientId,

    [Required(ErrorMessage = "Doctor is required.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid doctor.")]
    int DoctorId,

    [Required(ErrorMessage = "Appointment date is required.")]
    DateOnly AppointmentDate,

    [Required(ErrorMessage = "Appointment time is required.")]
    TimeSpan AppointmentTime,

    string? Status,

    [StringLength(500, ErrorMessage = "Reason must not exceed 500 characters.")]
    string? Reason
);
