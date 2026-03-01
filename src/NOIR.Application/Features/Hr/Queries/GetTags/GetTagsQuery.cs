namespace NOIR.Application.Features.Hr.Queries.GetTags;

public sealed record GetTagsQuery(
    EmployeeTagCategory? Category = null,
    bool? IsActive = null);
