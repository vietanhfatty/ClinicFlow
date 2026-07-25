using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace MyProject.Application.Services;

/// <summary>
/// Generates JWT access tokens embedding user identity and role-specific claims
/// (PatientId/DoctorId/StaffId) so downstream APIs can authorize without extra lookups.
/// </summary>
public class JwtTokenService
{
    private readonly string _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryMinutes;

    public JwtTokenService(IConfiguration configuration)
    {
        _key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");
        _issuer = configuration["Jwt:Issuer"] ?? "HospitalManagementApi";
        _audience = configuration["Jwt:Audience"] ?? "HospitalManagementClient";
        _expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var m) ? m : 60;
    }

    public (string Token, DateTime ExpiresAt) GenerateToken(
        int userId,
        string username,
        string roleName,
        string fullName,
        int? patientId = null,
        int? doctorId = null,
        int? staffId = null)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, roleName),
            new Claim("FullName", fullName)
        };

        if (patientId.HasValue)
        {
            claims.Add(new Claim("PatientId", patientId.Value.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, roleName)); // Re-add with patientId context
        }
        if (doctorId.HasValue)
            claims.Add(new Claim("DoctorId", doctorId.Value.ToString()));
        if (staffId.HasValue)
            claims.Add(new Claim("StaffId", staffId.Value.ToString()));

        // Ensure role is always in claims
        if (!claims.Any(c => c.Type == ClaimTypes.Role))
            claims.Add(new Claim(ClaimTypes.Role, roleName));

        var expiresAt = DateTime.UtcNow.AddMinutes(_expiryMinutes);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
