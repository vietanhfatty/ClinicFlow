using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MyProject.Application.DTOs;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;

namespace MyProject.Application.Services;

/// <summary>
/// Service for managing in-app notifications and dispatching notification events
/// from other services (appointments, lab tests, medical records, etc).
/// </summary>
public class NotificationService
{
    private readonly INotificationRepository _repo;

    public NotificationService(INotificationRepository repo)
    {
        _repo = repo;
    }

    public async Task<IEnumerable<NotificationDto>> GetByUserIdAsync(int userId)
    {
        var list = await _repo.GetByUserIdAsync(userId);
        return list.Select(MapToDto);
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _repo.GetUnreadCountAsync(userId);
    }

    public async Task MarkAsReadAsync(int userId, int notificationId)
    {
        var notification = await _repo.GetByIdAsync(notificationId);
        if (notification == null || notification.UserId != userId)
        {
            // Ignore silently: either not found or does not belong to this user.
            return;
        }

        await _repo.MarkAsReadAsync(notificationId);
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        await _repo.MarkAllAsReadAsync(userId);
    }

    /// <summary>
    /// Creates a new notification for a user. Intended to be called from other
    /// application services when a relevant event occurs (appointment confirmed,
    /// lab test completed, new medical record, etc).
    /// </summary>
    public async Task NotifyAsync(int userId, string title, string message, string type, int? relatedEntityId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            RelatedEntityId = relatedEntityId,
            IsRead = false
        };

        await _repo.AddAsync(notification);
    }

    private NotificationDto MapToDto(Notification n)
    {
        return new NotificationDto(
            n.NotificationId,
            n.UserId,
            n.Title,
            n.Message,
            n.Type,
            n.RelatedEntityId,
            n.IsRead,
            n.CreatedAt
        );
    }
}
