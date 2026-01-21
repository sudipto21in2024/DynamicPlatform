using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Platform.Core.Domain.Entities;

public class Project
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
    public virtual Tenant? Tenant { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Version { get; set; } = "1.0.0";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual ICollection<Artifact>? Artifacts { get; set; }
}
