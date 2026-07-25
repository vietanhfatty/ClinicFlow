using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs;
using MyProject.Application.Services;
using MyProject.Domain.IRepositories;

namespace MyProject.WebApi.Controllers;

[ApiController]
[Route("api/patient-portal")]
[Authorize(Roles = "Patient")]
public class PatientPortalController : ControllerBase
{
    private readonly AppointmentService _appointmentService;
    private readonly PaymentService _paymentService;
    private readonly AppointmentBillService _billService;
    private readonly PatientMedicalRecordService _medicalRecordService;
    private readonly AuthService _authService;
    private readonly IPatientRepository _patientRepo;

    public PatientPortalController(
        AppointmentService appointmentService,
        PaymentService paymentService,
        AppointmentBillService billService,
        PatientMedicalRecordService medicalRecordService,
        AuthService authService,
        IPatientRepository patientRepo)
    {
        _appointmentService = appointmentService;
        _paymentService = paymentService;
        _billService = billService;
        _medicalRecordService = medicalRecordService;
        _authService = authService;
        _patientRepo = patientRepo;
    }

    /// <summary>
    /// Gets the current patient ID from claims with fallback lookup
    /// </summary>
    private async Task<int> GetPatientIdAsync()
    {
        var patientIdClaim = User.FindFirst("PatientId")?.Value;
        if (!string.IsNullOrEmpty(patientIdClaim) && int.TryParse(patientIdClaim, out int patientId))
        {
            return patientId;
        }

        // Fallback: lookup by UserId
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
        {
            var patients = await _patientRepo.GetAllAsync();
            var patient = patients.FirstOrDefault(p => p.UserId == userId);
            if (patient != null)
            {
                return patient.PatientId;
            }
        }

        throw new UnauthorizedAccessException("Patient ID not found in claims");
    }

    /// <summary>
    /// Gets the current user ID from claims
    /// </summary>
    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userId;
    }

    #region Appointments

    /// <summary>
    /// Gets all appointments for the current patient
    /// </summary>
    [HttpGet("appointments")]
    public async Task<IActionResult> GetMyAppointments()
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            var appointments = await _appointmentService.GetByPatientIdAsync(patientId);
            return Ok(appointments);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific appointment for the current patient
    /// </summary>
    [HttpGet("appointments/{appointmentId}")]
    public async Task<IActionResult> GetMyAppointment(int appointmentId)
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            var appointment = await _appointmentService.GetByIdAsync(appointmentId);
            
            if (appointment == null)
                return NotFound(new { Message = "Appointment not found" });
            
            // Verify ownership
            if (appointment.PatientId != patientId)
                return Forbid();

            return Ok(appointment);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Gets upcoming appointments for the current patient
    /// </summary>
    [HttpGet("appointments/upcoming")]
    public async Task<IActionResult> GetUpcomingAppointments()
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            var allAppointments = await _appointmentService.GetByPatientIdAsync(patientId);
            
            var today = DateOnly.FromDateTime(DateTime.Today);
            var upcomingAppointments = new List<AppointmentDto>();
            
            foreach (var apt in allAppointments)
            {
                if (apt.AppointmentDate >= today && apt.Status != "Cancelled")
                {
                    upcomingAppointments.Add(apt);
                }
            }

            return Ok(upcomingAppointments.OrderBy(a => a.AppointmentDate).ThenBy(a => a.AppointmentTime));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    #endregion

    #region Medical Records

    /// <summary>
    /// Gets all medical records for the current patient
    /// </summary>
    [HttpGet("medical-records")]
    public async Task<IActionResult> GetMyMedicalRecords()
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            var records = await _medicalRecordService.GetPatientMedicalRecordsAsync(patientId);
            return Ok(records);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific medical record for the current patient
    /// </summary>
    [HttpGet("medical-records/{medicalRecordId}")]
    public async Task<IActionResult> GetMyMedicalRecord(int medicalRecordId)
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            var record = await _medicalRecordService.GetPatientMedicalRecordAsync(patientId, medicalRecordId);
            
            if (record == null)
                return NotFound(new { Message = "Medical record not found or does not belong to this patient" });

            return Ok(record);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    #endregion

    #region Lab Tests

    /// <summary>
    /// Gets all lab tests for the current patient
    /// </summary>
    [HttpGet("lab-tests")]
    public async Task<IActionResult> GetMyLabTests()
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            var labTests = await _medicalRecordService.GetPatientLabTestsAsync(patientId);
            return Ok(labTests);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Gets completed lab tests for the current patient
    /// </summary>
    [HttpGet("lab-tests/completed")]
    public async Task<IActionResult> GetMyCompletedLabTests()
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            var labTests = await _medicalRecordService.GetPatientCompletedLabTestsAsync(patientId);
            return Ok(labTests);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Gets pending lab tests for the current patient
    /// </summary>
    [HttpGet("lab-tests/pending")]
    public async Task<IActionResult> GetMyPendingLabTests()
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            var labTests = await _medicalRecordService.GetPatientPendingLabTestsAsync(patientId);
            return Ok(labTests);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    #endregion

    #region Payments

    /// <summary>
    /// Gets all payments for the current patient
    /// </summary>
    [HttpGet("payments")]
    public async Task<IActionResult> GetMyPayments()
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            var payments = await _paymentService.GetPatientPaymentsAsync(patientId);
            return Ok(payments);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Gets pending payments for the current patient
    /// </summary>
    [HttpGet("payments/pending")]
    public async Task<IActionResult> GetMyPendingPayments()
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            var payments = await _paymentService.GetPatientPendingPaymentsAsync(patientId);
            return Ok(payments);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Gets completed payments for the current patient
    /// </summary>
    [HttpGet("payments/completed")]
    public async Task<IActionResult> GetMyCompletedPayments()
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            var payments = await _paymentService.GetPatientCompletedPaymentsAsync(patientId);
            return Ok(payments);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific payment for the current patient
    /// </summary>
    [HttpGet("payments/{paymentId}")]
    public async Task<IActionResult> GetMyPayment(int paymentId)
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
            
            if (payment == null)
                return NotFound(new { Message = "Payment not found" });
            
            // Verify ownership
            if (payment.PatientId != patientId)
                return Forbid();

            return Ok(payment);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new payment request for the current patient
    /// </summary>
    [HttpPost("payments")]
    public async Task<IActionResult> RequestPayment([FromBody] CreatePaymentRequest request)
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            
            // Verify the request is for the current patient
            if (request.PatientId != patientId)
                return Forbid();

            var payment = await _paymentService.RequestPaymentAsync(request);
            return Created($"api/patient-portal/payments/{payment.PaymentId}", payment);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    #endregion

    #region Appointment Bills

    /// <summary>
    /// Gets all appointment bills for the current patient
    /// </summary>
    [HttpGet("my-bills")]
    public async Task<IActionResult> GetMyBills()
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            var bills = await _billService.GetBillsByPatientIdAsync(patientId);
            return Ok(bills);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Gets pending bills for the current patient
    /// </summary>
    [HttpGet("my-bills/pending")]
    public async Task<IActionResult> GetMyPendingBills()
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            var allBills = await _billService.GetBillsByPatientIdAsync(patientId);
            var pendingBills = allBills.Where(b => b.Status == "Pending");
            return Ok(pendingBills);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Gets completed/paid bills for the current patient
    /// </summary>
    [HttpGet("my-bills/completed")]
    public async Task<IActionResult> GetMyCompletedBills()
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            var allBills = await _billService.GetBillsByPatientIdAsync(patientId);
            var completedBills = allBills.Where(b => b.Status == "Paid");
            return Ok(completedBills);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific bill for the current patient
    /// </summary>
    [HttpGet("my-bills/{billId}")]
    public async Task<IActionResult> GetMyBill(int billId)
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            var bill = await _billService.GetBillByIdAsync(billId);
            
            if (bill == null)
                return NotFound(new { Message = "Bill not found" });
            
            // Verify ownership
            if (bill.PatientId != patientId)
                return Forbid();

            return Ok(bill);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    #endregion

    #region Prescriptions

    /// <summary>
    /// Gets a specific prescription's full detail for the current patient.
    /// </summary>
    [HttpGet("prescriptions/{prescriptionId}")]
    public async Task<IActionResult> GetMyPrescription(int prescriptionId)
    {
        try
        {
            var patientId = await GetPatientIdAsync();
            var prescription = await _medicalRecordService.GetPatientPrescriptionDetailAsync(patientId, prescriptionId);

            if (prescription == null)
                return NotFound(new { Message = "Prescription not found or does not belong to this patient" });

            return Ok(prescription);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    #endregion

    #region Account

    /// <summary>
    /// Changes the current patient's password.
    /// </summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { Message = "Invalid user identity." });
        }

        try
        {
            await _authService.ChangePasswordAsync(userId, request);
            return Ok(new { Message = "Password changed successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    #endregion

    #region Dashboard

    /// <summary>
    /// Gets dashboard summary for the current patient
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var patientId = await GetPatientIdAsync();

            // Get appointments
            var upcomingAppointments = await _appointmentService.GetByPatientIdAsync(patientId);
            var today = DateOnly.FromDateTime(DateTime.Today);
            var upcoming = upcomingAppointments
                .Where(a => a.AppointmentDate >= today && a.Status != "Cancelled")
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.AppointmentTime)
                .Take(3)
                .ToList();

            // Get medical records
            var medicalRecords = await _medicalRecordService.GetPatientMedicalRecordsAsync(patientId);
            var recentRecords = medicalRecords.Take(3).ToList();

            // Get pending lab tests
            var labTests = await _medicalRecordService.GetPatientPendingLabTestsAsync(patientId);
            var pendingLabTests = labTests.Take(3).ToList();

            // Get pending payments
            var payments = await _paymentService.GetPatientPendingPaymentsAsync(patientId);
            var pendingPayments = payments.Take(3).ToList();

            return Ok(new
            {
                UpcomingAppointments = upcoming,
                RecentMedicalRecords = recentRecords,
                PendingLabTests = pendingLabTests,
                PendingPayments = pendingPayments
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    #endregion
}
