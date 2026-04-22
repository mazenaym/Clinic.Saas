using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Enums
{
    public enum LabRequestStatus : short
    {
        Requested = 1,
        SampleTaken = 2,
        Processing = 3,
        Done = 4,
        Cancelled = 5
    }
}
