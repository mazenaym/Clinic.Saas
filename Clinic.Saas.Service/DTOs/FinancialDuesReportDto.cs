namespace Clinic.Saas.Service.DTOs;

public class FinancialDuesReportDto
{
    public FinancialDuesSummaryDto Summary { get; set; } = new();
    public List<FinancialDuesPatientDto> Patients { get; set; } = new();
}

public class FinancialDuesSummaryDto
{
    public decimal TotalOutstanding { get; set; }
    public decimal TotalPaid { get; set; }
    public int PatientsWithDebtCount { get; set; }
}

public class FinancialDuesPatientDto
{
    public Guid PatientId { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public DateTime? LastPaymentDate { get; set; }
}
