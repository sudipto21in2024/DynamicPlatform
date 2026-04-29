using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.LogisticsContext
{
    public class DeliveryPartner : Entity
    {
        public string Name { get; set; } = string.Empty;
        public string ContactNumber { get; set; } = string.Empty;
        public string Type { get; set; } = "Internal"; // Internal, External
        public bool IsActive { get; set; } = true;
        public string ApiEndpoint { get; set; } = string.Empty;
        public double Rating { get; set; }
    }
}
