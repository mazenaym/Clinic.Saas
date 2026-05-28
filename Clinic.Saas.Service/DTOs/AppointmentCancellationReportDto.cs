namespace Clinic.Saas.Service.DTOs;

public class AppointmentCancellationReportDto
{
    public Guid Id { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? CancelReason { get; set; }
    public DateTime UpdatedAt { get; set; }
}
