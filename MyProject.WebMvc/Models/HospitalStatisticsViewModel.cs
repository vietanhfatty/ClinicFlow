using System.Collections.Generic;

namespace MyProject.WebMvc.Models;

public class HospitalStatisticsViewModel
{
    public int TotalPatients { get; set; }
    public int TotalDoctors { get; set; }
    public int ActiveDoctors { get; set; }
    public int TotalStaff { get; set; }
    public int TotalMedicalRecords { get; set; }
    public int TotalAppointments { get; set; }
    public int TodayAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int InProgressAppointments { get; set; }

    public List<StatusCountItem> AppointmentsByStatus { get; set; } = new();
    public List<SpecializationCountItem> DoctorsBySpecialization { get; set; } = new();
    public List<MonthlyCountItem> MonthlyAppointments { get; set; } = new();
    public List<TopDoctorItem> TopDoctorsByAppointments { get; set; } = new();
}

public class StatusCountItem
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class SpecializationCountItem
{
    public string Specialization { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class MonthlyCountItem
{
    public string MonthLabel { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TopDoctorItem
{
    public string DoctorName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public int AppointmentCount { get; set; }
}
