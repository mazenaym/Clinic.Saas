using Clinic.Saas.Domain.Enums;

namespace Clinic.Saas.Domain.Entities;

public class InvoiceItem
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? ProcedureId { get; set; }
    public string Description { get; set; } = string.Empty;
    public ServiceType? ServiceType { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public int SortOrder { get; set; }
}
