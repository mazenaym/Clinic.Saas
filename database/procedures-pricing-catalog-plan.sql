/*
Procedures and pricing catalog foundation.

Apply this script manually after review. The project currently uses SQL plan
files instead of a clear EF migrations pipeline.
*/

IF OBJECT_ID(N'dbo.ProcedureCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProcedureCategories
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProcedureCategories PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Specialty NVARCHAR(100) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ProcedureCategories_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ProcedureCategories_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_ProcedureCategories_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UX_ProcedureCategories_Tenant_Id UNIQUE (TenantId, Id),
        CONSTRAINT FK_ProcedureCategories_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
    );

    CREATE UNIQUE INDEX UX_ProcedureCategories_Tenant_Name
        ON dbo.ProcedureCategories(TenantId, Name);
END;

IF OBJECT_ID(N'dbo.Procedures', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Procedures
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Procedures PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        CategoryId UNIQUEIDENTIFIER NULL,
        Name NVARCHAR(200) NOT NULL,
        Specialty NVARCHAR(100) NULL,
        DefaultPrice DECIMAL(18,2) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Procedures_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Procedures_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Procedures_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT UX_Procedures_Tenant_Id UNIQUE (TenantId, Id),
        CONSTRAINT FK_Procedures_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_Procedures_ProcedureCategories FOREIGN KEY (TenantId, CategoryId)
            REFERENCES dbo.ProcedureCategories(TenantId, Id),
        CONSTRAINT CK_Procedures_DefaultPrice_NonNegative CHECK (DefaultPrice >= 0)
    );

    CREATE UNIQUE INDEX UX_Procedures_Tenant_Name
        ON dbo.Procedures(TenantId, Name);

    CREATE INDEX IX_Procedures_Tenant_Active_Name
        ON dbo.Procedures(TenantId, IsActive, Name);
END;

IF OBJECT_ID(N'dbo.ProcedurePrices', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ProcedurePrices
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ProcedurePrices PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        ProcedureId UNIQUEIDENTIFIER NOT NULL,
        Price DECIMAL(18,2) NOT NULL,
        EffectiveFrom DATETIME2 NOT NULL CONSTRAINT DF_ProcedurePrices_EffectiveFrom DEFAULT SYSUTCDATETIME(),
        EffectiveTo DATETIME2 NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ProcedurePrices_IsActive DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ProcedurePrices_CreatedAt DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_ProcedurePrices_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_ProcedurePrices_Procedures FOREIGN KEY (TenantId, ProcedureId)
            REFERENCES dbo.Procedures(TenantId, Id),
        CONSTRAINT CK_ProcedurePrices_Price_NonNegative CHECK (Price >= 0)
    );

    CREATE INDEX IX_ProcedurePrices_Tenant_Procedure_Active
        ON dbo.ProcedurePrices(TenantId, ProcedureId, IsActive, EffectiveFrom DESC);
END;
