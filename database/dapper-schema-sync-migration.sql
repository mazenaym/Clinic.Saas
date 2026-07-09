/*
Dapper schema sync migration

Run this on existing developer/staging databases after the older migration scripts.
It is idempotent: every ALTER is guarded so the script can be run more than once.
*/

IF OBJECT_ID(N'dbo.SubscriptionPayments', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.SubscriptionPayments', 'Notes') IS NULL
        ALTER TABLE dbo.SubscriptionPayments ADD Notes NVARCHAR(500) NULL;

    IF COL_LENGTH('dbo.SubscriptionPayments', 'CreatedByUserId') IS NULL
        ALTER TABLE dbo.SubscriptionPayments ADD CreatedByUserId UNIQUEIDENTIFIER NULL;
END;

IF OBJECT_ID(N'dbo.PlatformSettings', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.PlatformSettings', 'UpdatedByUserId') IS NULL
        ALTER TABLE dbo.PlatformSettings ADD UpdatedByUserId UNIQUEIDENTIFIER NULL;
END;

IF OBJECT_ID(N'dbo.Payments', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Payments', 'RemainingAmount') IS NULL
        ALTER TABLE dbo.Payments
        ADD RemainingAmount DECIMAL(18,2) NOT NULL
            CONSTRAINT DF_Payments_RemainingAmount DEFAULT 0;

    IF COL_LENGTH('dbo.Payments', 'RefundedAt') IS NULL
        ALTER TABLE dbo.Payments ADD RefundedAt DATETIME2 NULL;

    IF COL_LENGTH('dbo.Payments', 'RowVersion') IS NULL
        ALTER TABLE dbo.Payments ADD RowVersion ROWVERSION NOT NULL;
END;

IF OBJECT_ID(N'dbo.PaymentItems', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.PaymentItems', 'TotalPrice') IS NULL
        ALTER TABLE dbo.PaymentItems
        ADD TotalPrice DECIMAL(18,2) NOT NULL
            CONSTRAINT DF_PaymentItems_TotalPrice DEFAULT 0;
END;

IF OBJECT_ID(N'dbo.Tenants', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Tenants', 'SubscriptionState') IS NULL
        ALTER TABLE dbo.Tenants ADD SubscriptionState NVARCHAR(30) NOT NULL
            CONSTRAINT DF_Tenants_SubscriptionState DEFAULT N'Trial';

    IF COL_LENGTH('dbo.Tenants', 'TrialEndsAt') IS NULL
        ALTER TABLE dbo.Tenants ADD TrialEndsAt DATETIME2 NULL;
END;

IF OBJECT_ID(N'dbo.Visits', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Visits', 'FinalizedAt') IS NULL
        ALTER TABLE dbo.Visits ADD FinalizedAt DATETIME2 NULL;

    IF COL_LENGTH('dbo.Visits', 'FinalizedBy') IS NULL
        ALTER TABLE dbo.Visits ADD FinalizedBy UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH('dbo.Visits', 'RowVersion') IS NULL
        ALTER TABLE dbo.Visits ADD RowVersion ROWVERSION NOT NULL;
END;

IF OBJECT_ID(N'dbo.Patients', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Patients', 'RowVersion') IS NULL
        ALTER TABLE dbo.Patients ADD RowVersion ROWVERSION NOT NULL;
END;

IF OBJECT_ID(N'dbo.Appointments', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Appointments', 'RowVersion') IS NULL
        ALTER TABLE dbo.Appointments ADD RowVersion ROWVERSION NOT NULL;
END;

IF OBJECT_ID(N'dbo.Prescriptions', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.Prescriptions', 'RowVersion') IS NULL
        ALTER TABLE dbo.Prescriptions ADD RowVersion ROWVERSION NOT NULL;
END;

IF OBJECT_ID(N'dbo.PatientDocuments', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.PatientDocuments', 'RowVersion') IS NULL
        ALTER TABLE dbo.PatientDocuments ADD RowVersion ROWVERSION NOT NULL;
END;
