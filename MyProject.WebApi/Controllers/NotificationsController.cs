using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.Services;

namespace MyProject.WebApi.Controllers;

/// <summary>
/// Exposes in-app notifications for the currently authenticated user (any role).
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationService _notificationService;

    public NotificationsController(NotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in claims");
        }
        return userId;
    }

    /// <summary>
    /// Gets all notifications for the current user, most recent first.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications()
    {
        try
        {
            var userId = GetUserId();
            var notifications = await _notificationService.GetByUserIdAsync(userId);
            return Ok(notifications);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Gets the count of unread notifications for the current user.
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            var userId = GetUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { Count = count });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Marks a single notification as read. Ignored silently if it does not
    /// belong to the current user.
    /// </summary>
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            var userId = GetUserId();
            await _notificationService.MarkAsReadAsync(userId, id);
            return Ok(new { Message = "Notification marked as read." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Marks all notifications for the current user as read.
    /// </summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            var userId = GetUserId();
            await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { Message = "All notifications marked as read." });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { Message = ex.Message });
        }
    }
}
