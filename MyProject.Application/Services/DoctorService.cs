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
        return list.Select(MapToDto);
    }

    public async Task<DoctorDto?> GetByIdAsync(int id)
    {
        var d = await _repo.GetByIdAsync(id);
        return d is null ? null : MapToDto(d);
    }

    public async Task CreateAsync(CreateDoctorRequest req)
    {
        var existingUser = await _userRepo.GetByUsernameAsync(req.Username.Trim());
        if (existingUser != null)
        {
            throw new ArgumentException($"Username '{req.Username}' is already taken.");
        }

        var roles = await _roleRepo.GetAllAsync();
        var doctorRole = roles.FirstOrDefault(r => r.RoleName.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException("Role 'Doctor' not found in system.");

        var user = new User
        {
            FullName = req.FullName.Trim(),
            Username = req.Username.Trim(),
            PasswordHash = HashPassword(req.Password),
            Phone = req.Phone?.Trim(),
            RoleId = doctorRole.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var doctor = new Doctor
        {
            User = user,
            Specialization = req.Specialization.Trim(),
            ExperienceYears = req.ExperienceYears,
            Description = req.Description?.Trim()
        };

        await _repo.AddAsync(doctor);
    }

    public async Task UpdateAsync(int id, UpdateDoctorRequest req)
    {
        var doctor = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Doctor with ID {id} not found");

        doctor.User.FullName = req.FullName.Trim();
        doctor.User.Phone = req.Phone?.Trim();
        doctor.User.UpdatedAt = DateTime.UtcNow;

        doctor.Specialization = req.Specialization.Trim();
        doctor.ExperienceYears = req.ExperienceYears;
        doctor.Description = req.Description?.Trim();

        await _repo.UpdateAsync(doctor);
    }

    public async Task DeleteAsync(int id)
    {
        var doctor = await _repo.GetByIdAsync(id);
        if (doctor != null)
        {
            var userId = doctor.UserId;
            await _repo.DeleteAsync(id);
            await _userRepo.DeleteAsync(userId);
        }
    }

    private DoctorDto MapToDto(Doctor d)
    {
        return new DoctorDto(
            d.DoctorId,
            d.UserId,
            d.User.FullName,
            d.User.Username,
            d.User.Phone,
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
