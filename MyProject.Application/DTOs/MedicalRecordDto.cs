using System;
using System.Collections.Generic;

namespace MyProject.Application.DTOs;

public record MedicalRecordDto(
    int MedicalRecordId,
    int AppointmentId,
    string PatientName,
    string DoctorName,
    DateOnly AppointmentDate,
    TimeSpan AppointmentTime,
    string AppointmentStatus,
    string? Symptoms,
    string? Diagnosis,
    string? Treatment,
    string? Notes,
    DateTime CreatedAt,
    List<PrescriptionDto> Prescriptions,
    List<AppointmentLabTestDto> LabTests
);

public record CreateMedicalRecordRequest(
    int AppointmentId,
    string? Symptoms,
    string? Diagnosis,
    string? Treatment,
    string? Notes
);

public record PrescriptionDto(
    int PrescriptionId,
    int MedicalRecordId,
    string MedicineName,
    string? Dosage,
    int? Quantity,
    string? Instruction
);

public record CreatePrescriptionRequest(
    string MedicineName,
    string? Dosage,
    int? Quantity,
    string? Instruction
);

/// <summary>
/// Detailed view of a single prescription for the standalone view/print page,
/// including enough context (patient, doctor, dates) to render without extra lookups.
/// </summary>
public record PrescriptionDetailDto(
    int PrescriptionId,
    int MedicalRecordId,
    string MedicineName,
    string? Dosage,
    int? Quantity,
    string? Instruction,
    string PatientName,
    string DoctorName,
    DateTime PrescribedDate,
    string? Diagnosis
);
