using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using MyProject.Application.DTOs;

namespace MyProject.Application.Services;

/// <summary>
/// Service for calling Patient Portal API endpoints from WebMvc
/// </summary>
public class PatientPortalApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clientName = "WebApiClient";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public PatientPortalApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient GetClient() => _httpClientFactory.CreateClient(_clientName);

    #region Appointments

    /// <summary>
    /// Gets all appointments for the current patient
    /// </summary>
    public async Task<List<AppointmentDto>> GetMyAppointmentsAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("patient-portal/appointments");
        
        if (!response.IsSuccessStatusCode)
            return new List<AppointmentDto>();

        var json = await response.Content.ReadAsStringAsync();
        var appointments = JsonSerializer.Deserialize<List<AppointmentDto>>(json, _jsonOptions) 
            ?? new List<AppointmentDto>();
        return appointments;
    }

    /// <summary>
    /// Gets upcoming appointments for the current patient
    /// </summary>
    public async Task<List<AppointmentDto>> GetUpcomingAppointmentsAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("patient-portal/appointments/upcoming");
        
        if (!response.IsSuccessStatusCode)
            return new List<AppointmentDto>();

        var json = await response.Content.ReadAsStringAsync();
        var appointments = JsonSerializer.Deserialize<List<AppointmentDto>>(json, _jsonOptions) 
            ?? new List<AppointmentDto>();
        return appointments;
    }

    /// <summary>
    /// Gets a specific appointment
    /// </summary>
    public async Task<AppointmentDto?> GetMyAppointmentAsync(int appointmentId)
    {
        var client = GetClient();
        var response = await client.GetAsync($"patient-portal/appointments/{appointmentId}");
        
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AppointmentDto>(json, _jsonOptions);
    }

    #endregion

    #region Medical Records

    /// <summary>
    /// Gets all medical records for the current patient
    /// </summary>
    public async Task<List<MedicalRecordDto>> GetMyMedicalRecordsAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("patient-portal/medical-records");
        
        if (!response.IsSuccessStatusCode)
            return new List<MedicalRecordDto>();

        var json = await response.Content.ReadAsStringAsync();
        var records = JsonSerializer.Deserialize<List<MedicalRecordDto>>(json, _jsonOptions) 
            ?? new List<MedicalRecordDto>();
        return records;
    }

    /// <summary>
    /// Gets a specific medical record
    /// </summary>
    public async Task<MedicalRecordDto?> GetMyMedicalRecordAsync(int medicalRecordId)
    {
        var client = GetClient();
        var response = await client.GetAsync($"patient-portal/medical-records/{medicalRecordId}");
        
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<MedicalRecordDto>(json, _jsonOptions);
    }

    #endregion

    #region Lab Tests

    /// <summary>
    /// Gets all lab tests for the current patient
    /// </summary>
    public async Task<List<AppointmentLabTestDto>> GetMyLabTestsAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("patient-portal/lab-tests");
        
        if (!response.IsSuccessStatusCode)
            return new List<AppointmentLabTestDto>();

        var json = await response.Content.ReadAsStringAsync();
        var labTests = JsonSerializer.Deserialize<List<AppointmentLabTestDto>>(json, _jsonOptions) 
            ?? new List<AppointmentLabTestDto>();
        return labTests;
    }

    /// <summary>
    /// Gets completed lab tests for the current patient
    /// </summary>
    public async Task<List<AppointmentLabTestDto>> GetMyCompletedLabTestsAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("patient-portal/lab-tests/completed");
        
        if (!response.IsSuccessStatusCode)
            return new List<AppointmentLabTestDto>();

        var json = await response.Content.ReadAsStringAsync();
        var labTests = JsonSerializer.Deserialize<List<AppointmentLabTestDto>>(json, _jsonOptions) 
            ?? new List<AppointmentLabTestDto>();
        return labTests;
    }

    /// <summary>
    /// Gets pending lab tests for the current patient
    /// </summary>
    public async Task<List<AppointmentLabTestDto>> GetMyPendingLabTestsAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("patient-portal/lab-tests/pending");
        
        if (!response.IsSuccessStatusCode)
            return new List<AppointmentLabTestDto>();

        var json = await response.Content.ReadAsStringAsync();
        var labTests = JsonSerializer.Deserialize<List<AppointmentLabTestDto>>(json, _jsonOptions) 
            ?? new List<AppointmentLabTestDto>();
        return labTests;
    }

    #endregion

    #region Payments

    /// <summary>
    /// Gets all payments for the current patient
    /// </summary>
    public async Task<List<PaymentDto>> GetMyPaymentsAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("patient-portal/payments");
        
        if (!response.IsSuccessStatusCode)
            return new List<PaymentDto>();

        var json = await response.Content.ReadAsStringAsync();
        var payments = JsonSerializer.Deserialize<List<PaymentDto>>(json, _jsonOptions) 
            ?? new List<PaymentDto>();
        return payments;
    }

    /// <summary>
    /// Gets pending payments for the current patient
    /// </summary>
    public async Task<List<PaymentDto>> GetMyPendingPaymentsAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("patient-portal/payments/pending");
        
        if (!response.IsSuccessStatusCode)
            return new List<PaymentDto>();

        var json = await response.Content.ReadAsStringAsync();
        var payments = JsonSerializer.Deserialize<List<PaymentDto>>(json, _jsonOptions) 
            ?? new List<PaymentDto>();
        return payments;
    }

    /// <summary>
    /// Gets completed payments for the current patient
    /// </summary>
    public async Task<List<PaymentDto>> GetMyCompletedPaymentsAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("patient-portal/payments/completed");
        
        if (!response.IsSuccessStatusCode)
            return new List<PaymentDto>();

        var json = await response.Content.ReadAsStringAsync();
        var payments = JsonSerializer.Deserialize<List<PaymentDto>>(json, _jsonOptions) 
            ?? new List<PaymentDto>();
        return payments;
    }

    /// <summary>
    /// Gets a specific payment
    /// </summary>
    public async Task<PaymentDto?> GetMyPaymentAsync(int paymentId)
    {
        var client = GetClient();
        var response = await client.GetAsync($"patient-portal/payments/{paymentId}");
        
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PaymentDto>(json, _jsonOptions);
    }

    /// <summary>
    /// Creates a new payment request
    /// </summary>
    public async Task<PaymentDto?> RequestPaymentAsync(CreatePaymentRequest request)
    {
        var client = GetClient();
        var response = await client.PostAsJsonAsync("patient-portal/payments", request);
        
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("Message", out var msg))
                    throw new ArgumentException(msg.GetString());
            }
            catch (ArgumentException) { throw; }
            catch { }
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PaymentDto>(json, _jsonOptions);
    }

    /// <summary>
    /// Gets a specific prescription's full detail for the current patient
    /// (standalone view/print page).
    /// </summary>
    public async Task<PrescriptionDetailDto?> GetMyPrescriptionAsync(int prescriptionId)
    {
        var client = GetClient();
        var response = await client.GetAsync($"patient-portal/prescriptions/{prescriptionId}");

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<PrescriptionDetailDto>(json, _jsonOptions);
    }

    #endregion

    #region Appointment Bills

    /// <summary>
    /// Gets all bills for the current patient
    /// </summary>
    public async Task<List<AppointmentBillDto>> GetMyBillsAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("patient-portal/my-bills");

        if (!response.IsSuccessStatusCode)
            return new List<AppointmentBillDto>();

        var json = await response.Content.ReadAsStringAsync();
        var bills = JsonSerializer.Deserialize<List<AppointmentBillDto>>(json, _jsonOptions)
            ?? new List<AppointmentBillDto>();
        return bills;
    }

    /// <summary>
    /// Gets pending bills for the current patient
    /// </summary>
    public async Task<List<AppointmentBillDto>> GetMyPendingBillsAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("patient-portal/my-bills/pending");

        if (!response.IsSuccessStatusCode)
            return new List<AppointmentBillDto>();

        var json = await response.Content.ReadAsStringAsync();
        var bills = JsonSerializer.Deserialize<List<AppointmentBillDto>>(json, _jsonOptions)
            ?? new List<AppointmentBillDto>();
        return bills;
    }

    /// <summary>
    /// Gets completed bills for the current patient
    /// </summary>
    public async Task<List<AppointmentBillDto>> GetMyCompletedBillsAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("patient-portal/my-bills/completed");

        if (!response.IsSuccessStatusCode)
            return new List<AppointmentBillDto>();

        var json = await response.Content.ReadAsStringAsync();
        var bills = JsonSerializer.Deserialize<List<AppointmentBillDto>>(json, _jsonOptions)
            ?? new List<AppointmentBillDto>();
        return bills;
    }

    /// <summary>
    /// Gets a specific bill
    /// </summary>
    public async Task<AppointmentBillDto?> GetMyBillAsync(int billId)
    {
        var client = GetClient();
        var response = await client.GetAsync($"patient-portal/my-bills/{billId}");

        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<AppointmentBillDto>(json, _jsonOptions);
    }

    #endregion

    #region Dashboard

    /// <summary>
    /// Gets dashboard summary for the current patient
    /// </summary>
    public async Task<dynamic?> GetDashboardAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("patient-portal/dashboard");
        
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement;
    }

    #endregion
}
