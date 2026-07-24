using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MyProject.Application.DTOs;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;

namespace MyProject.Application.Services;

/// <summary>
/// Service for patient access to their medical records
/// </summary>
public class PatientMedicalRecordService
{
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IMedicalRecordRepository _medicalRecordRepo;
    private readonly IPrescriptionRepository _prescriptionRepo;
    private readonly IAppointmentLabTestRepository _appointmentLabTestRepo;

    public PatientMedicalRecordService(
        IAppointmentRepository appointmentRepo,
        IMedicalRecordRepository medicalRecordRepo,
        IPrescriptionRepository prescriptionRepo,
        IAppointmentLabTestRepository appointmentLabTestRepo)
    {
        _appointmentRepo = appointmentRepo;
        _medicalRecordRepo = medicalRecordRepo;
        _prescriptionRepo = prescriptionRepo;
        _appointmentLabTestRepo = appointmentLabTestRepo;
    }

    /// <summary>
    /// Gets medical records for a patient
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <returns>Collection of medical record DTOs for the patient</returns>
    public async Task<IEnumerable<MedicalRecordDto>> GetPatientMedicalRecordsAsync(int patientId)
    {
        var allAppointments = await _appointmentRepo.GetAllAsync();
        var patientAppointments = allAppointments
            .Where(a => a.PatientId == patientId)
            .ToList();

        var records = new List<MedicalRecordDto>();

        foreach (var appointment in patientAppointments)
        {
            var medicalRecord = await _medicalRecordRepo.GetByIdAsync(appointment.AppointmentId);
            if (medicalRecord != null)
            {
                records.Add(MapMedicalRecordToDto(medicalRecord, appointment));
            }
        }

        return records.OrderByDescending(r => r.CreatedAt).ToList();
    }

    /// <summary>
    /// Gets a specific medical record for a patient
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <param name="medicalRecordId">The medical record ID</param>
    /// <returns>Medical record DTO if found and belongs to patient; null otherwise</returns>
    public async Task<MedicalRecordDto?> GetPatientMedicalRecordAsync(int patientId, int medicalRecordId)
    {
        var record = await _medicalRecordRepo.GetByIdAsync(medicalRecordId);
        
        if (record?.Appointment?.PatientId != patientId)
        {
            return null; // Record does not belong to patient
        }

        return MapMedicalRecordToDto(record, record.Appointment);
    }

    /// <summary>
    /// Gets lab test results for a patient
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <returns>Collection of appointment lab test DTOs for the patient</returns>
    public async Task<IEnumerable<AppointmentLabTestDto>> GetPatientLabTestsAsync(int patientId)
    {
        var allAppointments = await _appointmentRepo.GetAllAsync();
        var patientAppointmentIds = allAppointments
            .Where(a => a.PatientId == patientId)
            .Select(a => a.AppointmentId)
            .ToList();

        var allLabTests = await _appointmentLabTestRepo.GetAllAsync();
        var patientLabTests = allLabTests
            .Where(alt => patientAppointmentIds.Contains(alt.AppointmentId))
            .ToList();

        return patientLabTests
            .Select(MapAppointmentLabTestToDto)
            .OrderByDescending(t => t.CreatedAt)
            .ToList();
    }

    /// <summary>
    /// Gets completed lab test results for a patient
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <returns>Collection of completed appointment lab test DTOs for the patient</returns>
    public async Task<IEnumerable<AppointmentLabTestDto>> GetPatientCompletedLabTestsAsync(int patientId)
    {
        var labTests = await GetPatientLabTestsAsync(patientId);
        return labTests
            .Where(t => t.Status == "Completed")
            .ToList();
    }

    /// <summary>
    /// Gets pending lab test results for a patient
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <returns>Collection of pending appointment lab test DTOs for the patient</returns>
    public async Task<IEnumerable<AppointmentLabTestDto>> GetPatientPendingLabTestsAsync(int patientId)
    {
        var labTests = await GetPatientLabTestsAsync(patientId);
        return labTests
            .Where(t => t.Status == "Pending")
            .ToList();
    }

    /// <summary>
    /// Gets prescriptions for a patient
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <returns>Collection of prescription DTOs for the patient</returns>
    public async Task<IEnumerable<PrescriptionDto>> GetPatientPrescriptionsAsync(int patientId)
    {
        var allAppointments = await _appointmentRepo.GetAllAsync();
        var patientAppointments = allAppointments
            .Where(a => a.PatientId == patientId)
            .ToList();

        var prescriptions = new List<PrescriptionDto>();

        foreach (var appointment in patientAppointments)
        {
            var medicalRecord = await _medicalRecordRepo.GetByIdAsync(appointment.AppointmentId);
            if (medicalRecord != null)
            {
                var recordPrescriptions = await _prescriptionRepo.GetByMedicalRecordIdAsync(medicalRecord.MedicalRecordId);
                foreach (var prescription in recordPrescriptions)
                {
                    prescriptions.Add(MapPrescriptionToDto(prescription, medicalRecord));
                }
            }
        }

        return prescriptions.ToList();
    }

    /// <summary>
    /// Gets a single prescription's full detail for a patient, verifying that the
    /// prescription's medical record belongs to one of the patient's own appointments.
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <param name="prescriptionId">The prescription ID</param>
    /// <returns>Prescription detail DTO if found and owned by patient; null otherwise</returns>
    public async Task<PrescriptionDetailDto?> GetPatientPrescriptionDetailAsync(int patientId, int prescriptionId)
    {
        var prescription = await _prescriptionRepo.GetByIdAsync(prescriptionId);
        var appointment = prescription?.MedicalRecord?.Appointment;

        if (prescription == null || appointment == null || appointment.PatientId != patientId)
        {
            return null; // Not found or does not belong to this patient
        }

        return new PrescriptionDetailDto(
            prescription.PrescriptionId,
            prescription.MedicalRecordId,
            prescription.MedicineName,
            prescription.Dosage,
            prescription.Quantity,
            prescription.Instruction,
            appointment.Patient?.FullName ?? "Unknown Patient",
            appointment.Doctor?.FullName ?? "Unknown Doctor",
            prescription.MedicalRecord!.CreatedAt,
            prescription.MedicalRecord!.Diagnosis
        );
    }

    /// <summary>
    /// Gets recent medical activity for a patient (summary)
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <param name="daysBack">Number of days to look back (default 90)</param>
    /// <returns>Summary of recent medical activity</returns>
    public async Task<dynamic> GetPatientRecentActivityAsync(int patientId, int daysBack = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);

        var allAppointments = await _appointmentRepo.GetAllAsync();
        var patientAppointments = allAppointments
            .Where(a => a.PatientId == patientId && a.CreatedAt >= cutoffDate)
            .ToList();

        var recentMedicalRecords = new List<MedicalRecordDto>();
        foreach (var appointment in patientAppointments)
        {
            var medicalRecord = await _medicalRecordRepo.GetByIdAsync(appointment.AppointmentId);
            if (medicalRecord != null)
            {
                recentMedicalRecords.Add(MapMedicalRecordToDto(medicalRecord, appointment));
            }
        }

        var labTests = await GetPatientLabTestsAsync(patientId);
        var recentLabTests = labTests
            .Where(t => t.CreatedAt >= cutoffDate)
            .ToList();

        var prescriptions = await GetPatientPrescriptionsAsync(patientId);

        return new
        {
            RecentAppointments = patientAppointments.Count,
            RecentMedicalRecords = recentMedicalRecords,
            RecentLabTests = recentLabTests,
            RecentPrescriptions = prescriptions.Count(),
            DateRange = new { From = cutoffDate, To = DateTime.UtcNow }
        };
    }

    /// <summary>
    /// Maps a MedicalRecord entity to MedicalRecordDto
    /// </summary>
    private MedicalRecordDto MapMedicalRecordToDto(MedicalRecord record, Appointment appointment)
    {
        var prescriptions = record.Prescriptions
            ?.Select(p => new PrescriptionDto(
                p.PrescriptionId,
                p.MedicalRecordId,
                p.MedicineName,
                p.Dosage,
                p.Quantity,
                p.Instruction
            ))
            .ToList() ?? new List<PrescriptionDto>();

        var labTests = appointment.AppointmentLabTests?.Select(lt => new AppointmentLabTestDto(
            lt.AppointmentLabTestId,
            lt.AppointmentId,
            appointment.Patient?.FullName ?? "Unknown",
            lt.Doctor?.FullName ?? "Unknown",
            lt.LabTestServiceId,
            lt.LabTestService?.ServiceName ?? "Unknown",
            lt.LabTestService?.Price ?? 0m,
            lt.TestDate,
            lt.Result ?? "",
            lt.Status,
            lt.Notes ?? "",
            lt.CreatedAt,
            new List<LabTestIndicatorValueDto>()
        )).ToList() ?? new List<AppointmentLabTestDto>();

        return new MedicalRecordDto(
            record.MedicalRecordId,
            record.AppointmentId,
            appointment.Patient?.FullName ?? "Unknown Patient",
            appointment.Doctor?.FullName ?? "Unknown Doctor",
            appointment.AppointmentDate,
            appointment.AppointmentTime,
            appointment.Status,
            record.Symptoms,
            record.Diagnosis,
            record.Treatment,
            record.Notes,
            record.CreatedAt,
            prescriptions,
            labTests
        );
    }

    /// <summary>
    /// Builds the structured indicator list for a lab test by combining the
    /// catalog definitions for the service with any saved values (ResultValues JSON).
    /// </summary>
    private static List<LabTestIndicatorValueDto> BuildIndicators(string serviceName, string? resultValuesJson)
    {
        var defs = LabTestIndicatorCatalog.GetIndicators(serviceName);
        if (defs.Count == 0) return new List<LabTestIndicatorValueDto>();

        Dictionary<string, string>? savedValues = null;
        if (!string.IsNullOrWhiteSpace(resultValuesJson))
        {
            try
            {
                savedValues = JsonSerializer.Deserialize<Dictionary<string, string>>(resultValuesJson);
            }
            catch (JsonException)
            {
                savedValues = null;
            }
        }

        return defs.Select(d => new LabTestIndicatorValueDto(
            d.Key,
            d.Label,
            d.Unit,
            d.NormalRange,
            savedValues != null && savedValues.TryGetValue(d.Key, out var v) ? v : null
        )).ToList();
    }

    /// <summary>
    /// Maps an AppointmentLabTest entity to AppointmentLabTestDto
    /// </summary>
    private AppointmentLabTestDto MapAppointmentLabTestToDto(AppointmentLabTest alt)
    {
        var patientName = alt.Appointment?.Patient?.FullName ?? "Unknown Patient";
        var doctorName = alt.Appointment?.Doctor?.FullName ?? "Unknown Doctor";
        var serviceName = alt.LabTestService?.ServiceName ?? "Unknown Service";
        var price = alt.LabTestService?.Price ?? 0m;

        return new AppointmentLabTestDto(
            alt.AppointmentLabTestId,
            alt.AppointmentId,
            patientName,
            doctorName,
            alt.LabTestServiceId,
            serviceName,
            price,
            alt.TestDate,
            alt.Result,
            alt.Status,
            alt.Notes,
            alt.CreatedAt,
            BuildIndicators(serviceName, alt.ResultValues)
        );
    }

    /// <summary>
    /// Maps a Prescription entity to PrescriptionDto
    /// </summary>
    private PrescriptionDto MapPrescriptionToDto(Prescription prescription, MedicalRecord medicalRecord)
    {
        return new PrescriptionDto(
            prescription.PrescriptionId,
            prescription.MedicalRecordId,
            prescription.MedicineName,
            prescription.Dosage,
            prescription.Quantity,
            prescription.Instruction
        );
    }
}
