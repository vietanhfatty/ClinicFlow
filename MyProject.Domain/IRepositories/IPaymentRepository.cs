using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyProject.Domain.Entities;

namespace MyProject.Domain.IRepositories;

public interface IPaymentRepository
{
    Task<IEnumerable<Payment>> GetAllAsync();
    Task<Payment?> GetByIdAsync(int id);
    Task<IEnumerable<Payment>> GetByPatientIdAsync(int patientId);
    Task AddAsync(Payment payment);
    Task UpdateAsync(Payment payment);
    Task DeleteAsync(int id);
    IQueryable<Payment> GetQueryable();
}
