using System.Collections.Generic;
using System.Threading.Tasks;
using MyProject.Domain.Entities;

namespace MyProject.Domain.IRepositories;

public interface IPrescriptionRepository
{
    Task<IEnumerable<Prescription>> GetByMedicalRecordIdAsync(int medicalRecordId);
    Task<Prescription?> GetByIdAsync(int id);
    Task AddAsync(Prescription prescription);
    Task DeleteAsync(int id);
}
