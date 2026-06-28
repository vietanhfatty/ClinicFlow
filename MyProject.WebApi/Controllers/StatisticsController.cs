using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.Services;

namespace MyProject.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StatisticsController : ControllerBase
{
    private readonly StatisticsService _service;

    public StatisticsController(StatisticsService service)
    {
        _service = service;
    }

    [HttpGet("hospital")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetHospitalStatistics()
    {
        var result = await _service.GetHospitalStatisticsAsync();
        return Ok(result);
    }

    [HttpGet("doctor-workload")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GetDoctorWorkload()
    {
        var result = await _service.GetDoctorWorkloadAsync();
        return Ok(result);
    }
}
