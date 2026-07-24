using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;

namespace MyProject.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly HospitalManagementDbContext _context;

    public PaymentRepository(HospitalManagementDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Payment>> GetAllAsync()
    {
        return await _context.Payments
            .Include(p => p.Patient)
            .Include(p => p.Appointment)
            .ToListAsync();
    }

    public async Task<Payment?> GetByIdAsync(int id)
    {
        return await _context.Payments
            .Include(p => p.Patient)
            .Include(p => p.Appointment)
            .FirstOrDefaultAsync(p => p.PaymentId == id);
    }

    public async Task<IEnumerable<Payment>> GetByPatientIdAsync(int patientId)
    {
        return await _context.Payments
            .Include(p => p.Patient)
            .Include(p => p.Appointment)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.RequestDate)
            .ToListAsync();
    }

    public async Task AddAsync(Payment payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Payment payment)
    {
        _context.Entry(payment).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment != null)
        {
            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
        }
    }

    public IQueryable<Payment> GetQueryable()
    {
        return _context.Payments.AsQueryable();
    }
}
