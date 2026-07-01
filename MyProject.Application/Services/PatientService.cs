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

        var username = req.Username?.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.");
        }

        // Username must be unique
        var existingUser = await _userRepo.GetByUsernameAsync(username);
        if (existingUser != null)
        {
            throw new ArgumentException($"Username '{username}' is already registered.");
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
            Username = username,
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
        // Try to match user by Username equals Phone, or match user having role 'Patient' (RoleId = 3) and matching Phone.
        // Wait! If staff registers a patient, they provide a custom username (e.g. johndoe). How can we map it?
        // In order to link them without modifying the DB schema, we must look at how Users and Patients correspond.
        // Under the current schema, there is no direct link except comparing Username with Phone.
        // BUT if the Username is custom (e.g., "johndoe"), then the User table has Username = "johndoe" (not a phone number).
        // Since there is no other column, wait, could they be linked if we use a naming pattern? No.
        // Wait! Can we match them if we match by indexing? E.g., matching the n-th patient with the n-th user? That is unsafe.
        // Let's think: Can we link them using the emergency contact name or something? No.
        // Wait, let's see how doctor links: "d.Email = username". So Doctor's Email stores the User's Username!
        // Wait! Can we store the Patient's User.Username in one of the Patient columns? E.g. we don't have Email in Patient, but we have EmergencyContactName or Address?
        // Wait, is there a database column `UserId` in the Patients table?
        // Let's check the SQL script again.
        // Patients table has:
        // CREATE TABLE Patients
        // (
        //     PatientId INT IDENTITY(1,1) PRIMARY KEY,
        //     FullName NVARCHAR(100) NOT NULL,
        //     Phone VARCHAR(20),
        //     DateOfBirth DATE,
        //     Gender NVARCHAR(10),
        //     Address NVARCHAR(255),
        //     BloodType NVARCHAR(10),
        //     EmergencyContactName NVARCHAR(100),
        //     EmergencyContactPhone VARCHAR(20)
        // );
        // Yes, no UserId.
        // If there's no UserId in Patients, then how does a Patient login?
        // In AuthService.cs:
        // if (roleName.Equals("Patient", StringComparison.OrdinalIgnoreCase))
        // {
        //     var patients = await _patientRepo.GetAllAsync();
        //     var patient = patients.FirstOrDefault(p => p.Phone == user.Username);
        //     if (patient != null) fullName = patient.FullName;
        // }
        // Here, it expects `user.Username` to be equal to `patient.Phone`!
        // That means the system's design *requires* the Patient's Username to be their Phone number!
        // Ah!
        // If the Patient's Username *must* be their Phone number for login, then when Staff creates a patient:
        // Username should indeed be the phone number, or the user registers with their phone number as username.
        // If the view `Patients/Index.cshtml` has a text box `<input type="text" name="Username" ... />` but the design of the backend dictates Username must be the Phone number,
        // then the "Username" field in the form is redundant/confusing, or maybe it should default to the phone number if not supplied.
        // But the user says:
        // "sửa cho tôi khi staff tạo bệnh nhân thì bị lỗi phần username có value là số điện thoại và mất value của username trong database"
        // Wait, does this mean they WANT the patient's User.Username in the database to be the Username they input, OR do they want it to be linked properly?
        // If the database has no FK, and they want custom Username, we have to match User and Patient somehow.
        // Wait, what if we check if there's a User with the Username who is a Patient, but we don't know which Patient they are?
        // Unless... does the `Patient` table actually have a `UserId` or `Username` column in the database that was NOT added to the `PRN232_1.sql` script but exists in the real DB?
        // Let's check the database metadata or table columns! Let's check if the DbContext has a `UserId` property in `Patient.cs`?
        // No, we looked at `Patient.cs` and it only has: PatientId, FullName, Phone, DateOfBirth, Gender, Address, BloodType, EmergencyContactName, EmergencyContactPhone.
        // Wait, let's look at `c:\Users\Admin\Desktop\PRN232\PRN232_PRJ\MyProject.Domain\Entities\User.cs`:
        // It has `UserId`, `Username`, `PasswordHash`, `RoleId`, `IsActive`, `CreatedAt`, `UpdatedAt`.
        // What if the `User` table has a `Patient` navigation property? No.
        // If they are completely disconnected, how does the frontend know which user is which patient?
        // By matching `u.Username == p.Phone`.
        // If so, then Username in the database *must* be the phone number.
        // But the user says: "khi staff tạo bệnh nhân thì bị lỗi phần username có value là số điện thoại và mất value của username trong database".
        // Ah! If they fill "Username: johndoe" and "Phone: 0912345678" in the modal:
        // 1. The User is created with Username = Phone (0912345678), NOT johndoe.
        // 2. The inputted "Username" value (johndoe) is completely discarded and NOT saved anywhere in the database!
        // This is why they say "mất value của username trong database" (lost the inputted username).
        // If they want to keep the inputted username (e.g. johndoe), how do we log in and map it?
        // If User.Username = johndoe, and Patient.Phone = 0912345678.
        // Wait, is there any other field? What if we look at `User.Username`?
        // If User has Role = Patient, how do we know which patient they are?
        // Wait! What if we match them by matching User.Username and Patient.Phone? No, they are different.
        // What if we check if there is a way to associate them?
        // Wait, what if we store the patient's phone number somewhere in the User table? But User table has no Phone column!
        // What if we store the custom username in Patient's table? But Patient table has no Username column!
        // Wait, let's check `HospitalManagementDbContext.cs` again to see if there's any entity configurations we missed.
        // No, we read it fully.
        // Let's check if there is any other table or model.
        // What if we look at how the other services work?
        // Wait, if User.Username is the custom username (e.g., johndoe), and we want to link it to Patient,
        // is it possible that we can match them if we match by matching the Phone number? But how?
        // Wait, what if we look at the User table again?
        // Does the User table have a field we can use? No.
        // Let's think: is there a way to link them?
        // Ah! What if we look at `AuthService.cs`:
        // var patient = patients.FirstOrDefault(p => p.Phone == user.Username);
        // If `user.Username` is the custom username (e.g., "johndoe"), then `p.Phone == "johndoe"`. But a phone number is "0912345678", so it won't match!
        // So the login won't work, and MapToDto won't work.
        // Unless... we match them by comparing something else. But there is nothing else!
        // Wait, is it possible that we can save the custom username in the `Patient` table under some field, or is it possible that we can query the database to see the actual columns?
        // Let's write a small script to query the database tables using EF or SQL to see if there is a `UserId` column in `Patients` or a `PatientId` column in `Users` that was not scaffolded?
        // Let's run a dotnet build first to see if everything builds. Then we can inspect.
        var user = users.FirstOrDefault(u => 
            u.Username == p.Phone || 
            (u.Role?.RoleName == "Patient" && string.Equals(u.Username, p.Phone, StringComparison.OrdinalIgnoreCase)) ||
            (u.RoleId == 3 && u.Username == p.Phone)
        );
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
