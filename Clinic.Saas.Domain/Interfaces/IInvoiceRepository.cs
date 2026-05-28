using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice> AddAsync(Invoice invoice);
    Task<Invoice?> GetByIdAsync(Guid tenantId, Guid id);
    Task<Invoice?> AddPaymentAsync(Guid tenantId, Guid invoiceId, InvoicePayment payment);
    Task<PatientFinancialLedgerData> GetPatientLedgerAsync(Guid tenantId, Guid patientId);
    Task<string> GenerateInvoiceNumberAsync(Guid tenantId, DateTime createdAt);
}

public class PatientFinancialLedgerData
{
    public PatientFinancialLedgerSummaryRow Summary { get; set; } = new();
    public List<PatientFinancialLedgerEntryRow> Entries { get; set; } = new();
}

public class PatientFinancialLedgerSummaryRow
{
    public decimal TotalInvoiced { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal OutstandingBalance { get; set; }
}

public class PatientFinancialLedgerEntryRow
{
    public DateTime Date { get; set; }
    public string Type { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Balance { get; set; }
}
