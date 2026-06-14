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

    public virtual DbSet<Appointment> Appointments { get; set; }

    public virtual DbSet<MedicalRecord> MedicalRecords { get; set; }

    public virtual DbSet<Prescription> Prescriptions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer("Server=localhost;uid=sa;password=123;database=HospitalManagementDB_V1;Encrypt=True;TrustServerCertificate=True;");
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
            entity.Property(e => e.FullName).HasMaxLength(100).HasColumnName("FullName");
            entity.Property(e => e.Username).HasMaxLength(100).IsUnicode(false).HasColumnName("Username");
            entity.Property(e => e.PasswordHash).HasMaxLength(255).HasColumnName("PasswordHash");
            entity.Property(e => e.Phone).HasMaxLength(20).IsUnicode(false).HasColumnName("Phone");
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

            entity.HasIndex(e => e.UserId, "UQ__Doctors__UserId").IsUnique();

            entity.Property(e => e.DoctorId).HasColumnName("DoctorId");
            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.Specialization).HasMaxLength(100).HasColumnName("Specialization");
            entity.Property(e => e.ExperienceYears).HasDefaultValue(0).HasColumnName("ExperienceYears");
            entity.Property(e => e.Description).HasMaxLength(1000).HasColumnName("Description");

            entity.HasOne(d => d.User).WithOne(p => p.Doctor)
                .HasForeignKey<Doctor>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Doctors_Users");
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patients");

            entity.HasIndex(e => e.UserId, "UQ__Patients__UserId").IsUnique();

            entity.Property(e => e.PatientId).HasColumnName("PatientId");
            entity.Property(e => e.UserId).HasColumnName("UserId");
            entity.Property(e => e.DateOfBirth).HasColumnName("DateOfBirth");
            entity.Property(e => e.Gender).HasMaxLength(10).HasColumnName("Gender");
            entity.Property(e => e.Address).HasMaxLength(255).HasColumnName("Address");
            entity.Property(e => e.BloodType).HasMaxLength(10).HasColumnName("BloodType");
            entity.Property(e => e.EmergencyContactName).HasMaxLength(100).HasColumnName("EmergencyContactName");
            entity.Property(e => e.EmergencyContactPhone).HasMaxLength(20).IsUnicode(false).HasColumnName("EmergencyContactPhone");

            entity.HasOne(d => d.User).WithOne(p => p.Patient)
                .HasForeignKey<Patient>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Patients_Users");
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointments");

            entity.Property(e => e.AppointmentId).HasColumnName("AppointmentId");
            entity.Property(e => e.PatientId).HasColumnName("PatientId");
            entity.Property(e => e.DoctorId).HasColumnName("DoctorId");
            entity.Property(e => e.AppointmentDate).HasColumnName("AppointmentDate");
            entity.Property(e => e.AppointmentTime).HasColumnName("AppointmentTime");
            entity.Property(e => e.Reason).HasMaxLength(500).HasColumnName("Reason");
            entity.Property(e => e.Status).HasMaxLength(20).IsUnicode(false).HasDefaultValue("Pending").HasColumnName("Status");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())").HasColumnType("datetime").HasColumnName("CreatedAt");

            entity.HasOne(d => d.Doctor).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.DoctorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Appointments_Doctors");

            entity.HasOne(d => d.Patient).WithMany(p => p.Appointments)
                .HasForeignKey(d => d.PatientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Appointments_Patients");
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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
