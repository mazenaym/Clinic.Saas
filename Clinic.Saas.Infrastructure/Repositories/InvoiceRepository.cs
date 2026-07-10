using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.Interfaces;
using Dapper;
using System.Data;

namespace Clinic.Saas.Infrastructure.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public InvoiceRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Invoice> AddAsync(Invoice invoice)
    {
        EnsureTenantId(invoice.TenantId);

        if (invoice.Id == Guid.Empty)
        {
            invoice.Id = Guid.NewGuid();
        }

        invoice.CreatedAt = invoice.CreatedAt == default ? DateTime.UtcNow : invoice.CreatedAt;
        invoice.UpdatedAt = invoice.UpdatedAt == default ? invoice.CreatedAt : invoice.UpdatedAt;

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(invoice.TenantId);
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            await ValidateReferencesAsync(connection, transaction, invoice.TenantId, invoice.PatientId, invoice.VisitId);

            if (string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
            {
                invoice.InvoiceNumber = await GenerateInvoiceNumberAsync(connection, transaction, invoice.TenantId, invoice.CreatedAt);
            }

            const string invoiceSql = @"
INSERT INTO dbo.Invoices
(
    Id, TenantId, PatientId, VisitId, InvoiceNumber, Subtotal, DiscountAmount,
    TaxAmount, GrandTotal, PaidAmount, RemainingAmount, Status, Notes,
    CreatedAt, UpdatedAt, CreatedBy
)
VALUES
(
    @Id, @TenantId, @PatientId, @VisitId, @InvoiceNumber, @Subtotal, @DiscountAmount,
    @TaxAmount, @GrandTotal, @PaidAmount, @RemainingAmount, @Status, @Notes,
    @CreatedAt, @UpdatedAt, @CreatedBy
);";

            await connection.ExecuteAsync(invoiceSql, invoice, transaction);

            const string itemSql = @"
INSERT INTO dbo.InvoiceItems
(
    Id, TenantId, InvoiceId, ProcedureId, Description, ServiceType, Quantity,
    UnitPrice, DiscountAmount, TaxAmount, LineTotal, SortOrder
)
VALUES
(
    @Id, @TenantId, @InvoiceId, @ProcedureId, @Description, @ServiceType, @Quantity,
    @UnitPrice, @DiscountAmount, @TaxAmount, @LineTotal, @SortOrder
);";

            foreach (var item in invoice.Items)
            {
                if (item.Id == Guid.Empty)
                {
                    item.Id = Guid.NewGuid();
                }

                item.TenantId = invoice.TenantId;
                item.InvoiceId = invoice.Id;
                await connection.ExecuteAsync(itemSql, item, transaction);
            }

            transaction.Commit();

            return await GetByIdAsync(invoice.TenantId, invoice.Id)
                ?? throw new InvalidOperationException("Invoice was not found after creation.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<Invoice?> GetByIdAsync(Guid tenantId, Guid id)
    {
        EnsureTenantId(tenantId);

        const string invoiceSql = InvoiceSelect + @"
WHERE i.TenantId = @TenantId
  AND i.Id = @Id;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        return await GetByIdInternalOrDefaultAsync(connection, invoiceSql, new { TenantId = tenantId, Id = id });
    }

    public async Task<Invoice?> AddPaymentAsync(Guid tenantId, Guid invoiceId, InvoicePayment payment)
    {
        EnsureTenantId(tenantId);

        if (payment.Id == Guid.Empty)
        {
            payment.Id = Guid.NewGuid();
        }

        payment.TenantId = tenantId;
        payment.InvoiceId = invoiceId;
        payment.PaidAt = payment.PaidAt == default ? DateTime.UtcNow : payment.PaidAt;

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            const string lockSql = @"
SELECT Id, GrandTotal, PaidAmount
FROM dbo.Invoices WITH (UPDLOCK, HOLDLOCK)
WHERE TenantId = @TenantId
  AND Id = @InvoiceId
  AND Status <> @CancelledStatus;";

            var invoice = await connection.QueryFirstOrDefaultAsync<InvoicePaymentUpdateRow>(
                lockSql,
                new { TenantId = tenantId, InvoiceId = invoiceId, CancelledStatus = InvoiceStatus.Cancelled },
                transaction);

            if (invoice is null)
            {
                transaction.Rollback();
                return null;
            }

            var newPaidAmount = invoice.PaidAmount + payment.Amount;
            if (newPaidAmount > invoice.GrandTotal)
            {
                throw new InvalidOperationException("Payment exceeds invoice remaining amount.");
            }

            var remainingAmount = invoice.GrandTotal - newPaidAmount;
            var status = remainingAmount == 0 ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;

            const string paymentSql = @"
INSERT INTO dbo.InvoicePayments
(
    Id, TenantId, InvoiceId, Amount, PaymentMethod, PaymentReference,
    Notes, PaidAt, CreatedBy
)
VALUES
(
    @Id, @TenantId, @InvoiceId, @Amount, @PaymentMethod, @PaymentReference,
    @Notes, @PaidAt, @CreatedBy
);";

            await connection.ExecuteAsync(paymentSql, payment, transaction);

            const string updateInvoiceSql = @"
UPDATE dbo.Invoices
SET PaidAmount = @PaidAmount,
    RemainingAmount = @RemainingAmount,
    Status = @Status,
    UpdatedAt = SYSUTCDATETIME()
WHERE TenantId = @TenantId
  AND Id = @InvoiceId;";

            await connection.ExecuteAsync(updateInvoiceSql, new
            {
                TenantId = tenantId,
                InvoiceId = invoiceId,
                PaidAmount = newPaidAmount,
                RemainingAmount = remainingAmount,
                Status = status
            }, transaction);

            transaction.Commit();

            return await GetByIdAsync(tenantId, invoiceId);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<PatientFinancialLedgerData> GetPatientLedgerAsync(Guid tenantId, Guid patientId)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
SELECT
    COALESCE(SUM(GrandTotal), 0) AS TotalInvoiced,
    COALESCE(SUM(PaidAmount), 0) AS TotalPaid,
    COALESCE(SUM(RemainingAmount), 0) AS OutstandingBalance
FROM dbo.Invoices
WHERE TenantId = @TenantId
  AND PatientId = @PatientId
  AND Status <> @CancelledStatus;

WITH LedgerRows AS
(
    SELECT
        i.CreatedAt AS [Date],
        CAST('invoice' AS nvarchar(20)) AS [Type],
        i.InvoiceNumber AS ReferenceNumber,
        CAST(N'Invoice' AS nvarchar(200)) AS [Description],
        i.GrandTotal AS Debit,
        CAST(0 AS decimal(18,2)) AS Credit,
        CAST(0 AS int) AS SortOrder
    FROM dbo.Invoices i
    WHERE i.TenantId = @TenantId
      AND i.PatientId = @PatientId
      AND i.Status <> @CancelledStatus

    UNION ALL

    SELECT
        ip.PaidAt AS [Date],
        CAST('payment' AS nvarchar(20)) AS [Type],
        i.InvoiceNumber AS ReferenceNumber,
        CAST(COALESCE(ip.PaymentReference, N'Invoice payment') AS nvarchar(200)) AS [Description],
        CAST(0 AS decimal(18,2)) AS Debit,
        ip.Amount AS Credit,
        CAST(1 AS int) AS SortOrder
    FROM dbo.InvoicePayments ip
    INNER JOIN dbo.Invoices i ON i.TenantId = ip.TenantId AND i.Id = ip.InvoiceId
    WHERE ip.TenantId = @TenantId
      AND i.PatientId = @PatientId
      AND i.Status <> @CancelledStatus
)
SELECT
    [Date],
    [Type],
    ReferenceNumber,
    [Description],
    Debit,
    Credit,
    SUM(Debit - Credit) OVER (ORDER BY [Date], SortOrder, ReferenceNumber ROWS UNBOUNDED PRECEDING) AS Balance
FROM LedgerRows
ORDER BY [Date], SortOrder, ReferenceNumber;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            TenantId = tenantId,
            PatientId = patientId,
            CancelledStatus = InvoiceStatus.Cancelled
        });

        return new PatientFinancialLedgerData
        {
            Summary = await multi.ReadFirstOrDefaultAsync<PatientFinancialLedgerSummaryRow>() ?? new PatientFinancialLedgerSummaryRow(),
            Entries = (await multi.ReadAsync<PatientFinancialLedgerEntryRow>()).ToList()
        };
    }

    public async Task<FinancialDuesReportData> GetFinancialDuesAsync(Guid tenantId, DateTime? from, DateTime? toExclusive, Guid? doctorId)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
WITH FilteredInvoices AS
(
    SELECT
        i.Id,
        i.PatientId,
        p.FullName AS PatientName,
        p.PhoneNumber AS Phone,
        i.GrandTotal,
        i.PaidAmount,
        i.RemainingAmount,
        payments.LastPaymentDate
    FROM dbo.Invoices i
    INNER JOIN dbo.Patients p ON p.TenantId = i.TenantId AND p.Id = i.PatientId
    LEFT JOIN dbo.Visits v ON v.TenantId = i.TenantId AND v.Id = i.VisitId
    OUTER APPLY
    (
        SELECT MAX(ip.PaidAt) AS LastPaymentDate
        FROM dbo.InvoicePayments ip
        WHERE ip.TenantId = i.TenantId
          AND ip.InvoiceId = i.Id
    ) payments
    WHERE i.TenantId = @TenantId
      AND p.IsDeleted = 0
      AND i.Status <> @CancelledStatus
      AND (@From IS NULL OR i.CreatedAt >= @From)
      AND (@ToExclusive IS NULL OR i.CreatedAt < @ToExclusive)
      AND (@DoctorId IS NULL OR v.DoctorId = @DoctorId)
),
DebtRows AS
(
    SELECT
        PatientId,
        PatientName,
        Phone,
        SUM(GrandTotal) AS TotalAmount,
        SUM(PaidAmount) AS PaidAmount,
        SUM(RemainingAmount) AS OutstandingAmount,
        MAX(LastPaymentDate) AS LastPaymentDate
    FROM FilteredInvoices
    GROUP BY PatientId, PatientName, Phone
    HAVING SUM(RemainingAmount) > 0
)
SELECT
    COALESCE(SUM(OutstandingAmount), 0) AS TotalOutstanding,
    COALESCE(SUM(PaidAmount), 0) AS TotalPaid,
    COUNT(1) AS PatientsWithDebtCount
FROM DebtRows;

WITH FilteredInvoices AS
(
    SELECT
        i.Id,
        i.PatientId,
        p.FullName AS PatientName,
        p.PhoneNumber AS Phone,
        i.GrandTotal,
        i.PaidAmount,
        i.RemainingAmount,
        payments.LastPaymentDate
    FROM dbo.Invoices i
    INNER JOIN dbo.Patients p ON p.TenantId = i.TenantId AND p.Id = i.PatientId
    LEFT JOIN dbo.Visits v ON v.TenantId = i.TenantId AND v.Id = i.VisitId
    OUTER APPLY
    (
        SELECT MAX(ip.PaidAt) AS LastPaymentDate
        FROM dbo.InvoicePayments ip
        WHERE ip.TenantId = i.TenantId
          AND ip.InvoiceId = i.Id
    ) payments
    WHERE i.TenantId = @TenantId
      AND p.IsDeleted = 0
      AND i.Status <> @CancelledStatus
      AND (@From IS NULL OR i.CreatedAt >= @From)
      AND (@ToExclusive IS NULL OR i.CreatedAt < @ToExclusive)
      AND (@DoctorId IS NULL OR v.DoctorId = @DoctorId)
)
SELECT
    PatientId,
    PatientName,
    Phone,
    SUM(GrandTotal) AS TotalAmount,
    SUM(PaidAmount) AS PaidAmount,
    SUM(RemainingAmount) AS OutstandingAmount,
    MAX(LastPaymentDate) AS LastPaymentDate
FROM FilteredInvoices
GROUP BY PatientId, PatientName, Phone
HAVING SUM(RemainingAmount) > 0
ORDER BY OutstandingAmount DESC, PatientName;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        using var multi = await connection.QueryMultipleAsync(sql, new
        {
            TenantId = tenantId,
            From = from,
            ToExclusive = toExclusive,
            DoctorId = doctorId,
            CancelledStatus = InvoiceStatus.Cancelled
        });

        return new FinancialDuesReportData
        {
            Summary = await multi.ReadFirstOrDefaultAsync<FinancialDuesSummaryRow>() ?? new FinancialDuesSummaryRow(),
            Patients = (await multi.ReadAsync<FinancialDuesPatientRow>()).ToList()
        };
    }

    public async Task<string> GenerateInvoiceNumberAsync(Guid tenantId, DateTime createdAt)
    {
        EnsureTenantId(tenantId);

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var invoiceNumber = await GenerateInvoiceNumberAsync(connection, transaction, tenantId, createdAt);
        transaction.Commit();
        return invoiceNumber;
    }

    private static async Task<string> GenerateInvoiceNumberAsync(IDbConnection connection, IDbTransaction transaction, Guid tenantId, DateTime createdAt)
    {
        const string sql = @"
SELECT ISNULL(MAX(TRY_CAST(RIGHT(InvoiceNumber, 5) AS INT)), 0) + 1
FROM dbo.Invoices WITH (UPDLOCK, HOLDLOCK)
WHERE TenantId = @TenantId
  AND InvoiceNumber LIKE @Prefix + '%';";

        var prefix = $"INV-{createdAt:yyyyMMdd}-";
        var nextNumber = await connection.ExecuteScalarAsync<int>(sql, new { TenantId = tenantId, Prefix = prefix }, transaction);
        return $"{prefix}{nextNumber:D5}";
    }

    private static async Task ValidateReferencesAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        Guid tenantId,
        Guid patientId,
        Guid? visitId)
    {
        const string sql = @"
SELECT
    PatientExists = CASE WHEN EXISTS (
        SELECT 1 FROM dbo.Patients
        WHERE TenantId = @TenantId AND Id = @PatientId AND IsDeleted = 0
    ) THEN 1 ELSE 0 END,
    VisitExists = CASE WHEN @VisitId IS NULL OR EXISTS (
        SELECT 1 FROM dbo.Visits
        WHERE TenantId = @TenantId AND Id = @VisitId AND PatientId = @PatientId AND IsDeleted = 0
    ) THEN 1 ELSE 0 END;";

        var status = await connection.QuerySingleAsync<InvoiceReferenceStatus>(
            sql,
            new { TenantId = tenantId, PatientId = patientId, VisitId = visitId },
            transaction);

        if (status.PatientExists == 0)
        {
            throw new InvalidOperationException("Patient does not belong to this tenant.");
        }

        if (status.VisitExists == 0)
        {
            throw new InvalidOperationException("Visit does not belong to this tenant and patient.");
        }
    }

    private const string InvoiceSelect = @"
SELECT
    i.*,
    p.FullName AS PatientName
FROM dbo.Invoices i
INNER JOIN dbo.Patients p ON p.Id = i.PatientId AND p.TenantId = i.TenantId
";

    private static async Task<Invoice?> GetByIdInternalOrDefaultAsync(IDbConnection connection, string invoiceSql, object parameters)
    {
        const string itemsSql = @"
SELECT *
FROM dbo.InvoiceItems
WHERE TenantId = @TenantId
  AND InvoiceId = @Id
ORDER BY SortOrder, Description;";

        const string paymentsSql = @"
SELECT *
FROM dbo.InvoicePayments
WHERE TenantId = @TenantId
  AND InvoiceId = @Id
ORDER BY PaidAt DESC;";

        var invoice = await connection.QueryFirstOrDefaultAsync<Invoice>(invoiceSql, parameters);
        if (invoice is null)
        {
            return null;
        }

        invoice.Items = (await connection.QueryAsync<InvoiceItem>(itemsSql, parameters)).ToList();
        invoice.Payments = (await connection.QueryAsync<InvoicePayment>(paymentsSql, parameters)).ToList();
        return invoice;
    }

    private static void EnsureTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantId is required.");
        }
    }

    private sealed class InvoiceReferenceStatus
    {
        public int PatientExists { get; set; }
        public int VisitExists { get; set; }
    }

    private sealed class InvoicePaymentUpdateRow
    {
        public Guid Id { get; set; }
        public decimal GrandTotal { get; set; }
        public decimal PaidAmount { get; set; }
    }
}
