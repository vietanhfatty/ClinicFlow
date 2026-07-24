using System;
using System.Collections.Generic;

namespace MyProject.Domain.Entities;

public partial class Doctor
{
    public int DoctorId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string Specialization { get; set; } = null!;

    public int ExperienceYears { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<AppointmentLabTest> AppointmentLabTests { get; set; } = new List<AppointmentLabTest>();
}
