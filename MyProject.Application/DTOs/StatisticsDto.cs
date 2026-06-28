using System.Collections.Generic;

namespace MyProject.Application.DTOs;

public record HospitalStatisticsDto(
    int TotalPatients,
    int TotalDoctors,
    int ActiveDoctors,
    int TotalStaff,
    int TotalMedicalRecords,
    int TotalAppointments,
    int TodayAppointments,
    int CompletedAppointments,
    int PendingAppointments,
    int CancelledAppointments,
    int InProgressAppointments,
    List<StatusCountDto> AppointmentsByStatus,
    List<SpecializationCountDto> DoctorsBySpecialization,
    List<MonthlyCountDto> MonthlyAppointments,
    List<TopDoctorDto> TopDoctorsByAppointments
);

public record StatusCountDto(string Status, int Count, double Percentage);

public record SpecializationCountDto(string Specialization, int Count);

public record MonthlyCountDto(string MonthLabel, int Count);

public record TopDoctorDto(string DoctorName, string Specialization, int AppointmentCount);

public record DoctorWorkloadDto(
    int TotalDoctors,
    int TotalTodayAppointments,
    int TotalPendingToday,
    int TotalInProgressToday,
    int TotalCompletedToday,
    List<DoctorWorkloadItemDto> Doctors
);

public record DoctorWorkloadItemDto(
    int DoctorId,
    string DoctorName,
    string Specialization,
    bool IsActive,
    int TodayTotal,
    int TodayPending,
    int TodayConfirmed,
    int TodayInProgress,
    int TodayCompleted,
    int WeekTotal,
    int TotalAppointments,
    double WorkloadPercentage
);
