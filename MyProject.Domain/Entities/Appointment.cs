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

    /// <summary>Thời điểm bệnh nhân thực sự check-in (null nếu chưa đến).</summary>
    public DateTime? CheckInTime { get; set; }

    /// <summary>
    /// Mốc thời gian dùng để sắp xếp hàng chờ (ORDER BY). Set một lần tại thời điểm check-in:
    /// còn trong grace period -> giờ đặt gốc; walk-in hoặc Late -> giờ thực đến.
    /// </summary>
    public DateTime? QueuePriorityTime { get; set; }

    /// <summary>Đánh dấu ca không đặt trước (khách vãng lai đến trực tiếp).</summary>
    public bool IsWalkIn { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Doctor Doctor { get; set; } = null!;

    public virtual Patient Patient { get; set; } = null!;

    public virtual Staff? Staff { get; set; }

    public virtual MedicalRecord? MedicalRecord { get; set; }

    public virtual ICollection<AppointmentLabTest> AppointmentLabTests { get; set; } = new List<AppointmentLabTest>();
}
