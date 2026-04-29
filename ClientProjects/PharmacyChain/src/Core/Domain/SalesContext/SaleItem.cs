using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.SalesContext
{
    public class SaleItem : Entity
    {
        public Guid SaleId { get; set; }
        public Guid MedicineId { get; set; }
        public Guid BatchId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
