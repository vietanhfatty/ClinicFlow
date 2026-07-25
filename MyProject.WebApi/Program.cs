using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.IdentityModel.Tokens;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;
using MyProject.Infrastructure.Repositories;
using MyProject.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Add DbContext
builder.Services.AddDbContext<HospitalManagementDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection") 
        ?? "Server=VIETANHFATTY\\SQLEXPRESS;uid=sa;password=1234567890;database=HospitalManagementDB;Encrypt=True;TrustServerCertificate=True;",
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

// Register Repositories
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IMedicalRecordRepository, MedicalRecordRepository>();
builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<ILabTestServiceRepository, LabTestServiceRepository>();
builder.Services.AddScoped<IAppointmentLabTestRepository, AppointmentLabTestRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IAppointmentBillRepository, AppointmentBillRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();

// Register HttpClient factory (used by LabTestApiService and other API client services)
builder.Services.AddHttpClient("WebApiClient", client =>
{
    var baseUrl = builder.Configuration["WebApiClient:BaseUrl"] ?? "https://localhost:7001/api/";
    client.BaseAddress = new Uri(baseUrl);
});

// Register Services
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<DoctorService>();
builder.Services.AddScoped<AppointmentService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<MedicalRecordService>();
builder.Services.AddScoped<StaffService>();
builder.Services.AddScoped<StatisticsService>();
builder.Services.AddScoped<MyProject.Application.Services.LabTestService>();
builder.Services.AddScoped<LabTestApiService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<AppointmentBillService>();
builder.Services.AddScoped<PatientMedicalRecordService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddSingleton<JwtTokenService>();

// Queue settings + background service that marks overdue appointments as "Late"
builder.Services.Configure<MyProject.Application.Configuration.QueueSettings>(
    builder.Configuration.GetSection("QueueSettings"));
builder.Services.AddHostedService<MyProject.WebApi.BackgroundServices.LateAppointmentBackgroundService>();

// Register JWT Bearer Authentication
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "HospitalManagementApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "HospitalManagementClient";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
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
