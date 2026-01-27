namespace Platform.Engine.Models.DataExecution;

/// <summary>
/// Defines the type of data operation to execute
/// </summary>
public enum OperationType
{
    Query,
    Aggregate,
    Union,
    Pivot,
    Execute
}

/// <summary>
/// Root metadata structure for data operations
/// </summary>
public class DataOperationMetadata
{
    public OperationType OperationType { get; set; }
    
    public string? RootEntity { get; set; }
    
    public List<FieldDefinition>? Fields { get; set; }
    
    public List<JoinDefinition>? Joins { get; set; }
    
    public FilterGroup? Filters { get; set; }
    
    public List<AggregationDefinition>? Aggregations { get; set; }
    
    public List<GroupByDefinition>? GroupBy { get; set; }
    
    public List<OrderByDefinition>? OrderBy { get; set; }
    
    public int? Limit { get; set; }
    
    public int? Offset { get; set; }
    
    public List<CalculatedFieldDefinition>? CalculatedFields { get; set; }
    
    public List<WindowFunctionDefinition>? WindowFunctions { get; set; }
    
    public List<ExternalSourceDefinition>? ExternalSources { get; set; }
    
    public List<DataOperationMetadata>? UnionQueries { get; set; }
    
    public string? UnionType { get; set; } // "All" or "Distinct"
}

/// <summary>
/// Defines a field to select in the query
/// </summary>
public class FieldDefinition
{
    public string Field { get; set; } = string.Empty;
    
    public string? Alias { get; set; }
    
    public string? Value { get; set; } // For static values in UNION queries
}

/// <summary>
/// Defines a join between entities
/// </summary>
public class JoinDefinition
{
    public string Entity { get; set; } = string.Empty;
    
    public JoinType Type { get; set; }
    
    public string On { get; set; } = string.Empty; // e.g., "Appointment.PatientId == Patient.Id"
    
    public string? Alias { get; set; }
}

public enum JoinType
{
    Inner,
    Left,
    Right,
    Full
}

/// <summary>
/// Defines an aggregation function
/// </summary>
public class AggregationDefinition
{
    public AggregateFunction Function { get; set; }
    
    public string Field { get; set; } = string.Empty;
    
    public string Alias { get; set; } = string.Empty;
    
    public NullHandling? NullHandling { get; set; }
}

public enum AggregateFunction
{
    Sum,
    Count,
    Average,
    Min,
    Max,
    CountDistinct
}

public enum NullHandling
{
    IgnoreNulls,
    TreatAsZero
}

/// <summary>
/// Defines a GROUP BY clause
/// </summary>
public class GroupByDefinition
{
    public string Field { get; set; } = string.Empty;
    
    public string? DatePart { get; set; } // "Year", "Month", "Day", etc.
    
    public string? Alias { get; set; }
}

/// <summary>
/// Defines an ORDER BY clause
/// </summary>
public class OrderByDefinition
{
    public string Field { get; set; } = string.Empty;
    
    public SortDirection Direction { get; set; }
}

public enum SortDirection
{
    ASC,
    DESC
}

/// <summary>
/// Defines a calculated field
/// </summary>
public class CalculatedFieldDefinition
{
    public string Alias { get; set; } = string.Empty;
    
    public string Expression { get; set; } = string.Empty;
    
    public string DataType { get; set; } = "String"; // "String", "Number", "Decimal", "Boolean", "DateTime"
}

/// <summary>
/// Defines a window function
/// </summary>
public class WindowFunctionDefinition
{
    public WindowFunction Function { get; set; }
    
    public List<string>? PartitionBy { get; set; }
    
    public List<OrderByDefinition>? OrderBy { get; set; }
    
    public string Alias { get; set; } = string.Empty;
}

public enum WindowFunction
{
    RowNumber,
    Rank,
    DenseRank,
    Lead,
    Lag
}

/// <summary>
/// Defines an external data source integration
/// </summary>
public class ExternalSourceDefinition
{
    public ExternalSourceType Type { get; set; }
    
    public string Endpoint { get; set; } = string.Empty;
    
    public string Method { get; set; } = "GET";
    
    public string? AuthProfile { get; set; }
    
    public Dictionary<string, string>? RequestMapping { get; set; }
    
    public Dictionary<string, string>? ResponseMapping { get; set; }
}

public enum ExternalSourceType
{
    API,
    WebService,
    GraphQL
}
