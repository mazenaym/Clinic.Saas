using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Infrastructure.Data;
using Dapper;

namespace Clinic.Saas.Infrastructure.Repositories;

public class PrescriptionRepository : IPrescriptionRepository
{
    private readonly DapperContext _context;

    public PrescriptionRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<Prescription> AddAsync(Prescription entity)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        using var connection = _context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
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
            return await GetByIdInternalAsync(connection, entity.Id);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<Prescription?> GetByIdAsync(Guid id)
    {
        using var connection = _context.CreateConnection();
        return await GetByIdInternalOrDefaultAsync(connection, id, null);
    }

    public async Task<Prescription?> GetByIdAsync(Guid tenantId, Guid id)
    {
        using var connection = _context.CreateConnection();
        return await GetByIdInternalOrDefaultAsync(connection, id, tenantId);
    }

    public async Task<IEnumerable<Prescription>> GetAllAsync()
    {
        const string sql = PrescriptionSelect + @"
WHERE pr.IsActive = 1
ORDER BY pr.CreatedAt DESC;";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<Prescription>(sql);
    }

    public async Task UpdateAsync(Prescription entity)
    {
        const string sql = @"
UPDATE dbo.Prescriptions
SET Notes = @Notes,
    QrCode = @QrCode,
    PdfUrl = @PdfUrl,
    SentViaWhatsapp = @SentViaWhatsapp,
    SentViaSms = @SentViaSms,
    IsActive = @IsActive
WHERE Id = @Id;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"
UPDATE dbo.Prescriptions
SET IsActive = 0
WHERE Id = @Id;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<IEnumerable<Prescription>> GetByPatientIdAsync(Guid tenantId, Guid patientId)
    {
        const string sql = PrescriptionSelect + @"
WHERE pr.TenantId = @TenantId
  AND pr.PatientId = @PatientId
  AND pr.IsActive = 1
ORDER BY pr.CreatedAt DESC;";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<Prescription>(sql, new { TenantId = tenantId, PatientId = patientId });
    }

    private static async Task<Prescription> GetByIdInternalAsync(System.Data.IDbConnection connection, Guid id)
    {
        var prescription = await GetByIdInternalOrDefaultAsync(connection, id, null);
        if (prescription is null)
        {
            throw new InvalidOperationException("Prescription was not found after creation.");
        }

        return prescription;
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

    private static async Task<Prescription?> GetByIdInternalOrDefaultAsync(System.Data.IDbConnection connection, Guid id, Guid? tenantId)
    {
        const string prescriptionSql = PrescriptionSelect + @"
WHERE pr.Id = @Id
  AND pr.IsActive = 1
  AND (@TenantId IS NULL OR pr.TenantId = @TenantId);";

        const string itemsSql = @"
SELECT * FROM dbo.PrescriptionItems
WHERE PrescriptionId = @Id
ORDER BY SortOrder, DrugName;";

        var prescription = await connection.QueryFirstOrDefaultAsync<Prescription>(prescriptionSql, new { Id = id, TenantId = tenantId });
        if (prescription is null)
        {
            return null;
        }

        var items = await connection.QueryAsync<PrescriptionItem>(itemsSql, new { Id = id });
        prescription.Items = items.ToList();

        return prescription;
    }
}
