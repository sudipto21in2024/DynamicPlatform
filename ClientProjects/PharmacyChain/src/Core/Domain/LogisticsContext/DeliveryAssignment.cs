using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.LogisticsContext
{
    public class DeliveryAssignment : Entity
    {
        public Guid OnlineOrderId { get; set; }
        public Guid DeliveryPartnerId { get; set; }
        public string DeliveryStatus { get; set; } = "Assigned";
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PickedUpAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string FailedReason { get; set; } = string.Empty;
    }
}
