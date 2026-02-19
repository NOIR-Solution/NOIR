namespace NOIR.Application.Features.CustomerGroups.Commands.UpdateCustomerGroup;

/// <summary>
/// Wolverine handler for updating a customer group.
/// </summary>
public class UpdateCustomerGroupCommandHandler
{
    private readonly IRepository<CustomerGroup, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerGroupCommandHandler(
        IRepository<CustomerGroup, Guid> repository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CustomerGroupDto>> Handle(
        UpdateCustomerGroupCommand command,
        CancellationToken cancellationToken)
    {
        // Get the group for update
        var spec = new CustomerGroupByIdForUpdateSpec(command.Id);
        var group = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (group == null)
        {
            return Result.Failure<CustomerGroupDto>(
                Error.NotFound($"Customer group with ID '{command.Id}' was not found.", ErrorCodes.CustomerGroup.NotFound));
        }

        // Check if name already exists (for different group)
        if (group.Name != command.Name)
        {
            var nameSpec = new CustomerGroupNameExistsSpec(command.Name, command.Id);
            var existing = await _repository.FirstOrDefaultAsync(nameSpec, cancellationToken);
            if (existing != null)
            {
                return Result.Failure<CustomerGroupDto>(
                    Error.Conflict($"A customer group with name '{command.Name}' already exists.", ErrorCodes.CustomerGroup.DuplicateName));
            }
        }

        // Update the group
        group.Update(command.Name, command.Description);
        group.SetActive(command.IsActive);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CustomerGroupMapper.ToDto(group));
    }
}
