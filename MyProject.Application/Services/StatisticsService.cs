using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using MyProject.Application.DTOs;
using MyProject.Domain.IRepositories;

namespace MyProject.Application.Services;

public class StatisticsService
{
    private readonly PatientService _patientService;
    private readonly DoctorService _doctorService;
    private readonly StaffService _staffService;
    private readonly AppointmentService _appointmentService;
    private readonly MedicalRecordService _medicalRecordService;
    private readonly IUserRepository _userRepo;

    public StatisticsService(
        PatientService patientService,
        DoctorService doctorService,
        StaffService staffService,
        AppointmentService appointmentService,
        MedicalRecordService medicalRecordService,
        IUserRepository userRepo)
    {
        _patientService = patientService;
        _doctorService = doctorService;
        _staffService = staffService;
        _appointmentService = appointmentService;
        _medicalRecordService = medicalRecordService;
        _userRepo = userRepo;
    }

    public async Task<HospitalStatisticsDto> GetHospitalStatisticsAsync()
    {
        var patients = (await _patientService.GetAllAsync()).ToList();
        var doctors = (await _doctorService.GetAllAsync()).ToList();
        var staff = (await _staffService.GetAllAsync()).ToList();
        var appointments = (await _appointmentService.GetAllAsync()).ToList();
        var medicalRecords = (await _medicalRecordService.GetAllAsync()).ToList();
        var users = (await _userRepo.GetAllAsync()).ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var totalAppointments = appointments.Count;

        var statusGroups = appointments
            .GroupBy(a => a.Status)
            .Select(g => new StatusCountDto(
                g.Key,
                g.Count(),
                totalAppointments > 0
                    ? Math.Round(g.Count() * 100.0 / totalAppointments, 1)
                    : 0))
            .OrderByDescending(s => s.Count)
            .ToList();

        var monthlyAppointments = Enumerable.Range(0, 6)
            .Select(i => DateTime.Today.AddMonths(-5 + i))
            .Select(monthStart =>
            {
                var monthEnd = monthStart.AddMonths(1);
                var count = appointments.Count(a =>
                {
                    var date = a.AppointmentDate.ToDateTime(TimeOnly.MinValue);
                    return date >= monthStart && date < monthEnd;
                });

                return new MonthlyCountDto(
                    monthStart.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                    count);
            })
            .ToList();

        var topDoctors = appointments
            .GroupBy(a => new { a.DoctorId, a.DoctorName })
            .Select(g =>
            {
                var doctor = doctors.FirstOrDefault(d => d.DoctorId == g.Key.DoctorId);
                return new TopDoctorDto(
                    g.Key.DoctorName,
                    doctor?.Specialization ?? "General",
                    g.Count());
            })
            .OrderByDescending(d => d.AppointmentCount)
            .Take(5)
            .ToList();

        var activeDoctors = doctors.Count(d =>
            !string.IsNullOrEmpty(d.Username) &&
            users.Any(u => u.Username == d.Username && u.IsActive));

        return new HospitalStatisticsDto(
            TotalPatients: patients.Count,
            TotalDoctors: doctors.Count,
            ActiveDoctors: activeDoctors,
            TotalStaff: staff.Count,
            TotalMedicalRecords: medicalRecords.Count,
            TotalAppointments: totalAppointments,
            TodayAppointments: appointments.Count(a => a.AppointmentDate == today),
            CompletedAppointments: appointments.Count(a => a.Status == "Completed"),
            PendingAppointments: appointments.Count(a => a.Status == "Pending"),
            CancelledAppointments: appointments.Count(a => a.Status == "Cancelled"),
            InProgressAppointments: appointments.Count(a => a.Status == "InProgress"),
            AppointmentsByStatus: statusGroups,
            DoctorsBySpecialization: doctors
                .GroupBy(d => d.Specialization)
                .Select(g => new SpecializationCountDto(g.Key, g.Count()))
                .OrderByDescending(s => s.Count)
                .ToList(),
            MonthlyAppointments: monthlyAppointments,
            TopDoctorsByAppointments: topDoctors);
    }

    public async Task<DoctorWorkloadDto> GetDoctorWorkloadAsync()
    {
        var doctors = (await _doctorService.GetAllAsync()).ToList();
        var appointments = (await _appointmentService.GetAllAsync()).ToList();
        var users = (await _userRepo.GetAllAsync()).ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var weekEnd = weekStart.AddDays(7);

        var doctorItems = doctors.Select(d =>
        {
            var doctorAppointments = appointments.Where(a => a.DoctorId == d.DoctorId).ToList();
            var todayAppointments = doctorAppointments.Where(a => a.AppointmentDate == today).ToList();
            var weekAppointments = doctorAppointments.Where(a =>
                a.AppointmentDate >= weekStart && a.AppointmentDate < weekEnd).ToList();

            var isActive = !string.IsNullOrEmpty(d.Username) &&
                users.Any(u => u.Username == d.Username && u.IsActive);

            return new DoctorWorkloadItemDto(
                DoctorId: d.DoctorId,
                DoctorName: d.FullName,
                Specialization: d.Specialization,
                IsActive: isActive,
                TodayTotal: todayAppointments.Count(a => a.Status != "Cancelled"),
                TodayPending: todayAppointments.Count(a => a.Status == "Pending"),
                TodayConfirmed: todayAppointments.Count(a => a.Status == "Confirmed"),
                TodayInProgress: todayAppointments.Count(a => a.Status == "InProgress"),
                TodayCompleted: todayAppointments.Count(a => a.Status == "Completed"),
                WeekTotal: weekAppointments.Count(a => a.Status != "Cancelled"),
                TotalAppointments: doctorAppointments.Count(a => a.Status != "Cancelled"),
                WorkloadPercentage: 0);
        }).ToList();

        var maxToday = doctorItems.Any() ? doctorItems.Max(d => d.TodayTotal) : 0;
        if (maxToday > 0)
        {
            doctorItems = doctorItems
                .Select(d => d with
                {
                    WorkloadPercentage = Math.Round(d.TodayTotal * 100.0 / maxToday, 1)
                })
                .OrderByDescending(d => d.TodayTotal)
                .ThenBy(d => d.DoctorName)
                .ToList();
        }
        else
        {
            doctorItems = doctorItems
                .OrderBy(d => d.DoctorName)
                .ToList();
        }

        var allToday = appointments.Where(a => a.AppointmentDate == today).ToList();

        return new DoctorWorkloadDto(
            TotalDoctors: doctors.Count,
            TotalTodayAppointments: allToday.Count(a => a.Status != "Cancelled"),
            TotalPendingToday: allToday.Count(a => a.Status == "Pending"),
            TotalInProgressToday: allToday.Count(a => a.Status == "InProgress"),
            TotalCompletedToday: allToday.Count(a => a.Status == "Completed"),
            Doctors: doctorItems);
    }
}
