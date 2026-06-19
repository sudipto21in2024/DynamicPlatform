using System.Collections.Generic;
using System.Text.RegularExpressions;

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
    public string Type { get; set; } = "string"; // "string", "int", "guid", "datetime", "decimal", "bool", "enum"
    public bool IsRequired { get; set; }
    public int MaxLength { get; set; }
    
    public List<ValidationRule> Rules { get; set; } = new();

    // ── Display Metadata ──
    /// <summary>Human-readable label (e.g. "First Name"). Auto-generated from Name if empty.</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>Input placeholder text (e.g. "Enter first name").</summary>
    public string Placeholder { get; set; } = string.Empty;
    /// <summary>Help icon hover text for contextual guidance.</summary>
    public string Tooltip { get; set; } = string.Empty;
    /// <summary>Longer description or help text for the field.</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Custom error message when the field fails "required" validation.</summary>
    public string ValidationMessage { get; set; } = string.Empty;

    // ── Defaults & Behavior ──
    /// <summary>Default value for new records (stored as string, parsed at runtime).</summary>
    public string DefaultValue { get; set; } = string.Empty;
    /// <summary>If true, field is non-editable after initial creation.</summary>
    public bool IsReadOnly { get; set; }
    /// <summary>If true, field is computed server-side and not user-entered.</summary>
    public bool IsComputed { get; set; }

    // ── Database Hints ──
    /// <summary>Minimum string length constraint.</summary>
    public int MinLength { get; set; }
    /// <summary>If true, a database index is created on this column.</summary>
    public bool IsIndexed { get; set; }
    /// <summary>If true, a unique constraint is applied to this column.</summary>
    public bool IsUnique { get; set; }
    /// <summary>Override the default database column name.</summary>
    public string ColumnName { get; set; } = string.Empty;

    // ── UI Control Hints ──
    /// <summary>Sort order in forms and tables (lower = first).</summary>
    public int DisplayOrder { get; set; }
    /// <summary>Number of grid columns the field spans in form layouts (1 or 2).</summary>
    public int GridSpan { get; set; } = 1;
    /// <summary>If true, exclude from list/table views.</summary>
    public bool HideInTable { get; set; }
    /// <summary>If true, exclude from form generation.</summary>
    public bool HideInForm { get; set; }

    // ── Lookup/Enum Reference ──
    /// <summary>Name of the linked EnumMetadata when Type is "enum".</summary>
    public string EnumReference { get; set; } = string.Empty;

    // ── Scriban Helpers ──
    /// <summary>Auto-generate a display label from the Name if Label is empty (e.g. "FirstName" → "First Name").</summary>
    public string DisplayLabel => string.IsNullOrEmpty(Label)
        ? Regex.Replace(Name, "([A-Z])", " $1").Trim()
        : Label;

    /// <summary>Maps the field type to a C# type string for code generation.</summary>
    public string CsharpType => Type.ToLower() switch 
    {
        "string" => "string",
        "int" => "int",
        "guid" => "Guid",
        "datetime" => "DateTime",
        "decimal" => "decimal",
        "bool" => "bool",
        _ => Type // Custom Enum or Entity reference
    };

    /// <summary>Maps the field type to a Java type string for code generation.</summary>
    public string JavaType => Type.ToLower() switch
    {
        "string" => "String",
        "int" => "Integer",
        "guid" => "java.util.UUID",
        "datetime" => "LocalDateTime",
        "decimal" => "BigDecimal",
        "bool" => "Boolean",
        _ => Type
    };

    /// <summary>Maps the field type to a Python type string for code generation.</summary>
    public string PythonType => Type.ToLower() switch
    {
        "string" => "str",
        "int" => "int",
        "guid" => "uuid.UUID",
        "datetime" => "datetime",
        "decimal" => "float",
        "bool" => "bool",
        _ => Type
    };

    /// <summary>Maps the field type to a TypeScript type string for code generation.</summary>
    public string TsType => Type.ToLower() switch
    {
        "string" => "string",
        "int" => "number",
        "guid" => "string",
        "datetime" => "Date",
        "decimal" => "number",
        "bool" => "boolean",
        _ => Type
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
    /// <summary>Rule type: Regex, Range, Email, Phone, MinLength, MinValue, MaxValue, URL, CreditCard, Comparison, Custom.</summary>
    public string Type { get; set; } = string.Empty;
    /// <summary>Rule parameter value (e.g. regex pattern, "min,max" for Range, numeric threshold).</summary>
    public string Value { get; set; } = string.Empty;
    /// <summary>Error message displayed when validation fails.</summary>
    public string ErrorMessage { get; set; } = string.Empty;
    /// <summary>For "Comparison" type: the name of the other field to compare against.</summary>
    public string CompareField { get; set; } = string.Empty;
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

public enum FormMode
{
    Create,
    Edit,
    View,
    Clone,
    InlineEdit
}

public class FormContext
{
    // Indicates whether the form is used for creating a new entity or editing an existing one
    public FormMode Mode { get; set; } = FormMode.Create;

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

public class BusinessRuleMetadata
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TargetEntity { get; set; } = string.Empty;
    public string Trigger { get; set; } = "BeforeSave"; // BeforeSave, AfterSave
    public string Condition { get; set; } = string.Empty; // e.g., "ConsultationFee > 1000"
    public string Action { get; set; } = string.Empty; // e.g., "Set IsPremium = true"
}
