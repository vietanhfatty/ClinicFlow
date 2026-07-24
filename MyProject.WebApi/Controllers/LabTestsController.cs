using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs;
using MyProject.Application.Services;

namespace MyProject.WebApi.Controllers;

/// <summary>
/// API controller for lab test operations including service catalog, 
/// lab test requests, results, and statistics
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LabTestsController : ControllerBase
{
    private readonly LabTestService _service;

    /// <summary>
    /// Initializes a new instance of the LabTestsController class
    /// </summary>
    /// <param name="service">The lab test service instance</param>
    public LabTestsController(LabTestService service)
    {
        _service = service;
    }

    /// <summary>
    /// Gets all lab test services from the catalog
    /// </summary>
    /// <returns>A list of all available lab test services</returns>
    [HttpGet("services")]
    public async Task<IActionResult> GetAllServices()
    {
        var services = await _service.GetAllLabTestServicesAsync();
        return Ok(services);
    }

    /// <summary>
    /// Gets a specific lab test service by ID
    /// </summary>
    /// <param name="id">The ID of the lab test service</param>
    /// <returns>The lab test service details or 404 if not found</returns>
    [HttpGet("services/{id}")]
    public async Task<IActionResult> GetServiceById(int id)
    {
        var service = await _service.GetLabTestServiceByIdAsync(id);
        if (service == null)
            return NotFound();
        return Ok(service);
    }

    /// <summary>
    /// Gets only active lab test services from the catalog
    /// </summary>
    /// <returns>A list of active lab test services</returns>
    [HttpGet("services/active")]
    public async Task<IActionResult> GetActiveServices()
    {
        var services = await _service.GetActiveLabTestServicesAsync();
        return Ok(services);
    }

    /// <summary>
    /// Gets all lab tests requested for a specific appointment
    /// </summary>
    /// <param name="appointmentId">The ID of the appointment</param>
    /// <returns>A list of lab tests for the appointment</returns>
    [HttpGet("by-appointment/{appointmentId}")]
    public async Task<IActionResult> GetByAppointment(int appointmentId)
    {
        var tests = await _service.GetLabTestsByAppointmentAsync(appointmentId);
        return Ok(tests);
    }

    /// <summary>
    /// Gets all lab tests for a specific patient across all appointments
    /// </summary>
    /// <param name="patientId">The ID of the patient</param>
    /// <returns>A list of all lab tests for the patient</returns>
    [HttpGet("by-patient/{patientId}")]
    public async Task<IActionResult> GetByPatient(int patientId)
    {
        var tests = await _service.GetLabTestsByPatientAsync(patientId);
        return Ok(tests);
    }

    /// <summary>
    /// Gets all lab tests requested by a specific doctor
    /// </summary>
    /// <param name="doctorId">The ID of the doctor</param>
    /// <returns>A list of lab tests requested by the doctor</returns>
    [HttpGet("by-doctor/{doctorId}")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> GetByDoctor(int doctorId)
    {
        var tests = await _service.GetLabTestsByDoctorAsync(doctorId);
        return Ok(tests);
    }

    /// <summary>
    /// Gets all lab tests with Pending status
    /// </summary>
    /// <returns>A list of pending lab tests</returns>
    [HttpGet("pending")]
    [Authorize(Roles = "Admin,Staff,Doctor")]
    public async Task<IActionResult> GetPendingTests()
    {
        var tests = await _service.GetPendingLabTestsAsync();
        return Ok(tests);
    }

    /// <summary>
    /// Gets all lab tests with Completed status
    /// </summary>
    /// <returns>A list of completed lab tests</returns>
    [HttpGet("completed")]
    [Authorize(Roles = "Admin,Staff,Doctor")]
    public async Task<IActionResult> GetCompletedTests()
    {
        var tests = await _service.GetCompletedLabTestsAsync();
        return Ok(tests);
    }

    /// <summary>
    /// Gets dashboard statistics for lab tests
    /// </summary>
    /// <returns>Statistics including total, completed, and pending counts with breakdowns</returns>
    [HttpGet("statistics")]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> GetStatistics()
    {
        var statistics = await _service.GetLabTestStatisticsAsync();
        return Ok(statistics);
    }

    /// <summary>
    /// Creates a new lab test request for an appointment. Called by a Doctor when
    /// ordering tests at the start of an examination.
    /// </summary>
    /// <param name="request">The lab test request details</param>
    /// <returns>The created lab test information or appropriate error status</returns>
    [HttpPost("create")]
    [Authorize(Roles = "Doctor,Admin")]
    public async Task<IActionResult> CreateLabTest([FromBody] CreateLabTestRequest request)
    {
        try
        {
            var result = await _service.CreateAppointmentLabTestAsync(request);
            return CreatedAtAction(nameof(GetByAppointment), new { appointmentId = result.AppointmentId }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Updates lab test results with test date, result, and status. Called by
    /// Staff (lab technicians) once the test has been performed.
    /// </summary>
    /// <param name="id">The ID of the lab test to update</param>
    /// <param name="request">The updated lab test result information</param>
    /// <returns>The updated lab test information or appropriate error status</returns>
    [HttpPut("{id}/result")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> UpdateLabTestResult(int id, [FromBody] UpdateLabTestResultRequest request)
    {
        try
        {
            var result = await _service.UpdateLabTestResultAsync(id, request);
            return Ok(result);
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
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Updates only the price of a lab test service in the catalog. This is the
    /// sole catalog-modifying operation exposed to Admin; the service catalog
    /// itself (name/description/category/active state) is managed via seed data only.
    /// </summary>
    /// <param name="id">The ID of the lab test service</param>
    /// <param name="request">The request containing the new price</param>
    /// <returns>The updated lab test service or appropriate error status</returns>
    [HttpPut("services/{id}/price")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateServicePrice(int id, [FromBody] UpdateLabTestServicePriceRequest request)
    {
        try
        {
            var result = await _service.UpdateLabTestServicePriceAsync(id, request.Price);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a lab test record
    /// </summary>
    /// <param name="id">The ID of the lab test to delete</param>
    /// <returns>No content on success or appropriate error status</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteLabTest(int id)
    {
        try
        {
            await _service.DeleteLabTestAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}
