using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Enums
{
    public enum OnlineBookingStatus : short
    {
        Pending = 1,
        Confirmed = 2,
        Rejected = 3,
        Expired = 4
    }
}
