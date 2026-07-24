using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MyProject.Application.DTOs;

namespace MyProject.Application.Services;

/// <summary>
/// HTTP API client service for lab test operations
/// </summary>
public class LabTestApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clientName = "WebApiClient";

    /// <summary>
    /// Initializes a new instance of the LabTestApiService class
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory for creating clients</param>
    public LabTestApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <summary>
    /// Gets the configured HTTP client
    /// </summary>
    /// <returns>An instance of HttpClient configured for the API</returns>
    private HttpClient GetClient() => _httpClientFactory.CreateClient(_clientName);

    /// <summary>
    /// Gets all lab test services from the catalog
    /// </summary>
    /// <returns>Collection of all lab test service DTOs</returns>
    public async Task<List<LabTestServiceDto>> GetAllLabTestServicesAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("labtests/services");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<LabTestServiceDto>>() ?? new List<LabTestServiceDto>();
    }

    /// <summary>
    /// Gets a specific lab test service by ID
    /// </summary>
    /// <param name="id">The lab test service ID</param>
    /// <returns>Lab test service DTO if found; null otherwise</returns>
    public async Task<LabTestServiceDto?> GetLabTestServiceByIdAsync(int id)
    {
        var client = GetClient();
        var response = await client.GetAsync($"labtests/services/{id}");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<LabTestServiceDto>();
    }

    /// <summary>
    /// Gets only active lab test services
    /// </summary>
    /// <returns>Collection of active lab test service DTOs</returns>
    public async Task<List<LabTestServiceDto>> GetActiveLabTestServicesAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("labtests/services/active");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<LabTestServiceDto>>() ?? new List<LabTestServiceDto>();
    }

    /// <summary>
    /// Gets all lab tests requested for a specific appointment
    /// </summary>
    /// <param name="appointmentId">The appointment ID</param>
    /// <returns>Collection of appointment lab test DTOs</returns>
    public async Task<List<AppointmentLabTestDto>> GetLabTestsByAppointmentAsync(int appointmentId)
    {
        var client = GetClient();
        var response = await client.GetAsync($"labtests/by-appointment/{appointmentId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AppointmentLabTestDto>>() ?? new List<AppointmentLabTestDto>();
    }

    /// <summary>
    /// Gets all lab tests for a specific patient across all appointments
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <returns>Collection of appointment lab test DTOs</returns>
    public async Task<List<AppointmentLabTestDto>> GetLabTestsByPatientAsync(int patientId)
    {
        var client = GetClient();
        var response = await client.GetAsync($"labtests/by-patient/{patientId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AppointmentLabTestDto>>() ?? new List<AppointmentLabTestDto>();
    }

    /// <summary>
    /// Gets all lab tests requested by a specific doctor
    /// </summary>
    /// <param name="doctorId">The doctor ID</param>
    /// <returns>Collection of appointment lab test DTOs</returns>
    public async Task<List<AppointmentLabTestDto>> GetLabTestsByDoctorAsync(int doctorId)
    {
        var client = GetClient();
        var response = await client.GetAsync($"labtests/by-doctor/{doctorId}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AppointmentLabTestDto>>() ?? new List<AppointmentLabTestDto>();
    }

    /// <summary>
    /// Gets all lab tests with Pending status
    /// </summary>
    /// <returns>Collection of pending appointment lab test DTOs</returns>
    public async Task<List<AppointmentLabTestDto>> GetPendingLabTestsAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("labtests/pending");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AppointmentLabTestDto>>() ?? new List<AppointmentLabTestDto>();
    }

    /// <summary>
    /// Gets all lab tests with Completed status
    /// </summary>
    /// <returns>Collection of completed appointment lab test DTOs</returns>
    public async Task<List<AppointmentLabTestDto>> GetCompletedLabTestsAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("labtests/completed");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<AppointmentLabTestDto>>() ?? new List<AppointmentLabTestDto>();
    }

    /// <summary>
    /// Creates a new lab test request for an appointment
    /// </summary>
    /// <param name="request">The request containing appointment and lab test details</param>
    /// <returns>The created appointment lab test DTO</returns>
    /// <exception cref="InvalidOperationException">Thrown when POST operation fails</exception>
    public async Task<AppointmentLabTestDto> CreateLabTestAsync(CreateLabTestRequest request)
    {
        var client = GetClient();
        var response = await client.PostAsJsonAsync("labtests/create", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AppointmentLabTestDto>() ?? throw new InvalidOperationException("Failed to deserialize created lab test.");
    }

    /// <summary>
    /// Updates lab test results with validation
    /// </summary>
    /// <param name="labTestId">The appointment lab test ID</param>
    /// <param name="request">The request containing updated test results and status</param>
    /// <returns>The updated appointment lab test DTO</returns>
    /// <exception cref="InvalidOperationException">Thrown when PUT operation fails</exception>
    public async Task<AppointmentLabTestDto> UpdateLabTestResultAsync(int labTestId, UpdateLabTestResultRequest request)
    {
        var client = GetClient();
        var response = await client.PutAsJsonAsync($"labtests/{labTestId}/result", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AppointmentLabTestDto>() ?? throw new InvalidOperationException("Failed to deserialize updated lab test.");
    }

    /// <summary>
    /// Deletes a lab test record
    /// </summary>
    /// <param name="labTestId">The appointment lab test ID to delete</param>
    public async Task DeleteLabTestAsync(int labTestId)
    {
        var client = GetClient();
        var response = await client.DeleteAsync($"labtests/{labTestId}");
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Gets dashboard statistics for lab tests
    /// </summary>
    /// <returns>Statistics DTO with total, completed, pending counts and breakdowns</returns>
    public async Task<LabTestStatisticsDto?> GetLabTestStatisticsAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("labtests/statistics");
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<LabTestStatisticsDto>();
    }

    public async Task<List<AppointmentLabTestDto>> GetByAppointmentIdAsync(int appointmentId)
        => await GetLabTestsByAppointmentAsync(appointmentId);

    public async Task<List<LabTestServiceDto>> GetActiveServicesAsync()
        => await GetActiveLabTestServicesAsync();

    /// <summary>
    /// Updates the price of a lab test service (Admin only)
    /// </summary>
    /// <param name="id">The lab test service ID</param>
    /// <param name="request">The request containing the new price</param>
    /// <returns>The updated lab test service DTO</returns>
    /// <exception cref="InvalidOperationException">Thrown when PUT operation fails</exception>
    public async Task<LabTestServiceDto> UpdateServicePriceAsync(int id, UpdateLabTestServicePriceRequest request)
    {
        var client = GetClient();
        var response = await client.PutAsJsonAsync($"labtests/services/{id}/price", request);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<LabTestServiceDto>() ?? throw new InvalidOperationException("Failed to deserialize updated lab test service.");
    }
}
