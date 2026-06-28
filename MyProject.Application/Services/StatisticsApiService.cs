using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MyProject.Application.DTOs;

namespace MyProject.Application.Services;

public class StatisticsApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _clientName = "WebApiClient";

    public StatisticsApiService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    private HttpClient GetClient() => _httpClientFactory.CreateClient(_clientName);

    public async Task<HospitalStatisticsDto> GetHospitalStatisticsAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("statistics/hospital");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HospitalStatisticsDto>()
            ?? throw new HttpRequestException("Failed to deserialize hospital statistics response.");
    }

    public async Task<DoctorWorkloadDto> GetDoctorWorkloadAsync()
    {
        var client = GetClient();
        var response = await client.GetAsync("statistics/doctor-workload");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DoctorWorkloadDto>()
            ?? throw new HttpRequestException("Failed to deserialize doctor workload response.");
    }
}
