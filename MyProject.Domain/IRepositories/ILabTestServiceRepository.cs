using System.Collections.Generic;
using System.Threading.Tasks;
using MyProject.Domain.Entities;

namespace MyProject.Domain.IRepositories;

public interface ILabTestServiceRepository
{
    Task<IEnumerable<LabTestService>> GetAllAsync();
    Task<IEnumerable<LabTestService>> GetActiveServicesAsync();
    Task<LabTestService?> GetByIdAsync(int id);
    Task AddAsync(LabTestService labTestService);
    Task UpdateAsync(LabTestService labTestService);
    Task DeleteAsync(int id);
}
