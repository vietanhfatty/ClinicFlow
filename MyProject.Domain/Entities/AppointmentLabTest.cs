using System;
using System.Collections.Generic;

namespace MyProject.Domain.Entities;

public partial class AppointmentLabTest
{
    public int AppointmentLabTestId { get; set; }

    public int AppointmentId { get; set; }

    public int LabTestServiceId { get; set; }

    public int? DoctorId { get; set; }

    public DateTime? TestDate { get; set; }

    public string? Result { get; set; }

    /// <summary>
    /// JSON-serialized dictionary of indicator key -> value for lab tests that
    /// have a defined set of indicators (see LabTestIndicatorCatalog). Null for
    /// lab test services without structured indicators (e.g. imaging).
    /// </summary>
    public string? ResultValues { get; set; }

    public string Status { get; set; } = "Pending";

    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Appointment Appointment { get; set; } = null!;

    public virtual LabTestService LabTestService { get; set; } = null!;

    public virtual Doctor? Doctor { get; set; }
}
