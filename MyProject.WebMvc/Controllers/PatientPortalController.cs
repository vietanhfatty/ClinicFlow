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
    private readonly AppointmentApiService _appointmentService;
    private readonly PatientApiService _patientService;
    private readonly DoctorApiService _doctorService;
    private readonly MedicalRecordApiService _medicalRecordService;

    public PatientPortalController(
        AppointmentApiService appointmentService,
        PatientApiService patientService,
        DoctorApiService doctorService,
        MedicalRecordApiService medicalRecordService)
    {
        _appointmentService = appointmentService;
        _patientService = patientService;
        _doctorService = doctorService;
        _medicalRecordService = medicalRecordService;
    }

    private async Task<PatientDto?> GetCurrentPatientAsync()
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
            return null;

        var patients = await _patientService.GetAllAsync();
        return patients.FirstOrDefault(p => p.UserId == userId);
    }

    public async Task<IActionResult> Dashboard()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return RedirectToAction("Logout", "Account");

        var appointments = await _appointmentService.GetAllAsync();
        var myAppointments = appointments
            .Where(a => a.PatientId == patient.PatientId)
            .OrderByDescending(a => a.AppointmentDate)
            .ThenBy(a => a.AppointmentTime)
            .ToList();

        ViewBag.UpcomingAppointments = myAppointments
            .Where(a => a.AppointmentDate >= DateOnly.FromDateTime(DateTime.Today) && a.Status != "Completed")
            .ToList();

        return View(patient);
    }

    [HttpGet]
    public async Task<IActionResult> BookAppointment()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return RedirectToAction("Logout", "Account");

        ViewBag.Doctors = await _doctorService.GetAllAsync();
        var request = new CreateAppointmentRequest(
            PatientId: patient.PatientId,
            DoctorId: 0,
            AppointmentDate: DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            AppointmentTime: TimeSpan.FromHours(9),
            Status: "Pending",
            Reason: ""
        );
        return View(request);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BookAppointment(CreateAppointmentRequest request)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Doctors = await _doctorService.GetAllAsync();
            return View(request);
        }

        try
        {
            await _appointmentService.CreateAsync(request);
            TempData["SuccessMessage"] = "Appointment requested successfully! Waiting for confirmation.";
            return RedirectToAction(nameof(Dashboard));
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            ViewBag.Doctors = await _doctorService.GetAllAsync();
            return View(request);
        }
    }

    public async Task<IActionResult> MyHistory()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return RedirectToAction("Logout", "Account");

        var appointments = await _appointmentService.GetAllAsync();
        var list = appointments.Where(a => a.PatientId == patient.PatientId)
            .OrderByDescending(a => a.AppointmentDate)
            .ToList();

        return View(list);
    }

    public async Task<IActionResult> MyMedicalRecords()
    {
        var patient = await GetCurrentPatientAsync();
        if (patient == null) return RedirectToAction("Logout", "Account");

        var list = await _medicalRecordService.GetByPatientIdAsync(patient.PatientId);
        return View(list);
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
    public async Task<IActionResult> GetBookedTimes(int doctorId, string date)
    {
        if (!DateOnly.TryParse(date, out var dateOnly))
        {
            return BadRequest("Invalid date format.");
        }

        var appointments = await _appointmentService.GetAllAsync();
        var bookedTimes = appointments
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate == dateOnly && !a.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            .Select(a => a.AppointmentTime.ToString())
            .ToList();

        return Json(bookedTimes);
    }
}
