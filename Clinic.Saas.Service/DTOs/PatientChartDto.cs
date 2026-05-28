namespace Clinic.Saas.Service.DTOs;

public class PatientChartDto
{
    public PatientChartDemographicsDto Patient { get; set; } = new();
    public PatientChartMedicalWarningsDto MedicalWarnings { get; set; } = new();
    public List<PatientChartVisitDto> RecentVisits { get; set; } = new();
    public List<PatientChartPrescriptionSummaryDto> RecentPrescriptions { get; set; } = new();
    public List<PatientChartAppointmentDto> RecentAppointments { get; set; } = new();
    public PatientChartPaymentSummaryDto PaymentSummary { get; set; } = new();
    public List<PatientChartDocumentDto> Documents { get; set; } = new();
    public List<PatientTimelineItemDto> Timeline { get; set; } = new();
}

public class PatientChartDemographicsDto
{
    public Guid Id { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public int? Age => DateOfBirth.HasValue ? DateTime.Now.Year - DateOfBirth.Value.Year : null;
    public string Gender { get; set; } = string.Empty;
    public string? BloodType { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? InsuranceCompany { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? RowVersion { get; set; }
}

public class PatientChartMedicalWarningsDto
{
    public string? DrugAllergies { get; set; }
    public string? ChronicDiseases { get; set; }
}

public class PatientChartVisitDto
{
    public Guid Id { get; set; }
    public DateTime VisitDate { get; set; }
    public string VisitType { get; set; } = string.Empty;
    public string ChiefComplaint { get; set; } = string.Empty;
    public string? Diagnosis { get; set; }
    public string? DiagnosisCode { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string? RowVersion { get; set; }
}

public class PatientChartPrescriptionSummaryDto
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public int ItemCount { get; set; }
    public string? ItemsSummary { get; set; }
    public bool SentViaWhatsapp { get; set; }
    public string? RowVersion { get; set; }
}

public class PatientChartAppointmentDto
{
    public Guid Id { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? RowVersion { get; set; }
}

public class PatientChartPaymentSummaryDto
{
    public int InvoiceCount { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
    public DateTime? LastPaymentAt { get; set; }
}

public class PatientChartDocumentDto
{
    public Guid Id { get; set; }
    public Guid? VisitId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int FileSizeKb { get; set; }
    public string FileType { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? RowVersion { get; set; }
}
