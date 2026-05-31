namespace Clinic.Saas.Service.DTOs;

public class AppointmentScheduleDto
{
    public DateTime Date { get; set; }
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public int SlotDurationMin { get; set; }
    public List<ScheduleDoctorDto> Doctors { get; set; } = new();
    public List<ScheduleAppointmentDto> Appointments { get; set; } = new();
}

public class ScheduleDoctorDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Specialty { get; set; }
}

public class ScheduleAppointmentDto
{
    public Guid Id { get; set; }
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string? PatientPhone { get; set; }
    public string? Notes { get; set; }
    public string? RowVersion { get; set; }
}
