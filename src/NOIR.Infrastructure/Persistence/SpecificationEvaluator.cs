namespace NOIR.Infrastructure.Persistence;

/// <summary>
/// Evaluates specifications and applies them to IQueryable.
/// This is the central query builder that applies all specification features.
///
/// Performance features:
/// - AsNoTracking for read-only queries
/// - AsNoTrackingWithIdentityResolution for identity consistency
/// - AsSplitQuery to prevent cartesian explosion
/// - TagWith for query debugging/profiling
/// - Efficient criteria application (multiple WHERE clauses)
///
/// SECURITY: Query tags are sanitized to prevent SQL comment injection.
/// Tags are included in generated SQL as comments. Never include user input
/// in tags - use hardcoded strings only.
///
/// Based on patterns from:
/// - https://github.com/ardalis/Specification
/// - https://learn.microsoft.com/en-us/ef/core/performance/
/// </summary>
public static class SpecificationEvaluator
{
    /// <summary>
    /// Applies a specification to the queryable source.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="specification">The specification to apply.</param>
    /// <param name="evaluateCriteriaOnly">
    /// When true, only applies criteria (WHERE) without ordering, paging, or includes.
    /// Useful for Count/Any operations.
    /// </param>
    /// <returns>The queryable with the specification applied.</returns>
    public static IQueryable<T> GetQuery<T>(
        IQueryable<T> source,
        ISpecification<T> specification,
        bool evaluateCriteriaOnly = false)
        where T : class
    {
        var query = source;

        // Apply query tags first (for debugging/profiling)
        query = ApplyQueryTags(query, specification);

        // Apply global filter behaviors
        query = ApplyFilterBehaviors(query, specification);

        // Apply tracking behavior
        query = ApplyTrackingBehavior(query, specification);

        // Apply split query behavior
        query = ApplySplitQueryBehavior(query, specification);

        // Apply criteria (WHERE clauses) - always applied
        query = ApplyCriteria(query, specification);

        // For count/any operations, skip ordering, includes, and paging
        if (evaluateCriteriaOnly)
        {
            return query;
        }

        // Apply includes (eager loading)
        query = ApplyIncludes(query, specification);

        // Apply ordering
        query = ApplyOrdering(query, specification);

        // Apply paging (must be after ordering)
        query = ApplyPaging(query, specification);

        return query;
    }

    /// <summary>
    /// Applies a specification with projection to the queryable source.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="TResult">The projected result type.</typeparam>
    /// <param name="source">The queryable source.</param>
    /// <param name="specification">The specification with projection.</param>
    /// <returns>The queryable with projection applied.</returns>
    public static IQueryable<TResult> GetQuery<T, TResult>(
        IQueryable<T> source,
        ISpecification<T, TResult> specification)
        where T : class
    {
        // First apply the base specification
        var query = GetQuery(source, (ISpecification<T>)specification);

        // Then apply projection if available
        if (specification.Selector is not null)
        {
            return query.Select(specification.Selector);
        }

        // If no selector, this is a programming error
        throw new InvalidOperationException(
            "Specification with projection must have a Selector defined.");
    }

    /// <summary>
    /// Gets a query for counting (without paging, ordering, or includes).
    /// More efficient than full query for aggregate operations.
    /// </summary>
    public static IQueryable<T> GetQueryForCount<T>(
        IQueryable<T> source,
        ISpecification<T> specification)
        where T : class
    {
        return GetQuery(source, specification, evaluateCriteriaOnly: true);
    }

    #region Private Helper Methods

    private static IQueryable<T> ApplyQueryTags<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        foreach (var tag in specification.QueryTags)
        {
            // Sanitize tags to prevent SQL comment injection
            // Tags are embedded as SQL comments, so we strip dangerous characters
            var sanitizedTag = SanitizeQueryTag(tag);
            if (!string.IsNullOrWhiteSpace(sanitizedTag))
            {
                query = query.TagWith(sanitizedTag);
            }
        }
        return query;
    }

    /// <summary>
    /// Sanitizes a query tag to prevent SQL comment injection.
    /// Removes SQL comment terminators and other dangerous characters.
    ///
    /// SECURITY: Query tags are embedded as SQL comments. If user input
    /// were included in a tag (which should never happen), these characters
    /// could break out of the comment and inject SQL.
    /// </summary>
    private static string SanitizeQueryTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return string.Empty;
        }

        // Remove SQL comment terminators and newlines that could break out of comments
        return tag
            .Replace("*/", "", StringComparison.Ordinal)  // End of block comment
            .Replace("/*", "", StringComparison.Ordinal)  // Start of block comment
            .Replace("--", "", StringComparison.Ordinal)  // Line comment
            .Replace(";", "", StringComparison.Ordinal)   // Statement terminator
            .Replace("\r\n", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Replace("\r", " ", StringComparison.Ordinal)
            .Trim();
    }

    private static IQueryable<T> ApplyFilterBehaviors<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        // Apply ignore query filters (soft delete, tenant, etc.)
        if (specification.IgnoreQueryFilters)
        {
            query = query.IgnoreQueryFilters();
        }

        // Apply ignore auto-includes
        if (specification.IgnoreAutoIncludes)
        {
            query = query.IgnoreAutoIncludes();
        }

        return query;
    }

    private static IQueryable<T> ApplyTrackingBehavior<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        // AsNoTrackingWithIdentityResolution takes precedence over AsNoTracking
        // (better performance than tracking, maintains identity consistency)
        if (specification.AsNoTrackingWithIdentityResolution)
        {
            query = query.AsNoTrackingWithIdentityResolution();
        }
        else if (specification.AsNoTracking)
        {
            query = query.AsNoTracking();
        }
        // If neither is set, default EF tracking behavior is used

        return query;
    }

    private static IQueryable<T> ApplySplitQueryBehavior<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        if (specification.AsSplitQuery)
        {
            query = query.AsSplitQuery();
        }

        return query;
    }

    private static IQueryable<T> ApplyCriteria<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        // Apply all WHERE expressions (combined with AND logic by EF)
        foreach (var whereExpression in specification.WhereExpressions)
        {
            query = query.Where(whereExpression);
        }

        return query;
    }

    private static IQueryable<T> ApplyIncludes<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        // Apply expression-based includes
        foreach (var include in specification.Includes)
        {
            query = query.Include(include);
        }

        // Apply string-based includes (for dynamic/nested scenarios)
        foreach (var includeString in specification.IncludeStrings)
        {
            query = query.Include(includeString);
        }

        return query;
    }

    private static IQueryable<T> ApplyOrdering<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        IOrderedQueryable<T>? orderedQuery = null;

        // Apply primary ordering
        if (specification.OrderBy is not null)
        {
            orderedQuery = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending is not null)
        {
            orderedQuery = query.OrderByDescending(specification.OrderByDescending);
        }

        // Apply secondary orderings
        if (orderedQuery is not null)
        {
            foreach (var thenBy in specification.ThenByExpressions)
            {
                orderedQuery = orderedQuery.ThenBy(thenBy);
            }

            foreach (var thenByDescending in specification.ThenByDescendingExpressions)
            {
                orderedQuery = orderedQuery.ThenByDescending(thenByDescending);
            }

            return orderedQuery;
        }

        return query;
    }

    private static IQueryable<T> ApplyPaging<T>(IQueryable<T> query, ISpecification<T> specification)
        where T : class
    {
        if (specification.Skip.HasValue)
        {
            query = query.Skip(specification.Skip.Value);
        }

        if (specification.Take.HasValue)
        {
            query = query.Take(specification.Take.Value);
        }

        return query;
    }

    #endregion
}
