using Microsoft.EntityFrameworkCore;
using MyProject.Domain.Entities;
using MyProject.Domain.IRepositories;
using MyProject.Infrastructure.Repositories;
using MyProject.Application.Services;
using MyProject.WebMvc.Handlers;

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

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<BearerTokenForwardingHandler>();

// Register Repositories
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IAppointmentRepository, AppointmentRepository>();
builder.Services.AddScoped<IMedicalRecordRepository, MedicalRecordRepository>();
builder.Services.AddScoped<IPrescriptionRepository, PrescriptionRepository>();
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IAppointmentLabTestRepository, AppointmentLabTestRepository>();

// Register Services
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<PatientService>();
builder.Services.AddScoped<DoctorService>();
builder.Services.AddScoped<AppointmentService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<MedicalRecordService>();
builder.Services.AddScoped<StaffService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddScoped<PatientMedicalRecordService>();
builder.Services.AddScoped<NotificationService>();

// Register API Services (for calling WebApi via HttpClient)
builder.Services.AddScoped<RoleApiService>();
builder.Services.AddScoped<PatientApiService>();
builder.Services.AddScoped<DoctorApiService>();
builder.Services.AddScoped<AppointmentApiService>();
builder.Services.AddScoped<MedicalRecordApiService>();
builder.Services.AddScoped<StaffApiService>();
builder.Services.AddScoped<StatisticsApiService>();
builder.Services.AddScoped<PatientPortalApiService>();
builder.Services.AddScoped<AuthApiService>();
builder.Services.AddScoped<NotificationApiService>();
builder.Services.AddScoped<LabTestApiService>();

// Register Cookie Authentication
builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = System.TimeSpan.FromMinutes(60);
    });

// Add HttpClient for calling WebApi
builder.Services.AddHttpClient("WebApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:7281/api/");
    client.Timeout = TimeSpan.FromSeconds(30);
}).AddHttpMessageHandler<BearerTokenForwardingHandler>();

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();
