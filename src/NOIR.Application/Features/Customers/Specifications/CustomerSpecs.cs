namespace NOIR.Application.Features.Customers.Specifications;

/// <summary>
/// Specification to get a customer by ID with addresses loaded.
/// </summary>
public sealed class CustomerByIdSpec : Specification<Domain.Entities.Customer.Customer>
{
    public CustomerByIdSpec(Guid customerId)
    {
        Query.Where(c => c.Id == customerId)
            .Include(c => c.Addresses)
            .TagWith("CustomerById");
    }
}

/// <summary>
/// Specification to get a customer by ID for update (with tracking).
/// </summary>
public sealed class CustomerByIdForUpdateSpec : Specification<Domain.Entities.Customer.Customer>
{
    public CustomerByIdForUpdateSpec(Guid customerId)
    {
        Query.Where(c => c.Id == customerId)
            .Include(c => c.Addresses)
            .AsTracking()
            .TagWith("CustomerByIdForUpdate");
    }
}

/// <summary>
/// Specification to get a customer by UserId.
/// </summary>
public sealed class CustomerByUserIdSpec : Specification<Domain.Entities.Customer.Customer>
{
    public CustomerByUserIdSpec(string userId)
    {
        Query.Where(c => c.UserId == userId)
            .TagWith("CustomerByUserId");
    }
}

/// <summary>
/// Specification to get customers by segment.
/// </summary>
public sealed class CustomersBySegmentSpec : Specification<Domain.Entities.Customer.Customer>
{
    public CustomersBySegmentSpec(CustomerSegment segment, int? skip = null, int? take = null)
    {
        Query.Where(c => c.Segment == segment)
            .OrderByDescending(c => c.TotalSpent)
            .TagWith("CustomersBySegment");

        if (skip.HasValue)
            Query.Skip(skip.Value);

        if (take.HasValue)
            Query.Take(take.Value);
    }
}

/// <summary>
/// Specification to filter customers with pagination, search, segment, and tier.
/// </summary>
public sealed class CustomersFilterSpec : Specification<Domain.Entities.Customer.Customer>
{
    public CustomersFilterSpec(
        int skip = 0,
        int take = 20,
        string? search = null,
        CustomerSegment? segment = null,
        CustomerTier? tier = null,
        bool? isActive = null,
        string? sortBy = null,
        bool sortDescending = true)
    {
        Query.TagWith("CustomersFilter");

        if (!string.IsNullOrEmpty(search))
        {
            Query.Where(c =>
                c.Email.Contains(search) ||
                c.FirstName.Contains(search) ||
                c.LastName.Contains(search) ||
                (c.Phone != null && c.Phone.Contains(search)));
        }

        if (segment.HasValue)
            Query.Where(c => c.Segment == segment.Value);

        if (tier.HasValue)
            Query.Where(c => c.Tier == tier.Value);

        if (isActive.HasValue)
            Query.Where(c => c.IsActive == isActive.Value);

        // Sorting
        switch (sortBy?.ToLowerInvariant())
        {
            case "name":
                if (sortDescending)
                    Query.OrderByDescending(c => c.LastName).ThenByDescending(c => c.FirstName);
                else
                    Query.OrderBy(c => c.LastName).ThenBy(c => c.FirstName);
                break;
            case "totalorders":
                if (sortDescending) Query.OrderByDescending(c => c.TotalOrders);
                else Query.OrderBy(c => c.TotalOrders);
                break;
            case "totalspent":
                if (sortDescending) Query.OrderByDescending(c => c.TotalSpent);
                else Query.OrderBy(c => c.TotalSpent);
                break;
            case "loyaltypoints":
                if (sortDescending) Query.OrderByDescending(c => c.LoyaltyPoints);
                else Query.OrderBy(c => c.LoyaltyPoints);
                break;
            case "lastorderdate":
                if (sortDescending) Query.OrderByDescending(c => c.LastOrderDate ?? DateTimeOffset.MinValue);
                else Query.OrderBy(c => c.LastOrderDate ?? DateTimeOffset.MinValue);
                break;
            default:
                Query.OrderByDescending(c => c.CreatedAt);
                break;
        }

        Query.Skip(skip).Take(take);
    }
}

/// <summary>
/// Specification to count customers matching filter criteria.
/// </summary>
public sealed class CustomersCountSpec : Specification<Domain.Entities.Customer.Customer>
{
    public CustomersCountSpec(
        string? search = null,
        CustomerSegment? segment = null,
        CustomerTier? tier = null,
        bool? isActive = null)
    {
        Query.TagWith("CustomersCount");

        if (!string.IsNullOrEmpty(search))
        {
            Query.Where(c =>
                c.Email.Contains(search) ||
                c.FirstName.Contains(search) ||
                c.LastName.Contains(search) ||
                (c.Phone != null && c.Phone.Contains(search)));
        }

        if (segment.HasValue)
            Query.Where(c => c.Segment == segment.Value);

        if (tier.HasValue)
            Query.Where(c => c.Tier == tier.Value);

        if (isActive.HasValue)
            Query.Where(c => c.IsActive == isActive.Value);
    }
}

/// <summary>
/// Specification to get top spending customers.
/// </summary>
public sealed class TopSpendersSpec : Specification<Domain.Entities.Customer.Customer>
{
    public TopSpendersSpec(int count = 10)
    {
        Query.Where(c => c.IsActive)
            .OrderByDescending(c => c.TotalSpent)
            .Take(count)
            .TagWith("TopSpenders");
    }
}

/// <summary>
/// Specification to check if a customer email already exists in the tenant.
/// </summary>
public sealed class CustomerByEmailSpec : Specification<Domain.Entities.Customer.Customer>
{
    public CustomerByEmailSpec(string email)
    {
        Query.Where(c => c.Email == email)
            .TagWith("CustomerByEmail");
    }
}

/// <summary>
/// Specification to get customers by a list of IDs (batch validation).
/// </summary>
public sealed class CustomersByIdsSpec : Specification<Domain.Entities.Customer.Customer>
{
    public CustomersByIdsSpec(List<Guid> customerIds)
    {
        Query.Where(c => customerIds.Contains(c.Id))
            .TagWith("CustomersByIds");
    }
}
