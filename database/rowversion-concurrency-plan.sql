/*
RowVersion concurrency plan

This project uses Dapper SQL scripts rather than a tracked EF migrations history, so
do not run this automatically against a developer database. Review and apply through
the normal database deployment path.
*/

IF COL_LENGTH('dbo.Patients', 'RowVersion') IS NULL
BEGIN
    ALTER TABLE dbo.Patients ADD RowVersion ROWVERSION NOT NULL;
END;

IF COL_LENGTH('dbo.Appointments', 'RowVersion') IS NULL
BEGIN
    ALTER TABLE dbo.Appointments ADD RowVersion ROWVERSION NOT NULL;
END;

IF COL_LENGTH('dbo.Visits', 'RowVersion') IS NULL
BEGIN
    ALTER TABLE dbo.Visits ADD RowVersion ROWVERSION NOT NULL;
END;

IF COL_LENGTH('dbo.Prescriptions', 'RowVersion') IS NULL
BEGIN
    ALTER TABLE dbo.Prescriptions ADD RowVersion ROWVERSION NOT NULL;
END;

IF COL_LENGTH('dbo.Payments', 'RowVersion') IS NULL
BEGIN
    ALTER TABLE dbo.Payments ADD RowVersion ROWVERSION NOT NULL;
END;

IF COL_LENGTH('dbo.PatientDocuments', 'RowVersion') IS NULL
BEGIN
    ALTER TABLE dbo.PatientDocuments ADD RowVersion ROWVERSION NOT NULL;
END;
