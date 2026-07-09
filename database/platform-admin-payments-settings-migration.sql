IF COL_LENGTH('dbo.SubscriptionPayments', 'Notes') IS NULL
    ALTER TABLE dbo.SubscriptionPayments ADD Notes NVARCHAR(500) NULL;

IF COL_LENGTH('dbo.SubscriptionPayments', 'CreatedByUserId') IS NULL
    ALTER TABLE dbo.SubscriptionPayments ADD CreatedByUserId UNIQUEIDENTIFIER NULL;

IF COL_LENGTH('dbo.PlatformSettings', 'UpdatedByUserId') IS NULL
    ALTER TABLE dbo.PlatformSettings ADD UpdatedByUserId UNIQUEIDENTIFIER NULL;

MERGE dbo.PlatformSettings AS target
USING (VALUES
    (N'ExpiringSoonThresholdDays', N'7'),
    (N'AutoSuspendExpiredClinics', N'false'),
    (N'PaymentMethodsEnabled', N'Cash,Card,Bank Transfer'),
    (N'TaxPercentage', N'0')
) AS source ([Key], [Value])
ON target.[Key] = source.[Key]
WHEN NOT MATCHED THEN INSERT ([Key], [Value], UpdatedAtUtc)
VALUES (source.[Key], source.[Value], SYSUTCDATETIME());
