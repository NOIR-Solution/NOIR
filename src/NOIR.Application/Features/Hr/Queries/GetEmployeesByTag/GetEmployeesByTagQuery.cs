namespace NOIR.Application.Features.Hr.Queries.GetEmployeesByTag;

public sealed record GetEmployeesByTagQuery(
    Guid TagId,
    int Page = 1,
    int PageSize = 20);
