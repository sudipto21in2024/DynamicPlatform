using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.StoreContext
{
    public class Staff : Entity
    {
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // Pharmacist, Cashier, Manager
        public Guid StoreId { get; set; }
    }
}
