using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs;
using MyProject.Application.Services;

namespace MyProject.WebMvc.Controllers;

[Authorize(Roles = "Patient")]
public class PatientPortalController : Controller
{
    private readonly PatientPortalApiService _patientPortalService;
    private readonly PatientApiService _patientService;

    public PatientPortalController(
        PatientPortalApiService patientPortalService,
        PatientApiService patientService)
    {
        _patientPortalService = patientPortalService;
        _patientService = patientService;
    }

    private async Task<PatientDto?> GetCurrentPatientAsync()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            return null;

        var patients = await _patientService.GetAllAsync();

        // Prefer UserId link, fallback to Phone==Username for legacy patients
        var patient = patients.FirstOrDefault(p => p.UserId == userId);
        if (patient == null)
        {
            var username = User.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(username))
                patient = patients.FirstOrDefault(p => p.Phone == username);
        }
        return patient;
    }

    public async Task<IActionResult> Dashboard()
    {
        try
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Logout", "Account");

            // Get dashboard data using the Patient Portal API
            var upcomingAppointments = await _patientPortalService.GetUpcomingAppointmentsAsync();
            var pendingPayments = await _patientPortalService.GetMyPendingPaymentsAsync();
            var recentLabTests = await _patientPortalService.GetMyPendingLabTestsAsync();

            ViewBag.UpcomingAppointments = upcomingAppointments;
            ViewBag.PendingPayments = pendingPayments;
            ViewBag.RecentLabTests = recentLabTests.Take(3).ToList();

            return View(patient);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Unable to load dashboard data. Please try again.";
            return View();
        }
    }

    public async Task<IActionResult> MyHistory()
    {
        try
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Logout", "Account");

            // Get all appointments using Patient Portal API
            var appointments = await _patientPortalService.GetMyAppointmentsAsync();
            var sortedAppointments = appointments
                .OrderByDescending(a => a.AppointmentDate)
                .ThenByDescending(a => a.AppointmentTime)
                .ToList();

            return View(sortedAppointments);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Unable to load appointment history. Please try again.";
            return View(new List<AppointmentDto>());
        }
    }

    public async Task<IActionResult> MyMedicalRecords()
    {
        try
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Logout", "Account");

            // Get medical records data using Patient Portal API
            var medicalRecords = await _patientPortalService.GetMyMedicalRecordsAsync();
            var labTests = await _patientPortalService.GetMyCompletedLabTestsAsync();
            var prescriptions = new List<PrescriptionDto>();

            // Extract prescriptions from medical records
            foreach (var record in medicalRecords)
            {
                if (record.Prescriptions != null)
                {
                    prescriptions.AddRange(record.Prescriptions);
                }
            }

            ViewBag.LabTests = labTests;
            ViewBag.Prescriptions = prescriptions;

            return View(medicalRecords);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Unable to load medical records. Please try again.";
            return View(new List<MedicalRecordDto>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> MyProfile()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return RedirectToAction("Logout", "Account");

        var request = new UpdatePatientRequest(
            FullName: patient.FullName,
            Phone: patient.Phone,
            DateOfBirth: patient.DateOfBirth,
            Gender: patient.Gender,
            Address: patient.Address,
            BloodType: patient.BloodType,
            EmergencyContactName: patient.EmergencyContactName,
            EmergencyContactPhone: patient.EmergencyContactPhone
        );
        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MyProfile(UpdatePatientRequest request)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return RedirectToAction("Logout", "Account");

        if (!ModelState.IsValid) return View(request);

        try
        {
            await _patientService.UpdateAsync(patient.PatientId, request);
            TempData["SuccessMessage"] = "Profile updated successfully.";
            return RedirectToAction(nameof(Dashboard));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return View(request);
        }
    }

    [HttpGet]
    public async Task<IActionResult> PrescriptionDetails(int id)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return RedirectToAction("Logout", "Account");

        var prescription = await _patientPortalService.GetMyPrescriptionAsync(id);
        if (prescription == null)
        {
            TempData["ErrorMessage"] = "Prescription not found.";
            return RedirectToAction(nameof(MyMedicalRecords));
        }

        return View(prescription);
    }

    public async Task<IActionResult> PaymentHistory()
    {
        try
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Logout", "Account");

            var payments = await _patientPortalService.GetMyPaymentsAsync();
            return View(payments);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Unable to load payment history. Please try again.";
            return View(new List<PaymentDto>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> RequestPayment()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return RedirectToAction("Logout", "Account");

        var appointments = await _patientPortalService.GetMyAppointmentsAsync();
        var completedAppointments = appointments
            .Where(a => a.Status == "Completed")
            .ToList();

        ViewBag.CompletedAppointments = completedAppointments;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestPayment(int AppointmentId, string? Reason)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return RedirectToAction("Logout", "Account");

        if (AppointmentId <= 0)
        {
            TempData["ErrorMessage"] = "Please select an appointment.";
            return RedirectToAction(nameof(RequestPayment));
        }

        try
        {
            var request = new CreatePaymentRequest(
                PatientId: patient.PatientId,
                AppointmentId: AppointmentId,
                Reason: Reason
            );

            await _patientPortalService.RequestPaymentAsync(request);
            TempData["SuccessMessage"] = "Payment request submitted successfully!";
            return RedirectToAction(nameof(PaymentHistory));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error submitting payment request: {ex.Message}";
            return RedirectToAction(nameof(RequestPayment));
        }
    }
}
