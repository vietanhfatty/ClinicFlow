using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs;
using MyProject.Application.Services;

namespace MyProject.WebApi.Controllers;

[ApiController]
[Route("api/patient-portal")]
[Authorize(Roles = "Patient")]
public class PatientPortalController : ControllerBase
{
    private readonly AppointmentService _appointmentService;
    private readonly PaymentService _paymentService;
    private readonly PatientMedicalRecordService _medicalRecordService;
    private readonly AuthService _authService;

    public PatientPortalController(
        AppointmentService appointmentService,
        PaymentService paymentService,
        PatientMedicalRecordService medicalRecordService,
        AuthService authService)
    {
        _appointmentService = appointmentService;
        _paymentService = paymentService;
        _medicalRecordService = medicalRecordService;
        _authService = authService;
    }

    /// <summary>
    /// Gets the current patient ID from claims
    /// </summary>
    private int GetPatientId()
    {
        var patientIdClaim = User.FindFirst("PatientId")?.Value;
        if (!int.TryParse(patientIdClaim, out int patientId))
        {
            throw new UnauthorizedAccessException("Patient ID not found in claims");
        }
        return patientId;
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
            var patientId = GetPatientId();
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
            var patientId = GetPatientId();
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
            var patientId = GetPatientId();
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
            var patientId = GetPatientId();
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
            var patientId = GetPatientId();
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
            var patientId = GetPatientId();
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
            var patientId = GetPatientId();
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
            var patientId = GetPatientId();
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
            var patientId = GetPatientId();
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
            var patientId = GetPatientId();
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
            var patientId = GetPatientId();
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
            var patientId = GetPatientId();
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
            var patientId = GetPatientId();
            
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

    #region Prescriptions

    /// <summary>
    /// Gets a specific prescription's full detail for the current patient.
    /// </summary>
    [HttpGet("prescriptions/{prescriptionId}")]
    public async Task<IActionResult> GetMyPrescription(int prescriptionId)
    {
        try
        {
            var patientId = GetPatientId();
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
            var patientId = GetPatientId();

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
