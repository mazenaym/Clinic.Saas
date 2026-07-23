/*
Real invoice module foundation.

Apply manually after review. The project currently uses SQL plan files rather
than a clear EF migrations pipeline.
*/

IF OBJECT_ID(N'dbo.Invoices', N'U') IS NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Patients') AND name = N'UX_Patients_Tenant_Id')
        CREATE UNIQUE INDEX UX_Patients_Tenant_Id ON dbo.Patients(TenantId, Id);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Visits') AND name = N'UX_Visits_Tenant_Id')
        CREATE UNIQUE INDEX UX_Visits_Tenant_Id ON dbo.Visits(TenantId, Id);

    CREATE TABLE dbo.Invoices
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_Invoices PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        PatientId UNIQUEIDENTIFIER NOT NULL,
        VisitId UNIQUEIDENTIFIER NULL,
        InvoiceNumber NVARCHAR(30) NOT NULL,
        Subtotal DECIMAL(18,2) NOT NULL,
        DiscountAmount DECIMAL(18,2) NOT NULL,
        TaxAmount DECIMAL(18,2) NOT NULL,
        GrandTotal DECIMAL(18,2) NOT NULL,
        PaidAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_Invoices_PaidAmount DEFAULT 0,
        RemainingAmount DECIMAL(18,2) NOT NULL,
        [Status] SMALLINT NOT NULL,
        Notes NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Invoices_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NOT NULL CONSTRAINT DF_Invoices_UpdatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy UNIQUEIDENTIFIER NULL,
        InsuranceCompany NVARCHAR(200) NULL,
        InsuranceNumber NVARCHAR(100) NULL,
        ReceiptUrl NVARCHAR(500) NULL,
        RowVersion ROWVERSION NOT NULL,
        CONSTRAINT UX_Invoices_Tenant_Id UNIQUE (TenantId, Id),
        CONSTRAINT FK_Invoices_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
        CONSTRAINT FK_Invoices_Patients FOREIGN KEY (TenantId, PatientId) REFERENCES dbo.Patients(TenantId, Id),
        CONSTRAINT FK_Invoices_Visits FOREIGN KEY (TenantId, VisitId) REFERENCES dbo.Visits(TenantId, Id),
        CONSTRAINT CK_Invoices_Amounts_NonNegative CHECK
        (
            Subtotal >= 0 AND DiscountAmount >= 0 AND TaxAmount >= 0 AND
            GrandTotal >= 0 AND PaidAmount >= 0 AND RemainingAmount >= 0
        )
    );

    CREATE UNIQUE INDEX UX_Invoices_Tenant_Number ON dbo.Invoices(TenantId, InvoiceNumber);
    CREATE INDEX IX_Invoices_Tenant_Patient_CreatedAt ON dbo.Invoices(TenantId, PatientId, CreatedAt DESC);
END;

IF OBJECT_ID(N'dbo.InvoiceItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvoiceItems
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_InvoiceItems PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        InvoiceId UNIQUEIDENTIFIER NOT NULL,
        ProcedureId UNIQUEIDENTIFIER NULL,
        Description NVARCHAR(200) NOT NULL,
        ServiceType SMALLINT NULL,
        Quantity INT NOT NULL,
        UnitPrice DECIMAL(18,2) NOT NULL,
        DiscountAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_InvoiceItems_DiscountAmount DEFAULT 0,
        TaxAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_InvoiceItems_TaxAmount DEFAULT 0,
        LineTotal DECIMAL(18,2) NOT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_InvoiceItems_SortOrder DEFAULT 0,
        CONSTRAINT FK_InvoiceItems_Invoices FOREIGN KEY (TenantId, InvoiceId) REFERENCES dbo.Invoices(TenantId, Id),
        CONSTRAINT CK_InvoiceItems_Amounts_NonNegative CHECK
        (
            Quantity > 0 AND UnitPrice >= 0 AND DiscountAmount >= 0 AND
            TaxAmount >= 0 AND LineTotal >= 0
        )
    );

    CREATE INDEX IX_InvoiceItems_Tenant_Invoice ON dbo.InvoiceItems(TenantId, InvoiceId, SortOrder);
END;

IF OBJECT_ID(N'dbo.InvoicePayments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InvoicePayments
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_InvoicePayments PRIMARY KEY,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        InvoiceId UNIQUEIDENTIFIER NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        PaymentMethod SMALLINT NOT NULL,
        PaymentReference NVARCHAR(200) NULL,
        Notes NVARCHAR(MAX) NULL,
        PaidAt DATETIME2 NOT NULL CONSTRAINT DF_InvoicePayments_PaidAt DEFAULT SYSUTCDATETIME(),
        CreatedBy UNIQUEIDENTIFIER NULL,
        CONSTRAINT FK_InvoicePayments_Invoices FOREIGN KEY (TenantId, InvoiceId) REFERENCES dbo.Invoices(TenantId, Id),
        CONSTRAINT CK_InvoicePayments_Amount_Positive CHECK (Amount > 0)
    );

    CREATE INDEX IX_InvoicePayments_Tenant_Invoice ON dbo.InvoicePayments(TenantId, InvoiceId, PaidAt DESC);
END;
