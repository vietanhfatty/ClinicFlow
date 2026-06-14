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

public class PatientService
{
    private readonly IPatientRepository _repo;
    private readonly IUserRepository _userRepo;
    private readonly IRoleRepository _roleRepo;

    public PatientService(IPatientRepository repo, IUserRepository userRepo, IRoleRepository roleRepo)
    {
        _repo = repo;
        _userRepo = userRepo;
        _roleRepo = roleRepo;
    }

    public async Task<IEnumerable<PatientDto>> GetAllAsync()
    {
        var list = await _repo.GetAllAsync();
        var users = await _userRepo.GetAllAsync();
        return list.Select(p => MapToDto(p, users));
    }

    public async Task<PatientDto?> GetByIdAsync(int id)
    {
        var p = await _repo.GetByIdAsync(id);
        if (p is null) return null;
        var users = await _userRepo.GetAllAsync();
        return MapToDto(p, users);
    }

    public async Task CreateAsync(CreatePatientRequest req)
    {
        ValidatePatientData(req.Phone, req.DateOfBirth, req.EmergencyContactPhone);

        var phone = req.Phone?.Trim();
        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new ArgumentException("Phone number is required.");
        }

        // Username (which is the phone number) must be unique
        var existingUser = await _userRepo.GetByUsernameAsync(phone);
        if (existingUser != null)
        {
            throw new ArgumentException($"Phone number '{phone}' is already registered.");
        }

        // Phone unique validation
        var patients = await _repo.GetAllAsync();
        if (patients.Any(p => p.Phone == phone))
        {
            throw new ArgumentException($"Phone number '{phone}' is already in use by another patient.");
        }

        // Get Patient role
        var roles = await _roleRepo.GetAllAsync();
        var patientRole = roles.FirstOrDefault(r => r.RoleName.Equals("Patient", StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException("Role 'Patient' not found in system.");

        var user = new User
        {
            Username = phone,
            PasswordHash = HashPassword(req.Password),
            RoleId = patientRole.RoleId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var patient = new Patient
        {
            FullName = req.FullName.Trim(),
            Phone = phone,
            DateOfBirth = req.DateOfBirth,
            Gender = req.Gender?.Trim(),
            Address = req.Address?.Trim(),
            BloodType = req.BloodType?.Trim(),
            EmergencyContactName = req.EmergencyContactName?.Trim(),
            EmergencyContactPhone = req.EmergencyContactPhone?.Trim()
        };

        await _userRepo.AddAsync(user);
        await _repo.AddAsync(patient);
    }

    public async Task UpdateAsync(int id, UpdatePatientRequest req)
    {
        ValidatePatientData(req.Phone, req.DateOfBirth, req.EmergencyContactPhone);

        var patient = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Patient with ID {id} not found");

        var oldPhone = patient.Phone;
        var newPhone = req.Phone?.Trim();

        // Phone unique validation
        if (!string.IsNullOrWhiteSpace(newPhone) && newPhone != oldPhone)
        {
            var patients = await _repo.GetAllAsync();
            if (patients.Any(p => p.PatientId != id && p.Phone == newPhone))
            {
                throw new ArgumentException($"Phone number '{newPhone}' is already in use by another patient.");
            }
        }

        // Update corresponding User if phone changed
        if (!string.IsNullOrWhiteSpace(oldPhone))
        {
            var user = await _userRepo.GetByUsernameAsync(oldPhone);
            if (user != null)
            {
                if (!string.IsNullOrWhiteSpace(newPhone) && newPhone != oldPhone)
                {
                    user.Username = newPhone;
                }
                user.UpdatedAt = DateTime.UtcNow;
                await _userRepo.UpdateAsync(user);
            }
        }

        patient.FullName = req.FullName.Trim();
        patient.Phone = newPhone;
        patient.DateOfBirth = req.DateOfBirth;
        patient.Gender = req.Gender?.Trim();
        patient.Address = req.Address?.Trim();
        patient.BloodType = req.BloodType?.Trim();
        patient.EmergencyContactName = req.EmergencyContactName?.Trim();
        patient.EmergencyContactPhone = req.EmergencyContactPhone?.Trim();

        await _repo.UpdateAsync(patient);
    }

    private void ValidatePatientData(string? phone, DateOnly? dateOfBirth, string? emergencyPhone)
    {
        if (dateOfBirth.HasValue && dateOfBirth.Value > DateOnly.FromDateTime(DateTime.Today))
        {
            throw new ArgumentException("Date of birth cannot be in the future.");
        }

        if (!string.IsNullOrWhiteSpace(phone))
        {
            var p = phone.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(p, @"^0\d{9}$"))
            {
                throw new ArgumentException("Phone number must be a valid 10-digit number starting with 0.");
            }
        }

        if (!string.IsNullOrWhiteSpace(emergencyPhone))
        {
            var ep = emergencyPhone.Trim();
            if (!System.Text.RegularExpressions.Regex.IsMatch(ep, @"^0\d{9}$"))
            {
                throw new ArgumentException("Emergency contact phone number must be a valid 10-digit number starting with 0.");
            }
        }
    }

    public async Task DeleteAsync(int id)
    {
        var patient = await _repo.GetByIdAsync(id);
        if (patient != null && !string.IsNullOrWhiteSpace(patient.Phone))
        {
            var user = await _userRepo.GetByUsernameAsync(patient.Phone);
            if (user != null)
            {
                await _userRepo.DeleteAsync(user.UserId);
            }
        }
        await _repo.DeleteAsync(id);
    }

    public IQueryable<Patient> GetQueryable()
    {
        return _repo.GetQueryable();
    }

    private PatientDto MapToDto(Patient p, IEnumerable<User> users)
    {
        var user = users.FirstOrDefault(u => u.Username == p.Phone);
        return new PatientDto(
            p.PatientId,
            user?.UserId ?? 0,
            p.FullName,
            user?.Username ?? "",
            p.Phone,
            p.DateOfBirth,
            p.Gender,
            p.Address,
            p.BloodType,
            p.EmergencyContactName,
            p.EmergencyContactPhone
        );
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }
}
