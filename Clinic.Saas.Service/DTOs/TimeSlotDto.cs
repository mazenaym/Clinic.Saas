namespace Clinic.Saas.Service.DTOs;

public class TimeSlotDto
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; }
    public string DisplayText { get; set; } = string.Empty;
}
