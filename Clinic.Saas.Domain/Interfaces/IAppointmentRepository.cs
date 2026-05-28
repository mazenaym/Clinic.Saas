using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;

namespace Clinic.Saas.Domain.Interfaces;

public interface IAppointmentRepository : IBaseRepository<Appointment>
{
    Task<Appointment?> GetByIdAsync(Guid tenantId, Guid id);
    Task<IEnumerable<Appointment>> GetAllAsync(Guid tenantId);
    Task UpdateAsync(Guid tenantId, Appointment entity);
    Task DeleteAsync(Guid tenantId, Guid id);
    Task<bool> HasConflictAsync(Guid tenantId, Guid doctorId, DateTime appointmentDate, TimeSpan startTime, TimeSpan endTime, Guid? excludeId = null);
    Task<IEnumerable<Appointment>> GetByDateAsync(Guid tenantId, DateTime appointmentDate);
    Task<IEnumerable<TimeSlot>> GetBookedSlotsAsync(Guid tenantId, Guid doctorId, DateTime appointmentDate);
    Task<bool> UpdateStatusAsync(Guid tenantId, Guid id, AppointmentStatus status, string? cancelReason);
}

public class TimeSlot
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}
