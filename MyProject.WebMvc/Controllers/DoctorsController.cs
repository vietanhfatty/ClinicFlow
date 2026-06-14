using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs;
using MyProject.Application.Services;

namespace MyProject.WebMvc.Controllers;

[Authorize(Roles = "Admin")]
public class DoctorsController : Controller
{
    private readonly DoctorApiService _doctorService;

    public DoctorsController(DoctorApiService doctorService)
    {
        _doctorService = doctorService;
    }

    public async Task<IActionResult> Index(string? searchName, string? searchSpecialization)
    {
        var list = await _doctorService.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(searchName))
        {
            var name = searchName.Trim().ToLower();
            list = list.Where(d => d.FullName.ToLower().Contains(name)).ToList();
        }
        if (!string.IsNullOrWhiteSpace(searchSpecialization))
        {
            var spec = searchSpecialization.Trim().ToLower();
            list = list.Where(d => d.Specialization.ToLower().Contains(spec)).ToList();
        }

        ViewBag.SearchName = searchName;
        ViewBag.SearchSpecialization = searchSpecialization;

        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateDoctorRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = GetFirstModelError();
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _doctorService.CreateAsync(request);
            TempData["SuccessMessage"] = $"Doctor '{request.FullName}' registered successfully.";
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Failed to register doctor: " + ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateDoctorRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = GetFirstModelError();
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _doctorService.UpdateAsync(id, request);
            TempData["SuccessMessage"] = $"Doctor '{request.FullName}' updated successfully.";
        }
        catch (KeyNotFoundException)
        {
            TempData["ErrorMessage"] = "Doctor not found.";
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Failed to update doctor: " + ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _doctorService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Doctor deleted successfully.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Failed to delete doctor: " + ex.Message;
        }
        return RedirectToAction(nameof(Index));
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
