USE HospitalManagementDB_V1;
GO

-- Clear existing data (in reverse order of dependencies)
DELETE FROM Prescriptions;
DELETE FROM MedicalRecords;
DELETE FROM Appointments;
DELETE FROM Patients;
DELETE FROM Doctors;
DELETE FROM Users;
DELETE FROM Roles;
GO

-- Reset Identity Seeds (optional but helpful if table is not brand-new)
-- If the table is empty and has never had inserts, DBCC CHECKIDENT with RESEED 1 might make it start from 1.
-- But using SCOPE_IDENTITY() makes this completely independent of seed values!
DBCC CHECKIDENT ('Prescriptions', RESEED, 0);
DBCC CHECKIDENT ('MedicalRecords', RESEED, 0);
DBCC CHECKIDENT ('Appointments', RESEED, 0);
DBCC CHECKIDENT ('Patients', RESEED, 0);
DBCC CHECKIDENT ('Doctors', RESEED, 0);
DBCC CHECKIDENT ('Users', RESEED, 0);
DBCC CHECKIDENT ('Roles', RESEED, 0);
GO

-- Variables to hold dynamic IDs
DECLARE @AdminRoleId INT, @DoctorRoleId INT, @PatientRoleId INT;
DECLARE @UserAdminId INT, @UserDoctorAId INT, @UserDoctorBId INT, @UserPatientAId INT, @UserPatientBId INT;
DECLARE @DoctorAId INT, @DoctorBId INT;
DECLARE @PatientAId INT, @PatientBId INT;
DECLARE @AppointmentAId INT;
DECLARE @MedicalRecordAId INT;

-- 1. Insert Roles
INSERT INTO Roles (RoleName) VALUES ('Admin');
SET @AdminRoleId = SCOPE_IDENTITY();

INSERT INTO Roles (RoleName) VALUES ('Doctor');
SET @DoctorRoleId = SCOPE_IDENTITY();

INSERT INTO Roles (RoleName) VALUES ('Patient');
SET @PatientRoleId = SCOPE_IDENTITY();


-- 2. Insert Users (PasswordHash is SHA256 of '123456': 8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92)
INSERT INTO Users (FullName, Username, PasswordHash, Phone, RoleId, IsActive, CreatedAt) VALUES
(N'System Administrator', 'admin', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0911111111', @AdminRoleId, 1, GETDATE());
SET @UserAdminId = SCOPE_IDENTITY();

INSERT INTO Users (FullName, Username, PasswordHash, Phone, RoleId, IsActive, CreatedAt) VALUES
(N'Dr. Nguyen Van A', 'doctora', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0922222222', @DoctorRoleId, 1, GETDATE());
SET @UserDoctorAId = SCOPE_IDENTITY();

INSERT INTO Users (FullName, Username, PasswordHash, Phone, RoleId, IsActive, CreatedAt) VALUES
(N'Dr. Tran Thi B', 'doctorb', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0933333333', @DoctorRoleId, 1, GETDATE());
SET @UserDoctorBId = SCOPE_IDENTITY();

INSERT INTO Users (FullName, Username, PasswordHash, Phone, RoleId, IsActive, CreatedAt) VALUES
(N'Pham Minh Hoang', 'hoangpham', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0944444444', @PatientRoleId, 1, GETDATE());
SET @UserPatientAId = SCOPE_IDENTITY();

INSERT INTO Users (FullName, Username, PasswordHash, Phone, RoleId, IsActive, CreatedAt) VALUES
(N'Le Thanh Thao', 'thaole', '8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92', '0955555555', @PatientRoleId, 1, GETDATE());
SET @UserPatientBId = SCOPE_IDENTITY();


-- 3. Insert Doctors
INSERT INTO Doctors (UserId, Specialization, ExperienceYears, Description) VALUES
(@UserDoctorAId, N'Cardiology', 10, N'Expert in cardiac diseases and heart surgery.');
SET @DoctorAId = SCOPE_IDENTITY();

INSERT INTO Doctors (UserId, Specialization, ExperienceYears, Description) VALUES
(@UserDoctorBId, N'Pediatrics', 8, N'Dedicated child healthcare specialist.');
SET @DoctorBId = SCOPE_IDENTITY();


-- 4. Insert Patients
INSERT INTO Patients (UserId, DateOfBirth, Gender, Address, BloodType, EmergencyContactName, EmergencyContactPhone) VALUES
(@UserPatientAId, '1995-08-15', N'Male', N'123 Le Loi, District 1, HCMC', N'O+', N'Nguyen Thi C (Wife)', '0944444445');
SET @PatientAId = SCOPE_IDENTITY();

INSERT INTO Patients (UserId, DateOfBirth, Gender, Address, BloodType, EmergencyContactName, EmergencyContactPhone) VALUES
(@UserPatientBId, '2000-11-20', N'Female', N'456 Nguyen Hue, District 3, HCMC', N'A-', N'Le Van D (Father)', '0955555556');
SET @PatientBId = SCOPE_IDENTITY();


-- 5. Insert Appointments
INSERT INTO Appointments (PatientId, DoctorId, AppointmentDate, AppointmentTime, Reason, Status, CreatedAt) VALUES
(@PatientAId, @DoctorAId, CAST(GETDATE() AS DATE), '09:30:00', N'Regular chest pain and high heart rate checkup.', 'Confirmed', GETDATE());
SET @AppointmentAId = SCOPE_IDENTITY();

INSERT INTO Appointments (PatientId, DoctorId, AppointmentDate, AppointmentTime, Reason, Status, CreatedAt) VALUES
(@PatientBId, @DoctorBId, CAST(GETDATE() AS DATE), '14:00:00', N'Routine checkup for child flu symptoms.', 'Pending', GETDATE());

INSERT INTO Appointments (PatientId, DoctorId, AppointmentDate, AppointmentTime, Reason, Status, CreatedAt) VALUES
(@PatientAId, @DoctorBId, CAST(DATEADD(day, 1, GETDATE()) AS DATE), '10:00:00', N'Follow up for general physical test.', 'Pending', GETDATE());


-- 6. Insert Medical Records (Only for completed/confirmed appointments - e.g., AppointmentId = @AppointmentAId)
INSERT INTO MedicalRecords (AppointmentId, Symptoms, Diagnosis, Treatment, Notes, CreatedAt) VALUES
(@AppointmentAId, N'Mild chest pain after heavy exercises, fatigue.', N'Mild hypertension (Prehypertension stage).', N'Rest and prescription of Metoprolol 50mg.', N'Patient advised to monitor blood pressure daily.', GETDATE());
SET @MedicalRecordAId = SCOPE_IDENTITY();


-- 7. Insert Prescriptions
INSERT INTO Prescriptions (MedicalRecordId, MedicineName, Dosage, Quantity, Instruction) VALUES
(@MedicalRecordAId, N'Metoprolol 50mg', N'1 tablet daily in the morning', 30, N'Take after meal. Avoid strenuous activity.');
GO
