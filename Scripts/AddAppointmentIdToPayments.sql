-- Script: Add FK_Appointments to Payments table
-- Description: Add AppointmentId column and FK relationship to Payments table

-- Step 1: Add AppointmentId column if not exists
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Payments') AND name = 'AppointmentId')
BEGIN
    ALTER TABLE Payments ADD AppointmentId INT NULL;
    PRINT 'Column AppointmentId added to Payments table.';
END
ELSE
BEGIN
    PRINT 'Column AppointmentId already exists in Payments table.';
END
GO

-- Step 2: Create FK constraint if not exists
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Payments_Appointments')
BEGIN
    ALTER TABLE Payments
    ADD CONSTRAINT FK_Payments_Appointments
    FOREIGN KEY (AppointmentId) REFERENCES Appointments(AppointmentId);
    PRINT 'Foreign key FK_Payments_Appointments created.';
END
ELSE
BEGIN
    PRINT 'Foreign key FK_Payments_Appointments already exists.';
END
GO

-- Step 3: Create index on AppointmentId for better query performance
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('Payments') AND name = 'IX_Payments_AppointmentId')
BEGIN
    CREATE NONCLUSTERED INDEX IX_Payments_AppointmentId ON Payments(AppointmentId);
    PRINT 'Index IX_Payments_AppointmentId created.';
END
ELSE
BEGIN
    PRINT 'Index IX_Payments_AppointmentId already exists.';
END
GO

PRINT 'Done! Payment table now has AppointmentId foreign key.';
