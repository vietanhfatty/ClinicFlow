using Microsoft.AspNetCore.Mvc;
using MyProject.WebMvc.Models;
using System.Diagnostics;
using System.Threading.Tasks;
using MyProject.Application.Services;
using MyProject.Domain.IRepositories;
using System.Linq;
using System;
using Microsoft.AspNetCore.Authorization;

namespace MyProject.WebMvc.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly PatientService _patientService;
        private readonly IDoctorRepository _doctorRepo;
        private readonly AppointmentService _appointmentService;
        private readonly IUserRepository _userRepo;

        public HomeController(
            ILogger<HomeController> logger,
            PatientService patientService,
            IDoctorRepository doctorRepo,
            AppointmentService appointmentService,
            IUserRepository userRepo)
        {
            _logger = logger;
            _patientService = patientService;
            _doctorRepo = doctorRepo;
            _appointmentService = appointmentService;
            _userRepo = userRepo;
        }

        public async Task<IActionResult> Index()
        {
            if (User.IsInRole("Doctor"))
            {
                return RedirectToAction("Queue", "Appointments");
            }

            if (User.IsInRole("Staff"))
            {
                return RedirectToAction("Index", "Appointments");
            }

            if (User.IsInRole("Patient"))
            {
                return RedirectToAction("Dashboard", "PatientPortal");
            }

            var patients = await _patientService.GetAllAsync();
            var doctors = await _doctorRepo.GetAllAsync();
            var appointments = await _appointmentService.GetAllAsync();
            var users = await _userRepo.GetAllAsync();

            var today = DateTime.Today;

            var viewModel = new DashboardViewModel
            {
                TotalPatientsCount = patients.Count(),
                ActiveDoctorsCount = doctors.Count(d => users.Any(u => u.Username == d.Email && u.IsActive)),
                WaitingQueueCount = appointments.Count(a => a.Status == "Waiting" && a.AppointmentDate == DateOnly.FromDateTime(today)),
                TodayAppointmentsCount = appointments.Count(a => a.AppointmentDate == DateOnly.FromDateTime(today)),
                RecentCheckIns = appointments
                    .Where(a => a.AppointmentDate == DateOnly.FromDateTime(today))
                    .OrderByDescending(a => a.AppointmentId)
                    .Take(5)
                    .ToList()
            };

            // Fallback recent check-ins if no appointments today to make UI look complete
            if (!viewModel.RecentCheckIns.Any())
            {
                viewModel.RecentCheckIns = appointments
                    .OrderByDescending(a => a.AppointmentId)
                    .Take(5)
                    .ToList();
            }

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
