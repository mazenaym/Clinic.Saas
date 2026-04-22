using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Enums
{
    public enum ServiceType : short
    {
        Consultation = 1,
        Lab = 2,
        Radiology = 3,
        Procedure = 4,
        Drug = 5,
        Other = 6
    }
}
