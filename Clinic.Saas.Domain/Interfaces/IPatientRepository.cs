using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Domain.Enums;

namespace Clinic.Saas.Domain.Interfaces
{
    public interface IPatientRepository : IBaseRepository<Patient>
    {
        Task<Patient?> GetByIdAsync(Guid tenantId, Guid id);
        Task<IEnumerable<Patient>> GetAllAsync(Guid tenantId);
        Task UpdateAsync(Guid tenantId, Patient entity);
        Task DeleteAsync(Guid tenantId, Guid id, byte[] rowVersion);
        Task<Patient?> GetByPhoneAsync(Guid tenantId, string phone);
        Task<IEnumerable<Patient>> SearchAsync(Guid tenantId, string searchTerm);
        Task<IEnumerable<PatientTimelineRow>> GetTimelineAsync(Guid tenantId, Guid patientId);
        Task<PatientChartData> GetChartAsync(Guid tenantId, Guid patientId);
        Task<IEnumerable<PatientDuplicateRow>> FindDuplicatesAsync(Guid tenantId, string? phone, string? nationalId);
        Task<IEnumerable<PatientExportRow>> GetExportRowsAsync(Guid tenantId);
        Task<bool> ExistsAsync(Guid tenantId, string phone);
        Task<string> GenerateNextPatientCodeAsync(Guid tenantId);
    }

    public class PatientTimelineRow
    {
        public string Type { get; set; } = string.Empty;
        public Guid Id { get; set; }
        public DateTime Date { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Details { get; set; }
    }

    public class PatientChartData
    {
        public PatientChartPatientRow? Patient { get; set; }
        public List<PatientChartVisitRow> RecentVisits { get; set; } = new();
        public List<PatientChartPrescriptionRow> RecentPrescriptions { get; set; } = new();
        public List<PatientChartAppointmentRow> RecentAppointments { get; set; } = new();
        public PatientChartPaymentSummaryRow PaymentSummary { get; set; } = new();
        public List<PatientChartDocumentRow> Documents { get; set; } = new();
        public List<PatientTimelineRow> Timeline { get; set; } = new();
    }

    public class PatientChartPatientRow
    {
        public Guid Id { get; set; }
        public string PatientCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public Gender Gender { get; set; }
        public string? BloodType { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? InsuranceCompany { get; set; }
        public string? DrugAllergies { get; set; }
        public string? ChronicDiseases { get; set; }
        public DateTime CreatedAt { get; set; }
        public byte[] RowVersion { get; set; } = [];
    }

    public class PatientChartVisitRow
    {
        public Guid Id { get; set; }
        public DateTime VisitDate { get; set; }
        public VisitType VisitType { get; set; }
        public string ChiefComplaint { get; set; } = string.Empty;
        public string? Diagnosis { get; set; }
        public string? DiagnosisCode { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public byte[] RowVersion { get; set; } = [];
    }

    public class PatientChartPrescriptionRow
    {
        public Guid Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public string? ItemsSummary { get; set; }
        public bool SentViaWhatsapp { get; set; }
        public byte[] RowVersion { get; set; } = [];
    }

    public class PatientChartAppointmentRow
    {
        public Guid Id { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public AppointmentStatus Status { get; set; }
        public AppointmentType Type { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public byte[] RowVersion { get; set; } = [];
    }

    public class PatientChartPaymentSummaryRow
    {
        public int InvoiceCount { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalOutstanding { get; set; }
        public DateTime? LastPaymentAt { get; set; }
    }

    public class PatientChartDocumentRow
    {
        public Guid Id { get; set; }
        public Guid? VisitId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public int FileSizeKb { get; set; }
        public string FileType { get; set; } = string.Empty;
        public DocumentType DocumentType { get; set; }
        public string? Description { get; set; }
        public DateTime UploadedAt { get; set; }
        public byte[] RowVersion { get; set; } = [];
    }

    public class PatientDuplicateRow
    {
        public Guid Id { get; set; }
        public string PatientCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? NationalId { get; set; }
    }

    public class PatientExportRow
    {
        public string PatientCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? NationalId { get; set; }
        public string? Email { get; set; }
        public short Gender { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
