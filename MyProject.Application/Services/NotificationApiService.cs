using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using MyProject.Application.DTOs;

namespace MyProject.Application.Services;

/// <summary>
/// Calls MyProject.WebApi's /api/notifications endpoints from WebMvc.
/// </summary>
public class NotificationApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clientName = "WebApiClient";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public NotificationApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient GetClient() => _httpClientFactory.CreateClient(_clientName);

    public async Task<List<NotificationDto>> GetMyNotificationsAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("notifications");

        if (!response.IsSuccessStatusCode)
            return new List<NotificationDto>();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<NotificationDto>>(json, _jsonOptions)
            ?? new List<NotificationDto>();
    }

    public async Task<int> GetUnreadCountAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("notifications/unread-count");

        if (!response.IsSuccessStatusCode)
            return 0;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.TryGetProperty("count", out var c) ? c.GetInt32() : 0;
    }

    public async Task MarkAsReadAsync(int notificationId)
    {
        var client = GetClient();
        await client.PostAsync($"notifications/{notificationId}/read", null);
    }

    public async Task MarkAllAsReadAsync()
    {
        var client = GetClient();
        await client.PostAsync("notifications/read-all", null);
    }
}
