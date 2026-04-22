using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Enums
{
    public enum VisitType : short
    {
        New = 1,
        FollowUp = 2,
        Emergency = 3,
        Routine = 4
    }
}
