using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs;
using MyProject.Application.Services;

namespace MyProject.WebMvc.Controllers;

[Authorize(Roles = "Admin,Staff")]
public class PatientsController : Controller
{
    private readonly PatientApiService _patientService;
    private readonly AppointmentApiService _appointmentService;

    public PatientsController(PatientApiService patientService, AppointmentApiService appointmentService)
    {
        _patientService = patientService;
        _appointmentService = appointmentService;
    }

    public async Task<IActionResult> Index(
        string? searchName, string? searchPhone,
        string? filterGender, DateOnly? filterDobStart, DateOnly? filterDobEnd)
    {
        if (User.IsInRole("Doctor"))
        {
            TempData["ErrorMessage"] = "Doctors are not authorized to view the general patient list.";
            return RedirectToAction("Queue", "Appointments");
        }

        var list = await _patientService.GetAllAsync();
        var appointments = await _appointmentService.GetAllAsync();
        var today = DateOnly.FromDateTime(DateTime.Today);
        ViewBag.TodayAppointments = appointments.Where(a => a.AppointmentDate == today).ToList();

        if (!string.IsNullOrWhiteSpace(searchName))
        {
            var name = searchName.Trim().ToLower();
            list = list.Where(p => p.FullName.ToLower().Contains(name)).ToList();
        }
        if (!string.IsNullOrWhiteSpace(searchPhone))
            list = list.Where(p => p.Phone != null && p.Phone.Contains(searchPhone.Trim())).ToList();
        if (!string.IsNullOrWhiteSpace(filterGender) && filterGender != "All")
            list = list.Where(p => p.Gender == filterGender).ToList();
        if (filterDobStart.HasValue)
            list = list.Where(p => p.DateOfBirth.HasValue && p.DateOfBirth.Value >= filterDobStart.Value).ToList();
        if (filterDobEnd.HasValue)
            list = list.Where(p => p.DateOfBirth.HasValue && p.DateOfBirth.Value <= filterDobEnd.Value).ToList();

        ViewBag.SearchName = searchName;
        ViewBag.SearchPhone = searchPhone;
        ViewBag.FilterGender = filterGender;
        ViewBag.FilterDobStart = filterDobStart?.ToString("yyyy-MM-dd");
        ViewBag.FilterDobEnd = filterDobEnd?.ToString("yyyy-MM-dd");

        return View(list);
    }

    public async Task<IActionResult> Details(int id)
    {
        if (User.IsInRole("Doctor"))
        {
            TempData["ErrorMessage"] = "Doctors are not authorized to view detailed patient profiles.";
            return RedirectToAction("Queue", "Appointments");
        }

        var patient = await _patientService.GetByIdAsync(id);
        if (patient == null) return NotFound();
        return View(patient);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreatePatientRequest request)
    {
        if (User.IsInRole("Doctor"))
        {
            TempData["ErrorMessage"] = "Doctors are not authorized to register patients.";
            return RedirectToAction("Queue", "Appointments");
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = GetFirstModelError();
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _patientService.CreateAsync(request);
            TempData["SuccessMessage"] = $"Patient '{request.FullName}' registered successfully.";
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to register patient. Please try again.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdatePatientRequest request)
    {
        if (User.IsInRole("Doctor"))
        {
            TempData["ErrorMessage"] = "Doctors are not authorized to edit patient profiles.";
            return RedirectToAction("Queue", "Appointments");
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = GetFirstModelError();
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _patientService.UpdateAsync(id, request);
            TempData["SuccessMessage"] = $"Patient '{request.FullName}' updated successfully.";
        }
        catch (KeyNotFoundException)
        {
            TempData["ErrorMessage"] = "Patient not found.";
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to update patient. Please try again.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!User.IsInRole("Admin"))
        {
            TempData["ErrorMessage"] = "Only Administrators can delete patients.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _patientService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Patient deleted successfully.";
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to delete patient. They may have existing appointments.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateApi([FromBody] CreatePatientRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { Message = GetFirstModelError() });
        }

        try
        {
            await _patientService.CreateAsync(request);
            // Get the newly created patient by phone
            var list = await _patientService.GetAllAsync();
            var patient = list.FirstOrDefault(p => p.Phone == request.Phone);
            return Ok(patient);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = "Failed to register patient: " + ex.Message });
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
