using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.ProcurementContext
{
    public class Supplier : Entity
    {
        public string Name { get; set; } = string.Empty;
        public string ContactInfo { get; set; } = string.Empty;
        public string GstNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}
