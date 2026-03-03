namespace NOIR.Application.Features.CustomerGroups.Commands.CreateCustomerGroup;

/// <summary>
/// Wolverine handler for creating a new customer group.
/// </summary>
public class CreateCustomerGroupCommandHandler
{
    private readonly IRepository<CustomerGroup, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public CreateCustomerGroupCommandHandler(
        IRepository<CustomerGroup, Guid> repository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<CustomerGroupDto>> Handle(
        CreateCustomerGroupCommand command,
        CancellationToken cancellationToken)
    {
        var tenantId = _currentUser.TenantId;

        // Check if name already exists
        var nameSpec = new CustomerGroupNameExistsSpec(command.Name);
        var existing = await _repository.FirstOrDefaultAsync(nameSpec, cancellationToken);
        if (existing != null)
        {
            return Result.Failure<CustomerGroupDto>(
                Error.Conflict($"A customer group with name '{command.Name}' already exists.", ErrorCodes.CustomerGroup.DuplicateName));
        }

        // Create the group
        var group = CustomerGroup.Create(command.Name, command.Description, tenantId);

        await _repository.AddAsync(group, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "CustomerGroup",
            entityId: group.Id,
            operation: EntityOperation.Created,
            tenantId: _currentUser.TenantId!,
            ct: cancellationToken);

        return Result.Success(CustomerGroupMapper.ToDto(group));
    }
}
