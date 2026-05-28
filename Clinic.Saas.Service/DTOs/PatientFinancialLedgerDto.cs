namespace Clinic.Saas.Service.DTOs;

public class PatientFinancialLedgerDto
{
    public PatientFinancialLedgerSummaryDto Summary { get; set; } = new();
    public List<PatientFinancialLedgerEntryDto> Entries { get; set; } = new();
}

public class PatientFinancialLedgerSummaryDto
{
    public decimal TotalInvoiced { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal OutstandingBalance { get; set; }
}

public class PatientFinancialLedgerEntryDto
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}
