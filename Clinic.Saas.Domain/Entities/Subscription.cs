using Clinic.Saas.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Clinic.Saas.Domain.Entities
{
    public class Subscription
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public PlanType Plan { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal AmountPaid { get; set; }
        public SubscriptionStatus Status { get; set; }
        public string? PaymentRef { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
