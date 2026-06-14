using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;

namespace MyProject.Infrastructure.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly HospitalManagementDbContext _context;

    public AppointmentRepository(HospitalManagementDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Appointment>> GetAllAsync()
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Staff)
            .ToListAsync();
    }

    public async Task<Appointment?> GetByIdAsync(int id)
    {
        return await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .Include(a => a.Staff)
            .FirstOrDefaultAsync(a => a.AppointmentId == id);
    }

    public async Task AddAsync(Appointment appointment)
    {
        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Appointment appointment)
    {
        _context.Entry(appointment).State = EntityState.Modified;
        if (appointment.Patient != null)
        {
            _context.Entry(appointment.Patient).State = EntityState.Unchanged;
        }
        if (appointment.Doctor != null)
        {
            _context.Entry(appointment.Doctor).State = EntityState.Unchanged;
        }
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment != null)
        {
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
        }
    }
}
