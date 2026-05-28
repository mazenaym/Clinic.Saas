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
    Task<IEnumerable<Appointment>> GetByDateRangeAsync(Guid tenantId, DateTime from, DateTime to);
    Task<IEnumerable<AppointmentCancellationReportRow>> GetCancellationReportAsync(Guid tenantId, DateTime from, DateTime to);
    Task<IEnumerable<TimeSlot>> GetBookedSlotsAsync(Guid tenantId, Guid doctorId, DateTime appointmentDate);
    Task<bool> UpdateStatusAsync(Guid tenantId, Guid id, AppointmentStatus status, string? cancelReason);
}

public class TimeSlot
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class AppointmentCancellationReportRow
{
    public Guid Id { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? CancelReason { get; set; }
    public DateTime UpdatedAt { get; set; }
}
