using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using MyProject.Application.DTOs;

namespace MyProject.Application.Services;

/// <summary>
/// Calls MyProject.WebApi's /api/account endpoints from WebMvc (login, change password).
/// Keeps WebMvc free of any direct database access, per project constraint.
/// </summary>
public class AuthApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clientName = "WebApiClient";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient GetClient() => _httpClientFactory.CreateClient(_clientName);

    /// <summary>
    /// Logs a user in against the WebApi and returns the JWT + role/id claims needed
    /// to build the WebMvc browser session cookie.
    /// </summary>
    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var client = GetClient();
        var response = await client.PostAsJsonAsync("account/login", request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            var message = "Invalid username or password.";

            if (!string.IsNullOrEmpty(body) && body.StartsWith("{"))
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("Message", out var msg))
                        message = msg.GetString() ?? message;
                    else if (doc.RootElement.TryGetProperty("message", out var msg2))
                        message = msg2.GetString() ?? message;
                }
                catch { }
            }

            return new LoginResponse(false, message, null, null, null, null);
        }

        var json = await response.Content.ReadAsStringAsync();

        try
        {
            var result = JsonSerializer.Deserialize<LoginResponse>(json, _jsonOptions);
            if (result != null) return result;
        }
        catch (JsonException)
        {
            return new LoginResponse(false, "Invalid response from authentication service.", null, null, null, null);
        }

        return new LoginResponse(false, "Unable to reach authentication service.", null, null, null, null);
    }

    /// <summary>
    /// Changes the current (authenticated) user's password via the WebApi.
    /// Throws with the API's error message on failure so callers can surface it.
    /// </summary>
    public async Task ChangePasswordAsync(ChangePasswordRequest request)
    {
        var client = GetClient();
        var response = await client.PostAsJsonAsync("account/change-password", request);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            var message = "Failed to change password.";
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("Message", out var msg))
                    message = msg.GetString() ?? message;
            }
            catch { }

            throw new System.ArgumentException(message);
        }
    }
}
