using Clinic.Saas.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Interfaces
{
    public interface IPatientRepository :IBaseRepository<Patient>
    {
        Task<Patient?> GetByIdAsync(Guid tenantId, Guid id);
        Task<IEnumerable<Patient>> GetAllAsync(Guid tenantId);
        Task UpdateAsync(Guid tenantId, Patient entity);
        Task DeleteAsync(Guid tenantId, Guid id);
        Task<Patient?> GetByPhoneAsync(Guid tenantId, string phone);
        Task<IEnumerable<Patient>> SearchAsync(Guid tenantId, string searchTerm);
        Task<IEnumerable<PatientTimelineRow>> GetTimelineAsync(Guid tenantId, Guid patientId);
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
