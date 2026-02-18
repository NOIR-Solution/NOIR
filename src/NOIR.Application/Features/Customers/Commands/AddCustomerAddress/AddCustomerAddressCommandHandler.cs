namespace NOIR.Application.Features.Customers.Commands.AddCustomerAddress;

/// <summary>
/// Wolverine handler for adding a customer address.
/// </summary>
public class AddCustomerAddressCommandHandler
{
    private readonly IRepository<Domain.Entities.Customer.Customer, Guid> _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public AddCustomerAddressCommandHandler(
        IRepository<Domain.Entities.Customer.Customer, Guid> customerRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CustomerAddressDto>> Handle(
        AddCustomerAddressCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new CustomerByIdForUpdateSpec(command.CustomerId);
        var customer = await _customerRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (customer is null)
        {
            return Result.Failure<CustomerAddressDto>(
                Error.NotFound($"Customer with ID '{command.CustomerId}' not found.", "NOIR-CUSTOMER-002"));
        }

        // If new address is default, remove default from existing addresses of same type
        if (command.IsDefault)
        {
            foreach (var existingAddress in customer.Addresses.Where(a => a.IsDefault))
            {
                existingAddress.RemoveDefault();
            }
        }

        var address = Domain.Entities.Customer.CustomerAddress.Create(
            command.CustomerId,
            command.AddressType,
            command.FullName,
            command.Phone,
            command.AddressLine1,
            command.Province,
            command.AddressLine2,
            command.Ward,
            command.District,
            command.PostalCode,
            command.IsDefault,
            _currentUser.TenantId);

        customer.Addresses.Add(address);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CustomerMapper.ToAddressDto(address));
    }
}
