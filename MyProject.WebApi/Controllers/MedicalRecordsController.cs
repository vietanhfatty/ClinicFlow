using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs;
using MyProject.Application.Services;

namespace MyProject.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MedicalRecordsController : ControllerBase
{
    private readonly MedicalRecordService _service;

    public MedicalRecordsController(MedicalRecordService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null) return NotFound();

        // If patient, verify ownership
        if (User.IsInRole("Patient"))
        {
            var patientIdClaim = User.FindFirst("PatientId")?.Value;
            if (!int.TryParse(patientIdClaim, out int patientId))
            {
                return Unauthorized(new { Message = "Patient ID not found in token" });
            }

            var record = await _service.GetByIdAndPatientIdAsync(id, patientId);
            if (record == null)
            {
                return Forbid();
            }
            return Ok(record);
        }

        return Ok(result);
    }

    [HttpGet("by-appointment/{appointmentId}")]
    public async Task<IActionResult> GetByAppointmentId(int appointmentId)
    {
        var result = await _service.GetByAppointmentIdAsync(appointmentId);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("by-patient/{patientId}")]
    [Authorize(Roles = "Admin,Doctor,Staff")]
    public async Task<IActionResult> GetByPatientId(int patientId)
    {
        var result = await _service.GetByPatientIdAsync(patientId);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> Create([FromBody] CreateMedicalRecordRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = result.MedicalRecordId }, result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPost("{id}/prescriptions")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> AddPrescriptions(int id, [FromBody] List<CreatePrescriptionRequest> prescriptions)
    {
        try
        {
            await _service.AddPrescriptionsAsync(id, prescriptions);
            return Ok(new { Message = "Prescriptions added successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}
