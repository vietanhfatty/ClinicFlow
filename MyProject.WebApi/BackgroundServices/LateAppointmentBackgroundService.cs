using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MyProject.Application.Services;

namespace MyProject.WebApi.BackgroundServices;

/// <summary>
/// Periodically scans for Pending appointments that have passed their grace period
/// without checking in, and marks them as "Late" (firing a notification to the patient).
/// </summary>
public class LateAppointmentBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LateAppointmentBackgroundService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);

    public LateAppointmentBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<LateAppointmentBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        // Run once at startup, then on every tick.
        do
        {
            try
            {
                // AppointmentService and DbContext are Scoped, so create a fresh scope each pass.
                using var scope = _scopeFactory.CreateScope();
                var appointmentService = scope.ServiceProvider.GetRequiredService<AppointmentService>();
                var count = await appointmentService.MarkOverdueAppointmentsAsLateAsync();
                if (count > 0)
                {
                    _logger.LogInformation("Marked {Count} appointment(s) as Late.", count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while scanning for late appointments.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
