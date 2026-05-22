IF OBJECT_ID(N'dbo.Subscriptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Subscriptions
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Subscriptions PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        [Plan] SMALLINT NOT NULL,
        StartDate DATETIME2 NOT NULL,
        EndDate DATETIME2 NOT NULL,
        AmountPaid DECIMAL(18,2) NOT NULL,
        [Status] SMALLINT NOT NULL,
        PaymentRef NVARCHAR(200) NULL,
        Notes NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL,
        CONSTRAINT FK_Subscriptions_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Subscriptions_Tenant_EndDate' AND object_id = OBJECT_ID(N'dbo.Subscriptions'))
BEGIN
    CREATE INDEX IX_Subscriptions_Tenant_EndDate ON dbo.Subscriptions(TenantId, EndDate DESC);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Subscriptions_Status_CreatedAt' AND object_id = OBJECT_ID(N'dbo.Subscriptions'))
BEGIN
    CREATE INDEX IX_Subscriptions_Status_CreatedAt ON dbo.Subscriptions([Status], CreatedAt);
END;
