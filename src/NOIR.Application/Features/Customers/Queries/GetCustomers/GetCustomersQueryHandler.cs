namespace NOIR.Application.Features.Customers.Queries.GetCustomers;

/// <summary>
/// Wolverine handler for getting customers with pagination.
/// </summary>
public class GetCustomersQueryHandler
{
    private readonly IRepository<Domain.Entities.Customer.Customer, Guid> _customerRepository;

    public GetCustomersQueryHandler(IRepository<Domain.Entities.Customer.Customer, Guid> customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Result<PagedResult<CustomerSummaryDto>>> Handle(
        GetCustomersQuery query,
        CancellationToken cancellationToken)
    {
        var skip = (query.Page - 1) * query.PageSize;

        // Get total count
        var countSpec = new CustomersCountSpec(
            query.Search,
            query.Segment,
            query.Tier,
            query.IsActive);
        var totalCount = await _customerRepository.CountAsync(countSpec, cancellationToken);

        // Get customers
        var listSpec = new CustomersFilterSpec(
            skip,
            query.PageSize,
            query.Search,
            query.Segment,
            query.Tier,
            query.IsActive,
            query.SortBy,
            query.SortDescending);
        var customers = await _customerRepository.ListAsync(listSpec, cancellationToken);

        var items = customers.Select(CustomerMapper.ToSummaryDto).ToList();

        var pageIndex = query.Page - 1;
        return Result.Success(PagedResult<CustomerSummaryDto>.Create(
            items,
            totalCount,
            pageIndex,
            query.PageSize));
    }
}
