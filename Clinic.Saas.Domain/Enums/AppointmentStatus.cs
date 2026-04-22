using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Enums
{
    public enum AppointmentStatus : short
    {
        Scheduled = 1,
        Confirmed = 2,
        Completed = 3,
        Cancelled = 4,
        NoShow = 5
    }
}
