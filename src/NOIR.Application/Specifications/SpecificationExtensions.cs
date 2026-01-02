namespace NOIR.Application.Specifications;

/// <summary>
/// Extension methods for combining specifications.
/// </summary>
public static class SpecificationExtensions
{
    /// <summary>
    /// Creates a new specification that is satisfied when both specifications are satisfied (AND).
    /// </summary>
    public static AndSpecification<T> And<T>(this ISpecification<T> left, ISpecification<T> right) where T : class
        => new(left, right);

    /// <summary>
    /// Creates a new specification that is satisfied when either specification is satisfied (OR).
    /// </summary>
    public static OrSpecification<T> Or<T>(this ISpecification<T> left, ISpecification<T> right) where T : class
        => new(left, right);

    /// <summary>
    /// Creates a new specification that is satisfied when the specification is NOT satisfied (NOT).
    /// </summary>
    public static NotSpecification<T> Not<T>(this ISpecification<T> specification) where T : class
        => new(specification);
}

/// <summary>
/// Specification that combines two specifications with AND logic.
/// </summary>
public class AndSpecification<T> : Specification<T> where T : class
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    public AndSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left;
        _right = right;

        // Combine all where expressions from both specifications
        foreach (var where in left.WhereExpressions)
            AddWhereExpression(where);
        foreach (var where in right.WhereExpressions)
            AddWhereExpression(where);
    }

    /// <summary>
    /// Checks if an entity satisfies both specifications.
    /// </summary>
    public new bool IsSatisfiedBy(T entity)
    {
        return _left.IsSatisfiedBy(entity) && _right.IsSatisfiedBy(entity);
    }
}

/// <summary>
/// Specification that combines two specifications with OR logic.
/// </summary>
public class OrSpecification<T> : Specification<T> where T : class
{
    private readonly ISpecification<T> _left;
    private readonly ISpecification<T> _right;

    public OrSpecification(ISpecification<T> left, ISpecification<T> right)
    {
        _left = left;
        _right = right;

        // For OR, we need to create a combined expression
        // This is more complex as we can't just add both
        // We create a single expression that ORs all criteria
        var leftExpressions = left.WhereExpressions;
        var rightExpressions = right.WhereExpressions;

        if (leftExpressions.Count > 0 && rightExpressions.Count > 0)
        {
            var leftCombined = CombineExpressionsAnd(leftExpressions);
            var rightCombined = CombineExpressionsAnd(rightExpressions);
            var orExpression = CombineExpressionsOr(leftCombined, rightCombined);
            AddWhereExpression(orExpression);
        }
        else if (leftExpressions.Count > 0)
        {
            foreach (var expr in leftExpressions)
                AddWhereExpression(expr);
        }
        else if (rightExpressions.Count > 0)
        {
            foreach (var expr in rightExpressions)
                AddWhereExpression(expr);
        }
    }

    /// <summary>
    /// Checks if an entity satisfies either specification.
    /// </summary>
    public new bool IsSatisfiedBy(T entity)
    {
        return _left.IsSatisfiedBy(entity) || _right.IsSatisfiedBy(entity);
    }

    private static Expression<Func<T, bool>> CombineExpressionsAnd(IReadOnlyList<Expression<Func<T, bool>>> expressions)
    {
        if (expressions.Count == 0)
            return _ => true;

        var combined = expressions[0];
        for (int i = 1; i < expressions.Count; i++)
        {
            var right = expressions[i];
            var parameter = Expression.Parameter(typeof(T), "x");
            var body = Expression.AndAlso(
                Expression.Invoke(combined, parameter),
                Expression.Invoke(right, parameter));
            combined = Expression.Lambda<Func<T, bool>>(body, parameter);
        }
        return combined;
    }

    private static Expression<Func<T, bool>> CombineExpressionsOr(
        Expression<Func<T, bool>> left,
        Expression<Func<T, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var body = Expression.OrElse(
            Expression.Invoke(left, parameter),
            Expression.Invoke(right, parameter));
        return Expression.Lambda<Func<T, bool>>(body, parameter);
    }
}

/// <summary>
/// Specification that negates another specification.
/// </summary>
public class NotSpecification<T> : Specification<T> where T : class
{
    private readonly ISpecification<T> _inner;

    public NotSpecification(ISpecification<T> specification)
    {
        _inner = specification;

        // Create negated expressions
        foreach (var where in specification.WhereExpressions)
        {
            var parameter = where.Parameters[0];
            var negatedBody = Expression.Not(where.Body);
            var negatedExpression = Expression.Lambda<Func<T, bool>>(negatedBody, parameter);
            AddWhereExpression(negatedExpression);
        }
    }

    /// <summary>
    /// Checks if an entity does NOT satisfy the inner specification.
    /// </summary>
    public new bool IsSatisfiedBy(T entity)
    {
        return !_inner.IsSatisfiedBy(entity);
    }
}
