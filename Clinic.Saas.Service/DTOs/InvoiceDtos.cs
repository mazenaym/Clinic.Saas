using Clinic.Saas.Domain.Enums;

namespace Clinic.Saas.Service.DTOs;

public class InvoiceDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? VisitId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? RowVersion { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
    public List<InvoicePaymentDto> Payments { get; set; } = new();
}

public class InvoiceItemDto
{
    public Guid Id { get; set; }
    public Guid? ProcedureId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ServiceType { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public int SortOrder { get; set; }
}

public class InvoicePaymentDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }
    public DateTime PaidAt { get; set; }
}

public class CreateInvoiceDto
{
    public Guid PatientId { get; set; }
    public Guid? VisitId { get; set; }
    public string? Notes { get; set; }
    public List<CreateInvoiceItemDto> Items { get; set; } = new();
}

public class CreateInvoiceItemDto
{
    public Guid? ProcedureId { get; set; }
    public string Description { get; set; } = string.Empty;
    public ServiceType? ServiceType { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
}

public class AddInvoicePaymentDto
{
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }
}
