namespace Clinic.Saas.Domain.Enums;

public enum SubscriptionPaymentStatus : short
{
    Pending = 1,
    Paid = 2,
    Failed = 3,
    Refunded = 4
}
