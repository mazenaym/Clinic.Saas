-- =============================================================================
-- Migration: Merge Payment/PaymentItem into Invoice/InvoiceItem/InvoicePayment
-- Date: 2026-07-20
-- Description: Migrates all data from the legacy Payments/PaymentItems tables
--              into the new Invoices/InvoiceItems tables, then provides
--              commented-out DROP statements for cleanup after verification.
-- =============================================================================

-- =============================================================================
-- STEP 1: Add InsuranceCompany, InsuranceNumber, ReceiptUrl columns to Invoices
-- (These columns are new on the Invoice entity to support insurance billing)
-- =============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Invoices') AND name = 'InsuranceCompany')
BEGIN
    ALTER TABLE dbo.Invoices ADD InsuranceCompany NVARCHAR(200) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Invoices') AND name = 'InsuranceNumber')
BEGIN
    ALTER TABLE dbo.Invoices ADD InsuranceNumber NVARCHAR(100) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Invoices') AND name = 'ReceiptUrl')
BEGIN
    ALTER TABLE dbo.Invoices ADD ReceiptUrl NVARCHAR(500) NULL;
END
GO

-- STEP 2: Create migration log table to track which Payments have been migrated
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PaymentToInvoiceMigrationLog')
BEGIN
    CREATE TABLE dbo.PaymentToInvoiceMigrationLog (
        PaymentId UNIQUEIDENTIFIER PRIMARY KEY,
        InvoiceId UNIQUEIDENTIFIER NOT NULL,
        MigratedAt DATETIME2 DEFAULT SYSUTCDATETIME()
    );
END
GO

-- Clean previous failed run (safe to re-run)
DELETE FROM dbo.PaymentToInvoiceMigrationLog;
GO

-- STEP 3: Migrate Payments into Invoices
-- Status mapping:
--   PaymentStatus.Pending(1)    -> InvoiceStatus.Draft(1)
--   PaymentStatus.Partial(2)    -> InvoiceStatus.PartiallyPaid(2)
--   PaymentStatus.Paid(3)       -> InvoiceStatus.Paid(3)
--   PaymentStatus.Refunded(4)   -> InvoiceStatus.Refunded(5)
--   PaymentStatus.Cancelled(5)  -> InvoiceStatus.Cancelled(4)

INSERT INTO dbo.Invoices
(
    Id, TenantId, PatientId, VisitId, InvoiceNumber, Subtotal, DiscountAmount,
    TaxAmount, GrandTotal, PaidAmount, RemainingAmount, Status, Notes,
    CreatedAt, UpdatedAt, CreatedBy,
    InsuranceCompany, InsuranceNumber, ReceiptUrl
)
SELECT
    p.Id,
    p.TenantId,
    p.PatientId,
    p.VisitId,
    p.InvoiceNumber,
    p.TotalAmount,           -- Map TotalAmount -> Subtotal
    p.DiscountAmount,
    p.TaxAmount,
    p.TotalAmount,           -- GrandTotal = TotalAmount (before payments)
    p.PaidAmount,
    CASE WHEN p.RemainingAmount < 0 THEN 0 ELSE p.RemainingAmount END,
    CASE p.Status
        WHEN 1 THEN 1   -- Pending -> Draft
        WHEN 2 THEN 2   -- Partial -> PartiallyPaid
        WHEN 3 THEN 3   -- Paid -> Paid
        WHEN 4 THEN 5   -- Refunded -> Refunded
        WHEN 5 THEN 4   -- Cancelled -> Cancelled
        ELSE 1          -- Default to Draft
    END,
    p.Notes,
    p.CreatedAt,
    p.UpdatedAt,
    p.CreatedBy,
    p.InsuranceCompany,
    p.InsuranceNumber,
    p.ReceiptUrl
FROM dbo.Payments p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Invoices i WHERE i.InvoiceNumber = p.InvoiceNumber AND i.TenantId = p.TenantId);

-- STEP 4: Migrate PaymentItems into InvoiceItems
-- Note: PaymentItem uses DiscountPct (percentage), InvoiceItem uses DiscountAmount (absolute)
-- We calculate the absolute discount from the percentage

INSERT INTO dbo.InvoiceItems
(
    Id, TenantId, InvoiceId, ProcedureId, Description, ServiceType, Quantity,
    UnitPrice, DiscountAmount, TaxAmount, LineTotal, SortOrder
)
SELECT
    NEWID(),
    p.TenantId,
    pi.PaymentId,
    NULL,                                    -- ProcedureId not available in PaymentItem
    pi.ServiceName,                          -- Map ServiceName -> Description
    pi.ServiceType,
    pi.Quantity,
    pi.UnitPrice,
    CASE
        WHEN pi.DiscountPct > 0
        THEN ROUND(pi.Quantity * pi.UnitPrice * pi.DiscountPct / 100.0, 2)
        ELSE 0
    END,                                     -- Convert DiscountPct to DiscountAmount
    0,                                       -- TaxAmount not tracked per-item in Payment
    pi.TotalPrice,                           -- LineTotal = TotalPrice
    pi.Quantity                              -- SortOrder = Quantity (approximate)
FROM dbo.PaymentItems pi
INNER JOIN dbo.Payments p ON p.Id = pi.PaymentId
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.InvoiceItems ii
    WHERE ii.InvoiceId = pi.PaymentId
      AND ii.Description = pi.ServiceName
);

-- STEP 5: Migrate Payment records with Refunded status into InvoicePayments
-- For payments that had a PaidAmount > 0, create corresponding InvoicePayment records

INSERT INTO dbo.InvoicePayments
(
    Id, TenantId, InvoiceId, Amount, PaymentMethod, PaymentReference,
    Notes, PaidAt, CreatedBy
)
SELECT
    NEWID(),
    p.TenantId,
    p.Id,
    p.PaidAmount,
    p.PaymentMethod,
    NULL,                                    -- No reference in legacy system
    p.Notes,
    p.CreatedAt,
    p.CreatedBy
FROM dbo.Payments p
WHERE p.PaidAmount > 0
  AND NOT EXISTS (SELECT 1 FROM dbo.InvoicePayments ip WHERE ip.InvoiceId = p.Id);

-- STEP 6: Log all migrated records
INSERT INTO dbo.PaymentToInvoiceMigrationLog (PaymentId, InvoiceId, MigratedAt)
SELECT p.Id, p.Id, SYSUTCDATETIME()
FROM dbo.Payments p
WHERE NOT EXISTS (SELECT 1 FROM dbo.PaymentToInvoiceMigrationLog m WHERE m.PaymentId = p.Id);

-- STEP 7: Verification queries (run these to check migration correctness)

-- Count comparison
SELECT
    'Payments' AS SourceTable,
    COUNT(*) AS RecordCount
FROM dbo.Payments
UNION ALL
SELECT
    'Invoices' AS TargetTable,
    COUNT(*) AS RecordCount
FROM dbo.Invoices
WHERE Id IN (SELECT PaymentId FROM dbo.PaymentToInvoiceMigrationLog);

-- Status distribution comparison
SELECT
    'Payments' AS Source,
    p.Status AS OldStatus,
    CASE p.Status WHEN 1 THEN 'Pending' WHEN 2 THEN 'Partial' WHEN 3 THEN 'Paid' WHEN 4 THEN 'Refunded' WHEN 5 THEN 'Cancelled' END AS OldStatusName,
    COUNT(*) AS Count
FROM dbo.Payments p
GROUP BY p.Status
UNION ALL
SELECT
    'Invoices' AS Source,
    i.Status AS NewStatus,
    CASE i.Status WHEN 1 THEN 'Draft' WHEN 2 THEN 'PartiallyPaid' WHEN 3 THEN 'Paid' WHEN 4 THEN 'Cancelled' WHEN 5 THEN 'Refunded' END AS NewStatusName,
    COUNT(*) AS Count
FROM dbo.Invoices i
INNER JOIN dbo.PaymentToInvoiceMigrationLog m ON m.InvoiceId = i.Id
GROUP BY i.Status;

-- Sum comparison
SELECT
    'Payments' AS Source,
    SUM(TotalAmount) AS TotalAmount_Sum,
    SUM(PaidAmount) AS PaidAmount_Sum,
    SUM(RemainingAmount) AS RemainingAmount_Sum
FROM dbo.Payments
UNION ALL
SELECT
    'Invoices (migrated)' AS Source,
    SUM(GrandTotal) AS GrandTotal_Sum,
    SUM(PaidAmount) AS PaidAmount_Sum,
    SUM(RemainingAmount) AS RemainingAmount_Sum
FROM dbo.Invoices
WHERE Id IN (SELECT PaymentId FROM dbo.PaymentToInvoiceMigrationLog);

-- =============================================================================
-- STEP 8: Migrate old Subscriptions into TenantSubscriptions + SubscriptionPayments
-- =============================================================================
-- Map old Subscriptions.Plan (PlanType enum) to SubscriptionPlans via Code/Name
-- Status mapping: old SubscriptionStatus matches new SubscriptionStatus (same enum values)

-- 8a: Migrate Subscriptions data that hasn't been migrated yet
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Subscriptions')
BEGIN
    INSERT INTO dbo.TenantSubscriptions
    (Id, TenantId, PlanId, Status, StartsAtUtc, EndsAtUtc, AutoRenew, GracePeriodDays, Notes, CreatedByUserId, CreatedAtUtc)
    SELECT
        s.Id,
        s.TenantId,
        (SELECT TOP 1 p.Id FROM dbo.SubscriptionPlans p
         WHERE p.IsActive = 1
           AND ((s.[Plan] = 1 AND p.Code IN (N'TRIAL', N'BASIC_MONTHLY'))
             OR (s.[Plan] = 2 AND p.Code = N'PRO_MONTHLY')
             OR (s.[Plan] = 3 AND p.Code = N'ANNUAL'))
         ORDER BY CASE WHEN s.[Status] = 4 AND p.Code = N'TRIAL' THEN 0 ELSE 1 END
        ) AS PlanId,
        s.[Status],
        s.StartDate,
        s.EndDate,
        0,
        0,
        s.Notes,
        NULL,
        s.CreatedAt
    FROM dbo.Subscriptions s
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.TenantSubscriptions ts
        WHERE ts.TenantId = s.TenantId AND ts.StartsAtUtc = s.StartDate AND ts.EndsAtUtc = s.EndDate
    );

    -- 8b: Create SubscriptionPayments records for old Subscriptions with AmountPaid > 0
    INSERT INTO dbo.SubscriptionPayments
    (Id, TenantId, SubscriptionId, Amount, Currency, PaymentStatus, PaymentMethod, ReferenceNumber, PaidAtUtc, CreatedAtUtc, Notes, CreatedByUserId)
    SELECT
        NEWID(),
        s.TenantId,
        s.Id,
        s.AmountPaid,
        N'EGP',
        2,                                    -- Paid
        LEFT(s.PaymentRef, 100),              -- PaymentMethod NVARCHAR(100)
        LEFT(s.PaymentRef, 200),              -- ReferenceNumber NVARCHAR(200)
        s.StartDate,
        s.CreatedAt,
        LEFT(s.Notes, 500),                   -- Notes NVARCHAR(500)
        NULL
    FROM dbo.Subscriptions s
    WHERE s.AmountPaid > 0
      AND NOT EXISTS (
          SELECT 1 FROM dbo.SubscriptionPayments sp WHERE sp.SubscriptionId = s.Id
      );
END
GO

-- =============================================================================
-- STEP 9: Schema cleanup (run after all data migration is verified)
-- =============================================================================

-- 9a: Convert RemainingAmount to a computed column (like Payments table)
-- This requires dropping the column and re-adding it as computed.
-- WARNING: Run this only AFTER the migration data is verified.
/*
ALTER TABLE dbo.Invoices DROP CONSTRAINT CK_Invoices_Amounts_NonNegative;
ALTER TABLE dbo.Invoices DROP COLUMN RemainingAmount;
ALTER TABLE dbo.Invoices ADD RemainingAmount AS (GrandTotal - PaidAmount) PERSISTED;
ALTER TABLE dbo.Invoices ADD CONSTRAINT CK_Invoices_Amounts_NonNegative
    CHECK (Subtotal >= 0 AND DiscountAmount >= 0 AND TaxAmount >= 0 AND GrandTotal >= 0 AND PaidAmount >= 0 AND RemainingAmount >= 0);
*/

-- 9b: Drop old views that reference Payments (if they exist)
-- DROP VIEW IF EXISTS dbo.v_DailyRevenue;
-- DROP VIEW IF EXISTS dbo.v_PatientSummary;

-- 9c: DROP old tables (UNCOMMENT AFTER VERIFICATION)
-- WARNING: This is irreversible. Only run after thorough testing.
/*
ALTER TABLE dbo.PaymentItems DROP CONSTRAINT IF EXISTS FK_PaymentItems_Payments_PaymentId;
DROP TABLE IF EXISTS dbo.PaymentItems;
DROP TABLE IF EXISTS dbo.Payments;
DROP TABLE IF EXISTS dbo.Subscriptions;
DROP TABLE IF EXISTS dbo.PaymentToInvoiceMigrationLog;
*/
