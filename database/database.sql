-- =========================================================================================
-- HỆ THỐNG QUẢN LÝ BỆNH VIỆN & PHÒNG KHÁM (CLINICFLOW / HOSPITAL MANAGEMENT SYSTEM)
-- FULL DATABASE SCRIPT - CẬP NHẬT HOÀN CHỈNH TẤT CẢ CÁC BẢNG & SCRIPT PHÁT SINH
-- =========================================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'HospitalManagementDB')
BEGIN
    CREATE DATABASE HospitalManagementDB;
END
GO

USE HospitalManagementDB;
GO

-- ====================================
-- 1. BẢNG ROLES (VAI TRÒ NGƯỜI DÙNG)
-- ====================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Roles' AND xtype='U')
BEGIN
    CREATE TABLE Roles
    (
        RoleId INT IDENTITY(1,1) PRIMARY KEY,
        RoleName VARCHAR(50) NOT NULL UNIQUE
    );
END
GO

-- ====================================
-- 2. BẢNG USERS (TÀI KHOẢN ĐĂNG NHẬP)
-- ====================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U')
BEGIN
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
END
GO

-- ====================================
-- 3. BẢNG STAFF (NHÂN VIÊN Y TẾ / TIẾP TÂN / THU NGÂN)
-- ====================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Staff' AND xtype='U')
BEGIN
    CREATE TABLE Staff
    (
        StaffId INT IDENTITY(1,1) PRIMARY KEY,
        FullName NVARCHAR(100) NOT NULL,
        Phone VARCHAR(20) NULL,
        Email VARCHAR(100) NULL,
        Position NVARCHAR(100) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- ====================================
-- 4. BẢNG DOCTORS (BÁC SĨ)
-- ====================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Doctors' AND xtype='U')
BEGIN
    CREATE TABLE Doctors
    (
        DoctorId INT IDENTITY(1,1) PRIMARY KEY,
        FullName NVARCHAR(100) NOT NULL,
        Phone VARCHAR(20) NULL,
        Email VARCHAR(100) NULL,
        Specialization NVARCHAR(100) NOT NULL,
        ExperienceYears INT DEFAULT 0,
        Description NVARCHAR(1000) NULL
    );
END
GO

-- ====================================
-- 5. BẢNG PATIENTS (BỆNH NHÂN)
-- ====================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Patients' AND xtype='U')
BEGIN
    CREATE TABLE Patients
    (
        PatientId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NULL, -- Liên kết với tài khoản User (Cổng thông tin bệnh nhân)
        FullName NVARCHAR(100) NOT NULL,
        Phone VARCHAR(20) NULL,
        DateOfBirth DATE NULL,
        Gender NVARCHAR(10) NULL,
        Address NVARCHAR(255) NULL,
        BloodType NVARCHAR(10) NULL,
        EmergencyContactName NVARCHAR(100) NULL,
        EmergencyContactPhone VARCHAR(20) NULL,
        CONSTRAINT FK_Patients_Users FOREIGN KEY(UserId) REFERENCES Users(UserId) ON DELETE SET NULL
    );
END
GO

-- ====================================
-- 6. BẢNG APPOINTMENTS (LỊCH KHÁM BỆNH)
-- ====================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Appointments' AND xtype='U')
BEGIN
    CREATE TABLE Appointments
    (
        AppointmentId INT IDENTITY(1,1) PRIMARY KEY,
        PatientId INT NOT NULL,
        DoctorId INT NOT NULL,
        StaffId INT NULL,
        AppointmentDate DATE NOT NULL,
        AppointmentTime TIME NOT NULL,
        Reason NVARCHAR(500) NULL,
        Status VARCHAR(20) NOT NULL DEFAULT 'Pending',
        CheckInTime DATETIME NULL,        -- Thời điểm bệnh nhân check-in tại phòng khám
        QueuePriorityTime DATETIME NULL,  -- Thời điểm sắp xếp thứ tự hàng chờ
        IsWalkIn BIT NOT NULL DEFAULT 0,  -- 1: Khách vãng lai trực tiếp, 0: Đặt trước
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

        CONSTRAINT FK_Appointments_Patients FOREIGN KEY(PatientId) REFERENCES Patients(PatientId),
        CONSTRAINT FK_Appointments_Doctors FOREIGN KEY(DoctorId) REFERENCES Doctors(DoctorId),
        CONSTRAINT FK_Appointments_Staff FOREIGN KEY(StaffId) REFERENCES Staff(StaffId)
    );
END
GO

-- ====================================
-- 7. BẢNG MEDICAL RECORDS (HỒ SƠ BỆNH ÁN)
-- ====================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='MedicalRecords' AND xtype='U')
BEGIN
    CREATE TABLE MedicalRecords
    (
        MedicalRecordId INT IDENTITY(1,1) PRIMARY KEY,
        AppointmentId INT NOT NULL UNIQUE,
        Symptoms NVARCHAR(MAX) NULL,
        Diagnosis NVARCHAR(MAX) NULL,
        Treatment NVARCHAR(MAX) NULL,
        Notes NVARCHAR(MAX) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

        CONSTRAINT FK_MedicalRecords_Appointments FOREIGN KEY(AppointmentId) REFERENCES Appointments(AppointmentId)
    );
END
GO

-- ====================================
-- 8. BẢNG PRESCRIPTIONS (ĐƠN THUỐC)
-- ====================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Prescriptions' AND xtype='U')
BEGIN
    CREATE TABLE Prescriptions
    (
        PrescriptionId INT IDENTITY(1,1) PRIMARY KEY,
        MedicalRecordId INT NOT NULL,
        MedicineName NVARCHAR(100) NOT NULL,
        Dosage NVARCHAR(100) NULL,
        Quantity INT NULL,
        Instruction NVARCHAR(500) NULL,

        CONSTRAINT FK_Prescriptions_MedicalRecords FOREIGN KEY(MedicalRecordId) REFERENCES MedicalRecords(MedicalRecordId)
    );
END
GO

-- ====================================
-- 9. BẢNG LAB TEST SERVICES (DANH MỤC DỊCH VỤ XÉT NGHIỆM)
-- ====================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='LabTestServices' AND xtype='U')
BEGIN
    CREATE TABLE LabTestServices
    (
        LabTestServiceId INT IDENTITY(1,1) PRIMARY KEY,
        ServiceName NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500) NULL,
        Price DECIMAL(18,2) NOT NULL DEFAULT 0,
        Category NVARCHAR(100) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
END
GO

-- ====================================
-- 10. BẢNG APPOINTMENT LAB TESTS (YÊU CẦU & KẾT QUẢ XÉT NGHIỆM)
-- ====================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AppointmentLabTests' AND xtype='U')
BEGIN
    CREATE TABLE AppointmentLabTests
    (
        AppointmentLabTestId INT IDENTITY(1,1) PRIMARY KEY,
        AppointmentId INT NOT NULL,
        LabTestServiceId INT NOT NULL,
        DoctorId INT NULL,
        TestDate DATETIME NULL,
        Result NVARCHAR(MAX) NULL,
        ResultValues NVARCHAR(MAX) NULL, -- Dữ liệu chỉ số chi tiết (JSON / Chuỗi giá trị)
        Status VARCHAR(20) NOT NULL DEFAULT 'Pending',
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),

        CONSTRAINT FK_AppointmentLabTests_Appointments FOREIGN KEY(AppointmentId) REFERENCES Appointments(AppointmentId),
        CONSTRAINT FK_AppointmentLabTests_LabTestServices FOREIGN KEY(LabTestServiceId) REFERENCES LabTestServices(LabTestServiceId),
        CONSTRAINT FK_AppointmentLabTests_Doctors FOREIGN KEY(DoctorId) REFERENCES Doctors(DoctorId)
    );
END
GO

-- ====================================
-- 11. BẢNG APPOINTMENT BILLS (HÓA ĐƠN KHÁM BỆNH & DỊCH VỤ)
-- ====================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AppointmentBills' AND xtype='U')
BEGIN
    CREATE TABLE AppointmentBills
    (
        BillId INT IDENTITY(1,1) PRIMARY KEY,
        AppointmentId INT NOT NULL,
        PatientId INT NOT NULL,
        StaffId INT NULL,
        ExaminationFee DECIMAL(18,2) NOT NULL DEFAULT 0,
        LabTestFee DECIMAL(18,2) NOT NULL DEFAULT 0,
        TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        Status VARCHAR(20) NOT NULL DEFAULT 'Pending',
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        PaidAt DATETIME NULL,
        Notes NVARCHAR(500) NULL,

        CONSTRAINT FK_AppointmentBills_Appointments FOREIGN KEY(AppointmentId) REFERENCES Appointments(AppointmentId),
        CONSTRAINT FK_AppointmentBills_Patients FOREIGN KEY(PatientId) REFERENCES Patients(PatientId),
        CONSTRAINT FK_AppointmentBills_Staff FOREIGN KEY(StaffId) REFERENCES Staff(StaffId)
    );
END
GO

-- ====================================
-- 12. BẢNG PAYMENTS (LỊCH SỬ THANH TOÁN)
-- ====================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Payments' AND xtype='U')
BEGIN
    CREATE TABLE Payments
    (
        PaymentId INT IDENTITY(1,1) PRIMARY KEY,
        PatientId INT NOT NULL,
        AppointmentId INT NULL,
        Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
        Reason NVARCHAR(500) NULL,
        Status VARCHAR(20) NOT NULL DEFAULT 'Pending',
        RequestDate DATETIME NOT NULL DEFAULT GETDATE(),
        PaidDate DATETIME NULL,

        CONSTRAINT FK_Payments_Patients FOREIGN KEY(PatientId) REFERENCES Patients(PatientId),
        CONSTRAINT FK_Payments_Appointments FOREIGN KEY(AppointmentId) REFERENCES Appointments(AppointmentId)
    );
END
GO

-- ====================================
-- 13. BẢNG NOTIFICATIONS (THÔNG BÁO)
-- ====================================
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Notifications' AND xtype='U')
BEGIN
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
END
GO

-- =========================================================================================
-- SEED DATA (DỮ LIỆU MẪU BAN ĐẦU)
-- =========================================================================================

-- 1. Insert Roles
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Admin')
    INSERT INTO Roles (RoleName) VALUES ('Admin');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Doctor')
    INSERT INTO Roles (RoleName) VALUES ('Doctor');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Staff')
    INSERT INTO Roles (RoleName) VALUES ('Staff');
IF NOT EXISTS (SELECT 1 FROM Roles WHERE RoleName = 'Patient')
    INSERT INTO Roles (RoleName) VALUES ('Patient');

-- 2. Insert LabTestServices
IF NOT EXISTS (SELECT 1 FROM LabTestServices)
BEGIN
    INSERT INTO LabTestServices (ServiceName, Description, Price, Category, IsActive) VALUES
    (N'Xét nghiệm máu tổng quát (CBC)', N'Kiểm tra các thành phần hồng cầu, bạch cầu, tiểu cầu', 150000, N'Xét nghiệm máu', 1),
    (N'Xét nghiệm đường huyết (Glucose)', N'Kiểm tra chỉ số đường huyết lúc đói', 80000, N'Sinh hóa', 1),
    (N'Xét nghiệm chức năng gan (ALT, AST)', N'Đánh giá tổn thương và chức năng tế bào gan', 200000, N'Sinh hóa', 1),
    (N'Xét nghiệm chức năng thận (Urea, Creatinine)', N'Đánh giá khả năng lọc của thận', 180000, N'Sinh hóa', 1),
    (N'Chụp X-quang ngực thẳng', N'Chẩn đoán tổn thương phổi, tim và lồng ngực', 250000, N'Chẩn đoán hình ảnh', 1),
    (N'Siêu âm tổng quát bụng', N'Tầm soát các cơ quan trong ổ bụng', 300000, N'Chẩn đoán hình ảnh', 1),
    (N'Điện tâm đồ (ECG)', N'Đo hoạt động điện của tim', 120000, N'Thăm dò chức năng', 1);
END
GO

PRINT 'Full Database Script executed successfully!';