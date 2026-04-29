using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.InventoryContext
{
    public class StockMovement : Entity
    {
        public string Type { get; set; } = string.Empty; // Sale, Purchase, Expiry, Damaged, Transfer
        public Guid BatchId { get; set; }
        public int Quantity { get; set; } // Positive for In, Negative for Out
        public Guid ReferenceId { get; set; } // SaleId, POId, or TransferId
        public DateTime MovementDate { get; set; } = DateTime.UtcNow;
    }
}
