using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Enums
{
    public enum NotificationStatus : short
    {
        Pending = 1,
        Sent = 2,
        Delivered = 3,
        Failed = 4,
        Cancelled = 5
    }
}
