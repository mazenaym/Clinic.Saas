IF COL_LENGTH('dbo.SubscriptionPayments', 'RefundedAmount') IS NULL
    ALTER TABLE dbo.SubscriptionPayments ADD RefundedAmount DECIMAL(18,2) NOT NULL CONSTRAINT DF_SubscriptionPayments_RefundedAmount DEFAULT 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_SubscriptionPayments_RevenueAnalytics' AND object_id=OBJECT_ID('dbo.SubscriptionPayments'))
    CREATE INDEX IX_SubscriptionPayments_RevenueAnalytics ON dbo.SubscriptionPayments(PaymentStatus, PaidAtUtc) INCLUDE(TenantId, SubscriptionId, Amount, RefundedAmount);
GO
