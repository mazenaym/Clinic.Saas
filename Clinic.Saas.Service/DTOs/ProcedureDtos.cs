namespace Clinic.Saas.Service.DTOs;

public class ProcedureDto
{
    public Guid Id { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public decimal DefaultPrice { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateProcedureDto
{
    public Guid? CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Specialty { get; set; }
    public decimal DefaultPrice { get; set; }
}

public class UpdateProcedureDto : CreateProcedureDto
{
}
