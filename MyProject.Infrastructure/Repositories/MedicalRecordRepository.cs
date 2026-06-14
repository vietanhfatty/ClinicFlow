using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;

namespace MyProject.Infrastructure.Repositories;

public class MedicalRecordRepository : IMedicalRecordRepository
{
    private readonly HospitalManagementDbContext _context;

    public MedicalRecordRepository(HospitalManagementDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<MedicalRecord>> GetAllAsync()
    {
        return await _context.MedicalRecords
            .Include(mr => mr.Appointment)
                .ThenInclude(a => a.Patient)
            .Include(mr => mr.Appointment)
                .ThenInclude(a => a.Doctor)
            .Include(mr => mr.Prescriptions)
            .ToListAsync();
    }

    public async Task<MedicalRecord?> GetByIdAsync(int id)
    {
        return await _context.MedicalRecords
            .Include(mr => mr.Appointment)
                .ThenInclude(a => a.Patient)
            .Include(mr => mr.Appointment)
                .ThenInclude(a => a.Doctor)
            .Include(mr => mr.Prescriptions)
            .FirstOrDefaultAsync(mr => mr.MedicalRecordId == id);
    }

    public async Task<MedicalRecord?> GetByAppointmentIdAsync(int appointmentId)
    {
        return await _context.MedicalRecords
            .Include(mr => mr.Appointment)
                .ThenInclude(a => a.Patient)
            .Include(mr => mr.Appointment)
                .ThenInclude(a => a.Doctor)
            .Include(mr => mr.Prescriptions)
            .FirstOrDefaultAsync(mr => mr.AppointmentId == appointmentId);
    }

    public async Task<IEnumerable<MedicalRecord>> GetByPatientIdAsync(int patientId)
    {
        return await _context.MedicalRecords
            .Include(mr => mr.Appointment)
                .ThenInclude(a => a.Patient)
            .Include(mr => mr.Appointment)
                .ThenInclude(a => a.Doctor)
            .Include(mr => mr.Prescriptions)
            .Where(mr => mr.Appointment.PatientId == patientId)
            .ToListAsync();
    }

    public async Task AddAsync(MedicalRecord medicalRecord)
    {
        _context.MedicalRecords.Add(medicalRecord);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(MedicalRecord medicalRecord)
    {
        _context.Entry(medicalRecord).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }
}
