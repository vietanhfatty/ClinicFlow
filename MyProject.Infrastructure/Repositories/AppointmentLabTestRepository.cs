using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;

namespace MyProject.Infrastructure.Repositories;

public class AppointmentLabTestRepository : IAppointmentLabTestRepository
{
    private readonly HospitalManagementDbContext _context;

    public AppointmentLabTestRepository(HospitalManagementDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<AppointmentLabTest>> GetAllAsync()
    {
        return await _context.AppointmentLabTests
            .Include(a => a.Appointment).ThenInclude(ap => ap.Patient)
            .Include(a => a.Appointment).ThenInclude(ap => ap.Doctor)
            .Include(a => a.LabTestService)
            .Include(a => a.Doctor)
            .ToListAsync();
    }

    public async Task<IEnumerable<AppointmentLabTest>> GetByAppointmentIdAsync(int appointmentId)
    {
        return await _context.AppointmentLabTests
            .Include(a => a.Appointment).ThenInclude(ap => ap.Patient)
            .Include(a => a.Appointment).ThenInclude(ap => ap.Doctor)
            .Include(a => a.LabTestService)
            .Include(a => a.Doctor)
            .Where(a => a.AppointmentId == appointmentId)
            .ToListAsync();
    }

    public async Task<AppointmentLabTest?> GetByIdAsync(int id)
    {
        return await _context.AppointmentLabTests
            .Include(a => a.Appointment)
            .Include(a => a.LabTestService)
            .Include(a => a.Doctor)
            .FirstOrDefaultAsync(a => a.AppointmentLabTestId == id);
    }

    public async Task AddAsync(AppointmentLabTest appointmentLabTest)
    {
        _context.AppointmentLabTests.Add(appointmentLabTest);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AppointmentLabTest appointmentLabTest)
    {
        _context.Entry(appointmentLabTest).State = EntityState.Modified;
        if (appointmentLabTest.Appointment != null)
        {
            _context.Entry(appointmentLabTest.Appointment).State = EntityState.Unchanged;
        }
        if (appointmentLabTest.LabTestService != null)
        {
            _context.Entry(appointmentLabTest.LabTestService).State = EntityState.Unchanged;
        }
        if (appointmentLabTest.Doctor != null)
        {
            _context.Entry(appointmentLabTest.Doctor).State = EntityState.Unchanged;
        }
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var item = await _context.AppointmentLabTests.FindAsync(id);
        if (item != null)
        {
            _context.AppointmentLabTests.Remove(item);
            await _context.SaveChangesAsync();
        }
    }
}
