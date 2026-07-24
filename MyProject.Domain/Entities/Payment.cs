using System;

namespace MyProject.Domain.Entities;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int PatientId { get; set; }

    public int? AppointmentId { get; set; }

    public decimal Amount { get; set; }

    public string Reason { get; set; } = null!;

    public string Status { get; set; } = "Pending"; // Pending, Completed, Cancelled

    public DateTime RequestDate { get; set; }

    public DateTime? PaidDate { get; set; }

    public virtual Patient Patient { get; set; } = null!;

    public virtual Appointment? Appointment { get; set; }
}
