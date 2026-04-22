using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Enums
{
    public enum NotificationChannel : short
    {
        SMS = 1,
        WhatsApp = 2,
        Email = 3,
        InApp = 4,
        Push = 5
    }
}
