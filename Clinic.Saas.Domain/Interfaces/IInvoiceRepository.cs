using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice> AddAsync(Invoice invoice);
    Task<Invoice?> GetByIdAsync(Guid tenantId, Guid id);
    Task<Invoice?> AddPaymentAsync(Guid tenantId, Guid invoiceId, InvoicePayment payment);
    Task<PatientFinancialLedgerData> GetPatientLedgerAsync(Guid tenantId, Guid patientId);
    Task<FinancialDuesReportData> GetFinancialDuesAsync(Guid tenantId, DateTime? from, DateTime? toExclusive, Guid? doctorId);
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

public class FinancialDuesReportData
{
    public FinancialDuesSummaryRow Summary { get; set; } = new();
    public List<FinancialDuesPatientRow> Patients { get; set; } = new();
}

public class FinancialDuesSummaryRow
{
    public decimal TotalOutstanding { get; set; }
    public decimal TotalPaid { get; set; }
    public int PatientsWithDebtCount { get; set; }
}

public class FinancialDuesPatientRow
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public DateTime? LastPaymentDate { get; set; }
}
