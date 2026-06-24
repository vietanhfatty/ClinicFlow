using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs;
using MyProject.Application.Services;

namespace MyProject.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly AppointmentService _service;

    public AppointmentsController(AppointmentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest request)
    {
        try
        {
            await _service.CreateAsync(request);
            return Created("", null);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAppointmentRequest request)
    {
        try
        {
            await _service.UpdateAsync(id, request);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }

    [HttpPost("{id}/checkin")]
    public async Task<IActionResult> CheckIn(int id)
    {
        try
        {
            await _service.CheckInAsync(id);
            return Ok(new { Message = "Patient checked in successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.InnerException?.Message ?? ex.Message });
        }
    }

    [HttpPost("{id}/confirm")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> Confirm(int id)
    {
        try
        {
            await _service.ConfirmAsync(id);
            return Ok(new { Message = "Appointment confirmed successfully." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.InnerException?.Message ?? ex.Message });
        }
    }

    [HttpPost("{id}/start")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> StartExamination(int id)
    {
        try
        {
            await _service.StartExaminationAsync(id);
            return Ok(new { Message = "Examination started." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.InnerException?.Message ?? ex.Message });
        }
    }

    [HttpPost("{id}/complete")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> Complete(int id)
    {
        try
        {
            await _service.CompleteAsync(id);
            return Ok(new { Message = "Appointment completed." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = ex.InnerException?.Message ?? ex.Message });
        }
    }

    [HttpGet("by-patient/{patientId}")]
    public async Task<IActionResult> GetByPatient(int patientId)
    {
        return Ok(await _service.GetByPatientIdAsync(patientId));
    }

    [HttpGet("busy-slots")]
    public async Task<IActionResult> GetBusySlots([FromQuery] int doctorId, [FromQuery] string date)
    {
        if (!DateOnly.TryParse(date, out DateOnly dateOnly))
        {
            return BadRequest(new { Message = "Invalid date format. Expected yyyy-MM-dd." });
        }

        var list = await _service.GetAllAsync();
        var activeAppointments = list
            .Where(a => a.DoctorId == doctorId && a.AppointmentDate == dateOnly && a.Status != "Cancelled")
            .ToList();

        var predefinedSlots = new List<TimeSpan>
        {
            new TimeSpan(8, 0, 0), new TimeSpan(8, 30, 0), new TimeSpan(9, 0, 0), new TimeSpan(9, 30, 0),
            new TimeSpan(10, 0, 0), new TimeSpan(10, 30, 0), new TimeSpan(11, 0, 0), new TimeSpan(11, 30, 0),
            new TimeSpan(13, 30, 0), new TimeSpan(14, 0, 0), new TimeSpan(14, 30, 0), new TimeSpan(15, 0, 0),
            new TimeSpan(15, 30, 0), new TimeSpan(16, 0, 0), new TimeSpan(16, 30, 0), new TimeSpan(17, 0, 0)
        };

        var busy = new List<string>();
        foreach (var slot in predefinedSlots)
        {
            var slotEnd = slot.Add(TimeSpan.FromMinutes(30));
            var isBusy = activeAppointments.Any(a => 
            {
                var apptStart = a.AppointmentTime;
                var apptEnd = apptStart.Add(TimeSpan.FromMinutes(30));
                return apptStart < slotEnd && slot < apptEnd;
            });

            if (isBusy)
            {
                busy.Add(slot.ToString(@"hh\:mm"));
            }
        }

        return Ok(busy);
    }
}
