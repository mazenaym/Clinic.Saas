using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Exceptions;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.Interfaces;
using Dapper;
using System.Data;

namespace Clinic.Saas.Infrastructure.Repositories;

public class VisitRepository : IVisitRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public VisitRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Visit> AddAsync(Visit entity)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        EnsureTenantId(entity.TenantId);

        entity.CreatedAt = entity.CreatedAt == default ? DateTime.UtcNow : entity.CreatedAt;
        entity.UpdatedAt = entity.UpdatedAt == default ? entity.CreatedAt : entity.UpdatedAt;

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(entity.TenantId);
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        try
        {
            const string validateReferencesSql = @"
SELECT
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

            var referenceStatus = await connection.QuerySingleAsync<VisitReferenceStatus>(
                validateReferencesSql,
                new
                {
                    entity.TenantId,
                    entity.PatientId,
                    entity.DoctorId
                },
                transaction);

            if (referenceStatus.PatientExists == 0)
            {
                throw new InvalidOperationException("Patient does not belong to this tenant.");
            }

            if (referenceStatus.DoctorExists == 0)
            {
                throw new InvalidOperationException("Doctor does not belong to this tenant.");
            }

            if (entity.AppointmentId.HasValue)
            {
                const string validateAppointmentSql = @"
SELECT COUNT(1)
FROM dbo.Appointments
WHERE Id = @AppointmentId
  AND TenantId = @TenantId
  AND PatientId = @PatientId
  AND DoctorId = @DoctorId
  AND IsDeleted = 0;";

                var exists = await connection.ExecuteScalarAsync<int>(validateAppointmentSql, new
                {
                    entity.AppointmentId,
                    entity.TenantId,
                    entity.PatientId,
                    entity.DoctorId
                }, transaction);

                if (exists == 0)
                {
                    throw new InvalidOperationException("Appointment does not belong to this tenant, patient, and doctor.");
                }
            }

            const string sql = @"
INSERT INTO dbo.Visits
(
    Id, TenantId, PatientId, AppointmentId, DoctorId, VisitDate, VisitType,
    ChiefComplaint, VitalSigns, ClinicalNotes, Diagnosis, DiagnosisCode,
    DifferentialDx, FollowUpDate, FollowUpNotes, IsDeleted, CreatedAt, UpdatedAt
)
VALUES
(
    @Id, @TenantId, @PatientId, @AppointmentId, @DoctorId, @VisitDate, @VisitType,
    @ChiefComplaint, @VitalSigns, @ClinicalNotes, @Diagnosis, @DiagnosisCode,
    @DifferentialDx, @FollowUpDate, @FollowUpNotes, @IsDeleted, @CreatedAt, @UpdatedAt
);";

            await connection.ExecuteAsync(sql, entity, transaction);

            if (entity.AppointmentId.HasValue)
            {
                const string updateAppointmentSql = @"
UPDATE dbo.Appointments
SET Status = @CompletedStatus,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @AppointmentId
  AND TenantId = @TenantId;";

                await connection.ExecuteAsync(updateAppointmentSql, new
                {
                    entity.AppointmentId,
                    entity.TenantId,
                    CompletedStatus = AppointmentStatus.Completed
                }, transaction);
            }

            transaction.Commit();

            var created = await GetByIdAsync(entity.TenantId, entity.Id);
            return created ?? throw new InvalidOperationException("Visit was not found after creation.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Task<Visit?> GetByIdAsync(Guid id) =>
        throw new NotSupportedException("Use GetByIdAsync(Guid tenantId, Guid id) for tenant-owned data.");

    public async Task<Visit?> GetByIdAsync(Guid tenantId, Guid id)
    {
        EnsureTenantId(tenantId);

        const string sql = VisitSelect + @"
WHERE v.Id = @Id
  AND v.TenantId = @TenantId
  AND v.IsDeleted = 0;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        return await connection.QueryFirstOrDefaultAsync<Visit>(sql, new { TenantId = tenantId, Id = id });
    }

    public Task<IEnumerable<Visit>> GetAllAsync() =>
        throw new NotSupportedException("Use GetAllAsync(Guid tenantId) for tenant-owned data.");

    public async Task<IEnumerable<Visit>> GetAllAsync(Guid tenantId)
    {
        EnsureTenantId(tenantId);

        const string sql = VisitSelect + @"
WHERE v.TenantId = @TenantId
  AND v.IsDeleted = 0
ORDER BY v.VisitDate DESC;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        return await connection.QueryAsync<Visit>(sql, new { TenantId = tenantId });
    }

    public Task UpdateAsync(Visit entity) =>
        throw new NotSupportedException("Use UpdateAsync(Guid tenantId, Visit entity) for tenant-owned data.");

    public async Task UpdateAsync(Guid tenantId, Visit entity)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
UPDATE dbo.Visits
SET AppointmentId = @AppointmentId,
    DoctorId = @DoctorId,
    VisitDate = @VisitDate,
    VisitType = @VisitType,
    ChiefComplaint = @ChiefComplaint,
    VitalSigns = @VitalSigns,
    ClinicalNotes = @ClinicalNotes,
    Diagnosis = @Diagnosis,
    DiagnosisCode = @DiagnosisCode,
    DifferentialDx = @DifferentialDx,
    FollowUpDate = @FollowUpDate,
    FollowUpNotes = @FollowUpNotes,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id
  AND TenantId = @TenantId
  AND RowVersion = @RowVersion;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        var rows = await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            TenantId = tenantId,
            entity.RowVersion,
            entity.AppointmentId,
            entity.DoctorId,
            entity.VisitDate,
            entity.VisitType,
            entity.ChiefComplaint,
            entity.VitalSigns,
            entity.ClinicalNotes,
            entity.Diagnosis,
            entity.DiagnosisCode,
            entity.DifferentialDx,
            entity.FollowUpDate,
            entity.FollowUpNotes
        });

        await ThrowIfConcurrencyConflictAsync(connection, tenantId, entity.Id, rows);
    }

    public Task DeleteAsync(Guid id) =>
        throw new NotSupportedException("Use DeleteAsync(Guid tenantId, Guid id) for tenant-owned data.");

    public async Task DeleteAsync(Guid tenantId, Guid id, byte[] rowVersion)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
UPDATE dbo.Visits
SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id
  AND TenantId = @TenantId
  AND RowVersion = @RowVersion;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        var rows = await connection.ExecuteAsync(sql, new { TenantId = tenantId, Id = id, RowVersion = rowVersion });
        await ThrowIfConcurrencyConflictAsync(connection, tenantId, id, rows);
    }

    public async Task<IEnumerable<Visit>> GetByPatientIdAsync(Guid tenantId, Guid patientId)
    {
        EnsureTenantId(tenantId);

        const string sql = VisitSelect + @"
WHERE v.TenantId = @TenantId
  AND v.PatientId = @PatientId
  AND v.IsDeleted = 0
ORDER BY v.VisitDate DESC;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        return await connection.QueryAsync<Visit>(sql, new { TenantId = tenantId, PatientId = patientId });
    }

    public async Task<int> UpdateClinicalDetailsAsync(Guid tenantId, Guid id, Visit entity)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
UPDATE dbo.Visits
SET VisitType = @VisitType,
    ChiefComplaint = @ChiefComplaint,
    VitalSigns = @VitalSigns,
    ClinicalNotes = @ClinicalNotes,
    Diagnosis = @Diagnosis,
    DiagnosisCode = @DiagnosisCode,
    FollowUpDate = @FollowUpDate,
    UpdatedAt = SYSUTCDATETIME()
WHERE TenantId = @TenantId
  AND Id = @Id
  AND IsDeleted = 0
  AND FinalizedAt IS NULL
  AND RowVersion = @RowVersion;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        var rows = await connection.ExecuteAsync(sql, new
        {
            TenantId = tenantId,
            Id = id,
            entity.VisitType,
            entity.ChiefComplaint,
            entity.VitalSigns,
            entity.ClinicalNotes,
            entity.Diagnosis,
            entity.DiagnosisCode,
            entity.FollowUpDate,
            entity.RowVersion
        });

        await ThrowIfConcurrencyConflictAsync(connection, tenantId, id, rows);
        return rows;
    }

    public async Task<int> FinalizeAsync(Guid tenantId, Guid id, Guid finalizedByUserId, byte[] rowVersion)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
UPDATE dbo.Visits
SET FinalizedAt = SYSUTCDATETIME(),
    FinalizedBy = @FinalizedByUserId,
    UpdatedAt = SYSUTCDATETIME()
WHERE TenantId = @TenantId
  AND Id = @Id
  AND IsDeleted = 0
  AND FinalizedAt IS NULL
  AND RowVersion = @RowVersion;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        var rows = await connection.ExecuteAsync(sql, new
        {
            TenantId = tenantId,
            Id = id,
            FinalizedByUserId = finalizedByUserId,
            RowVersion = rowVersion
        });

        await ThrowIfConcurrencyConflictAsync(connection, tenantId, id, rows);
        return rows;
    }

    public async Task<int> CountByDateAsync(Guid tenantId, DateTime date)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
SELECT COUNT(1)
FROM dbo.Visits
WHERE TenantId = @TenantId
  AND CAST(VisitDate AS date) = @VisitDate
  AND IsDeleted = 0;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        return await connection.ExecuteScalarAsync<int>(sql, new { TenantId = tenantId, VisitDate = date.Date });
    }

    private const string VisitSelect = @"
SELECT
    v.*,
    p.FullName AS PatientName,
    u.FullName AS DoctorName
FROM dbo.Visits v
INNER JOIN dbo.Patients p ON p.Id = v.PatientId AND p.TenantId = v.TenantId
INNER JOIN dbo.Users u ON u.Id = v.DoctorId AND u.TenantId = v.TenantId
";

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
FROM dbo.Visits
WHERE TenantId = @TenantId
  AND Id = @Id
  AND IsDeleted = 0;";

        var exists = await connection.ExecuteScalarAsync<int>(existsSql, new { TenantId = tenantId, Id = id });
        if (exists > 0)
        {
            throw new ConcurrencyConflictException("Visit was modified by another request.");
        }

        throw new RecordNotFoundException("Visit was not found.");
    }

    private sealed class VisitReferenceStatus
    {
        public int PatientExists { get; set; }
        public int DoctorExists { get; set; }
    }
}
