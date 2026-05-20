using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Infrastructure.Data;
using Dapper;
using System.Data;

namespace Clinic.Saas.Infrastructure.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly DapperContext _context;

    public AppointmentRepository(DapperContext context)
    {
        _context = context;
    }

    public async Task<Appointment> AddAsync(Appointment entity)
    {
        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        entity.CreatedAt = entity.CreatedAt == default ? DateTime.UtcNow : entity.CreatedAt;
        entity.UpdatedAt = entity.UpdatedAt == default ? entity.CreatedAt : entity.UpdatedAt;

        using var connection = _context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            const string conflictSql = @"
SELECT COUNT(1)
FROM dbo.Appointments WITH (UPDLOCK, HOLDLOCK)
WHERE TenantId = @TenantId
  AND DoctorId = @DoctorId
  AND AppointmentDate = @AppointmentDate
  AND IsDeleted = 0
  AND Status <> @CancelledStatus
  AND StartTime < @EndTime
  AND EndTime > @StartTime;";

            var conflicts = await connection.ExecuteScalarAsync<int>(conflictSql, new
            {
                entity.TenantId,
                entity.DoctorId,
                AppointmentDate = entity.AppointmentDate.Date,
                entity.EndTime,
                entity.StartTime,
                CancelledStatus = AppointmentStatus.Cancelled
            }, transaction);

            if (conflicts > 0)
            {
                throw new InvalidOperationException("Appointment conflicts with an existing appointment for the same doctor.");
            }

            const string sql = @"
INSERT INTO dbo.Appointments
(
    Id, TenantId, PatientId, DoctorId, AppointmentDate, StartTime, EndTime,
    Status, Type, Source, Notes, CancelReason, ReminderSent, IsDeleted,
    CreatedAt, UpdatedAt, CreatedBy
)
VALUES
(
    @Id, @TenantId, @PatientId, @DoctorId, @AppointmentDate, @StartTime, @EndTime,
    @Status, @Type, @Source, @Notes, @CancelReason, @ReminderSent, @IsDeleted,
    @CreatedAt, @UpdatedAt, @CreatedBy
);";

            await connection.ExecuteAsync(sql, entity, transaction);
            transaction.Commit();

            var created = await GetByIdAsync(entity.TenantId, entity.Id);
            return created ?? throw new InvalidOperationException("Appointment was not found after creation.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Task<Appointment?> GetByIdAsync(Guid id) => GetByIdInternalAsync(null, id);

    public Task<Appointment?> GetByIdAsync(Guid tenantId, Guid id) => GetByIdInternalAsync(tenantId, id);

    public Task<IEnumerable<Appointment>> GetAllAsync() => GetAllInternalAsync(null);

    public Task<IEnumerable<Appointment>> GetAllAsync(Guid tenantId) => GetAllInternalAsync(tenantId);

    public async Task UpdateAsync(Appointment entity)
    {
        const string sql = @"
UPDATE dbo.Appointments
SET PatientId = @PatientId,
    DoctorId = @DoctorId,
    AppointmentDate = @AppointmentDate,
    StartTime = @StartTime,
    EndTime = @EndTime,
    Status = @Status,
    Type = @Type,
    Source = @Source,
    Notes = @Notes,
    CancelReason = @CancelReason,
    ReminderSent = @ReminderSent,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id
  AND TenantId = @TenantId;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        const string sql = @"
UPDATE dbo.Appointments
SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new { Id = id });
    }

    public async Task<bool> HasConflictAsync(Guid tenantId, Guid doctorId, DateTime appointmentDate, TimeSpan startTime, TimeSpan endTime, Guid? excludeId = null)
    {
        const string sql = @"
SELECT COUNT(1)
FROM dbo.Appointments WITH (UPDLOCK, HOLDLOCK)
WHERE TenantId = @TenantId
  AND DoctorId = @DoctorId
  AND AppointmentDate = @AppointmentDate
  AND IsDeleted = 0
  AND Status <> @CancelledStatus
  AND (@ExcludeId IS NULL OR Id <> @ExcludeId)
  AND StartTime < @EndTime
  AND EndTime > @StartTime;";

        using var connection = _context.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(sql, new
        {
            TenantId = tenantId,
            DoctorId = doctorId,
            AppointmentDate = appointmentDate.Date,
            StartTime = startTime,
            EndTime = endTime,
            ExcludeId = excludeId,
            CancelledStatus = AppointmentStatus.Cancelled
        });

        return count > 0;
    }

    public async Task<IEnumerable<Appointment>> GetByDateAsync(Guid tenantId, DateTime appointmentDate)
    {
        const string sql = AppointmentSelect + @"
WHERE a.TenantId = @TenantId
  AND a.AppointmentDate = @AppointmentDate
  AND a.IsDeleted = 0
ORDER BY a.StartTime;";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<Appointment>(sql, new { TenantId = tenantId, AppointmentDate = appointmentDate.Date });
    }

    public async Task<IEnumerable<TimeSlot>> GetBookedSlotsAsync(Guid tenantId, Guid doctorId, DateTime appointmentDate)
    {
        const string sql = @"
SELECT StartTime, EndTime
FROM dbo.Appointments
WHERE TenantId = @TenantId
  AND DoctorId = @DoctorId
  AND AppointmentDate = @AppointmentDate
  AND IsDeleted = 0
  AND Status <> @CancelledStatus
ORDER BY StartTime;";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<TimeSlot>(sql, new
        {
            TenantId = tenantId,
            DoctorId = doctorId,
            AppointmentDate = appointmentDate.Date,
            CancelledStatus = AppointmentStatus.Cancelled
        });
    }

    public async Task<bool> UpdateStatusAsync(Guid tenantId, Guid id, AppointmentStatus status, string? cancelReason)
    {
        using var connection = _context.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        try
        {
            const string sql = @"
UPDATE dbo.Appointments
SET Status = @Status,
    CancelReason = @CancelReason,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id
  AND TenantId = @TenantId
  AND IsDeleted = 0;";

            var rows = await connection.ExecuteAsync(sql, new
            {
                TenantId = tenantId,
                Id = id,
                Status = status,
                CancelReason = cancelReason
            }, transaction);

            transaction.Commit();
            return rows > 0;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private const string AppointmentSelect = @"
SELECT
    a.*,
    p.FullName AS PatientName,
    p.PhoneNumber AS PatientPhone,
    u.FullName AS DoctorName
FROM dbo.Appointments a
INNER JOIN dbo.Patients p ON p.Id = a.PatientId AND p.TenantId = a.TenantId
INNER JOIN dbo.Users u ON u.Id = a.DoctorId AND u.TenantId = a.TenantId
";

    private async Task<Appointment?> GetByIdInternalAsync(Guid? tenantId, Guid id)
    {
        const string sql = AppointmentSelect + @"
WHERE a.Id = @Id
  AND a.IsDeleted = 0
  AND (@TenantId IS NULL OR a.TenantId = @TenantId);";

        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Appointment>(sql, new { TenantId = tenantId, Id = id });
    }

    private async Task<IEnumerable<Appointment>> GetAllInternalAsync(Guid? tenantId)
    {
        const string sql = AppointmentSelect + @"
WHERE a.IsDeleted = 0
  AND (@TenantId IS NULL OR a.TenantId = @TenantId)
ORDER BY a.AppointmentDate DESC, a.StartTime DESC;";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<Appointment>(sql, new { TenantId = tenantId });
    }
}
