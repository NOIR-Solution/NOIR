namespace NOIR.Application.Specifications;

/// <summary>
/// Base class for specifications without projection.
/// Inherit from this class to create reusable query specifications.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public abstract class Specification<T> : ISpecification<T> where T : class
{
    private readonly List<Expression<Func<T, bool>>> _whereExpressions = [];
    private readonly List<Expression<Func<T, object>>> _includes = [];
    private readonly List<string> _includeStrings = [];
    private readonly List<Expression<Func<T, object>>> _thenByExpressions = [];
    private readonly List<Expression<Func<T, object>>> _thenByDescendingExpressions = [];
    private readonly List<string> _queryTags = [];

    /// <summary>
    /// The fluent query builder. Use this in derived specification constructors.
    /// </summary>
    protected SpecificationBuilder<T> Query { get; }

    protected Specification()
    {
        Query = new SpecificationBuilder<T>(this);
    }

    #region ISpecification Implementation

    public IReadOnlyList<Expression<Func<T, bool>>> WhereExpressions => _whereExpressions;
    public IReadOnlyList<Expression<Func<T, object>>> Includes => _includes;
    public IReadOnlyList<string> IncludeStrings => _includeStrings;
    public Expression<Func<T, object>>? OrderBy { get; internal set; }
    public Expression<Func<T, object>>? OrderByDescending { get; internal set; }
    public IReadOnlyList<Expression<Func<T, object>>> ThenByExpressions => _thenByExpressions;
    public IReadOnlyList<Expression<Func<T, object>>> ThenByDescendingExpressions => _thenByDescendingExpressions;
    public int? Skip { get; internal set; }
    public int? Take { get; internal set; }
    public bool IsPagingEnabled => Skip.HasValue || Take.HasValue;
    public bool AsNoTracking { get; internal set; } = true; // Default to no tracking for reads
    public bool AsNoTrackingWithIdentityResolution { get; internal set; }
    public bool AsSplitQuery { get; internal set; }
    public bool IgnoreQueryFilters { get; internal set; }
    public bool IgnoreAutoIncludes { get; internal set; }
    public IReadOnlyList<string> QueryTags => _queryTags;

    #endregion

    #region In-Memory Evaluation

    public bool IsSatisfiedBy(T entity)
    {
        if (entity == null)
            return false;

        if (_whereExpressions.Count == 0)
            return true;

        foreach (var expression in _whereExpressions)
        {
            var compiled = expression.Compile();
            if (!compiled(entity))
                return false;
        }

        return true;
    }

    public IEnumerable<T> Evaluate(IEnumerable<T> entities)
    {
        return entities.Where(IsSatisfiedBy);
    }

    #endregion

    #region Internal Builder Methods

    internal void AddWhereExpression(Expression<Func<T, bool>> expression) => _whereExpressions.Add(expression);
    internal void AddInclude(Expression<Func<T, object>> include) => _includes.Add(include);
    internal void AddIncludeString(string includeString) => _includeStrings.Add(includeString);
    internal void AddThenBy(Expression<Func<T, object>> expression) => _thenByExpressions.Add(expression);
    internal void AddThenByDescending(Expression<Func<T, object>> expression) => _thenByDescendingExpressions.Add(expression);
    internal void AddQueryTag(string tag) => _queryTags.Add(tag);

    #endregion
}

/// <summary>
/// Base class for specifications with projection.
/// Use when you need to project entities to DTOs directly in the database.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TResult">The projected result type.</typeparam>
public abstract class Specification<T, TResult> : Specification<T>, ISpecification<T, TResult> where T : class
{
    /// <summary>
    /// The fluent query builder with projection support.
    /// </summary>
    protected new ProjectionSpecificationBuilder<T, TResult> Query { get; }

    protected Specification()
    {
        Query = new ProjectionSpecificationBuilder<T, TResult>(this);
    }

    public Expression<Func<T, TResult>>? Selector { get; internal set; }
}

/// <summary>
/// Fluent builder for creating specifications.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public class SpecificationBuilder<T> where T : class
{
    private readonly Specification<T> _specification;

    internal SpecificationBuilder(Specification<T> specification)
    {
        _specification = specification;
    }

    #region Filtering

    /// <summary>
    /// Adds a WHERE clause. Multiple calls are combined with AND.
    /// </summary>
    public SpecificationBuilder<T> Where(Expression<Func<T, bool>> expression)
    {
        _specification.AddWhereExpression(expression);
        return this;
    }

    #endregion

    #region Includes

    /// <summary>
    /// Includes a related entity for eager loading.
    /// </summary>
    public SpecificationBuilder<T> Include(Expression<Func<T, object>> include)
    {
        _specification.AddInclude(include);
        return this;
    }

    /// <summary>
    /// Includes nested navigation properties using string path.
    /// Example: "Orders.Items"
    /// </summary>
    public SpecificationBuilder<T> Include(string includeString)
    {
        _specification.AddIncludeString(includeString);
        return this;
    }

    #endregion

    #region Ordering

    /// <summary>
    /// Sets the primary ascending order.
    /// </summary>
    public SpecificationBuilder<T> OrderBy(Expression<Func<T, object>> expression)
    {
        _specification.OrderBy = expression;
        _specification.OrderByDescending = null;
        return this;
    }

    /// <summary>
    /// Sets the primary descending order.
    /// </summary>
    public SpecificationBuilder<T> OrderByDescending(Expression<Func<T, object>> expression)
    {
        _specification.OrderByDescending = expression;
        _specification.OrderBy = null;
        return this;
    }

    /// <summary>
    /// Adds a secondary ascending order.
    /// </summary>
    public SpecificationBuilder<T> ThenBy(Expression<Func<T, object>> expression)
    {
        _specification.AddThenBy(expression);
        return this;
    }

    /// <summary>
    /// Adds a secondary descending order.
    /// </summary>
    public SpecificationBuilder<T> ThenByDescending(Expression<Func<T, object>> expression)
    {
        _specification.AddThenByDescending(expression);
        return this;
    }

    #endregion

    #region Paging

    /// <summary>
    /// Applies pagination using page index and page size.
    /// </summary>
    /// <param name="pageIndex">Zero-based page index.</param>
    /// <param name="pageSize">Number of items per page.</param>
    public SpecificationBuilder<T> Paginate(int pageIndex, int pageSize)
    {
        _specification.Skip = pageIndex * pageSize;
        _specification.Take = pageSize;
        return this;
    }

    /// <summary>
    /// Skips a number of items.
    /// </summary>
    public SpecificationBuilder<T> Skip(int count)
    {
        _specification.Skip = count;
        return this;
    }

    /// <summary>
    /// Takes a number of items.
    /// </summary>
    public SpecificationBuilder<T> Take(int count)
    {
        _specification.Take = count;
        return this;
    }

    #endregion

    #region Query Behaviors

    /// <summary>
    /// Enables change tracking (disables default AsNoTracking).
    /// Use when you need to modify entities.
    /// </summary>
    public SpecificationBuilder<T> AsTracking()
    {
        _specification.AsNoTracking = false;
        _specification.AsNoTrackingWithIdentityResolution = false;
        return this;
    }

    /// <summary>
    /// Enables no-tracking with identity resolution.
    /// Better than full tracking when loading related entities multiple times.
    /// </summary>
    public SpecificationBuilder<T> AsNoTrackingWithIdentityResolution()
    {
        _specification.AsNoTracking = false;
        _specification.AsNoTrackingWithIdentityResolution = true;
        return this;
    }

    /// <summary>
    /// Splits the query into multiple database roundtrips.
    /// Use to prevent cartesian explosion with multiple collection includes.
    /// </summary>
    public SpecificationBuilder<T> AsSplitQuery()
    {
        _specification.AsSplitQuery = true;
        return this;
    }

    /// <summary>
    /// Bypasses global query filters (soft delete, tenant, etc.).
    /// </summary>
    public SpecificationBuilder<T> IgnoreQueryFilters()
    {
        _specification.IgnoreQueryFilters = true;
        return this;
    }

    /// <summary>
    /// Ignores auto-includes defined in the DbContext.
    /// </summary>
    public SpecificationBuilder<T> IgnoreAutoIncludes()
    {
        _specification.IgnoreAutoIncludes = true;
        return this;
    }

    #endregion

    #region Debugging

    /// <summary>
    /// Adds a tag to the generated SQL query as a comment.
    /// Useful for debugging and profiling.
    /// </summary>
    public SpecificationBuilder<T> TagWith(string tag)
    {
        _specification.AddQueryTag(tag);
        return this;
    }

    #endregion
}

/// <summary>
/// Fluent builder for creating projection specifications.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
/// <typeparam name="TResult">The projected result type.</typeparam>
public class ProjectionSpecificationBuilder<T, TResult> : SpecificationBuilder<T> where T : class
{
    private readonly Specification<T, TResult> _projectionSpecification;

    internal ProjectionSpecificationBuilder(Specification<T, TResult> specification) : base(specification)
    {
        _projectionSpecification = specification;
    }

    /// <summary>
    /// Sets the projection selector.
    /// </summary>
    public ProjectionSpecificationBuilder<T, TResult> Select(Expression<Func<T, TResult>> selector)
    {
        _projectionSpecification.Selector = selector;
        return this;
    }

    #region Override base methods to return ProjectionSpecificationBuilder

    public new ProjectionSpecificationBuilder<T, TResult> Where(Expression<Func<T, bool>> expression)
    {
        base.Where(expression);
        return this;
    }

    public new ProjectionSpecificationBuilder<T, TResult> Include(Expression<Func<T, object>> include)
    {
        base.Include(include);
        return this;
    }

    public new ProjectionSpecificationBuilder<T, TResult> Include(string includeString)
    {
        base.Include(includeString);
        return this;
    }

    public new ProjectionSpecificationBuilder<T, TResult> OrderBy(Expression<Func<T, object>> expression)
    {
        base.OrderBy(expression);
        return this;
    }

    public new ProjectionSpecificationBuilder<T, TResult> OrderByDescending(Expression<Func<T, object>> expression)
    {
        base.OrderByDescending(expression);
        return this;
    }

    public new ProjectionSpecificationBuilder<T, TResult> ThenBy(Expression<Func<T, object>> expression)
    {
        base.ThenBy(expression);
        return this;
    }

    public new ProjectionSpecificationBuilder<T, TResult> ThenByDescending(Expression<Func<T, object>> expression)
    {
        base.ThenByDescending(expression);
        return this;
    }

    public new ProjectionSpecificationBuilder<T, TResult> Paginate(int pageIndex, int pageSize)
    {
        base.Paginate(pageIndex, pageSize);
        return this;
    }

    public new ProjectionSpecificationBuilder<T, TResult> Skip(int count)
    {
        base.Skip(count);
        return this;
    }

    public new ProjectionSpecificationBuilder<T, TResult> Take(int count)
    {
        base.Take(count);
        return this;
    }

    public new ProjectionSpecificationBuilder<T, TResult> AsTracking()
    {
        base.AsTracking();
        return this;
    }

    public new ProjectionSpecificationBuilder<T, TResult> AsNoTrackingWithIdentityResolution()
    {
        base.AsNoTrackingWithIdentityResolution();
        return this;
    }

    public new ProjectionSpecificationBuilder<T, TResult> AsSplitQuery()
    {
        base.AsSplitQuery();
        return this;
    }

    public new ProjectionSpecificationBuilder<T, TResult> IgnoreQueryFilters()
    {
        base.IgnoreQueryFilters();
        return this;
    }

    public new ProjectionSpecificationBuilder<T, TResult> IgnoreAutoIncludes()
    {
        base.IgnoreAutoIncludes();
        return this;
    }

    public new ProjectionSpecificationBuilder<T, TResult> TagWith(string tag)
    {
        base.TagWith(tag);
        return this;
    }

    #endregion
}
