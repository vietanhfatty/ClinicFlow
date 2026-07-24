using System;
using System.Collections.Generic;

namespace MyProject.Domain.Entities;

public partial class Patient
{
    public int PatientId { get; set; }

    public int? UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Address { get; set; }

    public string? BloodType { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public virtual User? User { get; set; }

    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
