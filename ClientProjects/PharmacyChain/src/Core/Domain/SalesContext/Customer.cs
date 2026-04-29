using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.SalesContext
{
    public class Customer : Entity
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int LoyaltyPoints { get; set; }
        public string InsuranceProviderId { get; set; } = string.Empty;
    }
}
