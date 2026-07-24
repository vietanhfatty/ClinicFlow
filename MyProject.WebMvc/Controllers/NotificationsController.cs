using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.Services;

namespace MyProject.WebMvc.Controllers;

/// <summary>
/// Lightweight endpoints backing the notification bell dropdown (available to any
/// authenticated role). Delegates to MyProject.WebApi via NotificationApiService;
/// WebMvc never touches the database directly.
/// </summary>
[Authorize]
public class NotificationsController : Controller
{
    private readonly NotificationApiService _notificationApiService;

    public NotificationsController(NotificationApiService notificationApiService)
    {
        _notificationApiService = notificationApiService;
    }

    public async Task<IActionResult> Index()
    {
        var notifications = await _notificationApiService.GetMyNotificationsAsync();
        return View(notifications);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        await _notificationApiService.MarkAsReadAsync(id);
        return Ok();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _notificationApiService.MarkAllAsReadAsync();
        return Ok();
    }
}
