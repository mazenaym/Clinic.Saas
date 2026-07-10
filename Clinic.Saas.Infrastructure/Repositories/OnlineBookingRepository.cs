using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;
using Clinic.Saas.Domain.Interfaces;
using Clinic.Saas.Service.Interfaces;
using Dapper;

namespace Clinic.Saas.Infrastructure.Repositories;

public class OnlineBookingRepository : IOnlineBookingRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public OnlineBookingRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<OnlineBooking>> GetByTenantAsync(Guid tenantId)
    {
        EnsureTenantId(tenantId);

        const string sql = @"
SELECT Id, TenantId, AppointmentId, PatientName, PatientPhone, PatientEmail, RequestedDate, RequestedTime,
       DoctorId, Complaint, Status, ConfirmCode, RejectReason, CreatedAt
FROM dbo.OnlineBookings
WHERE TenantId = @TenantId
ORDER BY CreatedAt DESC;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        return await connection.QueryAsync<OnlineBooking>(sql, new { TenantId = tenantId });
    }

    public async Task<bool> UpdateStatusAsync(Guid tenantId, Guid bookingId, OnlineBookingStatus status, string? rejectReason = null)
    {
        EnsureTenantId(tenantId);

        var sql = rejectReason is null
            ? @"
UPDATE dbo.OnlineBookings
SET Status = @Status
WHERE TenantId = @TenantId
  AND Id = @BookingId;"
            : @"
UPDATE dbo.OnlineBookings
SET Status = @Status,
    RejectReason = @RejectReason
WHERE TenantId = @TenantId
  AND Id = @BookingId;";

        using var connection = await _connectionFactory.CreateOpenTenantConnectionAsync(tenantId);
        var rows = await connection.ExecuteAsync(sql, new
        {
            TenantId = tenantId,
            BookingId = bookingId,
            Status = status,
            RejectReason = rejectReason
        });

        return rows > 0;
    }

    private static void EnsureTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new InvalidOperationException("TenantId is required.");
        }
    }
}
