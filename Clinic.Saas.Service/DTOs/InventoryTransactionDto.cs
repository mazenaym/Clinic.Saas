using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class InventoryTransactionDto
    {
        public Guid Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string TxType { get; set; } = string.Empty;
        public int QuantityChange { get; set; }
        public int QuantityAfter { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
}
