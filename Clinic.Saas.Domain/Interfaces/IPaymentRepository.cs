using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface IPaymentRepository : IBaseRepository<Payment>
{
    Task<Payment?> GetByIdAsync(Guid tenantId, Guid id);
    Task<IEnumerable<Payment>> GetByPatientAsync(Guid tenantId, Guid patientId);
    Task UpdateAsync(Guid tenantId, Payment entity);
    Task<bool> UpdateWithItemsAsync(Guid tenantId, Payment entity);
    Task<bool> RefundAsync(Guid tenantId, Guid id, string? reason, byte[] rowVersion);
    Task DeleteAsync(Guid tenantId, Guid id, byte[] rowVersion);
    Task<string> GenerateInvoiceNumberAsync(Guid tenantId, DateTime createdAt);
    Task<IEnumerable<Payment>> GetByDateAsync(Guid tenantId, DateTime date);
    Task<IEnumerable<PaymentDebtRow>> GetDebtTrackingAsync(Guid tenantId);
    Task<IEnumerable<MonthlyRevenueRow>> GetMonthlyRevenueAsync(Guid tenantId, DateTime start, DateTime end);
}

public class PaymentDebtRow
{
    public Guid PatientId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public decimal TotalDebt { get; set; }
}

public class MonthlyRevenueRow
{
    public DateTime Date { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public int InvoiceCount { get; set; }
}
