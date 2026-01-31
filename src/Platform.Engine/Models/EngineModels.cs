using System.Collections.Generic;

namespace Platform.Engine.Models;

public class EntityMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();
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
    public Guid Id { get; set; } = Guid.NewGuid();
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
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public List<EnumValue> Values { get; set; } = new();
}

public class EnumValue
{
    public Guid Id { get; set; } = Guid.NewGuid();
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
    public Guid Id { get; set; } = Guid.NewGuid();
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
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public List<string> AllowedRoles { get; set; } = new();
    public List<WidgetMetadata> Widgets { get; set; } = new();
}

public class WidgetMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = "StatCard"; 
    public WidgetLayout Layout { get; set; } = new();
    
    // Dynamic Properties (replaces fixed Config)
    public Dictionary<string, object> Properties { get; set; } = new();
    
    // Universal Data Binding
    public WidgetDataSource Bindings { get; set; } = new();
    
    // Micro-interactions
    public List<WidgetInteraction> Interactions { get; set; } = new();
}

public class WidgetInteraction 
{
    public string Trigger { get; set; } = "onClick"; // onClick, onHover
    public string Action { get; set; } = "Navigate"; // Navigate, ShowModal
    public string Target { get; set; } = ""; // Route or Modal ID
    public Dictionary<string, string> Params { get; set; } = new();
}

public class WidgetDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Custom";
    public string Template { get; set; } = "<div>{{title}}</div>";
    public List<WidgetPropertyDef> PropertyDefinitions { get; set; } = new();
    public List<string> Events { get; set; } = new(); // e.g., ["onClick", "onSelectionChanged"]
}

public class WidgetPropertyDef
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Type { get; set; } = "string"; // string, number, boolean, color, enum
    public string DefaultValue { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new(); // For enum type
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

public class WidgetDataSource
{
    public string Provider { get; set; } = "Entity"; // Entity, API, Workflow, Static
    public string Source { get; set; } = string.Empty; // "Appointment" or "/api/..."
    public Dictionary<string, object> Params { get; set; } = new(); // Filter, Limit, etc.
    public Dictionary<string, string> Mapping { get; set; } = new(); // Map Source Field -> Widget Prop
    
    // Legacy support helpers (optional, can be removed if we migrate fully)
    public PaginationConfig Pagination { get; set; } = new();
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
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public List<FieldMetadata> Fields { get; set; } = new();
}
// Form models have been moved to Models/FormMetadata.cs to hold the context definition
// Form models have been moved to Models/FormMetadata.cs
