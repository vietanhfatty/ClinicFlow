using System;
using System.ComponentModel.DataAnnotations;

namespace MyProject.Domain.Entities;

public class AppointmentBill
{
    [Key]
    public int BillId { get; set; }
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public int? StaffId { get; set; }
    
    public decimal ExaminationFee { get; set; }   // Phí khám
    public decimal LabTestFee { get; set; }       // Phí xét nghiệm
    public decimal TotalAmount { get; set; }     // Tổng tiền
    
    public string Status { get; set; } = "Pending";  // Pending, Paid, Cancelled
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Notes { get; set; }
    
    public virtual Appointment Appointment { get; set; } = null!;
    public virtual Patient Patient { get; set; } = null!;
    public virtual Staff? Staff { get; set; }
}
