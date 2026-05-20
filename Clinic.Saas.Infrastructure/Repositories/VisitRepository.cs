using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Infrastructure.Data;
using Dapper;
using System.Data;

namespace Clinic.Saas.Infrastructure.Repositories;

public class VisitRepository : IVisitRepository
{
    private readonly DapperContext _context;

    public VisitRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<Visit> AddAsync(Visit entity)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        entity.CreatedAt = entity.CreatedAt == default ? DateTime.UtcNow : entity.CreatedAt;
        entity.UpdatedAt = entity.UpdatedAt == default ? entity.CreatedAt : entity.UpdatedAt;

        using var connection = _context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        try
        {
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

    public Task<Visit?> GetByIdAsync(Guid id) => GetByIdInternalAsync(null, id);

    public Task<Visit?> GetByIdAsync(Guid tenantId, Guid id) => GetByIdInternalAsync(tenantId, id);

    public Task<IEnumerable<Visit>> GetAllAsync() => GetAllInternalAsync(null);

    public Task<IEnumerable<Visit>> GetAllAsync(Guid tenantId) => GetAllInternalAsync(tenantId);

    public async Task UpdateAsync(Visit entity)
    {
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
  AND TenantId = @TenantId;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"
UPDATE dbo.Visits
SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<IEnumerable<Visit>> GetByPatientIdAsync(Guid tenantId, Guid patientId)
    {
        const string sql = VisitSelect + @"
WHERE v.TenantId = @TenantId
  AND v.PatientId = @PatientId
  AND v.IsDeleted = 0
ORDER BY v.VisitDate DESC;";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<Visit>(sql, new { TenantId = tenantId, PatientId = patientId });
    }

    public async Task<int> CountByDateAsync(Guid tenantId, DateTime date)
    {
        const string sql = @"
SELECT COUNT(1)
FROM dbo.Visits
WHERE TenantId = @TenantId
  AND CAST(VisitDate AS date) = @VisitDate
  AND IsDeleted = 0;";

        using var connection = _context.CreateConnection();
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

    private async Task<Visit?> GetByIdInternalAsync(Guid? tenantId, Guid id)
    {
        const string sql = VisitSelect + @"
WHERE v.Id = @Id
  AND v.IsDeleted = 0
  AND (@TenantId IS NULL OR v.TenantId = @TenantId);";

        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Visit>(sql, new { TenantId = tenantId, Id = id });
    }

    private async Task<IEnumerable<Visit>> GetAllInternalAsync(Guid? tenantId)
    {
        const string sql = VisitSelect + @"
WHERE v.IsDeleted = 0
  AND (@TenantId IS NULL OR v.TenantId = @TenantId)
ORDER BY v.VisitDate DESC;";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<Visit>(sql, new { TenantId = tenantId });
    }
}
