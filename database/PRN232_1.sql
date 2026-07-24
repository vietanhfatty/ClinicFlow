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
    CheckInTime DATETIME NULL,        -- Thời điểm bệnh nhân thực sự check-in
    QueuePriorityTime DATETIME NULL,  -- Mốc dùng để sắp xếp hàng chờ (grace -> giờ đặt; walk-in/Late -> giờ đến)
    IsWalkIn BIT NOT NULL DEFAULT 0,  -- Đánh dấu ca khách vãng lai đến trực tiếp
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

-- 7. LAB TEST SERVICES (DỊCH VỤ XÉT NGHIỆM)
CREATE TABLE LabTestServices
(
    LabTestServiceId INT IDENTITY(1,1) PRIMARY KEY,
    ServiceName NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    Price DECIMAL(18,2) NOT NULL DEFAULT 0,
    Category NVARCHAR(100),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
);

-- 8. APPOINTMENT LAB TESTS (YÊU CẦU XÉT NGHIỆM THEO LỊCH KHÂM)
CREATE TABLE AppointmentLabTests
(
    AppointmentLabTestId INT IDENTITY(1,1) PRIMARY KEY,
    AppointmentId INT NOT NULL,
    LabTestServiceId INT NOT NULL,
    DoctorId INT NULL,
    TestDate DATETIME NULL,
    Result NVARCHAR(MAX),
    Status VARCHAR(20) NOT NULL DEFAULT 'Pending',
    Notes NVARCHAR(500),
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_AppointmentLabTests_Appointments FOREIGN KEY(AppointmentId) REFERENCES Appointments(AppointmentId),
    CONSTRAINT FK_AppointmentLabTests_LabTestServices FOREIGN KEY(LabTestServiceId) REFERENCES LabTestServices(LabTestServiceId),
    CONSTRAINT FK_AppointmentLabTests_Doctors FOREIGN KEY(DoctorId) REFERENCES Doctors(DoctorId)
);

-- SEED DATA CHO LAB TEST SERVICES
INSERT INTO LabTestServices (ServiceName, Description, Price, Category, IsActive) VALUES
(N'Xét nghiệm máu tổng quát (CBC)', N'Kiểm tra các thành phần hồng cầu, bạch cầu, tiểu cầu', 150000, N'Xét nghiệm máu', 1),
(N'Xét nghiệm đường huyết (Glucose)', N'Kiểm tra chỉ số đường huyết lúc đói', 80000, N'Sinh hóa', 1),
(N'Xét nghiệm chức năng gan (ALT, AST)', N'Đánh giá tổn thương và chức năng tế bào gan', 200000, N'Sinh hóa', 1),
(N'Xét nghiệm chức năng thận (Urea, Creatinine)', N'Đánh giá khả năng lọc của thận', 180000, N'Sinh hóa', 1),
(N'Chụp X-quang ngực thẳng', N'Chẩn đoán tổn thương phổi, tim và lồng ngực', 250000, N'Chẩn đoán hình ảnh', 1),
(N'Siêu âm tổng quát bụng', N'Tầm soát các cơ quan trong ổ bụng', 300000, N'Chẩn đoán hình ảnh', 1),
(N'Điện tâm đồ (ECG)', N'Đo hoạt động điện của tim', 120000, N'Thăm dò chức năng', 1);

-- 9. NOTIFICATIONS (THÔNG BÁO TRONG ỨNG DỤNG)
CREATE TABLE Notifications
(
    NotificationId INT IDENTITY(1,1) PRIMARY KEY,
    UserId INT NOT NULL,
    Title NVARCHAR(200) NOT NULL,
    Message NVARCHAR(500) NOT NULL,
    Type VARCHAR(30) NOT NULL,       -- Appointment, LabTest, MedicalRecord, Payment
    RelatedEntityId INT NULL,
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

    CONSTRAINT FK_Notifications_Users FOREIGN KEY(UserId) REFERENCES Users(UserId)
);