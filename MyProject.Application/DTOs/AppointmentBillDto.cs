using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyProject.Application.DTOs;

public record AppointmentBillDto(
    int BillId,
    int AppointmentId,
    string AppointmentInfo,
    int PatientId,
    string PatientName,
    int? StaffId,
    string? StaffName,
    decimal ExaminationFee,
    decimal LabTestFee,
    decimal TotalAmount,
    string Status,
    DateTime CreatedAt,
    DateTime? PaidAt,
    string? Notes,
    List<AppointmentLabTestDto>? LabTests = null
);

public record CreateAppointmentBillRequest(
    [Required(ErrorMessage = "Appointment is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid appointment")]
    int AppointmentId,

    [Required(ErrorMessage = "Patient is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid patient")]
    int PatientId,

    [Required(ErrorMessage = "Examination fee is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Examination fee must be a positive number")]
    decimal ExaminationFee,

    string? Notes,

    [Range(0, double.MaxValue, ErrorMessage = "Lab test fee must be a positive number")]
    decimal LabTestFee = 0
);

public record UpdateAppointmentBillRequest(
    [Range(0, double.MaxValue, ErrorMessage = "Examination fee must be a positive number")]
    decimal? ExaminationFee,

    [Range(0, double.MaxValue, ErrorMessage = "Lab test fee must be a positive number")]
    decimal? LabTestFee,

    [StringLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    string? Notes
);

public record MarkBillAsPaidRequest(
    string? Notes = null,
    DateTime? PaidAt = null
);
