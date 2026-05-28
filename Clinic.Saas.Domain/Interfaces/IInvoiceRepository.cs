using Clinic.Saas.Domain.Entities;

namespace Clinic.Saas.Domain.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice> AddAsync(Invoice invoice);
    Task<Invoice?> GetByIdAsync(Guid tenantId, Guid id);
    Task<Invoice?> AddPaymentAsync(Guid tenantId, Guid invoiceId, InvoicePayment payment);
    Task<string> GenerateInvoiceNumberAsync(Guid tenantId, DateTime createdAt);
}
