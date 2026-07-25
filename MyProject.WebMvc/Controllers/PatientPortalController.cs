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

    [HttpGet]
    public async Task<IActionResult> MedicalRecordDetails(int id)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return RedirectToAction("Logout", "Account");

        var record = await _patientPortalService.GetMyMedicalRecordAsync(id);
        if (record == null)
        {
            TempData["ErrorMessage"] = "Medical record not found or you don't have permission to view it.";
            return RedirectToAction(nameof(MyMedicalRecords));
        }

        return View(record);
    }

    public async Task<IActionResult> PaymentHistory()
    {
        return RedirectToAction(nameof(MyBills));
    }

    public async Task<IActionResult> MyBills()
    {
        try
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return RedirectToAction("Logout", "Account");

            var bills = await _patientPortalService.GetMyBillsAsync();
            return View(bills);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Unable to load bills. Please try again.";
            return View(new List<AppointmentBillDto>());
        }
    }

    [HttpGet]
    public async Task<IActionResult> BillDetails(int id)
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return RedirectToAction("Logout", "Account");

        var bill = await _patientPortalService.GetMyBillAsync(id);
        if (bill == null)
        {
            TempData["ErrorMessage"] = "Bill not found or you don't have permission to view it.";
            return RedirectToAction(nameof(MyBills));
        }

        return View(bill);
    }
}
