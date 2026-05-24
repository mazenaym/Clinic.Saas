/*
ClinicFlow feature pack migration
Run after database/schema.sql on existing databases.
*/

IF COL_LENGTH('dbo.Tenants', 'TrialEndsAt') IS NULL
    ALTER TABLE dbo.Tenants ADD TrialEndsAt DATETIME2 NULL;

IF COL_LENGTH('dbo.Tenants', 'SubscriptionState') IS NULL
    ALTER TABLE dbo.Tenants ADD SubscriptionState NVARCHAR(30) NOT NULL CONSTRAINT DF_Tenants_SubscriptionState DEFAULT N'Trial';

IF COL_LENGTH('dbo.Tenants', 'MaxUsers') IS NULL
    ALTER TABLE dbo.Tenants ADD MaxUsers INT NOT NULL CONSTRAINT DF_Tenants_MaxUsers DEFAULT 2;

IF COL_LENGTH('dbo.Tenants', 'MaxPatientsPerMonth') IS NULL
    ALTER TABLE dbo.Tenants ADD MaxPatientsPerMonth INT NOT NULL CONSTRAINT DF_Tenants_MaxPatients DEFAULT 200;

IF OBJECT_ID('dbo.AuditLogs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuditLogs PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NULL,
        UserId UNIQUEIDENTIFIER NULL,
        Action NVARCHAR(100) NOT NULL,
        EntityName NVARCHAR(100) NOT NULL,
        EntityId UNIQUEIDENTIFIER NULL,
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        IpAddress NVARCHAR(100) NULL,
        UserAgent NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT SYSUTCDATETIME()
    );

    CREATE INDEX IX_AuditLogs_Tenant_CreatedAt ON dbo.AuditLogs(TenantId, CreatedAt DESC);
END

IF OBJECT_ID('dbo.PatientDocuments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PatientDocuments
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PatientDocuments PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        PatientId UNIQUEIDENTIFIER NOT NULL,
        VisitId UNIQUEIDENTIFIER NULL,
        FileName NVARCHAR(255) NOT NULL,
        FileUrl NVARCHAR(1000) NOT NULL,
        FileSizeKb INT NULL,
        FileType NVARCHAR(100) NOT NULL,
        DocumentType SMALLINT NOT NULL,
        Description NVARCHAR(500) NULL,
        UploadedBy UNIQUEIDENTIFIER NULL,
        UploadedAt DATETIME2 NOT NULL,
        CONSTRAINT FK_PatientDocuments_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_PatientDocuments_Patients FOREIGN KEY (PatientId) REFERENCES dbo.Patients(Id)
    );

    CREATE INDEX IX_PatientDocuments_Tenant_Patient ON dbo.PatientDocuments(TenantId, PatientId, UploadedAt DESC);
END

IF OBJECT_ID('dbo.Drugs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Drugs
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Drugs PRIMARY KEY,
        TradeName NVARCHAR(200) NOT NULL,
        GenericName NVARCHAR(200) NOT NULL,
        Category NVARCHAR(100) NULL,
        Strength NVARCHAR(100) NULL,
        Form NVARCHAR(100) NULL,
        Unit NVARCHAR(50) NULL,
        Contraindications NVARCHAR(MAX) NULL,
        Interactions NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Drugs_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL
    );

    CREATE INDEX IX_Drugs_Search ON dbo.Drugs(IsActive, TradeName, GenericName);
END

IF OBJECT_ID('dbo.OnlineBookings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OnlineBookings
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OnlineBookings PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        AppointmentId UNIQUEIDENTIFIER NULL,
        PatientName NVARCHAR(200) NOT NULL,
        PatientPhone NVARCHAR(50) NOT NULL,
        PatientEmail NVARCHAR(256) NULL,
        RequestedDate DATE NOT NULL,
        RequestedTime TIME NOT NULL,
        DoctorId UNIQUEIDENTIFIER NULL,
        Complaint NVARCHAR(1000) NULL,
        [Status] SMALLINT NOT NULL,
        ConfirmCode NVARCHAR(20) NOT NULL,
        RejectReason NVARCHAR(500) NULL,
        CreatedAt DATETIME2 NOT NULL,
        CONSTRAINT FK_OnlineBookings_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
    );

    CREATE INDEX IX_OnlineBookings_Tenant_Status ON dbo.OnlineBookings(TenantId, [Status], CreatedAt DESC);
END

IF OBJECT_ID('dbo.ClinicalTemplates', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ClinicalTemplates
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ClinicalTemplates PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Specialty NVARCHAR(100) NULL,
        ChiefComplaint NVARCHAR(1000) NULL,
        ClinicalNotes NVARCHAR(MAX) NULL,
        Diagnosis NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ClinicalTemplates_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL,
        UpdatedAt DATETIME2 NOT NULL,
        CONSTRAINT FK_ClinicalTemplates_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
    );
END

IF COL_LENGTH('dbo.Visits', 'FinalizedAt') IS NULL
    ALTER TABLE dbo.Visits ADD FinalizedAt DATETIME2 NULL;

IF COL_LENGTH('dbo.Visits', 'FinalizedBy') IS NULL
    ALTER TABLE dbo.Visits ADD FinalizedBy UNIQUEIDENTIFIER NULL;

IF COL_LENGTH('dbo.Payments', 'RefundedAt') IS NULL
    ALTER TABLE dbo.Payments ADD RefundedAt DATETIME2 NULL;
