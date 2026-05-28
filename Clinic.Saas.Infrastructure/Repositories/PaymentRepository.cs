using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.Interfaces;
using Dapper;
using System.Data;

namespace Clinic.Saas.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PaymentRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Payment> AddAsync(Payment entity)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        EnsureTenantId(entity.TenantId);

        entity.CreatedAt = entity.CreatedAt == default ? DateTime.UtcNow : entity.CreatedAt;
        entity.UpdatedAt = entity.UpdatedAt == default ? entity.CreatedAt : entity.UpdatedAt;

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            const string validateReferencesSql = @"
SELECT
    VisitExists = CASE WHEN EXISTS (
        SELECT 1
        FROM dbo.Visits
        WHERE TenantId = @TenantId
          AND Id = @VisitId
          AND PatientId = @PatientId
          AND IsDeleted = 0
    ) THEN 1 ELSE 0 END,
    PatientExists = CASE WHEN EXISTS (
        SELECT 1
        FROM dbo.Patients
        WHERE TenantId = @TenantId
          AND Id = @PatientId
          AND IsDeleted = 0
    ) THEN 1 ELSE 0 END;";

            var referenceStatus = await connection.QuerySingleAsync<PaymentReferenceStatus>(
                validateReferencesSql,
                new
                {
                    entity.TenantId,
                    entity.VisitId,
                    entity.PatientId
                },
                transaction);

            if (referenceStatus.VisitExists == 0)
            {
                throw new InvalidOperationException("Visit does not belong to this tenant and patient.");
            }

            if (referenceStatus.PatientExists == 0)
            {
                throw new InvalidOperationException("Patient does not belong to this tenant.");
            }

            if (string.IsNullOrWhiteSpace(entity.InvoiceNumber))
            {
                entity.InvoiceNumber = await GenerateInvoiceNumberAsync(connection, transaction, entity.TenantId, entity.CreatedAt == default ? DateTime.UtcNow : entity.CreatedAt);
            }

            const string paymentSql = @"
INSERT INTO dbo.Payments
(
    Id, TenantId, VisitId, PatientId, InvoiceNumber, TotalAmount, DiscountAmount,
    DiscountPct, TaxAmount, PaidAmount, PaymentMethod, Status,
    InsuranceCompany, InsuranceNumber, ReceiptUrl, Notes, CreatedAt, UpdatedAt, CreatedBy
)
VALUES
(
    @Id, @TenantId, @VisitId, @PatientId, @InvoiceNumber, @TotalAmount, @DiscountAmount,
    @DiscountPct, @TaxAmount, @PaidAmount, @PaymentMethod, @Status,
    @InsuranceCompany, @InsuranceNumber, @ReceiptUrl, @Notes, @CreatedAt, @UpdatedAt, @CreatedBy
);";

            await connection.ExecuteAsync(paymentSql, new
            {
                entity.Id,
                entity.TenantId,
                entity.VisitId,
                entity.PatientId,
                entity.InvoiceNumber,
                entity.TotalAmount,
                entity.DiscountAmount,
                entity.DiscountPct,
                entity.TaxAmount,
                entity.PaidAmount,
                entity.PaymentMethod,
                entity.Status,
                entity.InsuranceCompany,
                entity.InsuranceNumber,
                entity.ReceiptUrl,
                entity.Notes,
                entity.CreatedAt,
                entity.UpdatedAt,
                entity.CreatedBy
            }, transaction);

            if (entity.Items.Any())
            {
                const string itemSql = @"
INSERT INTO dbo.PaymentItems
(
    Id, PaymentId, ServiceName, ServiceType, Quantity, UnitPrice, DiscountPct
)
VALUES
(
    @Id, @PaymentId, @ServiceName, @ServiceType, @Quantity, @UnitPrice, @DiscountPct
);";

                foreach (var item in entity.Items)
                {
                    if (item.Id == Guid.Empty)
                    {
                        item.Id = Guid.NewGuid();
                    }

                    item.PaymentId = entity.Id;
                   
                    await connection.ExecuteAsync(itemSql, new
                    {
                        item.Id,
                        item.PaymentId,
                        item.ServiceName,
                        item.ServiceType,
                        item.Quantity,
                        item.UnitPrice,
                        item.DiscountPct
                    }, transaction);
                }
            }

            transaction.Commit();

            var created = await GetByIdInternalAsync(connection, entity.Id, entity.TenantId);
            return created;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Task<Payment?> GetByIdAsync(Guid id) =>
        throw new NotSupportedException("Use GetByIdAsync(Guid tenantId, Guid id) for tenant-owned data.");

    public async Task<Payment?> GetByIdAsync(Guid tenantId, Guid id)
    {
        EnsureTenantId(tenantId);

        const string paymentSql = PaymentSelect + @"
WHERE pay.Id = @Id
  AND pay.TenantId = @TenantId;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await GetByIdInternalOrDefaultAsync(connection, paymentSql, new { TenantId = tenantId, Id = id });
    }

    public async Task<IEnumerable<Payment>> GetByPatientAsync(Guid tenantId, Guid patientId)
    {
        EnsureTenantId(tenantId);

        const string sql = PaymentSelect + @"
WHERE pay.TenantId = @TenantId
  AND pay.PatientId = @PatientId
ORDER BY pay.CreatedAt DESC;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryAsync<Payment>(sql, new
        {
            TenantId = tenantId,
            PatientId = patientId
        });
    }

    public Task<IEnumerable<Payment>> GetAllAsync() =>
        throw new NotSupportedException("Use tenant-scoped payment queries for tenant-owned data.");

    public Task UpdateAsync(Payment entity) =>
        throw new NotSupportedException("Use UpdateAsync(Guid tenantId, Payment entity) for tenant-owned data.");

    public async Task UpdateAsync(Guid tenantId, Payment entity)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
UPDATE dbo.Payments
SET InvoiceNumber = @InvoiceNumber,
    TotalAmount = @TotalAmount,
    DiscountAmount = @DiscountAmount,
    DiscountPct = @DiscountPct,
    TaxAmount = @TaxAmount,
    PaidAmount = @PaidAmount,
    PaymentMethod = @PaymentMethod,
    Status = @Status,
    InsuranceCompany = @InsuranceCompany,
    InsuranceNumber = @InsuranceNumber,
    ReceiptUrl = @ReceiptUrl,
    Notes = @Notes,
    UpdatedAt = @UpdatedAt
WHERE Id = @Id
  AND TenantId = @TenantId;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            TenantId = tenantId,
            entity.InvoiceNumber,
            entity.TotalAmount,
            entity.DiscountAmount,
            entity.DiscountPct,
            entity.TaxAmount,
            entity.PaidAmount,
            entity.PaymentMethod,
            entity.Status,
            entity.InsuranceCompany,
            entity.InsuranceNumber,
            entity.ReceiptUrl,
            entity.Notes,
            entity.UpdatedAt
        });
    }

    public async Task<bool> UpdateWithItemsAsync(Guid tenantId, Payment entity)
    {
        EnsureTenantId(tenantId);

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            await ValidateReferencesAsync(connection, transaction, tenantId, entity.VisitId, entity.PatientId);

            const string updatePaymentSql = @"
UPDATE dbo.Payments
SET VisitId = @VisitId,
    PatientId = @PatientId,
    TotalAmount = @TotalAmount,
    DiscountAmount = @DiscountAmount,
    DiscountPct = @DiscountPct,
    TaxAmount = @TaxAmount,
    PaidAmount = @PaidAmount,
    PaymentMethod = @PaymentMethod,
    [Status] = @Status,
    InsuranceCompany = @InsuranceCompany,
    InsuranceNumber = @InsuranceNumber,
    Notes = @Notes,
    UpdatedAt = SYSUTCDATETIME()
WHERE TenantId = @TenantId
  AND Id = @Id;";

            var rows = await connection.ExecuteAsync(updatePaymentSql, new
            {
                TenantId = tenantId,
                entity.Id,
                entity.VisitId,
                entity.PatientId,
                entity.TotalAmount,
                entity.DiscountAmount,
                entity.DiscountPct,
                entity.TaxAmount,
                entity.PaidAmount,
                entity.PaymentMethod,
                entity.Status,
                entity.InsuranceCompany,
                entity.InsuranceNumber,
                entity.Notes
            }, transaction);

            if (rows == 0)
            {
                transaction.Rollback();
                return false;
            }

            const string deleteItemsSql = @"
DELETE pi
FROM dbo.PaymentItems pi
INNER JOIN dbo.Payments pay ON pay.Id = pi.PaymentId
WHERE pay.TenantId = @TenantId
  AND pay.Id = @PaymentId;";

            await connection.ExecuteAsync(deleteItemsSql, new
            {
                TenantId = tenantId,
                PaymentId = entity.Id
            }, transaction);

            if (entity.Items.Any())
            {
                const string insertItemSql = @"
INSERT INTO dbo.PaymentItems
(
    Id, PaymentId, ServiceName, ServiceType, Quantity, UnitPrice, DiscountPct
)
VALUES
(
    @Id, @PaymentId, @ServiceName, @ServiceType, @Quantity, @UnitPrice, @DiscountPct
);";

                foreach (var item in entity.Items)
                {
                    if (item.Id == Guid.Empty)
                    {
                        item.Id = Guid.NewGuid();
                    }

                    item.PaymentId = entity.Id;
                    await connection.ExecuteAsync(insertItemSql, new
                    {
                        item.Id,
                        item.PaymentId,
                        item.ServiceName,
                        item.ServiceType,
                        item.Quantity,
                        item.UnitPrice,
                        item.DiscountPct
                    }, transaction);
                }
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> RefundAsync(Guid tenantId, Guid id, string? reason)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
UPDATE dbo.Payments
SET Status = @Status,
    RefundedAt = SYSUTCDATETIME(),
    Notes = CONCAT(COALESCE(Notes, ''), @Reason),
    UpdatedAt = SYSUTCDATETIME()
WHERE TenantId = @TenantId
  AND Id = @Id;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        var rows = await connection.ExecuteAsync(sql, new
        {
            TenantId = tenantId,
            Id = id,
            Status = PaymentStatus.Refunded,
            Reason = $" Refund: {reason}"
        });

        return rows > 0;
    }

    public Task DeleteAsync(Guid id) =>
        throw new NotSupportedException("Use DeleteAsync(Guid tenantId, Guid id) for tenant-owned data.");

    public async Task DeleteAsync(Guid tenantId, Guid id)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
DELETE pi
FROM dbo.PaymentItems pi
INNER JOIN dbo.Payments pay ON pay.Id = pi.PaymentId
WHERE pay.Id = @Id
  AND pay.TenantId = @TenantId;

DELETE FROM dbo.Payments
WHERE Id = @Id
  AND TenantId = @TenantId;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        await connection.ExecuteAsync(sql, new { TenantId = tenantId, Id = id });
    }

    public async Task<string> GenerateInvoiceNumberAsync(Guid tenantId, DateTime createdAt)
    {
        EnsureTenantId(tenantId);

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var number = await GenerateInvoiceNumberAsync(connection, transaction, tenantId, createdAt);
        transaction.Commit();
        return number;
    }

    public async Task<IEnumerable<Payment>> GetByDateAsync(Guid tenantId, DateTime date)
    {
        EnsureTenantId(tenantId);

        const string sql = PaymentSelect + @"
WHERE pay.TenantId = @TenantId
  AND CAST(pay.CreatedAt AS date) = @PaymentDate
ORDER BY pay.CreatedAt DESC;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryAsync<Payment>(sql, new
        {
            TenantId = tenantId,
            PaymentDate = date.Date
        });
    }

    public async Task<IEnumerable<PaymentDebtRow>> GetDebtTrackingAsync(Guid tenantId)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
SELECT p.PatientId, pt.FullName, pt.PhoneNumber, SUM(p.RemainingAmount) AS TotalDebt
FROM dbo.Payments p
INNER JOIN dbo.Patients pt ON pt.Id = p.PatientId AND pt.TenantId = p.TenantId
WHERE p.TenantId = @TenantId
  AND p.RemainingAmount > 0
GROUP BY p.PatientId, pt.FullName, pt.PhoneNumber
ORDER BY TotalDebt DESC;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryAsync<PaymentDebtRow>(sql, new { TenantId = tenantId });
    }

    public async Task<IEnumerable<MonthlyRevenueRow>> GetMonthlyRevenueAsync(Guid tenantId, DateTime start, DateTime end)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
SELECT CAST(CreatedAt AS date) AS [Date],
       SUM(PaidAmount) AS PaidAmount,
       SUM(RemainingAmount) AS RemainingAmount,
       COUNT(1) AS InvoiceCount
FROM dbo.Payments
WHERE TenantId = @TenantId
  AND CreatedAt >= @Start
  AND CreatedAt < @End
GROUP BY CAST(CreatedAt AS date)
ORDER BY [Date];";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryAsync<MonthlyRevenueRow>(sql, new
        {
            TenantId = tenantId,
            Start = start,
            End = end
        });
    }

    private static async Task<string> GenerateInvoiceNumberAsync(IDbConnection connection, IDbTransaction transaction, Guid tenantId, DateTime createdAt)
    {
        const string sql = @"
SELECT ISNULL(MAX(TRY_CAST(RIGHT(InvoiceNumber, 5) AS INT)), 0) + 1
FROM dbo.Payments WITH (UPDLOCK, HOLDLOCK)
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
        Guid visitId,
        Guid patientId)
    {
        const string validateReferencesSql = @"
SELECT
    VisitExists = CASE WHEN EXISTS (
        SELECT 1
        FROM dbo.Visits
        WHERE TenantId = @TenantId
          AND Id = @VisitId
          AND PatientId = @PatientId
          AND IsDeleted = 0
    ) THEN 1 ELSE 0 END,
    PatientExists = CASE WHEN EXISTS (
        SELECT 1
        FROM dbo.Patients
        WHERE TenantId = @TenantId
          AND Id = @PatientId
          AND IsDeleted = 0
    ) THEN 1 ELSE 0 END;";

        var referenceStatus = await connection.QuerySingleAsync<PaymentReferenceStatus>(
            validateReferencesSql,
            new
            {
                TenantId = tenantId,
                VisitId = visitId,
                PatientId = patientId
            },
            transaction);

        if (referenceStatus.VisitExists == 0)
        {
            throw new InvalidOperationException("Visit does not belong to this tenant and patient.");
        }

        if (referenceStatus.PatientExists == 0)
        {
            throw new InvalidOperationException("Patient does not belong to this tenant.");
        }
    }

    private static async Task<Payment> GetByIdInternalAsync(IDbConnection connection, Guid id, Guid tenantId)
    {
        const string paymentSql = PaymentSelect + @"
WHERE pay.Id = @Id
  AND pay.TenantId = @TenantId;";

        var payment = await GetByIdInternalOrDefaultAsync(connection, paymentSql, new { TenantId = tenantId, Id = id });
        if (payment is null)
        {
            throw new InvalidOperationException("Payment was not found after creation.");
        }

        return payment;
    }

    private const string PaymentSelect = @"
SELECT
    pay.*,
    p.FullName AS PatientName
FROM dbo.Payments pay
INNER JOIN dbo.Patients p ON p.Id = pay.PatientId AND p.TenantId = pay.TenantId
";

    private static async Task<Payment?> GetByIdInternalOrDefaultAsync(IDbConnection connection, string paymentSql, object parameters)
    {
        const string itemsSql = @"
SELECT * FROM dbo.PaymentItems
WHERE PaymentId = @Id
ORDER BY ServiceName;";

        var payment = await connection.QueryFirstOrDefaultAsync<Payment>(paymentSql, parameters);
        if (payment is null)
        {
            return null;
        }

        var items = await connection.QueryAsync<PaymentItem>(itemsSql, new { Id = payment.Id });
        payment.Items = items.ToList();
        return payment;
    }

    private static void EnsureTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantId is required.");
        }
    }

    private sealed class PaymentReferenceStatus
    {
        public int VisitExists { get; set; }
        public int PatientExists { get; set; }
    }
}
