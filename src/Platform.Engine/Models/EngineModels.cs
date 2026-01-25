using System.Collections.Generic;

namespace Platform.Engine.Models;

public class EntityMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public List<FieldMetadata> Fields { get; set; } = new();
    public List<RelationMetadata> Relations { get; set; } = new();
    public EntityEventConfig Events { get; set; } = new();
}

public class EntityEventConfig
{
    public bool OnCreate { get; set; } = true;
    public bool OnUpdate { get; set; } = true;
    public bool OnDelete { get; set; } = true;
}

public class FieldMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "string"; // "string", "int", "guid"
    public bool IsRequired { get; set; }
    public int MaxLength { get; set; }
    
    public List<ValidationRule> Rules { get; set; } = new();

    // Helper for Scriban
    public string CsharpType => Type.ToLower() switch 
    {
        "string" => "string",
        "int" => "int",
        "guid" => "Guid",
        "datetime" => "DateTime",
        "decimal" => "decimal",
        "bool" => "bool",
        _ => "string"
    };
}

public class ValidationRule
{
    public string Type { get; set; } = string.Empty; // "Regex", "Range", "Email"
    public string Value { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

public enum RelationType
{
    OneToMany,
    ManyToOne,
    ManyToMany
}

public class RelationMetadata
{
    public string TargetEntity { get; set; } = string.Empty;
    public string ForeignKeyName { get; set; } = string.Empty; // Used for OneToMany/ManyToOne
    public string NavPropName { get; set; } = string.Empty;
    public RelationType Type { get; set; } = RelationType.ManyToOne;
    
    // For ManyToMany
    public string? InverseNavPropName { get; set; }
    public string? JoinTableName { get; set; }
}
