using System.Collections.Generic;
using System.Threading.Tasks;
using MyProject.Domain.Entities;

namespace MyProject.Domain.IRepositories;

public interface IMedicalRecordRepository
{
    Task<IEnumerable<MedicalRecord>> GetAllAsync();
    Task<MedicalRecord?> GetByIdAsync(int id);
    Task<MedicalRecord?> GetByAppointmentIdAsync(int appointmentId);
    Task<IEnumerable<MedicalRecord>> GetByPatientIdAsync(int patientId);
    Task<MedicalRecord?> GetByIdAndPatientIdAsync(int medicalRecordId, int patientId);
    Task AddAsync(MedicalRecord medicalRecord);
    Task UpdateAsync(MedicalRecord medicalRecord);
}
