using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Enums
{
    public enum AppointmentType : short
    {
        New = 1,
        FollowUp = 2,
        Emergency = 3,
        Telemedicine = 4
    }
}
