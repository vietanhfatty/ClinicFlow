using System;
using System.Collections.Generic;

namespace MyProject.Domain.Entities;

public partial class LabTestService
{
    public int LabTestServiceId { get; set; }

    public string ServiceName { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string? Category { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<AppointmentLabTest> AppointmentLabTests { get; set; } = new List<AppointmentLabTest>();
}
