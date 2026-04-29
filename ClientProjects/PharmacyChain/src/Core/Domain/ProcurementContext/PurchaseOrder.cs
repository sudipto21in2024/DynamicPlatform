using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.ProcurementContext
{
    public class PurchaseOrder : Entity
    {
        public Guid SupplierId { get; set; }
        public Guid StoreId { get; set; }
        public string Status { get; set; } = "Draft"; // Draft, Submitted, Received, Cancelled
        public DateTime ExpectedDate { get; set; }
        public decimal TotalCost { get; set; }
    }
}
