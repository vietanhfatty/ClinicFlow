using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using MyProject.Application.DTOs;

namespace MyProject.Application.Services;

public class AppointmentApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clientName = "WebApiClient";

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AppointmentApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient GetClient() => _httpClientFactory.CreateClient(_clientName);

    public async Task<List<AppointmentDto>> GetAllAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("appointments");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AppointmentDto>>() ?? new List<AppointmentDto>();
    }

    public async Task<AppointmentDto?> GetByIdAsync(int id)
    {
        var client = GetClient();
        var response = await client.GetAsync($"appointments/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AppointmentDto>();
    }

    public async Task CreateAsync(CreateAppointmentRequest request)
    {
        var client = GetClient();
        var response = await client.PostAsJsonAsync("appointments", request);
        await ThrowIfErrorAsync(response);
    }

    public async Task UpdateAsync(int id, UpdateAppointmentRequest request)
    {
        var client = GetClient();
        var response = await client.PutAsJsonAsync($"appointments/{id}", request);
        await ThrowIfErrorAsync(response);
    }

    public async Task DeleteAsync(int id)
    {
        var client = GetClient();
        var response = await client.DeleteAsync($"appointments/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task CheckInAsync(int id)
    {
        var client = GetClient();
        var response = await client.PostAsync($"appointments/{id}/checkin", null);
        await ThrowIfErrorAsync(response);
    }

    public async Task ConfirmAsync(int id)
    {
        var client = GetClient();
        var response = await client.PostAsync($"appointments/{id}/confirm", null);
        await ThrowIfErrorAsync(response);
    }

    public async Task StartExaminationAsync(int id)
    {
        var client = GetClient();
        var response = await client.PostAsync($"appointments/{id}/start", null);
        await ThrowIfErrorAsync(response);
    }

    public async Task CompleteAsync(int id)
    {
        var client = GetClient();
        var response = await client.PostAsync($"appointments/{id}/complete", null);
        await ThrowIfErrorAsync(response);
    }

    public async Task<List<AppointmentDto>> GetByPatientAsync(int patientId)
    {
        var client = GetClient();
        var response = await client.GetAsync($"appointments/by-patient/{patientId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AppointmentDto>>() ?? new List<AppointmentDto>();
    }

    public async Task<List<string>> GetBusySlotsAsync(int doctorId, string date)
    {
        var client = GetClient();
        var response = await client.GetAsync($"appointments/busy-slots?doctorId={doctorId}&date={date}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<string>>() ?? new List<string>();
    }

    private static async Task ThrowIfErrorAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync();
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("Message", out var msg))
                throw new ArgumentException(msg.GetString());
        }
        catch (ArgumentException) { throw; }
        catch { }
        response.EnsureSuccessStatusCode();
    }
}
