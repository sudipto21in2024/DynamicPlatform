using System;
using System.ComponentModel.DataAnnotations;

namespace Platform.Core.Domain.Entities;

public enum ArtifactType
{
    Entity = 1,
    Page = 2,
    Workflow = 3,
    Integration = 4,
    Connector = 5,
    SecurityConfig = 6,
    UsersConfig = 7,
    CustomObject = 8,
    Enum = 9,
    Form = 10,
    Widget = 11
}

public class Artifact
{
    [Key]
    public Guid Id { get; set; }

    public Guid ProjectId { get; set; }
    public virtual Project Project { get; set; }

    public ArtifactType Type { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Stores the pure JSON metadata definition.
    /// This is the "Source Code" of the low-code platform.
    /// </summary>
    public string Content { get; set; } = "{}";

    public DateTime LastModified { get; set; } = DateTime.UtcNow;
}
