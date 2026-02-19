namespace NOIR.Application.Features.CustomerGroups.Commands.RemoveCustomersFromGroup;

/// <summary>
/// Wolverine handler for removing customers from a group.
/// </summary>
public class RemoveCustomersFromGroupCommandHandler
{
    private readonly IRepository<CustomerGroup, Guid> _groupRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveCustomersFromGroupCommandHandler(
        IRepository<CustomerGroup, Guid> groupRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(
        RemoveCustomersFromGroupCommand command,
        CancellationToken cancellationToken)
    {
        // Verify group exists
        var groupSpec = new CustomerGroupByIdForUpdateSpec(command.CustomerGroupId);
        var group = await _groupRepository.FirstOrDefaultAsync(groupSpec, cancellationToken);

        if (group == null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Customer group with ID '{command.CustomerGroupId}' was not found.", ErrorCodes.CustomerGroup.NotFound));
        }

        // Get existing memberships
        var memberships = await _dbContext.CustomerGroupMemberships
            .Where(m => m.CustomerGroupId == command.CustomerGroupId && command.CustomerIds.Contains(m.CustomerId))
            .ToListAsync(cancellationToken);

        if (memberships.Count == 0)
        {
            return Result.Success(true); // None were members
        }

        // Remove memberships
        _dbContext.CustomerGroupMemberships.RemoveRange(memberships);

        // Update cached member count
        group.DecrementMemberCount(memberships.Count);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
