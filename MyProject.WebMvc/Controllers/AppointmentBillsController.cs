using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs;
using MyProject.Application.Services;
using MyProject.Domain.IRepositories;

namespace MyProject.WebMvc.Controllers;

[Authorize(Roles = "Admin,Staff")]
public class AppointmentBillsController : Controller
{
    private readonly AppointmentBillService _billService;
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IPatientRepository _patientRepo;
    private readonly IAppointmentLabTestRepository _labTestRepo;
    private readonly IAppointmentBillRepository _billRepo;

    public AppointmentBillsController(
        AppointmentBillService billService,
        IAppointmentRepository appointmentRepo,
        IPatientRepository patientRepo,
        IAppointmentLabTestRepository labTestRepo,
        IAppointmentBillRepository billRepo)
    {
        _billService = billService;
        _appointmentRepo = appointmentRepo;
        _patientRepo = patientRepo;
        _labTestRepo = labTestRepo;
        _billRepo = billRepo;
    }

    public async Task<IActionResult> Index(string? status, string? searchPatient)
    {
        var bills = await _billService.GetAllBillsAsync();
        var billList = bills.ToList();

        if (!string.IsNullOrEmpty(status) && status != "All")
        {
            billList = billList.Where(b => b.Status == status).ToList();
        }

        if (!string.IsNullOrWhiteSpace(searchPatient))
        {
            billList = billList.Where(b => b.PatientName.ToLower().Contains(searchPatient.ToLower())).ToList();
        }

        ViewBag.Status = status;
        ViewBag.SearchPatient = searchPatient;
        return View(billList);
    }

    public async Task<IActionResult> Details(int id)
    {
        var bill = await _billService.GetBillByIdAsync(id);
        if (bill == null)
        {
            TempData["ErrorMessage"] = "Bill not found.";
            return RedirectToAction(nameof(Index));
        }

        var appointment = await _appointmentRepo.GetByIdAsync(bill.AppointmentId);
        if (appointment != null)
        {
            var allLabTests = await _labTestRepo.GetAllAsync();
            var appointmentLabTests = allLabTests.Where(lt => lt.AppointmentId == bill.AppointmentId).ToList();
            ViewBag.LabTests = appointmentLabTests;
            ViewBag.Appointment = appointment;
        }

        return View(bill);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? appointmentId, int? patientId)
    {
        var patients = await _patientRepo.GetAllAsync();
        var appointments = await _appointmentRepo.GetAllAsync();
        var billedAppointmentIds = (await _billRepo.GetBilledAppointmentIdsAsync()).ToHashSet();

        // Get appointments that are completed/confirmed and NOT yet billed
        var unbilledAppointments = appointments
            .Where(a => (a.Status == "Completed" || a.Status == "Confirmed") && !billedAppointmentIds.Contains(a.AppointmentId))
            .OrderByDescending(a => a.AppointmentDate)
            .ToList();

        // Load all lab tests for fee calculation
        var allLabTests = await _labTestRepo.GetAllAsync();
        var labTestDtos = allLabTests.Select(lt => new MyProject.Application.DTOs.AppointmentLabTestDto(
            lt.AppointmentLabTestId,
            lt.AppointmentId,
            lt.Appointment?.Patient?.FullName ?? "Unknown",
            lt.Doctor?.FullName ?? "Unknown",
            lt.LabTestServiceId,
            lt.LabTestService?.ServiceName ?? "Unknown",
            lt.LabTestService?.Price ?? 0m,
            lt.TestDate,
            lt.Result ?? "",
            lt.Status,
            lt.Notes ?? "",
            lt.CreatedAt,
            new List<MyProject.Application.DTOs.LabTestIndicatorValueDto>()
        )).ToList();

        ViewBag.Patients = patients;
        ViewBag.UnbilledAppointments = unbilledAppointments;
        ViewBag.AllLabTests = labTestDtos;
        ViewBag.AppointmentId = appointmentId;
        ViewBag.PatientId = patientId;

        // Load lab tests for pre-selected appointment if provided
        if (appointmentId.HasValue)
        {
            var appointmentLabTests = labTestDtos.Where(lt => lt.AppointmentId == appointmentId.Value).ToList();
            decimal labTestFee = appointmentLabTests.Sum(lt => lt.Price);
            ViewBag.LabTests = appointmentLabTests;
            ViewBag.LabTestFee = labTestFee;
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateAppointmentBillRequest request)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Please check your input data.";
            return RedirectToAction(nameof(Create), new { appointmentId = request.AppointmentId, patientId = request.PatientId });
        }

        try
        {
            await _billService.CreateBillAsync(request);
            TempData["SuccessMessage"] = "Bill created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to create bill. Please try again.";
        }

        return RedirectToAction(nameof(Create), new { appointmentId = request.AppointmentId, patientId = request.PatientId });
    }

    [HttpGet]
    public async Task<IActionResult> CreateFromLabTests(int appointmentId)
    {
        var appointment = await _appointmentRepo.GetByIdAsync(appointmentId);
        if (appointment == null)
        {
            TempData["ErrorMessage"] = "Appointment not found.";
            return RedirectToAction(nameof(Index));
        }

        var allLabTests = await _labTestRepo.GetAllAsync();
        var appointmentLabTests = allLabTests.Where(lt => lt.AppointmentId == appointmentId).ToList();
        
        decimal labTestFee = appointmentLabTests.Sum(lt => lt.LabTestService?.Price ?? 0m);

        ViewBag.Appointment = appointment;
        ViewBag.LabTests = appointmentLabTests;
        ViewBag.LabTestFee = labTestFee;

        var model = new CreateBillFromLabTestsViewModel
        {
            AppointmentId = appointmentId,
            PatientId = appointment.PatientId,
            PatientName = appointment.Patient?.FullName ?? "Unknown",
            ExaminationFee = 0,
            Notes = null
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateFromLabTests(CreateBillFromLabTestsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var staffId = GetCurrentStaffIdAsync();
            await _billService.CreateBillFromLabTestsAsync(
                model.AppointmentId,
                model.PatientId,
                model.ExaminationFee,
                staffId,
                model.Notes
            );
            TempData["SuccessMessage"] = "Bill created successfully!";
            return RedirectToAction(nameof(Index));
        }
        catch (KeyNotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (ArgumentException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to create bill. Please try again.";
        }

        var appointment = await _appointmentRepo.GetByIdAsync(model.AppointmentId);
        ViewBag.Appointment = appointment;

        var allLabTests = await _labTestRepo.GetAllAsync();
        var labTests = allLabTests.Where(lt => lt.AppointmentId == model.AppointmentId).ToList();
        ViewBag.LabTests = labTests;
        ViewBag.LabTestFee = labTests.Sum(lt => lt.LabTestService?.Price ?? 0m);

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsPaid(int id)
    {
        try
        {
            var request = new MarkBillAsPaidRequest(Notes: null, PaidAt: DateTime.UtcNow);
            await _billService.MarkAsPaidAsync(id, request);
            TempData["SuccessMessage"] = "Bill marked as paid successfully!";
        }
        catch (KeyNotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to update bill. Please try again.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        try
        {
            await _billService.CancelBillAsync(id);
            TempData["SuccessMessage"] = "Bill cancelled successfully!";
        }
        catch (KeyNotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to cancel bill. Please try again.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _billService.DeleteBillAsync(id);
            TempData["SuccessMessage"] = "Bill deleted successfully!";
        }
        catch (KeyNotFoundException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }
        catch (Exception)
        {
            TempData["ErrorMessage"] = "Failed to delete bill. Please try again.";
        }

        return RedirectToAction(nameof(Index));
    }

    private int? GetCurrentStaffIdAsync()
    {
        var staffIdClaim = User.FindFirst("StaffId")?.Value;
        if (!string.IsNullOrEmpty(staffIdClaim) && int.TryParse(staffIdClaim, out int staffId))
        {
            return staffId;
        }
        return null;
    }
}

public class CreateBillFromLabTestsViewModel
{
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public string PatientName { get; set; } = "";
    public decimal ExaminationFee { get; set; }
    public string? Notes { get; set; }
}
