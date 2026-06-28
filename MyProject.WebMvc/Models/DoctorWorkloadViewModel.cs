using System.Collections.Generic;

namespace MyProject.WebMvc.Models;

public class DoctorWorkloadViewModel
{
    public int TotalDoctors { get; set; }
    public int TotalTodayAppointments { get; set; }
    public int TotalPendingToday { get; set; }
    public int TotalInProgressToday { get; set; }
    public int TotalCompletedToday { get; set; }
    public List<DoctorWorkloadItem> Doctors { get; set; } = new();
}

public class DoctorWorkloadItem
{
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int TodayTotal { get; set; }
    public int TodayPending { get; set; }
    public int TodayConfirmed { get; set; }
    public int TodayInProgress { get; set; }
    public int TodayCompleted { get; set; }
    public int WeekTotal { get; set; }
    public int TotalAppointments { get; set; }
    public double WorkloadPercentage { get; set; }
}
