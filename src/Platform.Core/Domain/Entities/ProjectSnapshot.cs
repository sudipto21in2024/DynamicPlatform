using System;
using System.ComponentModel.DataAnnotations;

namespace Platform.Core.Domain.Entities;

/// <summary>
/// Represents an immutable point-in-time snapshot of a project's metadata.
/// This allows for versioning, diffing, and safe rollbacks.
/// </summary>
public class ProjectSnapshot
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProjectId { get; set; }
    public virtual Project Project { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Stores the full project metadata at this point in time (all entities, workflows, etc.).
    /// This is typically a serialized collection of all current Artifacts.
    /// </summary>
    [Required]
    public string Content { get; set; } = "{}";

    [Required]
    [MaxLength(64)]
    public string Hash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Indicates if this snapshot has been published and applied to the database schema.
    /// </summary>
    public bool IsPublished { get; set; }
}
