using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;

namespace Clinic.Saas.Domain.Interfaces;

public interface IOnlineBookingRepository
{
    Task<IEnumerable<OnlineBooking>> GetByTenantAsync(Guid tenantId);
    Task<bool> UpdateStatusAsync(Guid tenantId, Guid bookingId, OnlineBookingStatus status, string? rejectReason = null);
}
