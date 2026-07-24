using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;

namespace MyProject.Infrastructure.Repositories;

public class LabTestServiceRepository : ILabTestServiceRepository
{
    private readonly HospitalManagementDbContext _context;

    public LabTestServiceRepository(HospitalManagementDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<LabTestService>> GetAllAsync()
    {
        return await _context.LabTestServices.ToListAsync();
    }

    public async Task<IEnumerable<LabTestService>> GetActiveServicesAsync()
    {
        return await _context.LabTestServices
            .Where(s => s.IsActive)
            .ToListAsync();
    }

    public async Task<LabTestService?> GetByIdAsync(int id)
    {
        return await _context.LabTestServices.FindAsync(id);
    }

    public async Task AddAsync(LabTestService labTestService)
    {
        _context.LabTestServices.Add(labTestService);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(LabTestService labTestService)
    {
        _context.Entry(labTestService).State = EntityState.Modified;
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var service = await _context.LabTestServices.FindAsync(id);
        if (service != null)
        {
            _context.LabTestServices.Remove(service);
            await _context.SaveChangesAsync();
        }
    }
}
