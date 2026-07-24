using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyProject.Application.DTOs;

/// <summary>
/// DTO for Lab Test Service (catalog information)
/// </summary>
public record LabTestServiceDto(
    int LabTestServiceId,
    string ServiceName,
    string? Description,
    decimal Price,
    string? Category,
    bool IsActive,
    DateTime CreatedAt
);

/// <summary>
/// A single structured indicator value entered for a lab test result
/// (e.g. "RBC" = "5.1" for a CBC test), enriched with display metadata.
/// </summary>
public record LabTestIndicatorValueDto(
    string Key,
    string Label,
    string? Unit,
    string? NormalRange,
    string? Value
);

/// <summary>
/// DTO for Appointment Lab Test (request/result information)
/// </summary>
public record AppointmentLabTestDto(
    int AppointmentLabTestId,
    int AppointmentId,
    string PatientName,
    string DoctorName,
    int LabTestServiceId,
    string ServiceName,
    decimal Price,
    DateTime? TestDate,
    string? Result,
    string Status,
    string? Notes,
    DateTime CreatedAt,
    List<LabTestIndicatorValueDto> Indicators
);

/// <summary>
/// Request to update the price of a lab test service (Admin only)
/// </summary>
public record UpdateLabTestServicePriceRequest(
    [Required(ErrorMessage = "Price is required")]
    [Range(0, double.MaxValue, ErrorMessage = "Price must be a non-negative value")]
    decimal Price
);

/// <summary>
/// Request to create a new lab test for an appointment
/// </summary>
public record CreateLabTestRequest(
    [Required(ErrorMessage = "Appointment is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid appointment")]
    int AppointmentId,

    [Required(ErrorMessage = "Lab test service is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid lab test service")]
    int LabTestServiceId,

    int? DoctorId,

    [StringLength(500, ErrorMessage = "Notes must not exceed 500 characters")]
    string? Notes
);

/// <summary>
/// Request to update lab test results. For services with structured
/// indicators (see LabTestIndicatorCatalog), <see cref="IndicatorValues"/> holds
/// the entered value per indicator key; <see cref="Result"/> is then optional
/// and only used as a free-text summary/conclusion. For services without
/// structured indicators, <see cref="Result"/> is the required free-text result.
/// </summary>
public record UpdateLabTestResultRequest(
    [Required(ErrorMessage = "Test date is required")]
    DateTime TestDate,

    string? Result,

    [Required(ErrorMessage = "Status is required")]
    string Status,

    [StringLength(500, ErrorMessage = "Notes must not exceed 500 characters")]
    string? Notes,

    Dictionary<string, string>? IndicatorValues = null
);

/// <summary>
/// Statistics for lab tests dashboard
/// </summary>
public record LabTestStatisticsDto(
    int TotalLabTests,
    int CompletedLabTests,
    int PendingLabTests,
    List<TopLabTestDto> TopLabTestsByFrequency,
    List<StatusCountDto> LabTestsByStatus
);

/// <summary>
/// Top lab tests by frequency of requests
/// </summary>
public record TopLabTestDto(
    string ServiceName,
    int RequestCount,
    decimal TotalPrice
);

/// <summary>
/// Status count for statistics
/// </summary>
public record StatusCountDto(
    string Status,
    int Count,
    double Percentage
);
