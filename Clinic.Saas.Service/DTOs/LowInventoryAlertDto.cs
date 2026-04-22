using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Service.DTOs
{
    public class LowInventoryAlertDto
    {
        public Guid Id { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int CurrentQuantity { get; set; }
        public int MinQuantity { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string AlertType { get; set; } = string.Empty;
    }
}
