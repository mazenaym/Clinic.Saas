using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Exceptions;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.Interfaces;
using Dapper;
using System.Data;

namespace Clinic.Saas.Infrastructure.Repositories;

public class PrescriptionRepository : IPrescriptionRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public PrescriptionRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Prescription> AddAsync(Prescription entity)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        EnsureTenantId(entity.TenantId);
        entity.CreatedAt = entity.CreatedAt == default ? DateTime.UtcNow : entity.CreatedAt;

        if (string.IsNullOrWhiteSpace(entity.QrCode))
        {
            entity.QrCode = $"RX-{entity.Id:N}";
        }

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        using var transaction = connection.BeginTransaction();

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
          AND DoctorId = @DoctorId
          AND IsDeleted = 0
    ) THEN 1 ELSE 0 END,
    PatientExists = CASE WHEN EXISTS (
        SELECT 1
        FROM dbo.Patients
        WHERE TenantId = @TenantId
          AND Id = @PatientId
          AND IsDeleted = 0
    ) THEN 1 ELSE 0 END,
    DoctorExists = CASE WHEN EXISTS (
        SELECT 1
        FROM dbo.Users
        WHERE TenantId = @TenantId
          AND Id = @DoctorId
          AND IsActive = 1
    ) THEN 1 ELSE 0 END;";

            var referenceStatus = await connection.QuerySingleAsync<PrescriptionReferenceStatus>(
                validateReferencesSql,
                new
                {
                    entity.TenantId,
                    entity.VisitId,
                    entity.PatientId,
                    entity.DoctorId
                },
                transaction);

            if (referenceStatus.VisitExists == 0)
            {
                throw new InvalidOperationException("Visit does not belong to this tenant, patient, and doctor.");
            }

            if (referenceStatus.PatientExists == 0)
            {
                throw new InvalidOperationException("Patient does not belong to this tenant.");
            }

            if (referenceStatus.DoctorExists == 0)
            {
                throw new InvalidOperationException("Doctor does not belong to this tenant.");
            }

            const string prescriptionSql = @"
INSERT INTO dbo.Prescriptions
(
    Id, TenantId, VisitId, PatientId, DoctorId, Notes, QrCode, PdfUrl,
    SentViaWhatsapp, SentViaSms, IsActive, CreatedAt
)
VALUES
(
    @Id, @TenantId, @VisitId, @PatientId, @DoctorId, @Notes, @QrCode, @PdfUrl,
    @SentViaWhatsapp, @SentViaSms, @IsActive, @CreatedAt
);";

            await connection.ExecuteAsync(prescriptionSql, entity, transaction);

            if (entity.Items.Any())
            {
                const string itemSql = @"
INSERT INTO dbo.PrescriptionItems
(
    Id, PrescriptionId, DrugId, DrugName, Dosage, Frequency, Duration, Route, Instructions, SortOrder
)
VALUES
(
    @Id, @PrescriptionId, @DrugId, @DrugName, @Dosage, @Frequency, @Duration, @Route, @Instructions, @SortOrder
);";

                foreach (var item in entity.Items)
                {
                    if (item.Id == Guid.Empty)
                    {
                        item.Id = Guid.NewGuid();
                    }

                    item.PrescriptionId = entity.Id;
                    await connection.ExecuteAsync(itemSql, item, transaction);
                }
            }

            transaction.Commit();
            var created = await GetByIdAsync(entity.TenantId, entity.Id);
            return created ?? throw new InvalidOperationException("Prescription was not found after creation.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Task<Prescription?> GetByIdAsync(Guid id) =>
        throw new NotSupportedException("Use GetByIdAsync(Guid tenantId, Guid id) for tenant-owned data.");

    public async Task<Prescription?> GetByIdAsync(Guid tenantId, Guid id)
    {
        EnsureTenantId(tenantId);

        const string prescriptionSql = PrescriptionSelect + @"
WHERE pr.Id = @Id
  AND pr.TenantId = @TenantId
  AND pr.IsActive = 1;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await GetByIdInternalOrDefaultAsync(connection, prescriptionSql, new { TenantId = tenantId, Id = id });
    }

    public Task<IEnumerable<Prescription>> GetAllAsync() =>
        throw new NotSupportedException("Use tenant-scoped prescription queries for tenant-owned data.");

    public Task UpdateAsync(Prescription entity) =>
        throw new NotSupportedException("Use UpdateAsync(Guid tenantId, Prescription entity) for tenant-owned data.");

    public async Task UpdateAsync(Guid tenantId, Prescription entity)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
UPDATE dbo.Prescriptions
SET Notes = @Notes,
    QrCode = @QrCode,
    PdfUrl = @PdfUrl,
    SentViaWhatsapp = @SentViaWhatsapp,
    SentViaSms = @SentViaSms,
    IsActive = @IsActive
WHERE Id = @Id
  AND TenantId = @TenantId
  AND RowVersion = @RowVersion;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        var rows = await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            TenantId = tenantId,
            entity.RowVersion,
            entity.Notes,
            entity.QrCode,
            entity.PdfUrl,
            entity.SentViaWhatsapp,
            entity.SentViaSms,
            entity.IsActive
        });

        await ThrowIfConcurrencyConflictAsync(connection, tenantId, entity.Id, rows);
    }

    public Task DeleteAsync(Guid id) =>
        throw new NotSupportedException("Use DeleteAsync(Guid tenantId, Guid id) for tenant-owned data.");

    public async Task DeleteAsync(Guid tenantId, Guid id, byte[] rowVersion)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
UPDATE dbo.Prescriptions
SET IsActive = 0
WHERE Id = @Id
  AND TenantId = @TenantId
  AND RowVersion = @RowVersion;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        var rows = await connection.ExecuteAsync(sql, new { TenantId = tenantId, Id = id, RowVersion = rowVersion });
        await ThrowIfConcurrencyConflictAsync(connection, tenantId, id, rows);
    }

    public async Task<IEnumerable<Prescription>> GetByPatientIdAsync(Guid tenantId, Guid patientId)
    {
        EnsureTenantId(tenantId);

        const string sql = PrescriptionSelect + @"
WHERE pr.TenantId = @TenantId
  AND pr.PatientId = @PatientId
  AND pr.IsActive = 1
ORDER BY pr.CreatedAt DESC;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryAsync<Prescription>(sql, new { TenantId = tenantId, PatientId = patientId });
    }

    public async Task<int> MarkSentViaWhatsappAsync(Guid tenantId, Guid id, byte[] rowVersion)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
UPDATE dbo.Prescriptions
SET SentViaWhatsapp = 1
WHERE TenantId = @TenantId
  AND Id = @Id
  AND IsActive = 1
  AND RowVersion = @RowVersion;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        var rows = await connection.ExecuteAsync(sql, new { TenantId = tenantId, Id = id, RowVersion = rowVersion });
        await ThrowIfConcurrencyConflictAsync(connection, tenantId, id, rows);
        return rows;
    }

    private const string PrescriptionSelect = @"
SELECT
    pr.*,
    p.FullName AS PatientName,
    u.FullName AS DoctorName
FROM dbo.Prescriptions pr
INNER JOIN dbo.Patients p ON p.Id = pr.PatientId AND p.TenantId = pr.TenantId
INNER JOIN dbo.Users u ON u.Id = pr.DoctorId AND u.TenantId = pr.TenantId
";

    private static async Task<Prescription?> GetByIdInternalOrDefaultAsync(IDbConnection connection, string prescriptionSql, object parameters)
    {
        const string itemsSql = @"
SELECT * FROM dbo.PrescriptionItems
WHERE PrescriptionId = @Id
ORDER BY SortOrder, DrugName;";

        var prescription = await connection.QueryFirstOrDefaultAsync<Prescription>(prescriptionSql, parameters);
        if (prescription is null)
        {
            return null;
        }

        var items = await connection.QueryAsync<PrescriptionItem>(itemsSql, new { Id = prescription.Id });
        prescription.Items = items.ToList();

        return prescription;
    }

    private static void EnsureTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantId is required.");
        }
    }

    private static async Task ThrowIfConcurrencyConflictAsync(IDbConnection connection, Guid tenantId, Guid id, int rows)
    {
        if (rows > 0)
        {
            return;
        }

        const string existsSql = @"
SELECT COUNT(1)
FROM dbo.Prescriptions
WHERE TenantId = @TenantId
  AND Id = @Id
  AND IsActive = 1;";

        var exists = await connection.ExecuteScalarAsync<int>(existsSql, new { TenantId = tenantId, Id = id });
        if (exists > 0)
        {
            throw new ConcurrencyConflictException("Prescription was modified by another request.");
        }

        throw new RecordNotFoundException("Prescription was not found.");
    }

    private sealed class PrescriptionReferenceStatus
    {
        public int VisitExists { get; set; }
        public int PatientExists { get; set; }
        public int DoctorExists { get; set; }
    }
}
