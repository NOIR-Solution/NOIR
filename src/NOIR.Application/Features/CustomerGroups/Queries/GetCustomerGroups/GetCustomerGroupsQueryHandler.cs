namespace NOIR.Application.Features.CustomerGroups.Queries.GetCustomerGroups;

/// <summary>
/// Wolverine handler for getting paged list of customer groups.
/// </summary>
public class GetCustomerGroupsQueryHandler
{
    private readonly IRepository<CustomerGroup, Guid> _repository;

    public GetCustomerGroupsQueryHandler(IRepository<CustomerGroup, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<Result<PagedResult<CustomerGroupListDto>>> Handle(
        GetCustomerGroupsQuery query,
        CancellationToken cancellationToken)
    {
        // Get paged groups
        var spec = new CustomerGroupsPagedSpec(
            query.Search,
            query.IsActive,
            query.PageNumber,
            query.PageSize);

        var groups = await _repository.ListAsync(spec, cancellationToken);

        // Get total count
        var countSpec = new CustomerGroupsCountSpec(
            query.Search,
            query.IsActive);

        var totalCount = await _repository.CountAsync(countSpec, cancellationToken);

        var items = groups.Select(CustomerGroupMapper.ToListDto).ToList();

        // PagedResult expects pageIndex (0-based), but query.PageNumber is 1-based
        var pageIndex = query.PageNumber - 1;

        return Result.Success(PagedResult<CustomerGroupListDto>.Create(
            items,
            totalCount,
            pageIndex,
            query.PageSize));
    }
}
