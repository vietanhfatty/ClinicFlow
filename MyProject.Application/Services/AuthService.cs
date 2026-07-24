using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using MyProject.Application.DTOs;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;

namespace MyProject.Application.Services;

public class AuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IPatientRepository _patientRepo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly IStaffRepository _staffRepo;

    public AuthService(
        IUserRepository userRepo,
        IPatientRepository patientRepo,
        IDoctorRepository doctorRepo,
        IStaffRepository staffRepo)
    {
        _userRepo = userRepo;
        _patientRepo = patientRepo;
        _doctorRepo = doctorRepo;
        _staffRepo = staffRepo;
    }

    public async Task<LoginResponse> LoginAsync(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return new LoginResponse(false, "Username and password are required.", null, null, null, null);
        }

        var user = await _userRepo.GetByUsernameAsync(username.Trim());
        if (user == null)
        {
            return new LoginResponse(false, "Invalid username or password.", null, null, null, null);
        }

        if (!user.IsActive)
        {
            return new LoginResponse(false, "This account is inactive.", null, null, null, null);
        }

        var inputHash = HashPassword(password);
        if (!string.Equals(user.PasswordHash, inputHash, StringComparison.OrdinalIgnoreCase))
        {
            return new LoginResponse(false, "Invalid username or password.", null, null, null, null);
        }

        var fullName = user.Username;
        var roleName = user.Role?.RoleName ?? "User";
        int? patientId = null;
        int? doctorId = null;
        int? staffId = null;

        if (roleName.Equals("Patient", StringComparison.OrdinalIgnoreCase))
        {
            var patients = await _patientRepo.GetAllAsync();
            var patient = patients.FirstOrDefault(p => p.UserId == user.UserId);
            if (patient != null)
            {
                fullName = patient.FullName;
                patientId = patient.PatientId;
            }
        }
        else if (roleName.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
        {
            var doctors = await _doctorRepo.GetAllAsync();
            var doctor = doctors.FirstOrDefault(d => 
                string.Equals(d.Email, user.Username, StringComparison.OrdinalIgnoreCase) ||
                (user.Username.StartsWith("doctor", StringComparison.OrdinalIgnoreCase) && 
                 int.TryParse(user.Username.Substring(6), out int num) && 
                 num == d.DoctorId) ||
                (user.Username.StartsWith("dr.", StringComparison.OrdinalIgnoreCase) && 
                 !string.IsNullOrEmpty(d.Email) && 
                 d.Email.Contains('@') && 
                 string.Equals(user.Username.Substring(3), d.Email.Split('@')[0].Split('.').LastOrDefault(), StringComparison.OrdinalIgnoreCase)));
            if (doctor != null)
            {
                fullName = doctor.FullName;
                doctorId = doctor.DoctorId;
            }
        }
        else if (!roleName.Equals("Patient", StringComparison.OrdinalIgnoreCase) && !roleName.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
        {
            var staffList = await _staffRepo.GetAllAsync();
            var uname = user.Username.Trim();

            var staff = staffList.FirstOrDefault(s => 
                string.Equals(s.Email, uname, StringComparison.OrdinalIgnoreCase) || 
                string.Equals(s.Phone, uname, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(s.Email) && s.Email.Split('@')[0].Equals(uname, StringComparison.OrdinalIgnoreCase)) ||
                (uname.StartsWith("staff", StringComparison.OrdinalIgnoreCase) && 
                 int.TryParse(uname.Substring(5), out int num) && 
                 num == s.StaffId) ||
                (uname.Contains('.') && s.FullName.EndsWith(uname.Split('.')[^1], StringComparison.OrdinalIgnoreCase)) ||
                (uname.Contains('_') && s.FullName.EndsWith(uname.Split('_')[^1], StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(uname) && s.FullName.Replace(" ", "").EndsWith(uname.Replace("staff", "").Replace(".", "").Replace("_", ""), StringComparison.OrdinalIgnoreCase))
            );

            if (staff == null && staffList.Any())
            {
                staff = staffList.FirstOrDefault();
            }

            if (staff != null)
            {
                fullName = staff.FullName;
                staffId = staff.StaffId;
            }
        }

        return new LoginResponse(
            true,
            "Login successful.",
            user.UserId,
            user.Username,
            roleName,
            fullName,
            PatientId: patientId,
            DoctorId: doctorId,
            StaffId: staffId
        );
    }

    /// <summary>
    /// Changes a user's password after verifying the current password.
    /// Reuses the existing unsalted SHA256 hashing to remain compatible with legacy credentials.
    /// </summary>
    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        var user = await _userRepo.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User with ID {userId} not found");

        var currentHash = HashPassword(request.CurrentPassword);
        if (!string.Equals(user.PasswordHash, currentHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Current password is incorrect.");
        }

        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
        {
            throw new ArgumentException("New password must be at least 6 characters.");
        }

        user.PasswordHash = HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepo.UpdateAsync(user);
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }
}
