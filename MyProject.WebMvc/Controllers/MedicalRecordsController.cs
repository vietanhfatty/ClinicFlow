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

    public MedicalRecordsController(
        MedicalRecordApiService medicalRecordService,
        PatientApiService patientApiService,
        AppointmentApiService appointmentApiService)
    {
        _medicalRecordService = medicalRecordService;
        _patientApiService = patientApiService;
        _appointmentApiService = appointmentApiService;
    }

    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Index()
    {
        var list = await _medicalRecordService.GetAllAsync();
        return View(list);
    }

    public async Task<IActionResult> Details(int id)
    {
        var record = await _medicalRecordService.GetByIdAsync(id);
        if (record == null) return NotFound();

        if (User.IsInRole("Patient"))
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var patients = await _patientApiService.GetAllAsync();
            var patient = patients.FirstOrDefault(p => p.UserId == userId);
            if (patient == null || record.PatientName != patient.FullName)
            {
                return Forbid();
            }
        }

        return View(record);
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

            TempData["SuccessMessage"] = "Medical record created successfully.";
            return RedirectToAction("Details", new { id = record.MedicalRecordId });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            var appt = await _appointmentApiService.GetByIdAsync(request.AppointmentId);
            ViewBag.Appointment = appt;
            return View(request);
        }
    }
}
