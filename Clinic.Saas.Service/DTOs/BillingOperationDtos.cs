namespace Clinic.Saas.Service.DTOs;

public class PaymentDebtDto
{
    public Guid PatientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public decimal TotalDebt { get; set; }
}

public class MonthlyRevenueDto
{
    public DateTime Date { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int InvoiceCount { get; set; }
}

public class ReceiptPdfDto
{
    public byte[] Content { get; set; } = [];
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
}
