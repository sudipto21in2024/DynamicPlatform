using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.LogisticsContext
{
    public class OnlineOrder : Entity
    {
        public string OrderNumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public Guid StoreId { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentStatus { get; set; } = "Pending";
        public string OrderStatus { get; set; } = "Created";
        public string DeliveryAddress { get; set; } = string.Empty;
        public string Pincode { get; set; } = string.Empty;
        public DateTime? EstimatedDeliveryTime { get; set; }
        public Guid? AssignedDeliveryPartnerId { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
    }
}
