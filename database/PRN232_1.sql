CREATE DATABASE HospitalManagementDB_V2;
GO

USE HospitalManagementDB_V2;
GO

-- ====================================
-- CỤM ĐỘC LẬP: ROLES & USERS (AUTHENTICATION)
-- ====================================
CREATE TABLE Roles
(
    RoleId INT IDENTITY(1,1) PRIMARY KEY,
    RoleName VARCHAR(50) NOT NULL UNIQUE
);

CREATE TABLE Users
(
    UserId INT IDENTITY(1,1) PRIMARY KEY,
    Username VARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(255) NOT NULL,
    RoleId INT NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME NULL,
    CONSTRAINT FK_Users_Roles FOREIGN KEY(RoleId) REFERENCES Roles(RoleId)
);

-- ====================================
-- CỤM NGHIỆP VỤ (CORE HOSPITAL LOGIC)
-- ====================================

-- 1. BẢNG STAFF (MỚI BỔ SUNG)
CREATE TABLE Staff
(
    StaffId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Phone VARCHAR(20),
    Email VARCHAR(100),
    Position NVARCHAR(100), -- Vị trí (VD: Tiếp tân, Kế toán, Thu ngân)
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- 2. DOCTORS (Đã bỏ FK tới Users, tự quản lý profile)
CREATE TABLE Doctors
(
    DoctorId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Phone VARCHAR(20),
    Email VARCHAR(100),
    Specialization NVARCHAR(100) NOT NULL,
    ExperienceYears INT DEFAULT 0,
    Description NVARCHAR(1000)
);

-- 3. PATIENTS (Đã bỏ FK tới Users, tự quản lý profile)
CREATE TABLE Patients
(
    PatientId INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Phone VARCHAR(20),
    DateOfBirth DATE,
    Gender NVARCHAR(10),
    Address NVARCHAR(255),
    BloodType NVARCHAR(10),
    EmergencyContactName NVARCHAR(100),
    EmergencyContactPhone VARCHAR(20)
);

-- 4. APPOINTMENTS (Bổ sung thêm StaffId để biết nhân viên nào duyệt/lập lịch)
CREATE TABLE Appointments
(
    AppointmentId INT IDENTITY(1,1) PRIMARY KEY,
    PatientId INT NOT NULL,
    DoctorId INT NOT NULL,
    StaffId INT NULL, -- Nhân viên tiếp nhận lịch (có thể NULL nếu đặt online)
    AppointmentDate DATE NOT NULL,
    AppointmentTime TIME NOT NULL,
    Reason NVARCHAR(500),
    Status VARCHAR(20) NOT NULL DEFAULT 'Pending',
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Appointments_Patients FOREIGN KEY(PatientId) REFERENCES Patients(PatientId),
    CONSTRAINT FK_Appointments_Doctors FOREIGN KEY(DoctorId) REFERENCES Doctors(DoctorId),
    CONSTRAINT FK_Appointments_Staff FOREIGN KEY(StaffId) REFERENCES Staff(StaffId)
);

-- 5. MEDICAL RECORDS
CREATE TABLE MedicalRecords
(
    MedicalRecordId INT IDENTITY(1,1) PRIMARY KEY,
    AppointmentId INT NOT NULL UNIQUE,
    Symptoms NVARCHAR(MAX),
    Diagnosis NVARCHAR(MAX),
    Treatment NVARCHAR(MAX),
    Notes NVARCHAR(MAX),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_MedicalRecords_Appointments FOREIGN KEY(AppointmentId) REFERENCES Appointments(AppointmentId)
);

-- 6. PRESCRIPTIONS
CREATE TABLE Prescriptions
(
    PrescriptionId INT IDENTITY(1,1) PRIMARY KEY,
    MedicalRecordId INT NOT NULL,
    MedicineName NVARCHAR(100) NOT NULL,
    Dosage NVARCHAR(100),
    Quantity INT,
    Instruction NVARCHAR(500),

    CONSTRAINT FK_Prescriptions_MedicalRecords FOREIGN KEY(MedicalRecordId) REFERENCES MedicalRecords(MedicalRecordId)
);