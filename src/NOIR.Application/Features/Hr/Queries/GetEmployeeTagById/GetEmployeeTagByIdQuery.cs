namespace NOIR.Application.Features.Hr.Queries.GetEmployeeTagById;

/// <summary>
/// Query to get a single employee tag by ID.
/// Used as a before-state resolver for auditing.
/// </summary>
public sealed record GetEmployeeTagByIdQuery(Guid Id);
