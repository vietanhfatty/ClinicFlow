using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs;

namespace MyProject.WebMvc.Components;

public class AppointmentCardComponent : ViewComponent
{
    public AppointmentCardComponent()
    {
    }

    public async Task<IViewComponentResult> InvokeAsync(AppointmentDto appointment)
    {
        return View("Default", appointment);
    }
}
