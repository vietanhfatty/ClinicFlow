using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyProject.Application.DTOs;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;

namespace MyProject.Application.Services;

public class AppointmentBillService
{
    private readonly IAppointmentBillRepository _billRepo;
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IAppointmentLabTestRepository _labTestRepo;

    public AppointmentBillService(
        IAppointmentBillRepository billRepo,
        IAppointmentRepository appointmentRepo,
        IAppointmentLabTestRepository labTestRepo)
    {
        _billRepo = billRepo;
        _appointmentRepo = appointmentRepo;
        _labTestRepo = labTestRepo;
    }

    public async Task<IEnumerable<AppointmentBillDto>> GetAllBillsAsync()
    {
        var bills = await _billRepo.GetAllAsync();
        return bills.Select(MapToDto).ToList();
    }

    public async Task<AppointmentBillDto?> GetBillByIdAsync(int billId)
    {
        var bill = await _billRepo.GetByIdAsync(billId);
        return bill == null ? null : MapToDto(bill);
    }

    public async Task<IEnumerable<AppointmentBillDto>> GetBillsByPatientIdAsync(int patientId)
    {
        var bills = await _billRepo.GetByPatientIdAsync(patientId);
        return bills.Select(MapToDto).ToList();
    }

    public async Task<IEnumerable<AppointmentBillDto>> GetBillsByAppointmentIdAsync(int appointmentId)
    {
        var bills = await _billRepo.GetByAppointmentIdAsync(appointmentId);
        return bills.Select(MapToDto).ToList();
    }

    public async Task<AppointmentBillDto?> GetBillByAppointmentIdAsync(int appointmentId)
    {
        var bill = await _billRepo.GetByAppointmentIdFirstOrDefaultAsync(appointmentId);
        return bill == null ? null : MapToDto(bill);
    }

    public async Task<AppointmentBillDto> CreateBillAsync(CreateAppointmentBillRequest request)
    {
        var appointment = await _appointmentRepo.GetByIdAsync(request.AppointmentId)
            ?? throw new KeyNotFoundException($"Appointment with ID {request.AppointmentId} not found");

        if (appointment.PatientId != request.PatientId)
        {
            throw new ArgumentException("Appointment does not belong to this patient.");
        }

        var existingBill = await _billRepo.GetByAppointmentIdFirstOrDefaultAsync(request.AppointmentId);
        if (existingBill != null)
        {
            throw new InvalidOperationException("A bill already exists for this appointment.");
        }

        decimal totalAmount = request.ExaminationFee + request.LabTestFee;

        var bill = new AppointmentBill
        {
            AppointmentId = request.AppointmentId,
            PatientId = request.PatientId,
            ExaminationFee = request.ExaminationFee,
            LabTestFee = request.LabTestFee,
            TotalAmount = totalAmount,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            Notes = request.Notes
        };

        await _billRepo.AddAsync(bill);

        var createdBill = await _billRepo.GetByIdAsync(bill.BillId);
        return MapToDto(createdBill!);
    }

    public async Task<AppointmentBillDto> CreateBillFromLabTestsAsync(int appointmentId, int patientId, decimal examinationFee, int? staffId = null, string? notes = null)
    {
        var appointment = await _appointmentRepo.GetByIdAsync(appointmentId)
            ?? throw new KeyNotFoundException($"Appointment with ID {appointmentId} not found");

        if (appointment.PatientId != patientId)
        {
            throw new ArgumentException("Appointment does not belong to this patient.");
        }

        var existingBill = await _billRepo.GetByAppointmentIdFirstOrDefaultAsync(appointmentId);
        if (existingBill != null)
        {
            throw new InvalidOperationException("A bill already exists for this appointment.");
        }

        var allLabTests = await _labTestRepo.GetAllAsync();
        var appointmentLabTests = allLabTests.Where(lt => lt.AppointmentId == appointmentId).ToList();
        decimal labTestFee = appointmentLabTests.Sum(lt => lt.LabTestService?.Price ?? 0m);

        decimal totalAmount = examinationFee + labTestFee;

        var bill = new AppointmentBill
        {
            AppointmentId = appointmentId,
            PatientId = patientId,
            StaffId = staffId,
            ExaminationFee = examinationFee,
            LabTestFee = labTestFee,
            TotalAmount = totalAmount,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            Notes = notes
        };

        await _billRepo.AddAsync(bill);

        var createdBill = await _billRepo.GetByIdAsync(bill.BillId);
        return MapToDto(createdBill!);
    }

    public async Task<AppointmentBillDto> UpdateBillAsync(int billId, UpdateAppointmentBillRequest request)
    {
        var bill = await _billRepo.GetByIdAsync(billId)
            ?? throw new KeyNotFoundException($"Bill with ID {billId} not found");

        if (bill.Status == "Paid")
        {
            throw new InvalidOperationException("Cannot update a paid bill.");
        }

        if (request.ExaminationFee.HasValue)
            bill.ExaminationFee = request.ExaminationFee.Value;

        if (request.LabTestFee.HasValue)
            bill.LabTestFee = request.LabTestFee.Value;

        if (request.Notes != null)
            bill.Notes = request.Notes;

        bill.TotalAmount = bill.ExaminationFee + bill.LabTestFee;

        await _billRepo.UpdateAsync(bill);

        var updatedBill = await _billRepo.GetByIdAsync(billId);
        return MapToDto(updatedBill!);
    }

    public async Task<AppointmentBillDto> MarkAsPaidAsync(int billId, MarkBillAsPaidRequest request)
    {
        var bill = await _billRepo.GetByIdAsync(billId)
            ?? throw new KeyNotFoundException($"Bill with ID {billId} not found");

        if (bill.Status == "Paid")
        {
            throw new InvalidOperationException("Bill has already been marked as paid.");
        }

        if (bill.Status == "Cancelled")
        {
            throw new InvalidOperationException("Cannot mark a cancelled bill as paid.");
        }

        bill.Status = "Paid";
        bill.PaidAt = request.PaidAt ?? DateTime.UtcNow;

        if (request.Notes != null)
            bill.Notes = request.Notes;

        await _billRepo.UpdateAsync(bill);

        var updatedBill = await _billRepo.GetByIdAsync(billId);
        return MapToDto(updatedBill!);
    }

    public async Task<AppointmentBillDto> CancelBillAsync(int billId)
    {
        var bill = await _billRepo.GetByIdAsync(billId)
            ?? throw new KeyNotFoundException($"Bill with ID {billId} not found");

        if (bill.Status == "Paid")
        {
            throw new InvalidOperationException("Cannot cancel a paid bill.");
        }

        if (bill.Status == "Cancelled")
        {
            throw new InvalidOperationException("Bill has already been cancelled.");
        }

        bill.Status = "Cancelled";
        await _billRepo.UpdateAsync(bill);

        var updatedBill = await _billRepo.GetByIdAsync(billId);
        return MapToDto(updatedBill!);
    }

    public async Task DeleteBillAsync(int billId)
    {
        var bill = await _billRepo.GetByIdAsync(billId)
            ?? throw new KeyNotFoundException($"Bill with ID {billId} not found");

        if (bill.Status == "Paid")
        {
            throw new InvalidOperationException("Cannot delete a paid bill.");
        }

        await _billRepo.DeleteAsync(billId);
    }

    private AppointmentBillDto MapToDto(AppointmentBill bill)
    {
        var appointmentInfo = bill.Appointment != null
            ? $"{bill.Appointment.AppointmentDate:yyyy-MM-dd} {bill.Appointment.AppointmentTime:hh\\:mm} — {bill.Appointment.Doctor?.FullName ?? "N/A"}"
            : null;

        var labTests = bill.Appointment?.AppointmentLabTests?.Select(lt => new AppointmentLabTestDto(
            lt.AppointmentLabTestId,
            lt.AppointmentId,
            lt.Appointment?.Patient?.FullName ?? bill.Patient?.FullName ?? "Unknown",
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
        )).ToList();

        return new AppointmentBillDto(
            bill.BillId,
            bill.AppointmentId,
            appointmentInfo ?? "",
            bill.PatientId,
            bill.Patient?.FullName ?? "Unknown Patient",
            bill.StaffId,
            bill.Staff?.FullName,
            bill.ExaminationFee,
            bill.LabTestFee,
            bill.TotalAmount,
            bill.Status,
            bill.CreatedAt,
            bill.PaidAt,
            bill.Notes,
            labTests
        );
    }
}
