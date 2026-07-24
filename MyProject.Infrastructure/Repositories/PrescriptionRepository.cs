using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;

namespace MyProject.Infrastructure.Repositories;

public class PrescriptionRepository : IPrescriptionRepository
{
    private readonly HospitalManagementDbContext _context;

    public PrescriptionRepository(HospitalManagementDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Prescription>> GetByMedicalRecordIdAsync(int medicalRecordId)
    {
        return await _context.Prescriptions
            .Where(p => p.MedicalRecordId == medicalRecordId)
            .ToListAsync();
    }

    public async Task<Prescription?> GetByIdAsync(int id)
    {
        return await _context.Prescriptions
            .Include(p => p.MedicalRecord)
                .ThenInclude(mr => mr.Appointment)
                    .ThenInclude(a => a.Patient)
            .Include(p => p.MedicalRecord)
                .ThenInclude(mr => mr.Appointment)
                    .ThenInclude(a => a.Doctor)
            .FirstOrDefaultAsync(p => p.PrescriptionId == id);
    }

    public async Task AddAsync(Prescription prescription)
    {
        _context.Prescriptions.Add(prescription);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var prescription = await _context.Prescriptions.FindAsync(id);
        if (prescription != null)
        {
            _context.Prescriptions.Remove(prescription);
            await _context.SaveChangesAsync();
        }
    }
}
