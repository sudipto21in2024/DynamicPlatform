using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.LogisticsContext
{
    public class DeliveryCoverageZone : Entity
    {
        public Guid DeliveryPartnerId { get; set; }
        public string Pincode { get; set; } = string.Empty;
        public int Priority { get; set; }
        public double EstimatedDeliveryHours { get; set; }
        public bool IsCODAllowed { get; set; }
    }
}
