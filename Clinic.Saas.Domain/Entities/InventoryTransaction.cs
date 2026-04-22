using Clinic.Saas.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Entities
{
    public class InventoryTransaction
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid InventoryId { get; set; }
        public Guid UserId { get; set; }
        public InventoryTxType TxType { get; set; }
        public int QuantityChange { get; set; }
        public int QuantityAfter { get; set; }
        public Guid? ReferenceId { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public Tenant Tenant { get; set; } = null!;
        public Inventory Inventory { get; set; } = null!;
        public User User { get; set; } = null!;
    }
}
