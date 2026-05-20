using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface IPaymentRepository : IBaseRepository<Payment>
{
    Task<Payment?> GetByIdAsync(Guid tenantId, Guid id);
    Task<string> GenerateInvoiceNumberAsync(Guid tenantId, DateTime createdAt);
    Task<IEnumerable<Payment>> GetByDateAsync(Guid tenantId, DateTime date);
}
