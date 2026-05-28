namespace Clinic.Saas.Domain.Entities;

public class ClinicalTemplate
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? ClinicalNotes { get; set; }
    public string? Diagnosis { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
