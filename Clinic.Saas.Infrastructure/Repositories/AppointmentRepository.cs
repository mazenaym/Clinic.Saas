using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.Interfaces;
using Dapper;
using System.Data;

namespace Clinic.Saas.Infrastructure.Repositories;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AppointmentRepository(IDbConnectionFactory context)
    {
        _connectionFactory = context;
    }

    public async Task<Appointment> AddAsync(Appointment entity)
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

            var referenceStatus = await connection.QuerySingleAsync<AppointmentReferenceStatus>(
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

    public Task<Appointment?> GetByIdAsync(Guid id) =>
        throw new NotSupportedException("Use GetByIdAsync(Guid tenantId, Guid id) for tenant-owned data.");

    public async Task<Appointment?> GetByIdAsync(Guid tenantId, Guid id)
    {
        EnsureTenantId(tenantId);

        const string sql = AppointmentSelect + @"
WHERE a.Id = @Id
  AND a.TenantId = @TenantId
  AND a.IsDeleted = 0;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryFirstOrDefaultAsync<Appointment>(sql, new
        {
            TenantId = tenantId,
            Id = id
        });
    }

    public Task<IEnumerable<Appointment>> GetAllAsync() =>
        throw new NotSupportedException("Use GetAllAsync(Guid tenantId) for tenant-owned data.");

    public async Task<IEnumerable<Appointment>> GetAllAsync(Guid tenantId)
    {
        EnsureTenantId(tenantId);

        const string sql = AppointmentSelect + @"
WHERE a.TenantId = @TenantId
  AND a.IsDeleted = 0
ORDER BY a.AppointmentDate DESC, a.StartTime DESC;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryAsync<Appointment>(sql, new
        {
            TenantId = tenantId
        });
    }

    public Task UpdateAsync(Appointment entity) =>
        throw new NotSupportedException("Use UpdateAsync(Guid tenantId, Appointment entity) for tenant-owned data.");

    public async Task UpdateAsync(Guid tenantId, Appointment entity)
    {
        EnsureTenantId(tenantId);

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

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        await connection.ExecuteAsync(sql, new
        {
            entity.Id,
            TenantId = tenantId,
            entity.PatientId,
            entity.DoctorId,
            AppointmentDate = entity.AppointmentDate.Date,
            entity.StartTime,
            entity.EndTime,
            entity.Status,
            entity.Type,
            entity.Source,
            entity.Notes,
            entity.CancelReason,
            entity.ReminderSent
        });
    }

    public Task DeleteAsync(Guid id) =>
        throw new NotSupportedException("Use DeleteAsync(Guid tenantId, Guid id) for tenant-owned data.");

    public async Task DeleteAsync(Guid tenantId, Guid id)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
UPDATE dbo.Appointments
SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id
  AND TenantId = @TenantId;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        await connection.ExecuteAsync(sql, new { TenantId = tenantId, Id = id });
    }

    public async Task<bool> HasConflictAsync(Guid tenantId, Guid doctorId, DateTime appointmentDate, TimeSpan startTime, TimeSpan endTime, Guid? excludeId = null)
    {
        EnsureTenantId(tenantId);

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

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
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
        EnsureTenantId(tenantId);

        const string sql = AppointmentSelect + @"
WHERE a.TenantId = @TenantId
  AND a.AppointmentDate = @AppointmentDate
  AND a.IsDeleted = 0
ORDER BY a.StartTime;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryAsync<Appointment>(sql, new { TenantId = tenantId, AppointmentDate = appointmentDate.Date });
    }

    public async Task<IEnumerable<Appointment>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to)
    {
        EnsureTenantId(tenantId);

        const string sql = AppointmentSelect + @"
WHERE a.TenantId = @TenantId
  AND a.AppointmentDate >= @From
  AND a.AppointmentDate < @To
  AND a.IsDeleted = 0
ORDER BY a.AppointmentDate, a.StartTime;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryAsync<Appointment>(sql, new
        {
            TenantId = tenantId,
            From = from.Date,
            To = to.Date
        });
    }

    public async Task<IEnumerable<AppointmentCancellationReportRow>> GetCancellationReportAsync(Guid tenantId, DateTime from, DateTime to)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
SELECT Id, AppointmentDate, StartTime, EndTime, CancelReason, UpdatedAt
FROM dbo.Appointments
WHERE TenantId = @TenantId
  AND Status = @Cancelled
  AND AppointmentDate >= @From
  AND AppointmentDate < @To
  AND IsDeleted = 0
ORDER BY AppointmentDate DESC;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
        return await connection.QueryAsync<AppointmentCancellationReportRow>(sql, new
        {
            TenantId = tenantId,
            Cancelled = AppointmentStatus.Cancelled,
            From = from.Date,
            To = to.Date
        });
    }

    public async Task<IEnumerable<TimeSlot>> GetBookedSlotsAsync(Guid tenantId, Guid doctorId, DateTime appointmentDate)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
SELECT StartTime, EndTime
FROM dbo.Appointments
WHERE TenantId = @TenantId
  AND DoctorId = @DoctorId
  AND AppointmentDate = @AppointmentDate
  AND IsDeleted = 0
  AND Status <> @CancelledStatus
ORDER BY StartTime;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
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
        EnsureTenantId(tenantId);

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync();
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

    private static void EnsureTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantId is required.");
        }
    }

    private sealed class AppointmentReferenceStatus
    {
        public int PatientExists { get; set; }
        public int DoctorExists { get; set; }
    }
}
