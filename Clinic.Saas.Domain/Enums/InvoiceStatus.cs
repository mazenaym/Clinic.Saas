namespace Clinic.Saas.Domain.Enums;

public enum InvoiceStatus : short
{
    Draft = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Cancelled = 4
}
