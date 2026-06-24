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

public class DoctorService
{
    private readonly IDoctorRepository _repo;
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;

    public DoctorService(IDoctorRepository repo, IUserRepository userRepo, IRoleRepository roleRepo)
    {
        _repo = repo;
        _userRepo = userRepo;
        _roleRepo = roleRepo;
    }

    public async Task<IEnumerable<DoctorDto>> GetAllAsync()
    {
        var list = await _repo.GetAllAsync();
        var users = await _userRepo.GetAllAsync();
        return list.Select(d => MapToDto(d, users));
    }

    public async Task<DoctorDto?> GetByIdAsync(int id)
    {
        var d = await _repo.GetByIdAsync(id);
        if (d is null) return null;
        var users = await _userRepo.GetAllAsync();
        return MapToDto(d, users);
    }

    public async Task CreateAsync(CreateDoctorRequest req)
    {
        var username = req.Username.Trim();
        var existingUser = await _userRepo.GetByUsernameAsync(username);
        if (existingUser != null)
        {
            throw new ArgumentException($"Username '{req.Username}' is already taken.");
        }

        var roles = await _roleRepo.GetAllAsync();
        var doctorRole = roles.FirstOrDefault(r => r.RoleName.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException("Role 'Doctor' not found in system.");

        var user = new User
        {
            Username = username,
            PasswordHash = HashPassword(req.Password),
            RoleId = doctorRole.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var doctor = new Doctor
        {
            FullName = req.FullName.Trim(),
            Phone = req.Phone?.Trim(),
            Email = username, // link Doctor.Email with User.Username
            Specialization = req.Specialization.Trim(),
            ExperienceYears = req.ExperienceYears,
            Description = req.Description?.Trim()
        };

        await _userRepo.AddAsync(user);
        await _repo.AddAsync(doctor);
    }

    public async Task UpdateAsync(int id, UpdateDoctorRequest req)
    {
        var doctor = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Doctor with ID {id} not found");

        doctor.FullName = req.FullName.Trim();
        doctor.Phone = req.Phone?.Trim();
        doctor.Specialization = req.Specialization.Trim();
        doctor.ExperienceYears = req.ExperienceYears;
        doctor.Description = req.Description?.Trim();

        await _repo.UpdateAsync(doctor);
    }

    public async Task DeleteAsync(int id)
    {
        var doctor = await _repo.GetByIdAsync(id);
        if (doctor != null && !string.IsNullOrWhiteSpace(doctor.Email))
        {
            var user = await _userRepo.GetByUsernameAsync(doctor.Email);
            if (user != null)
            {
                await _userRepo.DeleteAsync(user.UserId);
            }
        }
        await _repo.DeleteAsync(id);
    }

    private DoctorDto MapToDto(Doctor d, IEnumerable<User> users)
    {
        var user = users.FirstOrDefault(u => 
            string.Equals(u.Username, d.Email, StringComparison.OrdinalIgnoreCase) ||
            (u.Username.StartsWith("doctor", StringComparison.OrdinalIgnoreCase) && 
             int.TryParse(u.Username.Substring(6), out int num) && 
             num == d.DoctorId) ||
            (u.Username.StartsWith("dr.", StringComparison.OrdinalIgnoreCase) && 
             !string.IsNullOrEmpty(d.Email) && 
             d.Email.Contains('@') && 
             string.Equals(u.Username.Substring(3), d.Email.Split('@')[0].Split('.').LastOrDefault(), StringComparison.OrdinalIgnoreCase))
        );
        return new DoctorDto(
            d.DoctorId,
            user?.UserId ?? 0,
            d.FullName,
            user?.Username ?? "",
            d.Phone,
            d.Specialization,
            d.ExperienceYears,
            d.Description
        );
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }
}
