using System;
using Platform.Runtime.Domain;

namespace PharmacyChain.Core.Domain.SalesContext
{
    public class Prescription : Entity
    {
        public Guid CustomerId { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Status { get; set; } = "Draft"; // Draft, Verified, Rejected
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime IssueDate { get; set; }
    }
}
