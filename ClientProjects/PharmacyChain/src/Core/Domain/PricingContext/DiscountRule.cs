using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.PricingContext
{
    public class DiscountRule : Entity
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Percentage"; // Percentage, FlatOff, BuyXGetY
        public decimal Value { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal MinPurchaseAmount { get; set; }
    }
}
