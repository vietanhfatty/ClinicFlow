using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.OData;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;
using MyProject.Infrastructure.Repositories;
using MyProject.Application.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<HospitalManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Repositories
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IMedicalRecordRepository, MedicalRecordRepository>();
builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();

// Register Services
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<DoctorService>();
builder.Services.AddScoped<AppointmentService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<MedicalRecordService>();
builder.Services.AddScoped<StaffService>();

// Add Data Protection to share authentication cookies with WebMvc
builder.Services.AddDataProtection()
    .SetApplicationName("HospitalManagementSharedApp");

// Register Cookie Authentication
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = System.TimeSpan.FromMinutes(60);
    });

builder.Services.AddControllers()
    .AddOData(options => options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(100));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
