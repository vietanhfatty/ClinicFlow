using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MyProject.Domain.Entities;

public partial class HospitalManagementDbContext : DbContext
{
    public HospitalManagementDbContext()
    {
    }

    public HospitalManagementDbContext(DbContextOptions<HospitalManagementDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Doctor> Doctors { get; set; }

    public virtual DbSet<Patient> Patients { get; set; }

    public virtual DbSet<Staff> Staffs { get; set; }

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<MedicalRecord> MedicalRecords { get; set; }

    public virtual DbSet<Prescription> Prescriptions { get; set; }

    public virtual DbSet<LabTestService> LabTestServices { get; set; }

    public virtual DbSet<AppointmentLabTest> AppointmentLabTests { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<AppointmentBill> AppointmentBills { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=VIETANHFATTY\\SQLEXPRESS;uid=sa;password=123456;database=HospitalManagementDB;Encrypt=True;TrustServerCertificate=True;",
                sqlOptions => sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null));
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__RoleName").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("RoleId");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("RoleName");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasIndex(e => e.Username, "UQ__Users__Username").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.Username).HasMaxLength(100).IsUnicode(false).HasColumnName("Username");
            entity.Property(e => e.PasswordHash).HasMaxLength(255).HasColumnName("PasswordHash");
            entity.Property(e => e.RoleId).HasColumnName("RoleId");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("IsActive");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("CreatedAt");
            entity.Property(e => e.UpdatedAt).HasColumnType("datetime").HasColumnName("UpdatedAt");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Users_Roles");
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.ToTable("Doctors");

            entity.Property(e => e.DoctorId).HasColumnName("DoctorId");
            entity.Property(e => e.FullName).HasMaxLength(100).HasColumnName("FullName");
            entity.Property(e => e.Phone).HasMaxLength(20).IsUnicode(false).HasColumnName("Phone");
            entity.Property(e => e.Email).HasMaxLength(100).IsUnicode(false).HasColumnName("Email");
            entity.Property(e => e.Specialization).HasMaxLength(100).HasColumnName("Specialization");
            entity.Property(e => e.ExperienceYears).HasDefaultValue(0).HasColumnName("ExperienceYears");
            entity.Property(e => e.Description).HasMaxLength(1000).HasColumnName("Description");
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patients");

            entity.Property(e => e.PatientId).HasColumnName("PatientId");
            entity.Property(e => e.FullName).HasMaxLength(100).HasColumnName("FullName");
            entity.Property(e => e.Phone).HasMaxLength(20).IsUnicode(false).HasColumnName("Phone");
            entity.Property(e => e.DateOfBirth).HasColumnName("DateOfBirth");
            entity.Property(e => e.Gender).HasMaxLength(10).HasColumnName("Gender");
            entity.Property(e => e.Address).HasMaxLength(255).HasColumnName("Address");
            entity.Property(e => e.BloodType).HasMaxLength(10).HasColumnName("BloodType");
            entity.Property(e => e.EmergencyContactName).HasMaxLength(100).HasColumnName("EmergencyContactName");
            entity.Property(e => e.EmergencyContactPhone).HasMaxLength(20).IsUnicode(false).HasColumnName("EmergencyContactPhone");

            entity.HasOne(d => d.User)
                .WithOne(p => p.Patient)
                .HasForeignKey<Patient>(d => d.UserId)
                .HasConstraintName("FK_Patients_Users");
        });

        modelBuilder.Entity<Staff>(entity =>
        {
            entity.ToTable("Staff");

            entity.Property(e => e.StaffId).HasColumnName("StaffId");
            entity.Property(e => e.FullName).HasMaxLength(100).HasColumnName("FullName");
            entity.Property(e => e.Phone).HasMaxLength(20).IsUnicode(false).HasColumnName("Phone");
            entity.Property(e => e.Email).HasMaxLength(100).IsUnicode(false).HasColumnName("Email");
            entity.Property(e => e.Position).HasMaxLength(100).HasColumnName("Position");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("CreatedAt");
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointments");

            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentId");
            entity.Property(e => e.PatientId).HasColumnName("PatientId");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorId");
            entity.Property(e => e.StaffId).HasColumnName("StaffId");
            entity.Property(e => e.AppointmentDate).HasColumnName("AppointmentDate");
            entity.Property(e => e.AppointmentTime).HasColumnName("AppointmentTime");
            entity.Property(e => e.Reason).HasMaxLength(500).HasColumnName("Reason");
            entity.Property(e => e.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Pending").HasColumnName("Status");
            entity.Property(e => e.CheckInTime).HasColumnType("datetime").HasColumnName("CheckInTime");
            entity.Property(e => e.QueuePriorityTime).HasColumnType("datetime").HasColumnName("QueuePriorityTime");
            entity.Property(e => e.IsWalkIn).HasDefaultValue(false).HasColumnName("IsWalkIn");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("CreatedAt");

            entity.HasOne(d => d.Doctor).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Appointments_Doctors");

            entity.HasOne(d => d.Patient).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Appointments_Patients");

            entity.HasOne(d => d.Staff).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK_Appointments_Staff");
        });

        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.ToTable("MedicalRecords");

            entity.HasIndex(e => e.AppointmentId, "UQ__MedicalRecords__AppointmentId").IsUnique();

            entity.Property(e => e.MedicalRecordId).HasColumnName("MedicalRecordId");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentId");
            entity.Property(e => e.Symptoms).HasColumnName("Symptoms");
            entity.Property(e => e.Diagnosis).HasColumnName("Diagnosis");
            entity.Property(e => e.Treatment).HasColumnName("Treatment");
            entity.Property(e => e.Notes).HasColumnName("Notes");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("CreatedAt");

            entity.HasOne(d => d.Appointment).WithOne(p => p.MedicalRecord)
                .HasForeignKey<MedicalRecord>(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MedicalRecords_Appointments");
        });

        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.ToTable("Prescriptions");

            entity.Property(e => e.PrescriptionId).HasColumnName("PrescriptionId");
            entity.Property(e => e.MedicalRecordId).HasColumnName("MedicalRecordId");
            entity.Property(e => e.MedicineName).HasMaxLength(100).HasColumnName("MedicineName");
            entity.Property(e => e.Dosage).HasMaxLength(100).HasColumnName("Dosage");
            entity.Property(e => e.Quantity).HasColumnName("Quantity");
            entity.Property(e => e.Instruction).HasMaxLength(500).HasColumnName("Instruction");

            entity.HasOne(d => d.MedicalRecord).WithMany(p => p.Prescriptions)
                .HasForeignKey(d => d.MedicalRecordId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Prescriptions_MedicalRecords");
        });

        modelBuilder.Entity<LabTestService>(entity =>
        {
            entity.ToTable("LabTestServices");

            entity.Property(e => e.LabTestServiceId).HasColumnName("LabTestServiceId");
            entity.Property(e => e.ServiceName).HasMaxLength(100).HasColumnName("ServiceName");
            entity.Property(e => e.Description).HasMaxLength(500).HasColumnName("Description");
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)").HasColumnName("Price");
            entity.Property(e => e.Category).HasMaxLength(100).HasColumnName("Category");
            entity.Property(e => e.IsActive).HasDefaultValue(true).HasColumnName("IsActive");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("CreatedAt");
        });

        modelBuilder.Entity<AppointmentLabTest>(entity =>
        {
            entity.ToTable("AppointmentLabTests");

            entity.Property(e => e.AppointmentLabTestId).HasColumnName("AppointmentLabTestId");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentId");
            entity.Property(e => e.LabTestServiceId).HasColumnName("LabTestServiceId");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorId");
            entity.Property(e => e.TestDate).HasColumnType("datetime").HasColumnName("TestDate");
            entity.Property(e => e.Result).HasColumnName("Result");
            entity.Property(e => e.ResultValues).HasColumnName("ResultValues");
            entity.Property(e => e.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Pending").HasColumnName("Status");
            entity.Property(e => e.Notes).HasMaxLength(500).HasColumnName("Notes");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("CreatedAt");

            entity.HasOne(d => d.Appointment).WithMany(p => p.AppointmentLabTests)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AppointmentLabTests_Appointments");

            entity.HasOne(d => d.LabTestService).WithMany(p => p.AppointmentLabTests)
                .HasForeignKey(d => d.LabTestServiceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AppointmentLabTests_LabTestServices");

            entity.HasOne(d => d.Doctor).WithMany(p => p.AppointmentLabTests)
                .HasForeignKey(d => d.DoctorId)
                .HasConstraintName("FK_AppointmentLabTests_Doctors");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("Payments");

            entity.Property(e => e.PaymentId).HasColumnName("PaymentId");
            entity.Property(e => e.PatientId).HasColumnName("PatientId");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)").HasColumnName("Amount");
            entity.Property(e => e.Reason).HasMaxLength(500).HasColumnName("Reason");
            entity.Property(e => e.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Pending").HasColumnName("Status");
            entity.Property(e => e.RequestDate).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("RequestDate");
            entity.Property(e => e.PaidDate).HasColumnType("datetime").HasColumnName("PaidDate");

            entity.HasOne(d => d.Patient).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Payments_Patients");

            entity.HasOne(d => d.Appointment)
                .WithMany()
                .HasForeignKey(d => d.AppointmentId)
                .HasConstraintName("FK_Payments_Appointments");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notifications");

            entity.Property(e => e.NotificationId).HasColumnName("NotificationId");
            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.Title).HasMaxLength(200).HasColumnName("Title");
            entity.Property(e => e.Message).HasMaxLength(500).HasColumnName("Message");
            entity.Property(e => e.Type).HasMaxLength(30).IsUnicode(false).HasColumnName("Type");
            entity.Property(e => e.RelatedEntityId).HasColumnName("RelatedEntityId");
            entity.Property(e => e.IsRead).HasDefaultValue(false).HasColumnName("IsRead");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("CreatedAt");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notifications_Users");
        });

        modelBuilder.Entity<AppointmentBill>(entity =>
        {
            entity.ToTable("AppointmentBills");

            entity.HasKey(e => e.BillId);

            entity.Property(e => e.BillId).HasColumnName("BillId");
            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentId");
            entity.Property(e => e.PatientId).HasColumnName("PatientId");
            entity.Property(e => e.StaffId).HasColumnName("StaffId");
            entity.Property(e => e.ExaminationFee).HasColumnType("decimal(18, 2)").HasColumnName("ExaminationFee");
            entity.Property(e => e.LabTestFee).HasColumnType("decimal(18, 2)").HasColumnName("LabTestFee");
            entity.Property(e => e.TotalAmount).HasColumnType("decimal(18, 2)").HasColumnName("TotalAmount");
            entity.Property(e => e.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Pending").HasColumnName("Status");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("CreatedAt");
            entity.Property(e => e.PaidAt).HasColumnType("datetime").HasColumnName("PaidAt");
            entity.Property(e => e.Notes).HasMaxLength(500).HasColumnName("Notes");

            entity.HasOne(d => d.Appointment).WithMany(p => p.AppointmentBills)
                .HasForeignKey(d => d.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AppointmentBills_Appointments");

            entity.HasOne(d => d.Patient).WithMany(p => p.AppointmentBills)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_AppointmentBills_Patients");

            entity.HasOne(d => d.Staff).WithMany(p => p.AppointmentBills)
                .HasForeignKey(d => d.StaffId)
                .HasConstraintName("FK_AppointmentBills_Staff");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
