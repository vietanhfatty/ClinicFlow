using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;

namespace MyProject.Infrastructure.Repositories;

public class DoctorRepository : IDoctorRepository
{
    private readonly HospitalManagementDbContext _context;

    public DoctorRepository(HospitalManagementDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Doctor>> GetAllAsync()
    {
        return await _context.Doctors
            .ToListAsync();
    }

    public async Task<Doctor?> GetByIdAsync(int id)
    {
        return await _context.Doctors
            .FirstOrDefaultAsync(d => d.DoctorId == id);
    }

    public async Task AddAsync(Doctor doctor)
    {
        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Doctor doctor)
    {
        _context.Entry(doctor).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var doctor = await _context.Doctors.FindAsync(id);
        if (doctor != null)
        {
            _context.Doctors.Remove(doctor);
            await _context.SaveChangesAsync();
        }
    }
}
