namespace Clinic.Saas.Service.DTOs;

public class PrescriptionPdfDto
{
    public byte[] Content { get; set; } = [];
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
}

public class DrugCatalogItemDto
{
    public Guid Id { get; set; }
    public string TradeName { get; set; } = string.Empty;
    public string GenericName { get; set; } = string.Empty;
    public string? Strength { get; set; }
    public string? Form { get; set; }
    public string? Interactions { get; set; }
}

public class DrugInteractionWarningDto
{
    public string Drug { get; set; } = string.Empty;
    public string Warning { get; set; } = string.Empty;
}
