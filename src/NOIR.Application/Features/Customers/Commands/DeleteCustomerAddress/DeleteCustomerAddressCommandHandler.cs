namespace NOIR.Application.Features.Customers.Commands.DeleteCustomerAddress;

/// <summary>
/// Wolverine handler for deleting a customer address.
/// </summary>
public class DeleteCustomerAddressCommandHandler
{
    private readonly IRepository<Domain.Entities.Customer.Customer, Guid> _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public DeleteCustomerAddressCommandHandler(
        IRepository<Domain.Entities.Customer.Customer, Guid> customerRepository,
        IUnitOfWork unitOfWork,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<CustomerAddressDto>> Handle(
        DeleteCustomerAddressCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new CustomerByIdForUpdateSpec(command.CustomerId);
        var customer = await _customerRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (customer is null)
        {
            return Result.Failure<CustomerAddressDto>(
                Error.NotFound($"Customer with ID '{command.CustomerId}' not found.", "NOIR-CUSTOMER-002"));
        }

        var address = customer.Addresses.FirstOrDefault(a => a.Id == command.AddressId);

        if (address is null)
        {
            return Result.Failure<CustomerAddressDto>(
                Error.NotFound($"Address with ID '{command.AddressId}' not found.", "NOIR-CUSTOMER-005"));
        }

        var dto = CustomerMapper.ToAddressDto(address);
        customer.Addresses.Remove(address);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Customer",
            entityId: customer.Id,
            operation: EntityOperation.Updated,
            tenantId: customer.TenantId!,
            ct: cancellationToken);

        return Result.Success(dto);
    }
}
