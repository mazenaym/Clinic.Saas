namespace Clinic.Saas.Service.DTOs;

public class PatientDuplicateDto
{
    public Guid Id { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? NationalId { get; set; }
}

public class CsvFileDto
{
    public byte[] Content { get; set; } = [];
    public string ContentType { get; set; } = "text/csv";
    public string FileName { get; set; } = string.Empty;
}
