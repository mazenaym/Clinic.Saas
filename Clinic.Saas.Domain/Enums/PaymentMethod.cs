using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Enums
{
    public enum PaymentMethod : short
    {
        Cash = 1,
        Card = 2,
        BankTransfer = 3,
        Insurance = 4,
        Mixed = 5
    }
}
