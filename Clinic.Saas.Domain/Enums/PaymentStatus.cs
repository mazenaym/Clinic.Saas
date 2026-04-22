using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Enums
{
    public enum PaymentStatus : short
    {
        Pending = 1,
        Partial = 2,
        Paid = 3,
        Refunded = 4,
        Cancelled = 5
    }
}
