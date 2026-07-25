using System.Collections.Generic;
using System.Threading.Tasks;
using MyProject.Domain.Entities;

namespace MyProject.Domain.IRepositories;

public interface IAppointmentBillRepository
{
    Task<IEnumerable<AppointmentBill>> GetAllAsync();
    Task<AppointmentBill?> GetByIdAsync(int id);
    Task<IEnumerable<AppointmentBill>> GetByPatientIdAsync(int patientId);
    Task<IEnumerable<AppointmentBill>> GetByAppointmentIdAsync(int appointmentId);
    Task<AppointmentBill?> GetByAppointmentIdFirstOrDefaultAsync(int appointmentId);
    Task<IEnumerable<int>> GetBilledAppointmentIdsAsync();
    Task AddAsync(AppointmentBill bill);
    Task UpdateAsync(AppointmentBill bill);
    Task DeleteAsync(int id);
}
