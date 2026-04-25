using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Infrastructure.Data;
using Dapper;

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

        var sql = @"
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
);

SELECT * FROM dbo.Appointments WHERE Id = @Id;";

        using var connection = _context.CreateConnection();
        return await connection.QuerySingleAsync<Appointment>(sql, entity);
    }

    public async Task<Appointment?> GetByIdAsync(Guid id)
    {
        const string sql = @"
SELECT * FROM dbo.Appointments
WHERE Id = @Id AND IsDeleted = 0;";

        using var connection = _context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Appointment>(sql, new { Id = id });
    }

    public async Task<IEnumerable<Appointment>> GetAllAsync()
    {
        const string sql = @"
SELECT * FROM dbo.Appointments
WHERE IsDeleted = 0
ORDER BY AppointmentDate DESC, StartTime DESC;";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<Appointment>(sql);
    }

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
    UpdatedAt = @UpdatedAt
WHERE Id = @Id;";

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
        var sql = @"
SELECT COUNT(1)
FROM dbo.Appointments
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
        const string sql = @"
SELECT * FROM dbo.Appointments
WHERE TenantId = @TenantId
  AND AppointmentDate = @AppointmentDate
  AND IsDeleted = 0
ORDER BY StartTime;";

        using var connection = _context.CreateConnection();
        return await connection.QueryAsync<Appointment>(sql, new
        {
            TenantId = tenantId,
            AppointmentDate = appointmentDate.Date
        });
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

    public async Task UpdateStatusAsync(Guid id, AppointmentStatus status, string? cancelReason)
    {
        const string sql = @"
UPDATE dbo.Appointments
SET Status = @Status,
    CancelReason = @CancelReason,
    UpdatedAt = SYSUTCDATETIME()
WHERE Id = @Id;";

        using var connection = _context.CreateConnection();
        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            Status = status,
            CancelReason = cancelReason
        });
    }
}
