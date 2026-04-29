using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.PricingContext
{
    public class ApplicableScope : Entity
    {
        public Guid DiscountRuleId { get; set; }
        public string TargetType { get; set; } = "Medicine"; // Medicine, Category, Brand, WholeCart
        public Guid TargetId { get; set; } // Null if WholeCart
    }
}
