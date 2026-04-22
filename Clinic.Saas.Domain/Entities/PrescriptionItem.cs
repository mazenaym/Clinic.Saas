using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Clinic.Saas.Domain.Entities
{
    public class PrescriptionItem
    {
        public Guid Id { get; set; }
        public Guid PrescriptionId { get; set; }
        public Guid? DrugId { get; set; }
        public string DrugName { get; set; } = string.Empty;
        public string Dosage { get; set; } = string.Empty;
        public string Frequency { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string? Route { get; set; }
        public string? Instructions { get; set; }
        public int SortOrder { get; set; }

        // Navigation Properties
        public Prescription Prescription { get; set; } = null!;
        public Drug? Drug { get; set; }
    }
}
