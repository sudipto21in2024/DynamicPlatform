using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.InventoryContext
{
    public class Batch : Entity
    {
        public Guid MedicineId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; }
        public decimal MRP { get; set; }
        public decimal PurchasePrice { get; set; }
        public Guid StoreId { get; set; }
        public int Quantity { get; set; }
    }
}
