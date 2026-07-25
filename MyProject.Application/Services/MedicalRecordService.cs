using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyProject.Application.DTOs;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;

namespace MyProject.Application.Services;

public class MedicalRecordService
{
    private readonly IMedicalRecordRepository _medicalRecordRepo;
    private readonly IPrescriptionRepository _prescriptionRepo;

    public MedicalRecordService(
        IMedicalRecordRepository medicalRecordRepo,
        IPrescriptionRepository prescriptionRepo)
    {
        _medicalRecordRepo = medicalRecordRepo;
        _prescriptionRepo = prescriptionRepo;
    }

    public async Task<IEnumerable<MedicalRecordDto>> GetAllAsync()
    {
        var list = await _medicalRecordRepo.GetAllAsync();
        return list.Select(MapToDto);
    }

    public async Task<MedicalRecordDto?> GetByIdAsync(int id)
    {
        var record = await _medicalRecordRepo.GetByIdAsync(id);
        return record == null ? null : MapToDto(record);
    }

    public async Task<MedicalRecordDto?> GetByAppointmentIdAsync(int appointmentId)
    {
        var record = await _medicalRecordRepo.GetByAppointmentIdAsync(appointmentId);
        return record == null ? null : MapToDto(record);
    }

    public async Task<IEnumerable<MedicalRecordDto>> GetByPatientIdAsync(int patientId)
    {
        var list = await _medicalRecordRepo.GetByPatientIdAsync(patientId);
        return list.Select(MapToDto);
    }

    public async Task<MedicalRecordDto?> GetByIdAndPatientIdAsync(int medicalRecordId, int patientId)
    {
        var record = await _medicalRecordRepo.GetByIdAndPatientIdAsync(medicalRecordId, patientId);
        return record == null ? null : MapToDto(record);
    }

    public async Task<MedicalRecordDto> CreateAsync(CreateMedicalRecordRequest req)
    {
        var record = new MedicalRecord
        {
            AppointmentId = req.AppointmentId,
            Symptoms = req.Symptoms?.Trim(),
            Diagnosis = req.Diagnosis?.Trim(),
            Treatment = req.Treatment?.Trim(),
            Notes = req.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _medicalRecordRepo.AddAsync(record);

        // Fetch again to ensure all navigation properties are loaded
        var addedRecord = await _medicalRecordRepo.GetByIdAsync(record.MedicalRecordId);
        return MapToDto(addedRecord!);
    }

    public async Task AddPrescriptionsAsync(int medicalRecordId, List<CreatePrescriptionRequest> reqs)
    {
        foreach (var req in reqs)
        {
            var prescription = new Prescription
            {
                MedicalRecordId = medicalRecordId,
                MedicineName = req.MedicineName.Trim(),
                Dosage = req.Dosage?.Trim(),
                Quantity = req.Quantity,
                Instruction = req.Instruction?.Trim()
            };
            await _prescriptionRepo.AddAsync(prescription);
        }
    }

    private MedicalRecordDto MapToDto(MedicalRecord mr)
    {
        var patientName = mr.Appointment?.Patient?.FullName ?? "Unknown Patient";
        var doctorName = mr.Appointment?.Doctor?.FullName ?? "Unknown Doctor";

        var prescriptions = mr.Prescriptions.Select(p => new PrescriptionDto(
            p.PrescriptionId,
            p.MedicalRecordId,
            p.MedicineName,
            p.Dosage,
            p.Quantity,
            p.Instruction
        )).ToList();

        var labTests = mr.Appointment?.AppointmentLabTests?.Select(lt => new AppointmentLabTestDto(
            lt.AppointmentLabTestId,
            lt.AppointmentId,
            lt.Appointment?.Patient?.FullName ?? "Unknown",
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
            mr.MedicalRecordId,
            mr.AppointmentId,
            patientName,
            doctorName,
            mr.Appointment?.AppointmentDate ?? DateOnly.MinValue,
            mr.Appointment?.AppointmentTime ?? TimeSpan.Zero,
            mr.Appointment?.Status ?? "Unknown",
            mr.Symptoms,
            mr.Diagnosis,
            mr.Treatment,
            mr.Notes,
            mr.CreatedAt,
            prescriptions,
            labTests
        );
    }
}
