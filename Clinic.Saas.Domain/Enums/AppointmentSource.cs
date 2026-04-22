using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Enums
{
    public enum AppointmentSource : short
    {
        Reception = 1,
        OnlinePortal = 2,
        Phone = 3,
        WalkIn = 4
    }
}
