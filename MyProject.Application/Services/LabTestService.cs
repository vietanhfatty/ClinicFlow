using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MyProject.Application.DTOs;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;

namespace MyProject.Application.Services;

/// <summary>
/// Service for managing lab tests and lab test results for appointments
/// </summary>
public class LabTestService
{
    private readonly IAppointmentLabTestRepository _appointmentLabTestRepo;
    private readonly ILabTestServiceRepository _labTestServiceRepo;
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IPatientRepository _patientRepo;
    private readonly IDoctorRepository _doctorRepo;
    private readonly IUserRepository _userRepo;
    private readonly NotificationService _notificationService;

    /// <summary>
    /// Initializes a new instance of the LabTestService class with required repositories
    /// </summary>
    public LabTestService(
        IAppointmentLabTestRepository appointmentLabTestRepo,
        ILabTestServiceRepository labTestServiceRepo,
        IAppointmentRepository appointmentRepo,
        IPatientRepository patientRepo,
        IDoctorRepository doctorRepo,
        IUserRepository userRepo,
        NotificationService notificationService)
    {
        _appointmentLabTestRepo = appointmentLabTestRepo;
        _labTestServiceRepo = labTestServiceRepo;
        _appointmentRepo = appointmentRepo;
        _patientRepo = patientRepo;
        _doctorRepo = doctorRepo;
        _userRepo = userRepo;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Resolves the UserId that corresponds to a patient's login account.
    /// The legacy schema has no PatientId-&gt;UserId FK, so accounts are linked
    /// by convention: User.Username == Patient.Phone.
    /// </summary>
    private async Task<int?> ResolvePatientUserIdAsync(int patientId)
    {
        var patient = await _patientRepo.GetByIdAsync(patientId);
        if (patient == null || string.IsNullOrWhiteSpace(patient.Phone)) return null;
        var user = await _userRepo.GetByUsernameAsync(patient.Phone);
        return user?.UserId;
    }

    /// <summary>
    /// Gets all lab test services from the catalog
    /// </summary>
    /// <returns>Collection of all lab test service DTOs</returns>
    public async Task<IEnumerable<LabTestServiceDto>> GetAllLabTestServicesAsync()
    {
        var services = await _labTestServiceRepo.GetAllAsync();
        return services.Select(MapLabTestServiceToDto);
    }

    /// <summary>
    /// Gets only active lab test services
    /// </summary>
    /// <returns>Collection of active lab test service DTOs</returns>
    public async Task<IEnumerable<LabTestServiceDto>> GetActiveLabTestServicesAsync()
    {
        var services = await _labTestServiceRepo.GetActiveServicesAsync();
        return services.Select(MapLabTestServiceToDto);
    }

    /// <summary>
    /// Gets a single lab test service by ID
    /// </summary>
    /// <param name="id">The lab test service ID</param>
    /// <returns>Lab test service DTO if found; null otherwise</returns>
    public async Task<LabTestServiceDto?> GetLabTestServiceByIdAsync(int id)
    {
        var service = await _labTestServiceRepo.GetByIdAsync(id);
        return service == null ? null : MapLabTestServiceToDto(service);
    }

    /// <summary>
    /// Updates the price of a lab test service in the catalog. Admin-only operation;
    /// no other catalog fields (name, description, category, active state) can be
    /// modified through this API by design.
    /// </summary>
    /// <param name="id">The lab test service ID</param>
    /// <param name="newPrice">The new price to set</param>
    /// <returns>The updated lab test service DTO</returns>
    /// <exception cref="KeyNotFoundException">Thrown when the lab test service is not found</exception>
    public async Task<LabTestServiceDto> UpdateLabTestServicePriceAsync(int id, decimal newPrice)
    {
        var service = await _labTestServiceRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Lab test service with ID {id} not found");

        service.Price = newPrice;
        await _labTestServiceRepo.UpdateAsync(service);

        return MapLabTestServiceToDto(service);
    }

    /// <summary>
    /// Gets all lab tests requested for a specific appointment
    /// </summary>
    /// <param name="appointmentId">The appointment ID</param>
    /// <returns>Collection of appointment lab test DTOs</returns>
    public async Task<IEnumerable<AppointmentLabTestDto>> GetLabTestsByAppointmentAsync(int appointmentId)
    {
        var tests = await _appointmentLabTestRepo.GetByAppointmentIdAsync(appointmentId);
        return tests.Select(MapToDto);
    }

    /// <summary>
    /// Gets all lab tests for a specific patient across all appointments
    /// </summary>
    /// <param name="patientId">The patient ID</param>
    /// <returns>Collection of appointment lab test DTOs</returns>
    public async Task<IEnumerable<AppointmentLabTestDto>> GetLabTestsByPatientAsync(int patientId)
    {
        var allTests = await _appointmentLabTestRepo.GetAllAsync();
        var patientTests = allTests
            .Where(alt => alt.Appointment?.PatientId == patientId)
            .ToList();
        return patientTests.Select(MapToDto);
    }

    /// <summary>
    /// Gets all lab tests requested by a specific doctor
    /// </summary>
    /// <param name="doctorId">The doctor ID</param>
    /// <returns>Collection of appointment lab test DTOs</returns>
    public async Task<IEnumerable<AppointmentLabTestDto>> GetLabTestsByDoctorAsync(int doctorId)
    {
        var allTests = await _appointmentLabTestRepo.GetAllAsync();
        var doctorTests = allTests
            .Where(alt => alt.Appointment?.DoctorId == doctorId)
            .ToList();
        return doctorTests.Select(MapToDto);
    }

    /// <summary>
    /// Gets all lab tests with Pending status
    /// </summary>
    /// <returns>Collection of pending appointment lab test DTOs</returns>
    public async Task<IEnumerable<AppointmentLabTestDto>> GetPendingLabTestsAsync()
    {
        var allTests = await _appointmentLabTestRepo.GetAllAsync();
        var pendingTests = allTests
            .Where(alt => alt.Status == "Pending")
            .ToList();
        return pendingTests.Select(MapToDto);
    }

    /// <summary>
    /// Gets all lab tests with Completed status
    /// </summary>
    /// <returns>Collection of completed appointment lab test DTOs</returns>
    public async Task<IEnumerable<AppointmentLabTestDto>> GetCompletedLabTestsAsync()
    {
        var allTests = await _appointmentLabTestRepo.GetAllAsync();
        var completedTests = allTests
            .Where(alt => alt.Status == "Completed")
            .ToList();
        return completedTests.Select(MapToDto);
    }

    /// <summary>
    /// Creates a new lab test request for an appointment with validation
    /// </summary>
    /// <param name="req">The request containing appointment and lab test details</param>
    /// <returns>The created appointment lab test DTO</returns>
    /// <exception cref="KeyNotFoundException">Thrown when appointment or lab test service is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when lab test service is not active</exception>
    public async Task<AppointmentLabTestDto> CreateAppointmentLabTestAsync(CreateLabTestRequest req)
    {
        // Validate appointment exists
        var appointment = await _appointmentRepo.GetByIdAsync(req.AppointmentId)
            ?? throw new KeyNotFoundException($"Appointment with ID {req.AppointmentId} not found");

        // Validate lab test service exists
        var labTestService = await _labTestServiceRepo.GetByIdAsync(req.LabTestServiceId)
            ?? throw new KeyNotFoundException($"Lab test service with ID {req.LabTestServiceId} not found");

        // Validate lab test service is active
        if (!labTestService.IsActive)
        {
            throw new InvalidOperationException($"Lab test service '{labTestService.ServiceName}' is not active and cannot be used");
        }

        var appointmentLabTest = new AppointmentLabTest
        {
            AppointmentId = req.AppointmentId,
            LabTestServiceId = req.LabTestServiceId,
            DoctorId = req.DoctorId,
            Status = "Pending",
            Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _appointmentLabTestRepo.AddAsync(appointmentLabTest);

        // Fetch the created record to ensure navigation properties are loaded
        var createdTest = await _appointmentLabTestRepo.GetByIdAsync(appointmentLabTest.AppointmentLabTestId);
        return MapToDto(createdTest!);
    }

    /// <summary>
    /// Updates lab test results with validation
    /// </summary>
    /// <param name="labTestId">The appointment lab test ID</param>
    /// <param name="req">The request containing updated test results and status</param>
    /// <returns>The updated appointment lab test DTO</returns>
    /// <exception cref="KeyNotFoundException">Thrown when lab test is not found</exception>
    /// <exception cref="InvalidOperationException">Thrown when lab test status is not Pending</exception>
    public async Task<AppointmentLabTestDto> UpdateLabTestResultAsync(int labTestId, UpdateLabTestResultRequest req)
    {
        // Verify lab test exists
        var labTest = await _appointmentLabTestRepo.GetByIdAsync(labTestId)
            ?? throw new KeyNotFoundException($"Lab test with ID {labTestId} not found");

        // Verify lab test is in Pending status before updating results
        if (labTest.Status != "Pending")
        {
            throw new InvalidOperationException($"Cannot update results for lab test with status '{labTest.Status}'. Only lab tests with 'Pending' status can be updated");
        }

        var serviceName = labTest.LabTestService?.ServiceName ?? string.Empty;
        var indicatorDefs = LabTestIndicatorCatalog.GetIndicators(serviceName);

        // Services with structured indicators require at least one filled-in value;
        // services without them (e.g. imaging) fall back to the free-text result.
        if (indicatorDefs.Count > 0)
        {
            var values = req.IndicatorValues?
                .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
                .ToDictionary(kv => kv.Key, kv => kv.Value.Trim())
                ?? new Dictionary<string, string>();

            if (values.Count == 0)
            {
                throw new InvalidOperationException("At least one indicator value is required for this lab test.");
            }

            labTest.ResultValues = JsonSerializer.Serialize(values);
        }
        else if (string.IsNullOrWhiteSpace(req.Result))
        {
            throw new InvalidOperationException("Result is required for this lab test.");
        }

        // Update lab test with new values
        labTest.TestDate = req.TestDate;
        labTest.Result = string.IsNullOrWhiteSpace(req.Result) ? null : req.Result.Trim();
        labTest.Status = string.IsNullOrWhiteSpace(req.Status) ? "Completed" : req.Status.Trim();
        labTest.Notes = string.IsNullOrWhiteSpace(req.Notes) ? null : req.Notes.Trim();

        await _appointmentLabTestRepo.UpdateAsync(labTest);

        // Fetch the updated record to ensure navigation properties are loaded
        var updatedTest = await _appointmentLabTestRepo.GetByIdAsync(labTestId);

        if (labTest.Status == "Completed" && updatedTest?.Appointment != null)
        {
            var patientUserId = await ResolvePatientUserIdAsync(updatedTest.Appointment.PatientId);
            if (patientUserId.HasValue)
            {
                var notifyServiceName = updatedTest.LabTestService?.ServiceName ?? "xét nghiệm";
                await _notificationService.NotifyAsync(
                    patientUserId.Value,
                    "Kết quả xét nghiệm đã có",
                    $"Kết quả \"{notifyServiceName}\" của bạn đã có. Vui lòng kiểm tra trong hồ sơ bệnh án.",
                    "LabTest",
                    updatedTest.AppointmentLabTestId);
            }
        }

        return MapToDto(updatedTest!);
    }

    /// <summary>
    /// Gets dashboard statistics for lab tests
    /// </summary>
    /// <returns>Statistics DTO with total, completed, pending counts and breakdowns</returns>
    public async Task<LabTestStatisticsDto> GetLabTestStatisticsAsync()
    {
        var allTests = await _appointmentLabTestRepo.GetAllAsync();
        var allServices = await _labTestServiceRepo.GetAllAsync();

        var testsList = allTests.ToList();
        var servicesList = allServices.ToList();

        int totalCount = testsList.Count;
        int completedCount = testsList.Count(t => t.Status == "Completed");
        int pendingCount = testsList.Count(t => t.Status == "Pending");

        // Top lab tests by frequency
        var topLabTests = testsList
            .GroupBy(t => t.LabTestServiceId)
            .Select(g =>
            {
                var service = servicesList.FirstOrDefault(s => s.LabTestServiceId == g.Key);
                return new TopLabTestDto(
                    service?.ServiceName ?? "Unknown Service",
                    g.Count(),
                    service?.Price * g.Count() ?? 0m
                );
            })
            .OrderByDescending(t => t.RequestCount)
            .Take(10)
            .ToList();

        // Lab tests by status
        var statusCounts = new List<StatusCountDto>();
        var statuses = new[] { "Pending", "Completed", "Cancelled" };

        foreach (var status in statuses)
        {
            var statusCount = testsList.Count(t => t.Status == status);
            var percentage = totalCount > 0 ? (double)statusCount / totalCount * 100 : 0;

            statusCounts.Add(new StatusCountDto(
                status,
                statusCount,
                Math.Round(percentage, 2)
            ));
        }

        return new LabTestStatisticsDto(
            totalCount,
            completedCount,
            pendingCount,
            topLabTests,
            statusCounts
        );
    }

    /// <summary>
    /// Deletes a lab test record
    /// </summary>
    /// <param name="labTestId">The appointment lab test ID to delete</param>
    /// <exception cref="KeyNotFoundException">Thrown when lab test is not found</exception>
    public async Task DeleteLabTestAsync(int labTestId)
    {
        var labTest = await _appointmentLabTestRepo.GetByIdAsync(labTestId)
            ?? throw new KeyNotFoundException($"Lab test with ID {labTestId} not found");

        await _appointmentLabTestRepo.DeleteAsync(labTestId);
    }

    /// <summary>
    /// Builds the structured indicator list for a lab test by combining the
    /// catalog definitions for the service with any saved values (ResultValues JSON).
    /// </summary>
    private static List<LabTestIndicatorValueDto> BuildIndicators(string serviceName, string? resultValuesJson)
    {
        var defs = LabTestIndicatorCatalog.GetIndicators(serviceName);
        if (defs.Count == 0) return new List<LabTestIndicatorValueDto>();

        Dictionary<string, string>? savedValues = null;
        if (!string.IsNullOrWhiteSpace(resultValuesJson))
        {
            try
            {
                savedValues = JsonSerializer.Deserialize<Dictionary<string, string>>(resultValuesJson);
            }
            catch (JsonException)
            {
                savedValues = null;
            }
        }

        return defs.Select(d => new LabTestIndicatorValueDto(
            d.Key,
            d.Label,
            d.Unit,
            d.NormalRange,
            savedValues != null && savedValues.TryGetValue(d.Key, out var v) ? v : null
        )).ToList();
    }

    /// <summary>
    /// Maps an AppointmentLabTest entity to AppointmentLabTestDto
    /// </summary>
    private AppointmentLabTestDto MapToDto(AppointmentLabTest alt)
    {
        var patientName = alt.Appointment?.Patient?.FullName ?? "Unknown Patient";
        var doctorName = alt.Appointment?.Doctor?.FullName ?? "Unknown Doctor";
        var serviceName = alt.LabTestService?.ServiceName ?? "Unknown Service";
        var price = alt.LabTestService?.Price ?? 0m;

        return new AppointmentLabTestDto(
            alt.AppointmentLabTestId,
            alt.AppointmentId,
            patientName,
            doctorName,
            alt.LabTestServiceId,
            serviceName,
            price,
            alt.TestDate,
            alt.Result,
            alt.Status,
            alt.Notes,
            alt.CreatedAt,
            BuildIndicators(serviceName, alt.ResultValues)
        );
    }

    /// <summary>
    /// Maps a LabTestService entity to LabTestServiceDto
    /// </summary>
    private LabTestServiceDto MapLabTestServiceToDto(Domain.Entities.LabTestService labTestService)
    {
        return new LabTestServiceDto(
            labTestService.LabTestServiceId,
            labTestService.ServiceName,
            labTestService.Description,
            labTestService.Price,
            labTestService.Category,
            labTestService.IsActive,
            labTestService.CreatedAt
        );
    }
}
