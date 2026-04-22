using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Enums
{
    public enum DocumentType : short
    {
        LabResult = 1,
        Radiology = 2,
        Referral = 3,
        Insurance = 4,
        Consent = 5,
        Other = 6
    }
}
