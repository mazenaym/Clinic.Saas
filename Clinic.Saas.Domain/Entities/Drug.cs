using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Entities
{
    public class Drug
    {
        public Guid Id { get; set; }
        public string TradeName { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Strength { get; set; }
        public string? Form { get; set; }
        public string? Unit { get; set; }
        public string? Contraindications { get; set; }
        public string? Interactions { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }

        // Navigation Properties
        public ICollection<PrescriptionItem> PrescriptionItems { get; set; } = new List<PrescriptionItem>();
    }
}
