namespace Platform.Engine.Interfaces;

using Platform.Engine.Models.DataExecution;

/// <summary>
/// Interface for building dynamic queries from metadata
/// </summary>
public interface IQueryBuilder
{
    /// <summary>
    /// Builds a queryable from metadata
    /// </summary>
    IQueryable<object> BuildQuery(DataOperationMetadata metadata, ExecutionContext context);
    
    /// <summary>
    /// Applies filters to a query
    /// </summary>
    IQueryable<T> ApplyFilters<T>(IQueryable<T> query, FilterGroup? filters);
    
    /// <summary>
    /// Applies ordering to a query
    /// </summary>
    IQueryable<T> ApplyOrdering<T>(IQueryable<T> query, List<OrderByDefinition>? orderBy);
    
    /// <summary>
    /// Applies pagination to a query
    /// </summary>
    IQueryable<T> ApplyPagination<T>(IQueryable<T> query, int? limit, int? offset);
}
