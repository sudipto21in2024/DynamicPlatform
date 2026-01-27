namespace Platform.Engine.Services.DataExecution;

using System.Linq.Expressions;
using System.Reflection;
using Platform.Engine.Interfaces;
using Platform.Engine.Models.DataExecution;

/// <summary>
/// Builds dynamic LINQ queries from metadata
/// </summary>
public class DynamicQueryBuilder : IQueryBuilder
{
    private readonly IServiceProvider _serviceProvider;
    
    public DynamicQueryBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public IQueryable<object> BuildQuery(DataOperationMetadata metadata, ExecutionContext context)
    {
        if (string.IsNullOrEmpty(metadata.RootEntity))
        {
            throw new ArgumentException("RootEntity is required", nameof(metadata));
        }
        
        // Get the entity type dynamically
        var entityType = GetEntityType(metadata.RootEntity);
        if (entityType == null)
        {
            throw new InvalidOperationException($"Entity type '{metadata.RootEntity}' not found");
        }
        
        // Get the DbSet for this entity
        var query = GetEntityQueryable(entityType);
        
        // Apply joins if specified
        if (metadata.Joins != null && metadata.Joins.Any())
        {
            query = ApplyJoins(query, metadata.Joins, entityType);
        }
        
        // Apply filters
        if (metadata.Filters != null)
        {
            query = ApplyFiltersInternal(query, metadata.Filters, entityType);
        }
        
        // Apply RLS filters automatically
        query = ApplyRlsFilters(query, context, entityType);
        
        // Handle aggregations or field selection
        if (metadata.Aggregations != null && metadata.Aggregations.Any())
        {
            return ApplyAggregations(query, metadata, entityType);
        }
        else if (metadata.Fields != null && metadata.Fields.Any())
        {
            return ApplyFieldSelection(query, metadata.Fields, entityType);
        }
        
        // Apply ordering
        if (metadata.OrderBy != null && metadata.OrderBy.Any())
        {
            query = ApplyOrderingInternal(query, metadata.OrderBy, entityType);
        }
        
        // Apply pagination
        if (metadata.Limit.HasValue || metadata.Offset.HasValue)
        {
            query = ApplyPaginationInternal(query, metadata.Limit, metadata.Offset);
        }
        
        return query.Cast<object>();
    }
    
    public IQueryable<T> ApplyFilters<T>(IQueryable<T> query, FilterGroup? filters)
    {
        if (filters == null) return query;
        
        var parameter = Expression.Parameter(typeof(T), "x");
        var expression = BuildFilterExpression(filters, parameter, typeof(T));
        
        if (expression != null)
        {
            var lambda = Expression.Lambda<Func<T, bool>>(expression, parameter);
            query = query.Where(lambda);
        }
        
        return query;
    }
    
    public IQueryable<T> ApplyOrdering<T>(IQueryable<T> query, List<OrderByDefinition>? orderBy)
    {
        if (orderBy == null || !orderBy.Any()) return query;
        
        IOrderedQueryable<T>? orderedQuery = null;
        
        foreach (var order in orderBy)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = Expression.Property(parameter, order.Field);
            var lambda = Expression.Lambda(property, parameter);
            
            var methodName = orderedQuery == null
                ? (order.Direction == SortDirection.ASC ? "OrderBy" : "OrderByDescending")
                : (order.Direction == SortDirection.ASC ? "ThenBy" : "ThenByDescending");
            
            var method = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), property.Type);
            
            orderedQuery = (IOrderedQueryable<T>)method.Invoke(null, new object[] { orderedQuery ?? query, lambda })!;
        }
        
        return orderedQuery ?? query;
    }
    
    public IQueryable<T> ApplyPagination<T>(IQueryable<T> query, int? limit, int? offset)
    {
        if (offset.HasValue && offset.Value > 0)
        {
            query = query.Skip(offset.Value);
        }
        
        if (limit.HasValue && limit.Value > 0)
        {
            query = query.Take(limit.Value);
        }
        
        return query;
    }
    
    #region Private Helper Methods
    
    private Type? GetEntityType(string entityName)
    {
        // This would need to be implemented based on your entity registration
        // For now, using reflection to find the type
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var assembly in assemblies)
        {
            var type = assembly.GetTypes()
                .FirstOrDefault(t => t.Name == entityName && t.Namespace?.Contains("Models") == true);
            if (type != null) return type;
        }
        return null;
    }
    
    private IQueryable GetEntityQueryable(Type entityType)
    {
        // This would get the DbContext and call Set<T>()
        // Placeholder implementation - needs actual DbContext integration
        throw new NotImplementedException("Entity queryable retrieval needs DbContext integration");
    }
    
    private IQueryable ApplyJoins(IQueryable query, List<JoinDefinition> joins, Type rootType)
    {
        // For EF Core, we can use Include for navigation properties
        // For complex joins, we might need to use Join() method
        foreach (var join in joins)
        {
            // Simplified - actual implementation would parse the join condition
            // and apply appropriate Include or Join
            var includeMethod = typeof(Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions)
                .GetMethods()
                .First(m => m.Name == "Include" && m.GetParameters().Length == 2);
            
            // This is a placeholder - needs proper implementation
        }
        
        return query;
    }
    
    private IQueryable ApplyFiltersInternal(IQueryable query, FilterGroup filters, Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "x");
        var expression = BuildFilterExpression(filters, parameter, entityType);
        
        if (expression != null)
        {
            var lambda = Expression.Lambda(expression, parameter);
            var whereMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                .MakeGenericMethod(entityType);
            
            query = (IQueryable)whereMethod.Invoke(null, new object[] { query, lambda })!;
        }
        
        return query;
    }
    
    private Expression? BuildFilterExpression(FilterGroup group, ParameterExpression parameter, Type entityType)
    {
        if (group.Conditions == null || !group.Conditions.Any())
            return null;
        
        Expression? result = null;
        
        foreach (var condition in group.Conditions)
        {
            Expression? expr = null;
            
            if (condition is FilterCondition filterCondition)
            {
                expr = BuildConditionExpression(filterCondition, parameter, entityType);
            }
            else if (condition is FilterGroup nestedGroup)
            {
                expr = BuildFilterExpression(nestedGroup, parameter, entityType);
            }
            
            if (expr != null)
            {
                result = result == null
                    ? expr
                    : group.Operator == LogicalOperator.AND
                        ? Expression.AndAlso(result, expr)
                        : Expression.OrElse(result, expr);
            }
        }
        
        return result;
    }
    
    private Expression? BuildConditionExpression(FilterCondition condition, ParameterExpression parameter, Type entityType)
    {
        // Parse field path (e.g., "Patient.FirstName")
        var fieldParts = condition.Field.Split('.');
        Expression property = parameter;
        
        foreach (var part in fieldParts)
        {
            property = Expression.Property(property, part);
        }
        
        // Build the comparison expression based on operator
        Expression? comparison = condition.Operator switch
        {
            FilterOperator.Equals => Expression.Equal(property, Expression.Constant(condition.Value)),
            FilterOperator.NotEquals => Expression.NotEqual(property, Expression.Constant(condition.Value)),
            FilterOperator.GreaterThan => Expression.GreaterThan(property, Expression.Constant(condition.Value)),
            FilterOperator.LessThan => Expression.LessThan(property, Expression.Constant(condition.Value)),
            FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, Expression.Constant(condition.Value)),
            FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(property, Expression.Constant(condition.Value)),
            FilterOperator.Contains => BuildContainsExpression(property, condition.Value),
            FilterOperator.StartsWith => BuildStartsWithExpression(property, condition.Value),
            FilterOperator.EndsWith => BuildEndsWithExpression(property, condition.Value),
            FilterOperator.IsNull => Expression.Equal(property, Expression.Constant(null)),
            FilterOperator.IsNotNull => Expression.NotEqual(property, Expression.Constant(null)),
            _ => null
        };
        
        return comparison;
    }
    
    private Expression BuildContainsExpression(Expression property, object? value)
    {
        var method = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        return Expression.Call(property, method!, Expression.Constant(value));
    }
    
    private Expression BuildStartsWithExpression(Expression property, object? value)
    {
        var method = typeof(string).GetMethod("StartsWith", new[] { typeof(string) });
        return Expression.Call(property, method!, Expression.Constant(value));
    }
    
    private Expression BuildEndsWithExpression(Expression property, object? value)
    {
        var method = typeof(string).GetMethod("EndsWith", new[] { typeof(string) });
        return Expression.Call(property, method!, Expression.Constant(value));
    }
    
    private IQueryable ApplyRlsFilters(IQueryable query, ExecutionContext context, Type entityType)
    {
        // Check if entity has TenantId property
        var tenantIdProperty = entityType.GetProperty("TenantId");
        if (tenantIdProperty != null && !string.IsNullOrEmpty(context.TenantId))
        {
            var parameter = Expression.Parameter(entityType, "x");
            var property = Expression.Property(parameter, "TenantId");
            var constant = Expression.Constant(context.TenantId);
            var equals = Expression.Equal(property, constant);
            var lambda = Expression.Lambda(equals, parameter);
            
            var whereMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                .MakeGenericMethod(entityType);
            
            query = (IQueryable)whereMethod.Invoke(null, new object[] { query, lambda })!;
        }
        
        return query;
    }
    
    private IQueryable<object> ApplyAggregations(IQueryable query, DataOperationMetadata metadata, Type entityType)
    {
        // This is complex and would require dynamic GroupBy and Select
        // Placeholder for now
        throw new NotImplementedException("Aggregation support coming soon");
    }
    
    private IQueryable<object> ApplyFieldSelection(IQueryable query, List<FieldDefinition> fields, Type entityType)
    {
        // Build a dynamic Select expression
        var parameter = Expression.Parameter(entityType, "x");
        var bindings = new List<MemberBinding>();
        
        // Create anonymous type with selected fields
        // This is complex and needs proper implementation
        throw new NotImplementedException("Field selection support coming soon");
    }
    
    private IQueryable ApplyOrderingInternal(IQueryable query, List<OrderByDefinition> orderBy, Type entityType)
    {
        IOrderedQueryable? orderedQuery = null;
        
        foreach (var order in orderBy)
        {
            var parameter = Expression.Parameter(entityType, "x");
            var property = Expression.Property(parameter, order.Field);
            var lambda = Expression.Lambda(property, parameter);
            
            var methodName = orderedQuery == null
                ? (order.Direction == SortDirection.ASC ? "OrderBy" : "OrderByDescending")
                : (order.Direction == SortDirection.ASC ? "ThenBy" : "ThenByDescending");
            
            var method = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                .MakeGenericMethod(entityType, property.Type);
            
            orderedQuery = (IOrderedQueryable)method.Invoke(null, new object[] { orderedQuery ?? query, lambda })!;
        }
        
        return orderedQuery ?? query;
    }
    
    private IQueryable ApplyPaginationInternal(IQueryable query, int? limit, int? offset)
    {
        if (offset.HasValue && offset.Value > 0)
        {
            var skipMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == "Skip" && m.GetParameters().Length == 2);
            query = (IQueryable)skipMethod.MakeGenericMethod(query.ElementType)
                .Invoke(null, new object[] { query, offset.Value })!;
        }
        
        if (limit.HasValue && limit.Value > 0)
        {
            var takeMethod = typeof(Queryable).GetMethods()
                .First(m => m.Name == "Take" && m.GetParameters().Length == 2);
            query = (IQueryable)takeMethod.MakeGenericMethod(query.ElementType)
                .Invoke(null, new object[] { query, limit.Value })!;
        }
        
        return query;
    }
    
    #endregion
}
