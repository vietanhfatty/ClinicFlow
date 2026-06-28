using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyProject.Application.Services;
using MyProject.WebMvc.Models;

namespace MyProject.WebMvc.Pages.Statistics;

[Authorize(Roles = "Admin,Staff")]
public class DoctorWorkloadModel : PageModel
{
    private readonly StatisticsApiService _statisticsApiService;

    public DoctorWorkloadModel(StatisticsApiService statisticsApiService)
    {
        _statisticsApiService = statisticsApiService;
    }

    public DoctorWorkloadViewModel Workload { get; set; } = new();

    public async Task OnGetAsync()
    {
        var workload = await _statisticsApiService.GetDoctorWorkloadAsync();

        Workload = new DoctorWorkloadViewModel
        {
            TotalDoctors = workload.TotalDoctors,
            TotalTodayAppointments = workload.TotalTodayAppointments,
            TotalPendingToday = workload.TotalPendingToday,
            TotalInProgressToday = workload.TotalInProgressToday,
            TotalCompletedToday = workload.TotalCompletedToday,
            Doctors = workload.Doctors
                .Select(d => new DoctorWorkloadItem
                {
                    DoctorId = d.DoctorId,
                    DoctorName = d.DoctorName,
                    Specialization = d.Specialization,
                    IsActive = d.IsActive,
                    TodayTotal = d.TodayTotal,
                    TodayPending = d.TodayPending,
                    TodayConfirmed = d.TodayConfirmed,
                    TodayInProgress = d.TodayInProgress,
                    TodayCompleted = d.TodayCompleted,
                    WeekTotal = d.WeekTotal,
                    TotalAppointments = d.TotalAppointments,
                    WorkloadPercentage = d.WorkloadPercentage
                })
                .ToList()
        };
    }
}
