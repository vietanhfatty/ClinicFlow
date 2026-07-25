-- Create AppointmentBills table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AppointmentBills' AND xtype='U')
BEGIN
    CREATE TABLE AppointmentBills (
        BillId INT PRIMARY KEY IDENTITY(1,1),
        AppointmentId INT NOT NULL,
        PatientId INT NOT NULL,
        StaffId INT NULL,
        ExaminationFee DECIMAL(18, 2) NOT NULL DEFAULT 0,
        LabTestFee DECIMAL(18, 2) NOT NULL DEFAULT 0,
        TotalAmount DECIMAL(18, 2) NOT NULL DEFAULT 0,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Pending',
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        PaidAt DATETIME NULL,
        Notes NVARCHAR(500) NULL,
        CONSTRAINT FK_AppointmentBills_Appointments FOREIGN KEY (AppointmentId) REFERENCES Appointments(AppointmentId),
        CONSTRAINT FK_AppointmentBills_Patients FOREIGN KEY (PatientId) REFERENCES Patients(PatientId),
        CONSTRAINT FK_AppointmentBills_Staff FOREIGN KEY (StaffId) REFERENCES Staff(StaffId)
    );
    
    PRINT 'Table AppointmentBills created successfully';
END
ELSE
BEGIN
    PRINT 'Table AppointmentBills already exists';
END
