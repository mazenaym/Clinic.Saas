using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Infrastructure.Data;
using Dapper;
using System.Data;

namespace Clinic.Saas.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly DapperContext _context;

    public PaymentRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<Payment> AddAsync(Payment entity)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        using var connection = _context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
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
            return await GetByIdInternalAsync(connection, entity.Id, entity.TenantId);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<Payment?> GetByIdAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        return await GetByIdInternalOrDefaultAsync(connection, id, null);
    }

    public async Task<Payment?> GetByIdAsync(Guid tenantId, Guid id)
    {
        using var connection = _context.CreateConnection();
        return await GetByIdInternalOrDefaultAsync(connection, id, tenantId);
    }

    public async Task<IEnumerable<Payment>> GetAllAsync()
    {
        const string sql = @"
SELECT * FROM dbo.Payments
ORDER BY CreatedAt DESC;";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<Payment>(sql);
    }

    public async Task UpdateAsync(Payment entity)
    {
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
WHERE Id = @Id;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            entity.Id,
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

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"
DELETE FROM dbo.PaymentItems WHERE PaymentId = @Id;
DELETE FROM dbo.Payments WHERE Id = @Id;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<string> GenerateInvoiceNumberAsync(Guid tenantId, DateTime createdAt)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        var number = await GenerateInvoiceNumberAsync(connection, transaction, tenantId, createdAt);
        transaction.Commit();
        return number;
    }

    public async Task<IEnumerable<Payment>> GetByDateAsync(Guid tenantId, DateTime date)
    {
        const string sql = PaymentSelect + @"
WHERE pay.TenantId = @TenantId
  AND CAST(pay.CreatedAt AS date) = @PaymentDate
ORDER BY pay.CreatedAt DESC;";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<Payment>(sql, new
        {
            TenantId = tenantId,
            PaymentDate = date.Date
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

    private static async Task<Payment> GetByIdInternalAsync(System.Data.IDbConnection connection, Guid id, Guid tenantId)
    {
        var payment = await GetByIdInternalOrDefaultAsync(connection, id, tenantId);
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

    private static async Task<Payment?> GetByIdInternalOrDefaultAsync(System.Data.IDbConnection connection, Guid id, Guid? tenantId)
    {
        const string paymentSql = PaymentSelect + @"
WHERE pay.Id = @Id
  AND (@TenantId IS NULL OR pay.TenantId = @TenantId);";

        const string itemsSql = @"
SELECT * FROM dbo.PaymentItems
WHERE PaymentId = @Id
ORDER BY ServiceName;";

        var payment = await connection.QueryFirstOrDefaultAsync<Payment>(paymentSql, new { Id = id, TenantId = tenantId });
        if (payment is null)
        {
            return null;
        }

        var items = await connection.QueryAsync<PaymentItem>(itemsSql, new { Id = id });
        payment.Items = items.ToList();
        return payment;
    }
}
