# ClinicFlow - Clinic & Hospital Management System (.NET 8)

> 🌐 **Language / Ngôn ngữ:**  
> 🇬🇧 **English:** Default documentation (see below)  
> 🇻🇳 **Tiếng Việt:** Cuộn xuống bên dưới hoặc [bấm vào đây](#-clinicflow---hệ-thống-quản-lý-bệnh-viện--phòng-khám-net-8) để xem bản Tiếng Việt.

---

## 📌 Project Overview

**ClinicFlow** (PRN232_PRJ) is a comprehensive Clinic and Hospital Management System built on **.NET 8** following modern **Clean Architecture / N-Tier Architecture** principles. The application provides an all-in-one solution for managing medical appointments, electronic medical records (EMR), prescriptions, laboratory test orders and results, billing & payments, a dedicated patient portal, and business intelligence reporting.

The solution features a clear separation of concerns between a RESTful **Web API (ASP.NET Core Web API + OData)** backend and an interactive **ASP.NET Core Web MVC** frontend, secured with **JWT (JSON Web Token)** authentication and supported by automated background services.

---

## 🏗 System Architecture

The project is structured into 5 core layers/projects:

1. **`MyProject.Domain`**
   - Contains core domain entities: `User`, `Role`, `Doctor`, `Patient`, `Staff`, `Appointment`, `MedicalRecord`, `Prescription`, `AppointmentBill`, `AppointmentLabTest`, `LabTestService`, `Payment`, and `Notification`.
   - Defines generic and entity-specific repository interfaces (`IRepositories`).

2. **`MyProject.Application`**
   - Encapsulates core business logic (`Services`): `AppointmentService`, `AuthService`, `DoctorService`, `PatientService`, `LabTestService`, `MedicalRecordService`, `AppointmentBillService`, `PaymentService`, `StatisticsService`, etc.
   - Defines Data Transfer Objects (**DTOs**) and API Client Services for Web API communication.

3. **`MyProject.Infrastructure`**
   - Handles data access and persistence via **Entity Framework Core (EF Core)**.
   - Contains `HospitalManagementDbContext`, entity relationship configurations, seed data, and database migrations.
   - Implements repository interfaces (`Repositories`).

4. **`MyProject.WebApi`**
   - RESTful Web API providing endpoints for all system functionalities.
   - Integrated with **OData 8.x** for advanced querying (`$select`, `$filter`, `$orderby`, `$expand`, `$count`).
   - Implements **JWT Bearer Token** authentication and role-based authorization.
   - Runs `LateAppointmentBackgroundService` (`IHostedService`) to automatically detect and flag overdue appointments.
   - Integrated **Swagger UI** for API testing and documentation.

5. **`MyProject.WebMvc`**
   - Web user interface built with **ASP.NET Core MVC (Razor Views)** + Bootstrap 5.
   - Consumes `MyProject.WebApi` using `HttpClient` / API Services.
   - Implements role-tailored user interfaces (Admin, Doctor, Staff, Patient).

---

## 🔥 Key Features

### 1. 🔐 Authentication & Authorization
- Secure JWT Bearer Token authentication.
- Role-based access control (RBAC): **Admin**, **Doctor**, **Staff**, **Patient**.

### 2. 📅 Appointment Scheduling & Queue Management
- Patients and staff can schedule appointments by selecting doctors and time slots.
- Automatic queue number generation.
- Dynamic appointment status lifecycle: `Pending` -> `Confirmed` -> `Completed` / `Cancelled` / `Late`.
- Background task scanning to mark overdue appointments as `Late`.

### 3. 👨‍⚕️ Medical Records & E-Prescriptions
- Doctors record diagnoses, symptoms, and medical histories.
- Digital prescription issuing linked directly to medical records.

### 4. 🧪 Lab Tests & Service Catalog Management
- Management of laboratory test services and indicator catalogs (`LabTestService`, `LabTestIndicatorCatalog`).
- Doctor lab test ordering (`AppointmentLabTest`).
- Result entry and visualization.

### 5. 💳 Billing & Payment Gateway Processing
- Automated calculation of consultation fees and lab test service charges.
- Itemized invoice generation (`AppointmentBill`).
- Payment processing and transaction history tracking (`Payment`).

### 6. 👤 Patient Self-Service Portal
- Patients manage personal profiles, view medical history, check lab test results, view prescriptions, and track payment invoices.

### 7. 🔔 Real-Time & Event Notifications
- Automated notifications triggered for appointment updates, test results, and reminders.

### 8. 📊 Business Intelligence & Reporting
- Revenue reports across custom timeframes.
- Analytics on patient volume, completed consultations per doctor, and specialty performance.

---

## 🛠 Tech Stack

| Layer / Component | Technology / Library |
| :--- | :--- |
| **Framework** | .NET 8.0 / C# |
| **Web Architecture** | ASP.NET Core Web API, ASP.NET Core MVC |
| **Database** | Microsoft SQL Server |
| **ORM** | Entity Framework Core 8.0 (Code-First) |
| **API Protocol** | RESTful API + OData 8.x |
| **Security** | JWT Bearer Token Authentication |
| **Background Tasks** | `IHostedService` (`LateAppointmentBackgroundService`) |
| **API Docs** | Swagger UI (Swashbuckle) |
| **Frontend** | HTML5, CSS3, JavaScript, Bootstrap 5 |

---

## 📁 Project Structure

```text
PRN232_PRJ/
├── MyProject.Domain/                  # Core Domain Entities & Interfaces
│   ├── Entities/                      # User, Patient, Doctor, Appointment, Bill...
│   └── IRepositories/                 # Repository Interfaces
├── MyProject.Application/             # Business Logic & DTOs
│   ├── DTOs/                          # Data Transfer Objects
│   ├── Services/                      # Application & API Client Services
│   └── Configuration/                 # Application Configurations
├── MyProject.Infrastructure/          # Data Access & Database Context
│   ├── HospitalManagementDbContext.cs # EF Core DbContext
│   ├── Migrations/                    # EF Core Database Migrations
│   └── Repositories/                  # EF Core Repositories Implementation
├── MyProject.WebApi/                  # RESTful API Service
│   ├── Controllers/                   # Web API Controllers (OData enabled)
│   ├── BackgroundServices/            # Background Tasks (Late Appointment Scan)
│   └── Program.cs                     # API Pipeline & DI Configuration
├── MyProject.WebMvc/                  # Web Front-end (MVC)
│   ├── Controllers/                   # MVC Controllers
│   ├── Views/                         # Razor Views (UI)
│   ├── Handlers/                      # Authentication & HTTP Handlers
│   └── Program.cs                     # MVC Pipeline & Client DI Configuration
└── Project_PRN232.sln                 # Solution File
```

---

## 🚀 Getting Started & Setup Guide

### 1. Prerequisites
- **.NET 8.0 SDK** or later.
- **Microsoft SQL Server** (LocalDB or SQL Express).
- **Visual Studio 2022** / **VS Code**.

### 2. Configure Database Connection
Update the `DefaultConnection` string in `appsettings.json` under both `MyProject.WebApi` and `MyProject.WebMvc`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=HospitalManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 3. Database Migrations
Run the following command in Package Manager Console or Terminal:

```bash
dotnet ef database update --project MyProject.Infrastructure --startup-project MyProject.WebApi
```

### 4. Running the Application
Launch both **`MyProject.WebApi`** and **`MyProject.WebMvc`** simultaneously.

#### Using .NET CLI
```bash
# Terminal 1 - Launch Web API (default: https://localhost:7001)
cd MyProject.WebApi
dotnet run

# Terminal 2 - Launch Web MVC
cd MyProject.WebMvc
dotnet run
```

#### Using Visual Studio
- Right-click Solution `Project_PRN232.sln` -> Select **Set Startup Projects...**
- Select **Multiple startup projects**.
- Set Action to **Start** for both `MyProject.WebApi` and `MyProject.WebMvc`.
- Press `F5` to run.

---

## 🔗 Useful Links

- **Swagger API Documentation:** `https://localhost:7001/swagger`
- **Web Application (MVC):** `https://localhost:7002`

---

<br/>
<br/>

================================================================================

<br/>
<br/>

# 🇻🇳 ClinicFlow - Hệ Thống Quản Lý Bệnh Viện & Phòng Khám (.NET 8)

## 📌 Giới Thiệu Tổng Quan

**ClinicFlow** (PRN232_PRJ) là hệ thống phần mềm quản lý phòng khám và bệnh viện được thiết kế theo kiến trúc **Clean Architecture / N-Tier Architecture** hiện đại bằng .NET 8. Hệ thống cung cấp giải pháp toàn diện cho việc quản lý lịch khám, hồ sơ bệnh án, đơn thuốc, chỉ định xét nghiệm, hóa đơn thanh toán, cổng thông tin bệnh nhân và báo cáo thống kê.

Hệ thống tách biệt rõ ràng giữa backend **Web API (RESTful API + OData)** và giao diện front-end **Web MVC (Razor Views)**, hỗ trợ cơ chế xác thực an toàn qua **JWT (JSON Web Token)** cùng quy trình quản lý tác vụ ngầm tự động (Background Services).

---

## 🏗 Kiến Trúc Hệ Thống (Architecture)

Dự án được phân chia thành 5 layer / project chính:

1. **`MyProject.Domain`**
   - Chứa các thực thể dữ liệu lõi (**Entities**): `User`, `Role`, `Doctor`, `Patient`, `Staff`, `Appointment`, `MedicalRecord`, `Prescription`, `AppointmentBill`, `AppointmentLabTest`, `LabTestService`, `Payment`, `Notification`.
   - Định nghĩa các interface repository (`IRepositories`).

2. **`MyProject.Application`**
   - Chứa logic xử lý nghiệp vụ chính (**Services**): `AppointmentService`, `AuthService`, `DoctorService`, `PatientService`, `LabTestService`, `MedicalRecordService`, `AppointmentBillService`, `PaymentService`, `StatisticsService`, v.v.
   - Định nghĩa các Data Transfer Objects (**DTOs**) và API Client Services giao tiếp với Web API.

3. **`MyProject.Infrastructure`**
   - Quản lý truy xuất dữ liệu thông qua **Entity Framework Core (EF Core)**.
   - Chứa `HospitalManagementDbContext`, cấu hình quan hệ giữa các bảng, seed data và database migrations.
   - Triển khai chi tiết các repositories (`Repositories`).

4. **`MyProject.WebApi`**
   - RESTful API cung cấp endpoints cho toàn bộ hệ thống.
   - Tích hợp **OData** hỗ trợ truy vấn nâng cao (`$select`, `$filter`, `$orderby`, `$expand`, `$count`).
   - Xử lý xác thực người dùng bằng **JWT Bearer Token**.
   - Chạy dịch vụ ngầm `LateAppointmentBackgroundService` tự động kiểm tra và chuyển trạng thái các lịch khám quá hạn thành `Late`.
   - Tích hợp **Swagger UI** phục vụ thử nghiệm API.

5. **`MyProject.WebMvc`**
   - Giao diện người dùng trên nền **ASP.NET Core MVC (Razor Views)** + Bootstrap 5.
   - Giao tiếp với `MyProject.WebApi` thông qua `HttpClient` / API Services.
   - Phân quyền giao diện theo từng vai trò người dùng (Admin, Doctor, Staff, Patient).

---

## 🔥 Các Tính Năng Chính (Key Features)

### 1. 🔐 Quản lý Tài khoản & Phân quyền (Authentication & Authorization)
- Đăng ký, đăng nhập an toàn với JWT Token.
- Phân quyền người dùng theo vai trò: **Admin**, **Doctor (Bác sĩ)**, **Staff (Nhân viên y tế)**, **Patient (Bệnh nhân)**.

### 2. 📅 Quản lý Lịch khám (Appointment Management)
- Bệnh nhân hoặc nhân viên có thể đặt lịch khám bệnh, chọn bác sĩ và khung giờ.
- Tự động cấp số thứ tự khám.
- Cập nhật trạng thái lịch khám: `Pending` -> `Confirmed` -> `Completed` / `Cancelled` / `Late`.
- Background Task tự động phát hiện lịch hẹn trễ hạn để cập nhật trạng thái.

### 3. 👨‍⚕️ Quản lý Hồ sơ Y tế & Đơn thuốc (Medical Records & Prescriptions)
- Bác sĩ ghi nhận thông tin chẩn đoán, triệu chứng, tiểu sử bệnh của bệnh nhân.
- Kê đơn thuốc (Prescription) đi kèm hồ sơ khám.

### 4. 🧪 Quản lý Chỉ định & Kết quả Xét nghiệm (Lab Tests)
- Quản lý danh mục dịch vụ xét nghiệm và các chỉ số xét nghiệm (`LabTestService`, `LabTestIndicatorCatalog`).
- Bác sĩ chỉ định xét nghiệm cho bệnh nhân (`AppointmentLabTest`).
- Cập nhật và xem kết quả xét nghiệm trực quan.

### 5. 💳 Quản lý Hóa đơn & Thanh toán (Bills & Payments)
- Tự động tính toán chi phí khám bệnh + chi phí các dịch vụ xét nghiệm phát sinh.
- Xuất hóa đơn khám bệnh (`AppointmentBill`).
- Ghi nhận và quản lý lịch sử thanh toán (`Payment`).

### 6. 👤 Cổng thông tin Bệnh nhân (Patient Portal)
- Bệnh nhân tự quản lý hồ sơ cá nhân, xem lịch sử các lần khám bệnh, kết quả xét nghiệm, đơn thuốc và hóa đơn thanh toán của mình.

### 7. 🔔 Quản lý Thông báo (Notifications)
- Hệ thống gửi thông báo tự động cho bệnh nhân/bác sĩ khi có cập nhật lịch khám hoặc kết quả xét nghiệm.

### 8. 📊 Thống kê & Báo cáo (Statistics & Reporting)
- Báo cáo doanh thu phòng khám theo khoảng thời gian.
- Thống kê số lượng bệnh nhân, số ca khám theo bác sĩ và chuyên khoa.

---

## 🛠 Công Nghệ Sử Dụng (Tech Stack)

| Thành phần | Công nghệ / Thư viện |
| :--- | :--- |
| **Platform** | .NET 8.0 / C# |
| **Framework Web** | ASP.NET Core Web API, ASP.NET Core MVC |
| **Database** | Microsoft SQL Server |
| **ORM** | Entity Framework Core 8.0 (Code-First) |
| **API Protocol** | RESTful API + OData 8.x |
| **Xác thực** | JWT Bearer Authentication |
| **Background Task** | `IHostedService` (`LateAppointmentBackgroundService`) |
| **Tài liệu API** | Swagger UI (Swashbuckle) |
| **Giao diện** | HTML5, CSS3, JavaScript, Bootstrap 5 |

---

## 📁 Cấu Trúc Thư Mục (Project Structure)

```text
PRN232_PRJ/
├── MyProject.Domain/                  # Entities & Interfaces
│   ├── Entities/                      # User, Patient, Doctor, Appointment, Bill...
│   └── IRepositories/                 # Repository Interfaces
├── MyProject.Application/             # Business Logic & DTOs
│   ├── DTOs/                          # Data Transfer Objects
│   ├── Services/                      # Application & API Client Services
│   └── Configuration/                 # Application Configurations
├── MyProject.Infrastructure/          # Data Access & Database Context
│   ├── HospitalManagementDbContext.cs # EF Core DbContext
│   ├── Migrations/                    # EF Core Database Migrations
│   └── Repositories/                  # EF Core Repositories Implementation
├── MyProject.WebApi/                  # RESTful API Service
│   ├── Controllers/                   # Web API Controllers (OData enabled)
│   ├── BackgroundServices/            # Background Tasks (Late Appointment Scan)
│   └── Program.cs                     # API Pipeline & DI Configuration
├── MyProject.WebMvc/                  # Web Front-end (MVC)
│   ├── Controllers/                   # MVC Controllers
│   ├── Views/                         # Razor Views (UI)
│   ├── Handlers/                      # Authentication & HTTP Handlers
│   └── Program.cs                     # MVC Pipeline & Client DI Configuration
└── Project_PRN232.sln                 # Solution File
```

---

## 🚀 Hướng Dẫn Cài Đặt & Chạy Ứng Dụng

### 1. Yêu cầu hệ thống
- **.NET 8.0 SDK** trở lên.
- **Microsoft SQL Server** (LocalDB hoặc SQL Express).
- **Visual Studio 2022** hoặc **VS Code**.

### 2. Cấu hình Chuỗi Kết Nối Database
Mở file `appsettings.json` trong dự án `MyProject.WebApi` và `MyProject.WebMvc`, cập nhật lại chuỗi kết nối `DefaultConnection` phù hợp với máy của bạn:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=YOUR_SERVER_NAME;Database=HospitalManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### 3. Khởi tạo Cơ sở Dữ liệu (Database Migration)
Mở **Package Manager Console** trong Visual Studio (hoặc Terminal) và chạy lệnh:

```bash
dotnet ef database update --project MyProject.Infrastructure --startup-project MyProject.WebApi
```

### 4. Khởi chạy Ứng dụng
Bạn cần khởi chạy đồng thời 2 dự án: **`MyProject.WebApi`** và **`MyProject.WebMvc`**.

#### Cách 1: Sử dụng .NET CLI
```bash
# Terminal 1 - Chạy Web API (Port default: https://localhost:7001)
cd MyProject.WebApi
dotnet run

# Terminal 2 - Chạy Web MVC
cd MyProject.WebMvc
dotnet run
```

#### Cách 2: Sử dụng Visual Studio
- Nhấp chuột phải vào Solution `Project_PRN232.sln` -> Chọn **Configure Startup Projects...**
- Chọn **Multiple startup projects**.
- Đặt Action cho `MyProject.WebApi` và `MyProject.WebMvc` là **Start**.
- Nhấn `F5` để chạy.

---

## 🔗 Đường Dẫn Thử Nghiệm

- **Swagger API Documentation:** `https://localhost:7001/swagger`
- **Web App (MVC):** `https://localhost:7002`

---

## 📝 Đóng Góp & Bản Quyền

Dự án phục vụ cho môn học **PRN232 / PRN212**. Mọi đóng góp và chỉnh sửa vui lòng tuân thủ quy định của giảng viên và nhà trường.
