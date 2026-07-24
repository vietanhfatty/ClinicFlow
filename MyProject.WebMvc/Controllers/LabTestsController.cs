using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs;
using MyProject.Application.Services;

namespace MyProject.WebMvc.Controllers;

/// <summary>
/// Handles lab test result entry for Staff (lab technicians) and lab test
/// service pricing management for Admin. Lab test service catalog itself
/// (name/description/category/active state) is seeded and not editable here.
/// </summary>
[Authorize(Roles = "Admin,Staff")]
public class LabTestsController : Controller
{
    private readonly LabTestApiService _labTestService;

    public LabTestsController(LabTestApiService labTestService)
    {
        _labTestService = labTestService;
    }

    /// <summary>
    /// Lists lab tests for Staff to process. Supports filtering by status,
    /// by lab test service (type of test), and searching by patient/service name.
    /// Defaults to today's tests only, so the list doesn't get cluttered with
    /// the full backlog; Staff can switch to "All" to see everything.
    /// </summary>
    public async Task<IActionResult> Index(string? statusFilter, int? serviceFilter, string? search, string? dateFilter)
    {
        List<AppointmentLabTestDto> list = string.Equals(statusFilter, "Completed", StringComparison.OrdinalIgnoreCase)
            ? await _labTestService.GetCompletedLabTestsAsync()
            : await _labTestService.GetPendingLabTestsAsync();

        if (serviceFilter.HasValue && serviceFilter.Value > 0)
        {
            list = list.Where(t => t.LabTestServiceId == serviceFilter.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            list = list.Where(t =>
                t.PatientName.ToLower().Contains(term) ||
                t.ServiceName.ToLower().Contains(term)).ToList();
        }

        dateFilter = string.IsNullOrWhiteSpace(dateFilter) ? "Today" : dateFilter;
        if (string.Equals(dateFilter, "Today", StringComparison.OrdinalIgnoreCase))
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            list = list.Where(t => DateOnly.FromDateTime(t.CreatedAt) == today).ToList();
        }

        var services = await _labTestService.GetAllLabTestServicesAsync();

        ViewBag.StatusFilter = statusFilter ?? "Pending";
        ViewBag.ServiceFilter = serviceFilter;
        ViewBag.Search = search;
        ViewBag.DateFilter = dateFilter;
        ViewBag.Services = services.OrderBy(s => s.ServiceName).ToList();

        return View(list.OrderBy(t => t.CreatedAt).ToList());
    }

    /// <summary>
    /// Saves the result entered by a Staff lab technician for a pending lab test.
    /// For services with structured indicators, <paramref name="indicatorValues"/>
    /// carries the per-indicator entries (posted as indicatorValues[Key]=Value from
    /// dynamically rendered inputs); <paramref name="result"/> is then optional and
    /// only used as a free-text summary. For services without structured indicators
    /// (e.g. imaging), <paramref name="result"/> is the required free-text result.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateResult(int id, DateTime testDate, string? result, string? notes, Dictionary<string, string>? indicatorValues)
    {
        try
        {
            var request = new UpdateLabTestResultRequest(testDate, result, "Completed", notes, indicatorValues);
            await _labTestService.UpdateLabTestResultAsync(id, request);
            TempData["SuccessMessage"] = "Lab test result saved successfully.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Admin screen to view the lab test service catalog and adjust prices.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Services()
    {
        var services = await _labTestService.GetAllLabTestServicesAsync();
        return View(services.OrderBy(s => s.ServiceName).ToList());
    }

    /// <summary>
    /// Updates the price of a lab test service (Admin only).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdatePrice(int id, decimal price)
    {
        try
        {
            await _labTestService.UpdateServicePriceAsync(id, new UpdateLabTestServicePriceRequest(price));
            TempData["SuccessMessage"] = "Service price updated successfully.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        return RedirectToAction(nameof(Services));
    }
}
