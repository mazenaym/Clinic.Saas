using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Enums
{
    public enum SubscriptionStatus : short
    {
        Active = 1,
        Expired = 2,
        Cancelled = 3,
        Trial = 4,
        PastDue = 5,
        Suspended = 6
    }
}
