using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class InventoryDto
    {
        public Guid Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public int Quantity { get; set; }
        public int MinQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? Supplier { get; set; }
    }
}
