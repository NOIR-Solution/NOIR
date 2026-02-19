namespace NOIR.Application.Features.CustomerGroups.Commands.DeleteCustomerGroup;

/// <summary>
/// Wolverine handler for deleting a customer group (soft-delete).
/// </summary>
public class DeleteCustomerGroupCommandHandler
{
    private readonly IRepository<CustomerGroup, Guid> _groupRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCustomerGroupCommandHandler(
        IRepository<CustomerGroup, Guid> groupRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(
        DeleteCustomerGroupCommand command,
        CancellationToken cancellationToken)
    {
        // Get the group for update (need tracking for soft delete)
        var spec = new CustomerGroupByIdForUpdateSpec(command.Id);
        var group = await _groupRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (group == null)
        {
            return Result.Failure<bool>(
                Error.NotFound($"Customer group with ID '{command.Id}' was not found.", ErrorCodes.CustomerGroup.NotFound));
        }

        // Check if group has members
        var hasMembers = await _dbContext.CustomerGroupMemberships
            .AnyAsync(m => m.CustomerGroupId == command.Id, cancellationToken);

        if (hasMembers)
        {
            return Result.Failure<bool>(
                Error.Conflict("Cannot delete customer group that has members. Remove all members first.", ErrorCodes.CustomerGroup.HasMembers));
        }

        // Soft delete via repository Remove() — interceptor converts to IsDeleted=true
        group.MarkAsDeleted();
        _groupRepository.Remove(group);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(true);
    }
}
