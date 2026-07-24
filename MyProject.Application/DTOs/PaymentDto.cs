using System;
using System.ComponentModel.DataAnnotations;

namespace MyProject.Application.DTOs;

/// <summary>
/// DTO for displaying payment information
/// </summary>
public record PaymentDto(
    int PaymentId,
    int PatientId,
    string PatientName,
    int? AppointmentId,
    string? AppointmentInfo,
    decimal Amount,
    string Reason,
    string Status,
    DateTime RequestDate,
    DateTime? PaidDate,
    List<AppointmentLabTestDto>? LabTests = null
);

/// <summary>
/// Request to create a new payment request for a specific appointment.
/// Amount is auto-calculated from completed LabTests of that appointment.
/// </summary>
public record CreatePaymentRequest(
    [Required(ErrorMessage = "Patient is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid patient")]
    int PatientId,

    [Required(ErrorMessage = "Appointment is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select an appointment")]
    int AppointmentId,

    [StringLength(500, MinimumLength = 0, ErrorMessage = "Reason must be between 0 and 500 characters")]
    string? Reason
);

/// <summary>
/// Request to mark a payment as paid
/// </summary>
public record MarkPaymentAsPaidRequest(
    [Required(ErrorMessage = "Paid date is required")]
    DateTime PaidDate
);
