using System.Collections.Generic;
using System.Threading.Tasks;
using MyProject.Domain.Entities;

namespace MyProject.Domain.IRepositories;

public interface INotificationRepository
{
    Task<IEnumerable<Notification>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Notification>> GetUnreadByUserIdAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<Notification?> GetByIdAsync(int id);
    Task AddAsync(Notification notification);
    Task MarkAsReadAsync(int notificationId);
    Task MarkAllAsReadAsync(int userId);
}
