namespace NOIR.Application.Features.Blog.Queries.GetCategoryById;

/// <summary>
/// Query to get a single blog category by ID.
/// Used as a before-state resolver for auditing.
/// </summary>
public sealed record GetCategoryByIdQuery(Guid Id);
