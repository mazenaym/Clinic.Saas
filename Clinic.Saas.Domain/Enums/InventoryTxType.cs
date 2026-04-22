using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Enums
{
    public enum InventoryTxType : short
    {
        StockIn = 1,
        StockOut = 2,
        Adjustment = 3,
        Expired = 4,
        Returned = 5
    }
}
