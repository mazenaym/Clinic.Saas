using Clinic.Saas.Domain.Enums;

namespace Clinic.Saas.Service.DTOs;

public class ResetPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}

public class UpdateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? Phone { get; set; }
    public string? Specialty { get; set; }
    public string? LicenseNumber { get; set; }
}

public class UserPreferencesDto
{
    public string? AvatarUrl { get; set; }
    public string? Language { get; set; }
    public string? Theme { get; set; }
}

public class TenantSubscriptionStatusDto
{
    public string State { get; set; } = "Trial";
    public DateTime? TrialEndsAt { get; set; }
    public DateTime? SubscriptionEndsAt { get; set; }
    public int MaxUsers { get; set; }
    public int MaxPatientsPerMonth { get; set; }
}

public class UpdateClinicSettingsDto
{
    public string WorkingDays { get; set; } = "0111110";
    public TimeSpan OpenTime { get; set; } = new(9, 0, 0);
    public TimeSpan CloseTime { get; set; } = new(17, 0, 0);
    public int SlotDurationMin { get; set; } = 20;
    public decimal ConsultFee { get; set; }
    public bool SmsEnabled { get; set; }
    public bool WhatsappEnabled { get; set; }
    public bool EmailEnabled { get; set; }
    public string Language { get; set; } = "ar";
    public decimal TaxPct { get; set; }
}

public class RescheduleAppointmentDto
{
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
}

public class RejectOnlineBookingDto
{
    public string RejectReason { get; set; } = string.Empty;
}

public class OnlineBookingOperationDto
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string PatientPhone { get; set; } = string.Empty;
    public string? PatientEmail { get; set; }
    public DateTime RequestedDate { get; set; }
    public TimeSpan RequestedTime { get; set; }
    public Guid? DoctorId { get; set; }
    public string? Complaint { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ConfirmCode { get; set; } = string.Empty;
    public string? RejectReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateVisitDto : CreateVisitDto
{
}

public class ClinicalTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? ClinicalNotes { get; set; }
    public string? Diagnosis { get; set; }
}

public class CreateClinicalTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? ClinicalNotes { get; set; }
    public string? Diagnosis { get; set; }
}

public class UpdatePaymentDto : CreatePaymentDto
{
}

public class RefundPaymentDto
{
    public string? Reason { get; set; }
}

public class AuditLogDto
{
    public long Id { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string? NewValues { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PatientTimelineItemDto
{
    public string Type { get; set; } = string.Empty;
    public Guid Id { get; set; }
    public DateTime Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Details { get; set; }
}
