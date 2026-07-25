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

[Authorize]
public class MedicalRecordsController : Controller
{
    private readonly MedicalRecordApiService _medicalRecordService;
    private readonly PatientApiService _patientApiService;
    private readonly AppointmentApiService _appointmentApiService;
    private readonly DoctorApiService _doctorService;

    public MedicalRecordsController(
        MedicalRecordApiService medicalRecordService,
        PatientApiService patientApiService,
        AppointmentApiService appointmentApiService,
        DoctorApiService doctorService)
    {
        _medicalRecordService = medicalRecordService;
        _patientApiService = patientApiService;
        _appointmentApiService = appointmentApiService;
        _doctorService = doctorService;
    }

    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Index(string? search, string? status)
    {
        var list = await _medicalRecordService.GetAllAsync();
        var allRecords = list.ToList();

        // Filter by patient name (search)
        if (!string.IsNullOrWhiteSpace(search))
        {
            allRecords = allRecords
                .Where(r => r.PatientName.Contains(search, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Filter by appointment status
        if (!string.IsNullOrWhiteSpace(status))
        {
            allRecords = allRecords
                .Where(r => r.AppointmentStatus.Equals(status, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        ViewBag.SelectedPatientId = null;
        return View(allRecords);
    }

    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> PatientRecords(string patientName)
    {
        if (string.IsNullOrWhiteSpace(patientName))
            return RedirectToAction(nameof(Index));

        var allRecords = await _medicalRecordService.GetAllAsync();
        var records = allRecords
            .Where(r => r.PatientName == Uri.UnescapeDataString(patientName))
            .OrderByDescending(r => r.AppointmentDate)
            .ThenByDescending(r => r.AppointmentTime)
            .ToList();

        if (!records.Any())
            return RedirectToAction(nameof(Index));

        ViewBag.PatientName = records.First().PatientName;
        return View(records);
    }

    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var record = await _medicalRecordService.GetByIdAsync(id);
            if (record == null) return NotFound();

            if (User.IsInRole("Patient"))
            {
                // Get PatientId from claims
                var patientIdClaim = User.FindFirst("PatientId")?.Value;
                if (string.IsNullOrEmpty(patientIdClaim) || !int.TryParse(patientIdClaim, out int patientId))
                {
                    // Fallback: try to find patient by UserId
                    var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
                    var patients = await _patientApiService.GetAllAsync();
                    var patient = patients.FirstOrDefault(p => p.UserId == userId);
                    if (patient == null || record.PatientName != patient.FullName)
                    {
                        TempData["ErrorMessage"] = "You don't have permission to view this medical record.";
                        return RedirectToAction("MyMedicalRecords", "PatientPortal");
                    }
                }
                else
                {
                    // Verify by PatientId
                    var patients = await _patientApiService.GetAllAsync();
                    var patient = patients.FirstOrDefault(p => p.PatientId == patientId);
                    if (patient == null || record.PatientName != patient.FullName)
                    {
                        TempData["ErrorMessage"] = "You don't have permission to view this medical record.";
                        return RedirectToAction("MyMedicalRecords", "PatientPortal");
                    }
                }
            }

            return View(record);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Error loading medical record: " + ex.Message;
            return RedirectToAction("MyMedicalRecords", "PatientPortal");
        }
    }

    [Authorize(Roles = "Doctor")]
    [HttpGet]
    public async Task<IActionResult> Create(int appointmentId)
    {
        var appt = await _appointmentApiService.GetByIdAsync(appointmentId);
        if (appt == null) return NotFound();

        ViewBag.Appointment = appt;
        var request = new CreateMedicalRecordRequest(
            AppointmentId: appointmentId,
            Symptoms: appt.Reason,
            Diagnosis: "",
            Treatment: "",
            Notes: ""
        );
        return View(request);
    }

    [Authorize(Roles = "Doctor")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateMedicalRecordRequest request, string? medicinesJson)
    {
        if (!ModelState.IsValid)
        {
            var appt = await _appointmentApiService.GetByIdAsync(request.AppointmentId);
            ViewBag.Appointment = appt;
            return View(request);
        }

        try
        {
            var record = await _medicalRecordService.CreateAsync(request);

            if (!string.IsNullOrEmpty(medicinesJson))
            {
                var prescriptions = System.Text.Json.JsonSerializer.Deserialize<List<CreatePrescriptionRequest>>(medicinesJson);
                if (prescriptions != null && prescriptions.Any())
                {
                    await _medicalRecordService.AddPrescriptionsAsync(record.MedicalRecordId, prescriptions);
                }
            }

            TempData["SuccessMessage"] = "Tạo bệnh án thành công!";
            return RedirectToAction("CompletionSummary", new { id = record.MedicalRecordId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            var appt = await _appointmentApiService.GetByIdAsync(request.AppointmentId);
            ViewBag.Appointment = appt;
            return View(request);
        }
    }

    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> CompletionSummary(int id)
    {
        var record = await _medicalRecordService.GetByIdAsync(id);
        if (record == null) return NotFound();

        var appointment = await _appointmentApiService.GetByIdAsync(record.AppointmentId);
        var doctors = await _doctorService.GetAllAsync();
        ViewBag.Appointment = appointment;
        ViewBag.Doctors = doctors;

        return View(record);
    }
}
