using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.StoreContext
{
    public class Store : Entity
    {
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string LicenseNumber { get; set; } = string.Empty;
        public bool IsOnlineEnabled { get; set; }
    }
}
