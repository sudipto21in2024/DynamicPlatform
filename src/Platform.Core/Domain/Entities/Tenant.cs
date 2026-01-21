using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Platform.Core.Domain.Entities;

public class Tenant
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsActive { get; set; } = true;

    // Navigation
    public virtual ICollection<Project> Projects { get; set; } = new List<Project>();
}
