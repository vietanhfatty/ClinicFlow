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

public class StaffService
{
    private readonly IStaffRepository _repo;
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;
    private readonly IAppointmentRepository _appointmentRepo;

    public StaffService(IStaffRepository repo, IUserRepository userRepo, IRoleRepository roleRepo, IAppointmentRepository appointmentRepo)
    {
        _repo = repo;
        _userRepo = userRepo;
        _roleRepo = roleRepo;
        _appointmentRepo = appointmentRepo;
    }

    public async Task<IEnumerable<StaffDto>> GetAllAsync()
    {
        var list = await _repo.GetAllAsync();
        var users = await _userRepo.GetAllAsync();
        return list.Select(s => MapToDto(s, users));
    }

    public async Task<StaffDto?> GetByIdAsync(int id)
    {
        var s = await _repo.GetByIdAsync(id);
        if (s is null) return null;
        var users = await _userRepo.GetAllAsync();
        return MapToDto(s, users);
    }

    public async Task CreateAsync(CreateStaffRequest req)
    {
        var username = req.Username.Trim();
        var existingUser = await _userRepo.GetByUsernameAsync(username);
        if (existingUser != null)
        {
            throw new ArgumentException($"Username '{req.Username}' is already taken.");
        }

        var roles = await _roleRepo.GetAllAsync();
        var roleName = req.Position?.Trim() ?? "Receptionist";
        var staffRole = roles.FirstOrDefault(r => r.RoleName.Equals(roleName, StringComparison.OrdinalIgnoreCase))
            ?? roles.FirstOrDefault(r => r.RoleName.Equals("Receptionist", StringComparison.OrdinalIgnoreCase))
            ?? roles.FirstOrDefault(r => r.RoleName.Equals("Staff", StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Role '{roleName}' not found in the system. Please create the role first.");

        var user = new User
        {
            Username = username,
            PasswordHash = HashPassword(req.Password),
            RoleId = staffRole.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var staff = new Staff
        {
            FullName = req.FullName.Trim(),
            Phone = req.Phone?.Trim(),
            Email = req.Email?.Trim() ?? username, // Fallback to username if email is empty
            Position = req.Position?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _userRepo.AddAsync(user);
        await _repo.AddAsync(staff);
    }

    public async Task UpdateAsync(int id, UpdateStaffRequest req)
    {
        var staff = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Staff with ID {id} not found");

        staff.FullName = req.FullName.Trim();
        staff.Phone = req.Phone?.Trim();
        staff.Email = req.Email?.Trim();
        staff.Position = req.Position?.Trim();

        await _repo.UpdateAsync(staff);
    }

    public async Task DeleteAsync(int id)
    {
        var staff = await _repo.GetByIdAsync(id);
        if (staff != null)
        {
            // Nullify StaffId on all appointments associated with this staff
            var appointments = await _appointmentRepo.GetAllAsync();
            var relatedAppointments = appointments.Where(a => a.StaffId == id).ToList();
            foreach (var app in relatedAppointments)
            {
                app.StaffId = null;
                await _appointmentRepo.UpdateAsync(app);
            }

            // Find corresponding user matching staff Email, Phone, or sequential username format (staffXX)
            var users = await _userRepo.GetAllAsync();
            var user = users.FirstOrDefault(u => 
                string.Equals(u.Username, staff.Email, StringComparison.OrdinalIgnoreCase) || 
                string.Equals(u.Username, staff.Phone, StringComparison.OrdinalIgnoreCase) ||
                (u.Username.StartsWith("staff", StringComparison.OrdinalIgnoreCase) && 
                 int.TryParse(u.Username.Substring(5), out int num) && 
                 num == staff.StaffId)
            );
            if (user != null)
            {
                await _userRepo.DeleteAsync(user.UserId);
            }
        }
        await _repo.DeleteAsync(id);
    }

    private StaffDto MapToDto(Staff s, IEnumerable<User> users)
    {
        // Try to match user by staff Email or Phone, or by sequential username staff01 -> StaffId 1
        var user = users.FirstOrDefault(u => 
            string.Equals(u.Username, s.Email, StringComparison.OrdinalIgnoreCase) || 
            string.Equals(u.Username, s.Phone, StringComparison.OrdinalIgnoreCase) ||
            (u.Username.StartsWith("staff", StringComparison.OrdinalIgnoreCase) && 
             int.TryParse(u.Username.Substring(5), out int num) && 
             num == s.StaffId)
        );
        return new StaffDto(
            s.StaffId,
            user?.UserId ?? 0,
            s.FullName,
            user?.Username ?? "",
            s.Phone,
            s.Email,
            s.Position,
            s.CreatedAt
        );
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }
}
