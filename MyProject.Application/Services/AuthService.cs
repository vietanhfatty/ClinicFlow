using System;
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

        if (roleName.Equals("Patient", StringComparison.OrdinalIgnoreCase))
        {
            var patients = await _patientRepo.GetAllAsync();
            var patient = patients.FirstOrDefault(p => p.Phone == user.Username);
            if (patient != null) fullName = patient.FullName;
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
            if (doctor != null) fullName = doctor.FullName;
        }
        else if (roleName.Equals("Staff", StringComparison.OrdinalIgnoreCase) || roleName.Equals("Receptionist", StringComparison.OrdinalIgnoreCase))
        {
            var staffList = await _staffRepo.GetAllAsync();
            var staff = staffList.FirstOrDefault(s => 
                string.Equals(s.Email, user.Username, StringComparison.OrdinalIgnoreCase) || 
                string.Equals(s.Phone, user.Username, StringComparison.OrdinalIgnoreCase) ||
                (user.Username.StartsWith("staff", StringComparison.OrdinalIgnoreCase) && 
                 int.TryParse(user.Username.Substring(5), out int num) && 
                 num == s.StaffId));
            if (staff != null) fullName = staff.FullName;
        }

        return new LoginResponse(
            true,
            "Login successful.",
            user.UserId,
            user.Username,
            roleName,
            fullName
        );
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }
}
