using Clinic.Saas.Domain.Entities;
using Clinic.Saas.Service.DTOs;

namespace Clinic.Saas.Service.UseCases.Invoices;

internal static class InvoiceMapper
{
    public static InvoiceDto ToDto(Invoice invoice) => new()
    {
        Id = invoice.Id,
        PatientId = invoice.PatientId,
        VisitId = invoice.VisitId,
        InvoiceNumber = invoice.InvoiceNumber,
        PatientName = invoice.PatientName,
        Subtotal = invoice.Subtotal,
        DiscountAmount = invoice.DiscountAmount,
        TaxAmount = invoice.TaxAmount,
        GrandTotal = invoice.GrandTotal,
        PaidAmount = invoice.PaidAmount,
        RemainingAmount = invoice.RemainingAmount,
        Status = invoice.Status.ToString(),
        Notes = invoice.Notes,
        CreatedAt = invoice.CreatedAt,
        UpdatedAt = invoice.UpdatedAt,
        RowVersion = invoice.RowVersion.ToBase64RowVersion(),
        Items = invoice.Items.Select(item => new InvoiceItemDto
        {
            Id = item.Id,
            ProcedureId = item.ProcedureId,
            Description = item.Description,
            ServiceType = item.ServiceType?.ToString(),
            Quantity = item.Quantity,
            UnitPrice = item.UnitPrice,
            DiscountAmount = item.DiscountAmount,
            TaxAmount = item.TaxAmount,
            LineTotal = item.LineTotal,
            SortOrder = item.SortOrder
        }).ToList(),
        Payments = invoice.Payments.Select(payment => new InvoicePaymentDto
        {
            Id = payment.Id,
            Amount = payment.Amount,
            PaymentMethod = payment.PaymentMethod.ToString(),
            PaymentReference = payment.PaymentReference,
            Notes = payment.Notes,
            PaidAt = payment.PaidAt
        }).ToList()
    };
}
