namespace NOIR.Application.Features.CustomerGroups.Commands.AssignCustomersToGroup;

/// <summary>
/// Wolverine handler for assigning customers to a group.
/// </summary>
public class AssignCustomersToGroupCommandHandler
{
    private readonly IRepository<CustomerGroup, Guid> _groupRepository;
    private readonly IRepository<Domain.Entities.Customer.Customer, Guid> _customerRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AssignCustomersToGroupCommandHandler(
        IRepository<CustomerGroup, Guid> groupRepository,
        IRepository<Domain.Entities.Customer.Customer, Guid> customerRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _groupRepository = groupRepository;
        _customerRepository = customerRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<bool>> Handle(
        AssignCustomersToGroupCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Verify group exists
        var groupSpec = new CustomerGroupByIdForUpdateSpec(command.CustomerGroupId);
        var group = await _groupRepository.FirstOrDefaultAsync(groupSpec, cancellationToken);

        if (group == null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Customer group with ID '{command.CustomerGroupId}' was not found.", ErrorCodes.CustomerGroup.NotFound));
        }

        // Get existing memberships to avoid duplicates
        var existingCustomerIds = await _dbContext.CustomerGroupMemberships
            .Where(m => m.CustomerGroupId == command.CustomerGroupId && command.CustomerIds.Contains(m.CustomerId))
            .Select(m => m.CustomerId)
            .ToListAsync(cancellationToken);

        var existingSet = existingCustomerIds.ToHashSet();

        // Filter to only new customers
        var newCustomerIds = command.CustomerIds.Where(id => !existingSet.Contains(id)).ToList();

        if (newCustomerIds.Count == 0)
        {
            return Result.Success(true); // All already members
        }

        // Batch verify all customers exist (single query instead of N+1)
        var customersSpec = new CustomersByIdsSpec(newCustomerIds);
        var foundCustomers = await _customerRepository.ListAsync(customersSpec, cancellationToken);
        var foundIds = foundCustomers.Select(c => c.Id).ToHashSet();

        var missingIds = newCustomerIds.Where(id => !foundIds.Contains(id)).ToList();
        if (missingIds.Count > 0)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Customer with ID '{missingIds.First()}' was not found.", ErrorCodes.CustomerGroup.CustomerNotFound));
        }

        // Create memberships
        foreach (var customerId in newCustomerIds)
        {
            var membership = CustomerGroupMembership.Create(command.CustomerGroupId, customerId, tenantId);
            _dbContext.CustomerGroupMemberships.Add(membership);
        }

        // Update cached member count
        group.IncrementMemberCount(newCustomerIds.Count);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
