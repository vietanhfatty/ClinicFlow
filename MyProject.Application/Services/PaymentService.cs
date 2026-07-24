using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyProject.Application.DTOs;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;

namespace MyProject.Application.Services;

/// <summary>
/// Service for managing patient payments and payment requests
/// </summary>
public class PaymentService
{
    private readonly IPaymentRepository _paymentRepo;
    private readonly IPatientRepository _patientRepo;
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IAppointmentLabTestRepository _labTestRepo;

    public PaymentService(
        IPaymentRepository paymentRepo,
        IPatientRepository patientRepo,
        IAppointmentRepository appointmentRepo,
        IAppointmentLabTestRepository labTestRepo)
    {
        _paymentRepo = paymentRepo;
        _patientRepo = patientRepo;
        _appointmentRepo = appointmentRepo;
        _labTestRepo = labTestRepo;
    }

    /// <summary>
    /// Gets all payments
    /// </summary>
    /// <returns>Collection of all payment DTOs</returns>
    public async Task<IEnumerable<PaymentDto>> GetAllPaymentsAsync()
    {
        var payments = await _paymentRepo.GetAllAsync();
        return payments.Select(p => MapToDto(p)).ToList();
    }

    /// <summary>
    /// Gets a single payment by ID
    /// </summary>
    /// <param name="paymentId">The payment ID</param>
    /// <returns>Payment DTO if found; null otherwise</returns>
    public async Task<PaymentDto?> GetPaymentByIdAsync(int paymentId)
    {
        var payment = await _paymentRepo.GetByIdAsync(paymentId);
        return payment == null ? null : MapToDto(payment);
    }

    /// <summary>
    /// Gets all payments for a specific patient
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <returns>Collection of patient's payment DTOs</returns>
    public async Task<IEnumerable<PaymentDto>> GetPatientPaymentsAsync(int patientId)
    {
        var payments = await _paymentRepo.GetByPatientIdAsync(patientId);
        return payments.Select(p => MapToDto(p)).ToList();
    }

    /// <summary>
    /// Gets all pending payments for a specific patient
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <returns>Collection of pending payment DTOs</returns>
    public async Task<IEnumerable<PaymentDto>> GetPatientPendingPaymentsAsync(int patientId)
    {
        var payments = await _paymentRepo.GetByPatientIdAsync(patientId);
        return payments
            .Where(p => p.Status == "Pending")
            .Select(p => MapToDto(p))
            .ToList();
    }

    /// <summary>
    /// Gets all completed payments for a specific patient
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <returns>Collection of completed payment DTOs</returns>
    public async Task<IEnumerable<PaymentDto>> GetPatientCompletedPaymentsAsync(int patientId)
    {
        var payments = await _paymentRepo.GetByPatientIdAsync(patientId);
        return payments
            .Where(p => p.Status == "Completed")
            .Select(p => MapToDto(p))
            .ToList();
    }

    /// <summary>
    /// Creates a new payment request for a specific appointment.
    /// Amount is auto-calculated from completed LabTests of that appointment.
    /// </summary>
    /// <param name="req">The payment request details (AppointmentId required, Amount auto-calculated)</param>
    /// <returns>The created payment DTO</returns>
    /// <exception cref="KeyNotFoundException">Thrown when patient or appointment not found</exception>
    /// <exception cref="ArgumentException">Thrown when no completed lab tests found</exception>
    public async Task<PaymentDto> RequestPaymentAsync(CreatePaymentRequest req)
    {
        var patient = await _patientRepo.GetByIdAsync(req.PatientId)
            ?? throw new KeyNotFoundException($"Patient with ID {req.PatientId} not found");

        var appointment = await _appointmentRepo.GetByIdAsync(req.AppointmentId)
            ?? throw new KeyNotFoundException($"Appointment with ID {req.AppointmentId} not found");

        if (appointment.PatientId != req.PatientId)
        {
            throw new ArgumentException("Appointment does not belong to this patient.");
        }

        var allLabTests = await _labTestRepo.GetAllAsync();
        var completedTests = allLabTests
            .Where(lt => lt.AppointmentId == req.AppointmentId && lt.Status == "Completed")
            .ToList();

        if (!completedTests.Any())
        {
            throw new ArgumentException("No completed lab tests found for this appointment.");
        }

        decimal totalAmount = completedTests.Sum(lt => lt.LabTestService?.Price ?? 0m);

        var labTestDtos = completedTests.Select(lt => new AppointmentLabTestDto(
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
        )).ToList();

        var reason = !string.IsNullOrWhiteSpace(req.Reason)
            ? req.Reason.Trim()
            : $"Lab Tests: {string.Join(", ", labTestDtos.Select(lt => lt.ServiceName))}";

        var payment = new Payment
        {
            PatientId = req.PatientId,
            AppointmentId = req.AppointmentId,
            Amount = totalAmount,
            Reason = reason,
            Status = "Pending",
            RequestDate = DateTime.UtcNow,
            PaidDate = null
        };

        await _paymentRepo.AddAsync(payment);

        var createdPayment = await _paymentRepo.GetByIdAsync(payment.PaymentId);
        return MapToDto(createdPayment!, appointment, labTestDtos);
    }

    /// <summary>
    /// Marks a payment as paid
    /// </summary>
    /// <param name="paymentId">The payment ID</param>
    /// <param name="req">The mark as paid request with paid date</param>
    /// <returns>The updated payment DTO</returns>
    /// <exception cref="KeyNotFoundException">Thrown when payment is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when payment is already completed</exception>
    public async Task<PaymentDto> MarkAsPaidAsync(int paymentId, MarkPaymentAsPaidRequest req)
    {
        var payment = await _paymentRepo.GetByIdAsync(paymentId)
            ?? throw new KeyNotFoundException($"Payment with ID {paymentId} not found");

        if (payment.Status == "Completed")
        {
            throw new InvalidOperationException("Payment has already been marked as paid");
        }

        if (payment.Status == "Cancelled")
        {
            throw new InvalidOperationException("Cannot mark a cancelled payment as paid");
        }

        payment.Status = "Completed";
        payment.PaidDate = req.PaidDate;

        await _paymentRepo.UpdateAsync(payment);

        // Fetch the updated payment to ensure navigation properties are loaded
        var updatedPayment = await _paymentRepo.GetByIdAsync(paymentId);
        return MapToDto(updatedPayment!);
    }

    /// <summary>
    /// Cancels a payment request
    /// </summary>
    /// <param name="paymentId">The payment ID</param>
    /// <returns>The updated payment DTO</returns>
    /// <exception cref="KeyNotFoundException">Thrown when payment is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when payment cannot be cancelled</exception>
    public async Task<PaymentDto> CancelPaymentAsync(int paymentId)
    {
        var payment = await _paymentRepo.GetByIdAsync(paymentId)
            ?? throw new KeyNotFoundException($"Payment with ID {paymentId} not found");

        if (payment.Status == "Completed")
        {
            throw new InvalidOperationException("Cannot cancel a payment that has already been paid");
        }

        if (payment.Status == "Cancelled")
        {
            throw new InvalidOperationException("Payment has already been cancelled");
        }

        payment.Status = "Cancelled";

        await _paymentRepo.UpdateAsync(payment);

        // Fetch the updated payment to ensure navigation properties are loaded
        var updatedPayment = await _paymentRepo.GetByIdAsync(paymentId);
        return MapToDto(updatedPayment!);
    }

    /// <summary>
    /// Gets payment statistics
    /// </summary>
    /// <returns>Statistics about payments</returns>
    public async Task<dynamic> GetPaymentStatisticsAsync()
    {
        var payments = await _paymentRepo.GetAllAsync();
        var paymentsList = payments.ToList();

        var totalPayments = paymentsList.Count;
        var pendingPayments = paymentsList.Count(p => p.Status == "Pending");
        var completedPayments = paymentsList.Count(p => p.Status == "Completed");
        var cancelledPayments = paymentsList.Count(p => p.Status == "Cancelled");

        var totalAmount = paymentsList.Sum(p => p.Amount);
        var completedAmount = paymentsList
            .Where(p => p.Status == "Completed")
            .Sum(p => p.Amount);
        var pendingAmount = paymentsList
            .Where(p => p.Status == "Pending")
            .Sum(p => p.Amount);

        return new
        {
            TotalPayments = totalPayments,
            PendingPayments = pendingPayments,
            CompletedPayments = completedPayments,
            CancelledPayments = cancelledPayments,
            TotalAmount = totalAmount,
            CompletedAmount = completedAmount,
            PendingAmount = pendingAmount
        };
    }

    /// <summary>
    /// Maps a Payment entity to PaymentDto
    /// </summary>
    private PaymentDto MapToDto(Payment payment, Appointment? appointment = null, List<AppointmentLabTestDto>? labTests = null)
    {
        var appointmentInfo = appointment != null
            ? $"{appointment.AppointmentDate:yyyy-MM-dd} {appointment.AppointmentTime:hh\\:mm} — {appointment.Doctor?.FullName ?? "N/A"}"
            : null;

        return new PaymentDto(
            payment.PaymentId,
            payment.PatientId,
            payment.Patient?.FullName ?? "Unknown Patient",
            payment.AppointmentId,
            appointmentInfo,
            payment.Amount,
            payment.Reason,
            payment.Status,
            payment.RequestDate,
            payment.PaidDate,
            labTests
        );
    }
}
