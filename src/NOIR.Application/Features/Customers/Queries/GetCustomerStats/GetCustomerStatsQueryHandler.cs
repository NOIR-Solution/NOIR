namespace NOIR.Application.Features.Customers.Queries.GetCustomerStats;

/// <summary>
/// Wolverine handler for getting customer statistics.
/// </summary>
public class GetCustomerStatsQueryHandler
{
    private readonly IRepository<Domain.Entities.Customer.Customer, Guid> _customerRepository;

    public GetCustomerStatsQueryHandler(IRepository<Domain.Entities.Customer.Customer, Guid> customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Result<CustomerStatsDto>> Handle(
        GetCustomerStatsQuery query,
        CancellationToken cancellationToken)
    {
        // Get total and active counts in parallel
        var totalCountTask = _customerRepository.CountAsync(cancellationToken);
        var activeCountSpec = new CustomersCountSpec(isActive: true);
        var activeCountTask = _customerRepository.CountAsync(activeCountSpec, cancellationToken);

        await Task.WhenAll(totalCountTask, activeCountTask);

        var totalCount = totalCountTask.Result;
        var activeCount = activeCountTask.Result;

        // Get segment distribution
        var segmentDistribution = new List<SegmentDistributionDto>();
        foreach (var segment in Enum.GetValues<CustomerSegment>())
        {
            var segmentSpec = new CustomersCountSpec(segment: segment);
            var count = await _customerRepository.CountAsync(segmentSpec, cancellationToken);
            segmentDistribution.Add(new SegmentDistributionDto
            {
                Segment = segment,
                Count = count
            });
        }

        // Get tier distribution
        var tierDistribution = new List<TierDistributionDto>();
        foreach (var tier in Enum.GetValues<CustomerTier>())
        {
            var tierSpec = new CustomersCountSpec(tier: tier);
            var count = await _customerRepository.CountAsync(tierSpec, cancellationToken);
            tierDistribution.Add(new TierDistributionDto
            {
                Tier = tier,
                Count = count
            });
        }

        // Get top spenders
        var topSpendersSpec = new TopSpendersSpec(query.TopSpendersCount);
        var topSpenders = await _customerRepository.ListAsync(topSpendersSpec, cancellationToken);

        return Result.Success(new CustomerStatsDto
        {
            TotalCustomers = totalCount,
            ActiveCustomers = activeCount,
            SegmentDistribution = segmentDistribution,
            TierDistribution = tierDistribution,
            TopSpenders = topSpenders.Select(CustomerMapper.ToSummaryDto).ToList()
        });
    }
}
