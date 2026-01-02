namespace NOIR.Domain.Specifications;

/// <summary>
/// Base specification interface for encapsulating query logic.
/// Specifications allow composable, reusable, testable queries without exposing IQueryable.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface ISpecification<T> where T : class
{
    #region Criteria (WHERE)

    /// <summary>
    /// Collection of WHERE expressions to filter entities.
    /// Multiple expressions are combined with AND logic.
    /// </summary>
    IReadOnlyList<Expression<Func<T, bool>>> WhereExpressions { get; }

    #endregion

    #region Includes (Eager Loading)

    /// <summary>
    /// Expression-based includes for eager loading related entities.
    /// </summary>
    IReadOnlyList<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// String-based includes for nested navigation properties.
    /// Example: "Orders.Items"
    /// </summary>
    IReadOnlyList<string> IncludeStrings { get; }

    #endregion

    #region Ordering

    /// <summary>
    /// Primary ascending order expression.
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Primary descending order expression.
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Secondary ascending order expressions.
    /// Applied after primary ordering.
    /// </summary>
    IReadOnlyList<Expression<Func<T, object>>> ThenByExpressions { get; }

    /// <summary>
    /// Secondary descending order expressions.
    /// Applied after primary ordering.
    /// </summary>
    IReadOnlyList<Expression<Func<T, object>>> ThenByDescendingExpressions { get; }

    #endregion

    #region Paging

    /// <summary>
    /// Number of items to skip.
    /// </summary>
    int? Skip { get; }

    /// <summary>
    /// Number of items to take.
    /// </summary>
    int? Take { get; }

    /// <summary>
    /// Indicates if pagination is enabled.
    /// </summary>
    bool IsPagingEnabled { get; }

    #endregion

    #region Query Behaviors

    /// <summary>
    /// When true, disables change tracking for better performance.
    /// Default should be true for read operations.
    /// </summary>
    bool AsNoTracking { get; }

    /// <summary>
    /// When true, disables tracking but maintains identity resolution.
    /// Use when loading related entities that may appear multiple times.
    /// </summary>
    bool AsNoTrackingWithIdentityResolution { get; }

    /// <summary>
    /// When true, splits the query into multiple roundtrips.
    /// Use to prevent cartesian explosion with multiple collection includes.
    /// </summary>
    bool AsSplitQuery { get; }

    /// <summary>
    /// When true, bypasses global query filters (soft delete, tenant, etc.).
    /// </summary>
    bool IgnoreQueryFilters { get; }

    /// <summary>
    /// When true, ignores auto-includes defined in the DbContext.
    /// </summary>
    bool IgnoreAutoIncludes { get; }

    #endregion

    #region Debugging

    /// <summary>
    /// Tags to add to the generated SQL query as comments.
    /// Useful for debugging and profiling.
    /// </summary>
    IReadOnlyList<string> QueryTags { get; }

    #endregion

    #region In-Memory Evaluation

    /// <summary>
    /// Checks if an entity satisfies the specification criteria.
    /// Useful for in-memory validation.
    /// </summary>
    bool IsSatisfiedBy(T entity);

    /// <summary>
    /// Filters a collection of entities using the specification criteria.
    /// </summary>
    IEnumerable<T> Evaluate(IEnumerable<T> entities);

    #endregion
}

/// <summary>
/// Specification interface with projection support.
/// Use when you need to project entities to DTOs directly in the database.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TResult">The projected result type.</typeparam>
public interface ISpecification<T, TResult> : ISpecification<T> where T : class
{
    /// <summary>
    /// The projection selector to transform entities to the result type.
    /// </summary>
    Expression<Func<T, TResult>>? Selector { get; }
}
