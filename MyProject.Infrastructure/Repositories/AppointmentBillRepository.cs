using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;

namespace MyProject.Infrastructure.Repositories;

public class AppointmentBillRepository : IAppointmentBillRepository
{
    private readonly HospitalManagementDbContext _context;

    public AppointmentBillRepository(HospitalManagementDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AppointmentBill>> GetAllAsync()
    {
        return await _context.AppointmentBills
            .Include(b => b.Appointment)
                .ThenInclude(a => a.Patient)
            .Include(b => b.Appointment)
                .ThenInclude(a => a.Doctor)
            .Include(b => b.Patient)
            .Include(b => b.Staff)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<AppointmentBill?> GetByIdAsync(int id)
    {
        return await _context.AppointmentBills
            .Include(b => b.Appointment)
                .ThenInclude(a => a.Patient)
            .Include(b => b.Appointment)
                .ThenInclude(a => a.Doctor)
            .Include(b => b.Patient)
            .Include(b => b.Staff)
            .FirstOrDefaultAsync(b => b.BillId == id);
    }

    public async Task<IEnumerable<AppointmentBill>> GetByPatientIdAsync(int patientId)
    {
        return await _context.AppointmentBills
            .Include(b => b.Appointment)
                .ThenInclude(a => a.Doctor)
            .Include(b => b.Patient)
            .Include(b => b.Staff)
            .Where(b => b.PatientId == patientId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<AppointmentBill>> GetByAppointmentIdAsync(int appointmentId)
    {
        return await _context.AppointmentBills
            .Include(b => b.Appointment)
                .ThenInclude(a => a.Patient)
            .Include(b => b.Patient)
            .Include(b => b.Staff)
            .Where(b => b.AppointmentId == appointmentId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<int>> GetBilledAppointmentIdsAsync()
    {
        return await _context.AppointmentBills
            .Select(b => b.AppointmentId)
            .ToListAsync();
    }

    public async Task<AppointmentBill?> GetByAppointmentIdFirstOrDefaultAsync(int appointmentId)
    {
        return await _context.AppointmentBills
            .Include(b => b.Appointment)
                .ThenInclude(a => a.Patient)
            .Include(b => b.Appointment)
                .ThenInclude(a => a.Doctor)
            .Include(b => b.Patient)
            .Include(b => b.Staff)
            .FirstOrDefaultAsync(b => b.AppointmentId == appointmentId);
    }

    public async Task AddAsync(AppointmentBill bill)
    {
        _context.AppointmentBills.Add(bill);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AppointmentBill bill)
    {
        _context.Entry(bill).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var bill = await _context.AppointmentBills.FindAsync(id);
        if (bill != null)
        {
            _context.AppointmentBills.Remove(bill);
            await _context.SaveChangesAsync();
        }
    }
}
