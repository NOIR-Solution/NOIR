namespace NOIR.Application.Features.Customers.Commands.UpdateCustomerAddress;

/// <summary>
/// Wolverine handler for updating a customer address.
/// </summary>
public class UpdateCustomerAddressCommandHandler
{
    private readonly IRepository<Domain.Entities.Customer.Customer, Guid> _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerAddressCommandHandler(
        IRepository<Domain.Entities.Customer.Customer, Guid> customerRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CustomerAddressDto>> Handle(
        UpdateCustomerAddressCommand command,
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

        // If updating to default, remove default from other addresses
        if (command.IsDefault)
        {
            foreach (var existingAddress in customer.Addresses.Where(a => a.IsDefault && a.Id != command.AddressId))
            {
                existingAddress.RemoveDefault();
            }
        }

        address.Update(
            command.AddressType,
            command.FullName,
            command.Phone,
            command.AddressLine1,
            command.Province,
            command.AddressLine2,
            command.Ward,
            command.District,
            command.PostalCode,
            command.IsDefault);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CustomerMapper.ToAddressDto(address));
    }
}
