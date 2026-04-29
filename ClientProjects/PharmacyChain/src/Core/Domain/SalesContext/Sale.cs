using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.SalesContext
{
    public class Sale : Entity
    {
        public Guid StoreId { get; set; }
        public Guid CustomerId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Paid, Refunded
        public DateTime SaleDate { get; set; } = DateTime.UtcNow;
    }
}
