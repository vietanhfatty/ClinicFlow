using System;
using System.Collections.Generic;

namespace MyProject.Domain.Entities;

public partial class Appointment
{
    public int AppointmentId { get; set; }

    public int PatientId { get; set; }

    public int DoctorId { get; set; }

    public int? StaffId { get; set; }

    public DateOnly AppointmentDate { get; set; }

    public TimeSpan AppointmentTime { get; set; }

    public string? Reason { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual Staff? Staff { get; set; }

    public virtual MedicalRecord? MedicalRecord { get; set; }
}
