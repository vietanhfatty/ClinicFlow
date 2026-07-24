using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.Services;

namespace MyProject.WebMvc.Components;

/// <summary>
/// Renders the notification bell in the top header, showing unread count and
/// the most recent notifications for the current user. Data is fetched from
/// MyProject.WebApi via NotificationApiService (no direct DB access).
/// </summary>
public class NotificationBellViewComponent : ViewComponent
{
    private readonly NotificationApiService _notificationApiService;

    public NotificationBellViewComponent(NotificationApiService notificationApiService)
    {
        _notificationApiService = notificationApiService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (UserClaimsPrincipal?.Identity?.IsAuthenticated != true)
        {
            return Content(string.Empty);
        }

        var notifications = await _notificationApiService.GetMyNotificationsAsync();
        var unreadCount = await _notificationApiService.GetUnreadCountAsync();

        ViewBag.UnreadCount = unreadCount;
        return View("Default", notifications);
    }
}
