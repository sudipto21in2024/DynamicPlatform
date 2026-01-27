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
        _ => Type // If it's not a primitive, assume it's a custom Enum or Entity reference
    };
}

public class EnumMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public List<EnumValue> Values { get; set; } = new();
}

public class EnumValue
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
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

public class PageMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public List<string> AllowedRoles { get; set; } = new();
    public List<WidgetMetadata> Widgets { get; set; } = new();
}

public class WidgetMetadata
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = "StatCard"; 
    public WidgetLayout Layout { get; set; } = new();
    public WidgetConfig Config { get; set; } = new();
    public WidgetDataSource DataSource { get; set; } = new();
}

public class WidgetLayout
{
    public GridDimension Desktop { get; set; } = new() { ColSpan = 4, RowSpan = 2 };
    public GridDimension Tablet { get; set; } = new() { ColSpan = 6, RowSpan = 2 };
    public GridDimension Mobile { get; set; } = new() { ColSpan = 12, RowSpan = 2 };
    public int ZIndex { get; set; } = 1;
}

public class GridDimension
{
    public int ColStart { get; set; }
    public int ColSpan { get; set; }
    public int RowStart { get; set; }
    public int RowSpan { get; set; }
}

public class WidgetConfig
{
    public string Title { get; set; } = string.Empty;
    public string SubTitle { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Theme { get; set; } = "primary";
}

public class WidgetDataSource
{
    public string EntityName { get; set; } = string.Empty;
    public string DataType { get; set; } = "Entity"; // Entity or CustomObject
    public string Aggregate { get; set; } = "count"; // count, sum, avg, list
    public string DataField { get; set; } = string.Empty;
    public string Filter { get; set; } = string.Empty;
    public string Sort { get; set; } = string.Empty;
    public PaginationConfig Pagination { get; set; } = new();
    public int Limit { get; set; } = 10;
}

public class PaginationConfig
{
    public bool Enabled { get; set; } = false;
    public int PageSize { get; set; } = 25;
    public bool AllowClientOverride { get; set; } = true;
}

public class FormContext
{
    // Indicates whether the form is used for creating a new entity or editing an existing one
    public string Mode { get; set; } = "Create"; // or "Edit"

    // Optional parent entity identifier (e.g., a PatientId when creating an Appointment form)
    public string? ParentEntityId { get; set; }

    // Arbitrary key‑value pairs for additional runtime data (e.g., workflow IDs, UI flags)
    public Dictionary<string, string> AdditionalData { get; set; } = new();
}

public class CustomObjectMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public List<FieldMetadata> Fields { get; set; } = new();
}
// Form models have been moved to Models/FormMetadata.cs to hold the context definition
// Form models have been moved to Models/FormMetadata.cs
