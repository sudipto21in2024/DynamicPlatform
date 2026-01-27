namespace Platform.Engine.Models.DataExecution;

/// <summary>
/// Represents a group of filter conditions combined with AND/OR operators
/// </summary>
public class FilterGroup
{
    public LogicalOperator Operator { get; set; }
    
    public List<object> Conditions { get; set; } = new(); // Can contain FilterCondition or nested FilterGroup
}

/// <summary>
/// Represents a single filter condition
/// </summary>
public class FilterCondition
{
    public string Field { get; set; } = string.Empty;
    
    public FilterOperator Operator { get; set; }
    
    public object? Value { get; set; }
    
    public FilterConditionType? Type { get; set; }
    
    public DataOperationMetadata? Subquery { get; set; }
}

public enum LogicalOperator
{
    AND,
    OR
}

public enum FilterOperator
{
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    Contains,
    StartsWith,
    EndsWith,
    In,
    NotIn,
    IsNull,
    IsNotNull,
    Between,
    Exists,
    NotExists
}

public enum FilterConditionType
{
    Simple,
    Subquery
}
