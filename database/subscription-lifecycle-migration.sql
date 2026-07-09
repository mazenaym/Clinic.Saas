IF COL_LENGTH('dbo.Tenants', 'SubscriptionState') IS NULL
    ALTER TABLE dbo.Tenants ADD SubscriptionState NVARCHAR(30) NOT NULL CONSTRAINT DF_Tenants_SubscriptionState DEFAULT N'Trial';

IF COL_LENGTH('dbo.Tenants', 'TrialEndsAt') IS NULL
    ALTER TABLE dbo.Tenants ADD TrialEndsAt DATETIME2 NULL;

IF OBJECT_ID('dbo.SubscriptionPlans', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SubscriptionPlans
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SubscriptionPlans PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Code NVARCHAR(80) NOT NULL,
        Description NVARCHAR(1000) NULL,
        Price DECIMAL(18,2) NOT NULL,
        Currency NVARCHAR(10) NOT NULL CONSTRAINT DF_SubscriptionPlans_Currency DEFAULT N'EGP',
        DurationDays INT NOT NULL,
        MaxUsers INT NULL,
        MaxPatients INT NULL,
        MaxDoctors INT NULL,
        FeaturesJson NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_SubscriptionPlans_IsActive DEFAULT 1,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SubscriptionPlans_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc DATETIME2 NULL
    );

    CREATE UNIQUE INDEX UX_SubscriptionPlans_Code ON dbo.SubscriptionPlans(Code);
END;

IF OBJECT_ID('dbo.TenantSubscriptions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantSubscriptions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_TenantSubscriptions PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        PlanId UNIQUEIDENTIFIER NOT NULL,
        Status SMALLINT NOT NULL,
        StartsAtUtc DATETIME2 NOT NULL,
        EndsAtUtc DATETIME2 NOT NULL,
        RenewedAtUtc DATETIME2 NULL,
        CancelledAtUtc DATETIME2 NULL,
        SuspendedAtUtc DATETIME2 NULL,
        AutoRenew BIT NOT NULL CONSTRAINT DF_TenantSubscriptions_AutoRenew DEFAULT 0,
        GracePeriodDays INT NOT NULL CONSTRAINT DF_TenantSubscriptions_GracePeriodDays DEFAULT 0,
        LastCheckedAtUtc DATETIME2 NULL,
        Notes NVARCHAR(MAX) NULL,
        CreatedByUserId UNIQUEIDENTIFIER NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_TenantSubscriptions_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc DATETIME2 NULL,
        CONSTRAINT FK_TenantSubscriptions_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_TenantSubscriptions_Plans FOREIGN KEY (PlanId) REFERENCES dbo.SubscriptionPlans(Id)
    );

    CREATE INDEX IX_TenantSubscriptions_Tenant_End ON dbo.TenantSubscriptions(TenantId, EndsAtUtc DESC);
    CREATE INDEX IX_TenantSubscriptions_Status_End ON dbo.TenantSubscriptions(Status, EndsAtUtc);
END;

IF OBJECT_ID('dbo.SubscriptionPayments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SubscriptionPayments
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SubscriptionPayments PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        SubscriptionId UNIQUEIDENTIFIER NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Currency NVARCHAR(10) NOT NULL,
        PaymentStatus SMALLINT NOT NULL,
        PaymentMethod NVARCHAR(100) NULL,
        ReferenceNumber NVARCHAR(200) NULL,
        PaidAtUtc DATETIME2 NULL,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SubscriptionPayments_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_SubscriptionPayments_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_SubscriptionPayments_TenantSubscriptions FOREIGN KEY (SubscriptionId) REFERENCES dbo.TenantSubscriptions(Id)
    );

    CREATE INDEX IX_SubscriptionPayments_Tenant_CreatedAt ON dbo.SubscriptionPayments(TenantId, CreatedAtUtc DESC);
END;

IF OBJECT_ID('dbo.PlatformSettings', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlatformSettings
    (
        [Key] NVARCHAR(100) NOT NULL CONSTRAINT PK_PlatformSettings PRIMARY KEY,
        [Value] NVARCHAR(1000) NOT NULL,
        UpdatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PlatformSettings_UpdatedAtUtc DEFAULT SYSUTCDATETIME()
    );
END;

IF OBJECT_ID('dbo.PlatformNotifications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlatformNotifications
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PlatformNotifications PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NULL,
        Type NVARCHAR(80) NOT NULL,
        Message NVARCHAR(1000) NOT NULL,
        IsRead BIT NOT NULL CONSTRAINT DF_PlatformNotifications_IsRead DEFAULT 0,
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PlatformNotifications_CreatedAtUtc DEFAULT SYSUTCDATETIME()
    );
END;

MERGE dbo.SubscriptionPlans AS target
USING (VALUES
    (CONVERT(uniqueidentifier, '11111111-1111-1111-1111-111111111111'), N'Trial', N'TRIAL', N'14 day trial plan', 0.00, N'EGP', 14, 5, 200, 2, N'["trial"]', 1),
    (CONVERT(uniqueidentifier, '22222222-2222-2222-2222-222222222222'), N'Basic Monthly', N'BASIC_MONTHLY', N'Basic monthly clinic subscription', 999.00, N'EGP', 30, 10, 1000, 5, N'["appointments","patients","billing"]', 1),
    (CONVERT(uniqueidentifier, '33333333-3333-3333-3333-333333333333'), N'Pro Monthly', N'PRO_MONTHLY', N'Professional monthly clinic subscription', 1999.00, N'EGP', 30, 30, 5000, 15, N'["appointments","patients","billing","reports","online-booking"]', 1),
    (CONVERT(uniqueidentifier, '44444444-4444-4444-4444-444444444444'), N'Annual', N'ANNUAL', N'Annual clinic subscription', 19999.00, N'EGP', 365, NULL, NULL, NULL, N'["all"]', 1)
) AS source (Id, Name, Code, Description, Price, Currency, DurationDays, MaxUsers, MaxPatients, MaxDoctors, FeaturesJson, IsActive)
ON target.Code = source.Code
WHEN MATCHED THEN UPDATE SET
    Name = source.Name,
    Description = source.Description,
    Price = source.Price,
    Currency = source.Currency,
    DurationDays = source.DurationDays,
    MaxUsers = source.MaxUsers,
    MaxPatients = source.MaxPatients,
    MaxDoctors = source.MaxDoctors,
    FeaturesJson = source.FeaturesJson,
    IsActive = source.IsActive,
    UpdatedAtUtc = SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT
    (Id, Name, Code, Description, Price, Currency, DurationDays, MaxUsers, MaxPatients, MaxDoctors, FeaturesJson, IsActive, CreatedAtUtc)
VALUES
    (source.Id, source.Name, source.Code, source.Description, source.Price, source.Currency, source.DurationDays, source.MaxUsers, source.MaxPatients, source.MaxDoctors, source.FeaturesJson, source.IsActive, SYSUTCDATETIME());

MERGE dbo.PlatformSettings AS target
USING (VALUES
    (N'DefaultTrialDays', N'14'),
    (N'DefaultGracePeriodDays', N'0'),
    (N'Currency', N'EGP'),
    (N'SupportEmail', N'support@clinicflow.local'),
    (N'SupportPhone', N''),
    (N'SubscriptionExpiryWarningDays', N'7,3'),
    (N'MaintenanceMode', N'false')
) AS source ([Key], [Value])
ON target.[Key] = source.[Key]
WHEN NOT MATCHED THEN INSERT ([Key], [Value], UpdatedAtUtc) VALUES (source.[Key], source.[Value], SYSUTCDATETIME());

INSERT INTO dbo.TenantSubscriptions
(Id, TenantId, PlanId, Status, StartsAtUtc, EndsAtUtc, AutoRenew, GracePeriodDays, Notes, CreatedAtUtc)
SELECT NEWID(),
       s.TenantId,
       p.Id,
       s.Status,
       s.StartDate,
       s.EndDate,
       0,
       TRY_CAST((SELECT [Value] FROM dbo.PlatformSettings WHERE [Key] = N'DefaultGracePeriodDays') AS int),
       s.Notes,
       s.CreatedAt
FROM dbo.Subscriptions s
CROSS APPLY (
    SELECT TOP (1) Id
    FROM dbo.SubscriptionPlans p
    WHERE (s.[Plan] = 1 AND p.Code IN (N'TRIAL', N'BASIC_MONTHLY'))
       OR (s.[Plan] = 2 AND p.Code = N'PRO_MONTHLY')
       OR (s.[Plan] = 3 AND p.Code = N'ANNUAL')
    ORDER BY CASE WHEN s.Status = 4 AND p.Code = N'TRIAL' THEN 0 ELSE 1 END
) p
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.TenantSubscriptions existing
    WHERE existing.TenantId = s.TenantId
      AND existing.StartsAtUtc = s.StartDate
      AND existing.EndsAtUtc = s.EndDate
);

UPDATE t
SET SubscriptionState = CASE
        WHEN latest.Status = 4 THEN N'Trial'
        WHEN latest.Status = 1 THEN N'Active'
        WHEN latest.Status = 5 THEN N'PastDue'
        WHEN latest.Status = 6 THEN N'Suspended'
        WHEN latest.Status = 2 THEN N'Expired'
        WHEN latest.Status = 3 THEN N'Cancelled'
        ELSE SubscriptionState
    END,
    TrialEndsAt = CASE WHEN latest.Status = 4 THEN latest.EndsAtUtc ELSE TrialEndsAt END,
    IsActive = CASE WHEN latest.Status IN (2, 3, 6) THEN 0 ELSE IsActive END,
    UpdatedAt = SYSUTCDATETIME()
FROM dbo.Tenants t
OUTER APPLY (
    SELECT TOP (1) *
    FROM dbo.TenantSubscriptions s
    WHERE s.TenantId = t.Id
    ORDER BY s.EndsAtUtc DESC, s.CreatedAtUtc DESC
) latest
WHERE latest.Id IS NOT NULL;
