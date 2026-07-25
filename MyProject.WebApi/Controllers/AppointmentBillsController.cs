using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs;
using MyProject.Application.Services;
using MyProject.Domain.IRepositories;

namespace MyProject.WebApi.Controllers;

[ApiController]
[Route("api/appointment-bills")]
[Authorize(Roles = "Admin,Staff")]
public class AppointmentBillsController : ControllerBase
{
    private readonly AppointmentBillService _billService;
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IAppointmentLabTestRepository _labTestRepo;
    private readonly IStaffRepository _staffRepo;

    public AppointmentBillsController(
        AppointmentBillService billService,
        IAppointmentRepository appointmentRepo,
        IAppointmentLabTestRepository labTestRepo,
        IStaffRepository staffRepo)
    {
        _billService = billService;
        _appointmentRepo = appointmentRepo;
        _labTestRepo = labTestRepo;
        _staffRepo = staffRepo;
    }

    private int? GetCurrentStaffIdFromClaims()
    {
        var staffIdClaim = User.FindFirst("StaffId")?.Value;
        if (!string.IsNullOrEmpty(staffIdClaim) && int.TryParse(staffIdClaim, out int staffId))
        {
            return staffId;
        }
        return null;
    }

    /// <summary>
    /// Gets all bills (for Staff/Admin)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllBills()
    {
        var bills = await _billService.GetAllBillsAsync();
        return Ok(bills);
    }

    /// <summary>
    /// Gets a specific bill by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBill(int id)
    {
        var bill = await _billService.GetBillByIdAsync(id);
        if (bill == null)
            return NotFound(new { Message = "Bill not found" });

        return Ok(bill);
    }

    /// <summary>
    /// Gets bills for a specific appointment
    /// </summary>
    [HttpGet("appointment/{appointmentId}")]
    public async Task<IActionResult> GetBillsByAppointment(int appointmentId)
    {
        var bills = await _billService.GetBillsByAppointmentIdAsync(appointmentId);
        return Ok(bills);
    }

    /// <summary>
    /// Gets bills for a specific patient
    /// </summary>
    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetBillsByPatient(int patientId)
    {
        var bills = await _billService.GetBillsByPatientIdAsync(patientId);
        return Ok(bills);
    }

    /// <summary>
    /// Creates a new bill for an appointment
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateBill([FromBody] CreateAppointmentBillRequest request)
    {
        try
        {
            var staffId = GetCurrentStaffIdFromClaims();
            var bill = await _billService.CreateBillAsync(request);
            return Created($"/api/appointment-bills/{bill.BillId}", bill);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Creates a bill for an appointment based on lab tests
    /// </summary>
    [HttpPost("from-lab-tests")]
    public async Task<IActionResult> CreateBillFromLabTests([FromBody] CreateBillFromLabTestsRequest request)
    {
        try
        {
            var staffId = GetCurrentStaffIdFromClaims();
            var bill = await _billService.CreateBillFromLabTestsAsync(
                request.AppointmentId,
                request.PatientId,
                request.ExaminationFee,
                staffId,
                request.Notes
            );
            return Created($"/api/appointment-bills/{bill.BillId}", bill);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Updates a pending bill
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBill(int id, [FromBody] UpdateAppointmentBillRequest request)
    {
        try
        {
            var bill = await _billService.UpdateBillAsync(id, request);
            return Ok(bill);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Marks a bill as paid (patient has paid)
    /// </summary>
    [HttpPut("{id}/pay")]
    public async Task<IActionResult> MarkAsPaid(int id, [FromBody] MarkBillAsPaidRequest request)
    {
        try
        {
            var bill = await _billService.MarkAsPaidAsync(id, request);
            return Ok(bill);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Cancels a pending bill
    /// </summary>
    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelBill(int id)
    {
        try
        {
            var bill = await _billService.CancelBillAsync(id);
            return Ok(bill);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a pending bill
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBill(int id)
    {
        try
        {
            await _billService.DeleteBillAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Gets lab tests for an appointment (to help calculate bill)
    /// </summary>
    [HttpGet("appointment/{appointmentId}/lab-tests")]
    public async Task<IActionResult> GetAppointmentLabTests(int appointmentId)
    {
        var appointment = await _appointmentRepo.GetByIdAsync(appointmentId);
        if (appointment == null)
            return NotFound(new { Message = "Appointment not found" });

        var allLabTests = await _labTestRepo.GetAllAsync();
        var appointmentLabTests = allLabTests.Where(lt => lt.AppointmentId == appointmentId).ToList();

        var labTestDtos = appointmentLabTests.Select(lt => new
        {
            lt.AppointmentLabTestId,
            lt.AppointmentId,
            ServiceName = lt.LabTestService?.ServiceName ?? "Unknown",
            Price = lt.LabTestService?.Price ?? 0m,
            lt.Status,
            lt.TestDate
        }).ToList();

        decimal totalLabTestFee = labTestDtos.Sum(lt => lt.Price);

        return Ok(new
        {
            AppointmentId = appointmentId,
            LabTests = labTestDtos,
            TotalLabTestFee = totalLabTestFee,
            HasCompletedLabTests = appointmentLabTests.Any(lt => lt.Status == "Completed")
        });
    }
}

public record CreateBillFromLabTestsRequest(
    int AppointmentId,
    int PatientId,
    decimal ExaminationFee,
    string? Notes
);
