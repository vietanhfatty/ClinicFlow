using System.Collections.Generic;
using System.Threading.Tasks;
using MyProject.Domain.Entities;

namespace MyProject.Domain.IRepositories;

public interface IAppointmentLabTestRepository
{
    Task<IEnumerable<AppointmentLabTest>> GetAllAsync();
    Task<IEnumerable<AppointmentLabTest>> GetByAppointmentIdAsync(int appointmentId);
    Task<AppointmentLabTest?> GetByIdAsync(int id);
    Task AddAsync(AppointmentLabTest appointmentLabTest);
    Task UpdateAsync(AppointmentLabTest appointmentLabTest);
    Task DeleteAsync(int id);
}
