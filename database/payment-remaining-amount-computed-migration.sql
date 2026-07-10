SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Payments', 'RemainingAmount') IS NOT NULL
BEGIN
    DECLARE @defaultConstraint sysname;
    SELECT @defaultConstraint = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('dbo.Payments')
      AND c.name = 'RemainingAmount';

    IF @defaultConstraint IS NOT NULL
    BEGIN
        DECLARE @dropDefaultSql nvarchar(max) = N'ALTER TABLE dbo.Payments DROP CONSTRAINT ' + QUOTENAME(@defaultConstraint);
        EXEC sp_executesql @dropDefaultSql;
    END;

    ALTER TABLE dbo.Payments DROP COLUMN RemainingAmount;
END;

ALTER TABLE dbo.Payments ADD RemainingAmount AS
    (TotalAmount + TaxAmount - DiscountAmount - PaidAmount) PERSISTED;

IF COL_LENGTH('dbo.PaymentItems', 'TotalPrice') IS NOT NULL
BEGIN
    DECLARE @itemDefaultConstraint sysname;
    SELECT @itemDefaultConstraint = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.default_object_id = dc.object_id
    WHERE c.object_id = OBJECT_ID('dbo.PaymentItems')
      AND c.name = 'TotalPrice';

    IF @itemDefaultConstraint IS NOT NULL
    BEGIN
        DECLARE @dropItemDefaultSql nvarchar(max) = N'ALTER TABLE dbo.PaymentItems DROP CONSTRAINT ' + QUOTENAME(@itemDefaultConstraint);
        EXEC sp_executesql @dropItemDefaultSql;
    END;

    ALTER TABLE dbo.PaymentItems DROP COLUMN TotalPrice;
END;

ALTER TABLE dbo.PaymentItems ADD TotalPrice AS
    ((Quantity * UnitPrice) * (1 - DiscountPct / 100.0)) PERSISTED;

COMMIT TRANSACTION;
