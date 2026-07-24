using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs;
using MyProject.Application.Services;

namespace MyProject.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly JwtTokenService _jwtTokenService;

    public AccountController(AuthService authService, JwtTokenService jwtTokenService)
    {
        _authService = authService;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request.Username, request.Password);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        var (token, expiresAt) = _jwtTokenService.GenerateToken(
            result.UserId!.Value,
            result.Username!,
            result.RoleName!,
            result.FullName!,
            result.PatientId,
            result.DoctorId,
            result.StaffId);

        var response = result with { AccessToken = token, ExpiresAt = expiresAt };

        return Ok(response);
    }

    /// <summary>
    /// JWT is stateless; logout is handled client-side by discarding the token.
    /// This endpoint exists for API symmetry / future token revocation support.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return Ok(new { Message = "Logout successful" });
    }

    /// <summary>
    /// Allows an authenticated user to change their own password.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { Message = "Invalid user identity." });
        }

        try
        {
            await _authService.ChangePasswordAsync(userId, request);
            return Ok(new { Message = "Password changed successfully." });
        }
        catch (System.Collections.Generic.KeyNotFoundException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (System.ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }
}
