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

[Authorize(Roles = "Admin,Doctor,Staff")]
public class AppointmentsController : Controller
{
    private readonly AppointmentApiService _appointmentService;
    private readonly PatientApiService _patientService;
    private readonly DoctorApiService _doctorService;
    private readonly MedicalRecordApiService _medicalRecordService;

    public AppointmentsController(
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

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Doctor"))
        {
            return RedirectToAction(nameof(Queue));
        }

        var list = await _appointmentService.GetAllAsync();
        await PopulateDropdownsViewBag();
        return View(list);
    }

    public async Task<IActionResult> Queue()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            TempData["ErrorMessage"] = "Could not identify logged-in user.";
            return RedirectToAction(nameof(Index));
        }

        var doctors = await _doctorService.GetAllAsync();
        var doctor = doctors.FirstOrDefault(d => d.UserId == userId);
        if (doctor == null)
        {
            TempData["ErrorMessage"] = "Could not identify logged-in doctor profile.";
            return RedirectToAction(nameof(Index));
        }

        var list = await _appointmentService.GetAllAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        var queue = list
            .Where(a => a.DoctorId == doctor.DoctorId && (a.Status == "Confirmed" || a.Status == "InProgress") && a.AppointmentDate == today)
            .OrderBy(a => a.AppointmentTime)
            .ToList();

        ViewBag.DoctorName = User.FindFirst("FullName")?.Value ?? User.Identity?.Name;
        return View(queue);
    }

    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> MyAppointments()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            TempData["ErrorMessage"] = "Could not identify logged-in user.";
            return RedirectToAction(nameof(Index));
        }

        var doctors = await _doctorService.GetAllAsync();
        var doctor = doctors.FirstOrDefault(d => d.UserId == userId);
        if (doctor == null)
        {
            TempData["ErrorMessage"] = "Could not identify logged-in doctor profile.";
            return RedirectToAction(nameof(Index));
        }

        var list = await _appointmentService.GetAllAsync();
        var myAppointments = list
            .Where(a => a.DoctorId == doctor.DoctorId)
            .OrderByDescending(a => a.AppointmentDate)
            .ToList();

        ViewBag.DoctorName = User.FindFirst("FullName")?.Value ?? User.Identity?.Name;
        return View(myAppointments);
    }

    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> MySchedule()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out int userId))
        {
            TempData["ErrorMessage"] = "Could not identify logged-in user.";
            return RedirectToAction(nameof(Index));
        }

        var doctors = await _doctorService.GetAllAsync();
        var doctor = doctors.FirstOrDefault(d => d.UserId == userId);
        if (doctor == null)
        {
            TempData["ErrorMessage"] = "Could not identify logged-in doctor profile.";
            return RedirectToAction(nameof(Index));
        }

        var list = await _appointmentService.GetAllAsync();
        var myAppointments = list
            .Where(a => a.DoctorId == doctor.DoctorId && (a.Status == "Confirmed" || a.Status == "InProgress" || a.Status == "Completed"))
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.AppointmentTime)
            .ToList();

        ViewBag.DoctorName = User.FindFirst("FullName")?.Value ?? User.Identity?.Name;
        return View(myAppointments);
    }

    public async Task<IActionResult> Details(int id)
    {
        var appointment = await _appointmentService.GetByIdAsync(id);
        if (appointment == null) return NotFound();

        if (User.IsInRole("Doctor") || User.IsInRole("Admin") || User.IsInRole("Staff"))
        {
            var medicalHistory = await _medicalRecordService.GetByPatientIdAsync(appointment.PatientId);
            ViewBag.MedicalHistory = medicalHistory.OrderByDescending(mr => mr.CreatedAt).ToList();
        }

        return View(appointment);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Create(int? patientId)
    {
        await PopulateDropdownsViewBag();
        var request = new CreateAppointmentRequest(
            PatientId: patientId ?? 0,
            DoctorId: 0,
            AppointmentDate: DateOnly.FromDateTime(DateTime.Now.AddDays(1)), // default to tomorrow
            AppointmentTime: TimeSpan.FromHours(9),
            Status: "Pending",
            Reason: null
        );
        return View(request);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAppointmentRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = GetFirstModelError();
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _appointmentService.CreateAsync(request);
            TempData["SuccessMessage"] = "Appointment booked successfully.";
        }
        catch (KeyNotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to book appointment. Please try again.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Edit(int id)
    {
        var appt = await _appointmentService.GetByIdAsync(id);
        if (appt == null) return NotFound();

        await PopulateDropdownsViewBag();

        var request = new UpdateAppointmentRequest(
            PatientId: appt.PatientId,
            DoctorId: appt.DoctorId,
            AppointmentDate: appt.AppointmentDate,
            AppointmentTime: appt.AppointmentTime,
            Status: appt.Status,
            Reason: appt.Reason
        );

        return View(request);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateAppointmentRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = GetFirstModelError();
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _appointmentService.UpdateAsync(id, request);
            TempData["SuccessMessage"] = "Appointment updated successfully.";
        }
        catch (KeyNotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to update appointment. Please try again.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ActionName("Delete")]
    [Authorize(Roles = "Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _appointmentService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Appointment deleted successfully.";
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to delete appointment. Please try again.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(int id)
    {
        try
        {
            await _appointmentService.CheckInAsync(id);
            TempData["SuccessMessage"] = "Check-in successful! Patient has been queued.";
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Doctor,Admin,Staff")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Confirm(int id)
    {
        try
        {
            await _appointmentService.ConfirmAsync(id);
            TempData["SuccessMessage"] = "Appointment confirmed successfully.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        
        var referer = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer))
        {
            return Redirect(referer);
        }
        return RedirectToAction(nameof(Queue));
    }

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartExamination(int id)
    {
        try
        {
            await _appointmentService.StartExaminationAsync(id);
            TempData["SuccessMessage"] = "Examination started.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        
        var referer = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer))
        {
            return Redirect(referer);
        }
        return RedirectToAction(nameof(Queue));
    }

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete(int id)
    {
        try
        {
            await _appointmentService.CompleteAsync(id);
            TempData["SuccessMessage"] = "Appointment completed. Please write the medical record.";
            return RedirectToAction("Create", "MedicalRecords", new { appointmentId = id });
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
            return RedirectToAction(nameof(Queue));
        }
    }

    private async Task PopulateDropdownsViewBag()
    {
        ViewBag.Patients = await _patientService.GetAllAsync();
        ViewBag.Doctors = await _doctorService.GetAllAsync();
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GetBusySlots(int doctorId, string date)
    {
        try
        {
            var busySlots = await _appointmentService.GetBusySlotsAsync(doctorId, date);
            return Json(busySlots);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    private string GetFirstModelError()
    {
        foreach (var state in ModelState.Values)
            foreach (var error in state.Errors)
                if (!string.IsNullOrEmpty(error.ErrorMessage))
                    return error.ErrorMessage;
        return "Invalid input. Please check your data.";
    }
}
