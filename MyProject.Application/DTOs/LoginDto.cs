using System;
using System.ComponentModel.DataAnnotations;

namespace MyProject.Application.DTOs;

public record LoginRequest(string Username, string Password);

public record LoginResponse(
    bool Success,
    string Message,
    int? UserId,
    string? Username,
    string? RoleName,
    string? FullName,
    string? AccessToken = null,
    int? PatientId = null,
    int? DoctorId = null,
    int? StaffId = null,
    DateTime? ExpiresAt = null
);

public record ChangePasswordRequest(
    [Required(ErrorMessage = "Current password is required.")]
    string CurrentPassword,

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(255, MinimumLength = 6, ErrorMessage = "New password must be at least 6 characters.")]
    string NewPassword)
{
    /// <summary>
    /// View-only field used by the WebMvc form to confirm the new password.
    /// Not sent to / used by the WebApi; excluded from equality by design of records
    /// would require manual override, but it is harmless as an extra property here.
    /// </summary>
    [Compare("NewPassword", ErrorMessage = "Password confirmation does not match.")]
    public string ConfirmPassword { get; init; } = string.Empty;
}
