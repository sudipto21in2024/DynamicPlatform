using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.PricingContext
{
    public class CouponCode : Entity
    {
        public string Code { get; set; } = string.Empty;
        public Guid DiscountRuleId { get; set; }
        public int UsageLimit { get; set; }
        public int UsageCount { get; set; }
    }
}
