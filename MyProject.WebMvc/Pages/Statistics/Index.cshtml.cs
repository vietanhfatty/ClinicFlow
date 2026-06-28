using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyProject.Application.Services;
using MyProject.WebMvc.Models;

namespace MyProject.WebMvc.Pages.Statistics;

[Authorize(Roles = "Admin")]
public class IndexModel : PageModel
{
    private readonly StatisticsApiService _statisticsApiService;

    public IndexModel(StatisticsApiService statisticsApiService)
    {
        _statisticsApiService = statisticsApiService;
    }

    public HospitalStatisticsViewModel Statistics { get; set; } = new();

    public async Task OnGetAsync()
    {
        var stats = await _statisticsApiService.GetHospitalStatisticsAsync();

        Statistics = new HospitalStatisticsViewModel
        {
            TotalPatients = stats.TotalPatients,
            TotalDoctors = stats.TotalDoctors,
            ActiveDoctors = stats.ActiveDoctors,
            TotalStaff = stats.TotalStaff,
            TotalMedicalRecords = stats.TotalMedicalRecords,
            TotalAppointments = stats.TotalAppointments,
            TodayAppointments = stats.TodayAppointments,
            CompletedAppointments = stats.CompletedAppointments,
            PendingAppointments = stats.PendingAppointments,
            CancelledAppointments = stats.CancelledAppointments,
            InProgressAppointments = stats.InProgressAppointments,
            AppointmentsByStatus = stats.AppointmentsByStatus
                .Select(s => new StatusCountItem
                {
                    Status = s.Status,
                    Count = s.Count,
                    Percentage = s.Percentage
                })
                .ToList(),
            DoctorsBySpecialization = stats.DoctorsBySpecialization
                .Select(s => new SpecializationCountItem
                {
                    Specialization = s.Specialization,
                    Count = s.Count
                })
                .ToList(),
            MonthlyAppointments = stats.MonthlyAppointments
                .Select(m => new MonthlyCountItem
                {
                    MonthLabel = m.MonthLabel,
                    Count = m.Count
                })
                .ToList(),
            TopDoctorsByAppointments = stats.TopDoctorsByAppointments
                .Select(d => new TopDoctorItem
                {
                    DoctorName = d.DoctorName,
                    Specialization = d.Specialization,
                    AppointmentCount = d.AppointmentCount
                })
                .ToList()
        };
    }
}
