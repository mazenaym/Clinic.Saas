using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Infrastructure.Data;
using Dapper;

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
);

SELECT * FROM dbo.Visits WHERE Id = @Id;";

        using var connection = _context.CreateConnection();
        return await connection.QuerySingleAsync<Visit>(sql, entity);
    }

    public async Task<Visit?> GetByIdAsync(Guid id)
    {
        const string sql = @"
SELECT * FROM dbo.Visits
WHERE Id = @Id AND IsDeleted = 0;";

        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Visit>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Visit>> GetAllAsync()
    {
        const string sql = @"
SELECT * FROM dbo.Visits
WHERE IsDeleted = 0
ORDER BY VisitDate DESC;";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<Visit>(sql);
    }

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
    UpdatedAt = @UpdatedAt
WHERE Id = @Id;";

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

    public async Task<IEnumerable<Visit>> GetByPatientIdAsync(Guid patientId)
    {
        const string sql = @"
SELECT * FROM dbo.Visits
WHERE PatientId = @PatientId
  AND IsDeleted = 0
ORDER BY VisitDate DESC;";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<Visit>(sql, new { PatientId = patientId });
    }
}
