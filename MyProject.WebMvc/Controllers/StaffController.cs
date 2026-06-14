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
public class StaffController : Controller
{
    private readonly StaffApiService _staffService;

    public StaffController(StaffApiService staffService)
    {
        _staffService = staffService;
    }

    public async Task<IActionResult> Index(string? searchName, string? searchPosition)
    {
        var list = await _staffService.GetAllAsync();

        if (!string.IsNullOrWhiteSpace(searchName))
        {
            var name = searchName.Trim().ToLower();
            list = list.Where(s => s.FullName.ToLower().Contains(name)).ToList();
        }
        if (!string.IsNullOrWhiteSpace(searchPosition) && searchPosition != "All")
        {
            list = list.Where(s => s.Position == searchPosition).ToList();
        }

        // Distinct positions for filter dropdown
        var allStaff = await _staffService.GetAllAsync();
        ViewBag.Positions = allStaff.Select(s => s.Position).Where(p => !string.IsNullOrEmpty(p)).Distinct().ToList();

        ViewBag.SearchName = searchName;
        ViewBag.SearchPosition = searchPosition;

        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateStaffRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = GetFirstModelError();
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _staffService.CreateAsync(request);
            TempData["SuccessMessage"] = $"Staff '{request.FullName}' registered successfully.";
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to register staff. Please check if username is already taken.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UpdateStaffRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = GetFirstModelError();
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _staffService.UpdateAsync(id, request);
            TempData["SuccessMessage"] = $"Staff '{request.FullName}' updated successfully.";
        }
        catch (KeyNotFoundException)
        {
            TempData["ErrorMessage"] = "Staff not found.";
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to update staff. Please try again.";
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        try
        {
            await _staffService.DeleteAsync(id);
            TempData["SuccessMessage"] = "Staff deleted successfully.";
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to delete staff.";
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
