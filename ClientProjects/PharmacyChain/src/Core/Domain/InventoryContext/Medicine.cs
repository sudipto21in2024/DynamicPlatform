using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.InventoryContext
{
    public class Medicine : Entity
    {
        public string Name { get; set; } = string.Empty;
        public string GenericName { get; set; } = string.Empty;
        public string Manufacturer { get; set; } = string.Empty;
        public bool IsPrescriptionRequired { get; set; }
        public bool IsControlledSubstance { get; set; }
    }
}
