namespace NOIR.Application.Features.Customers.Commands.UpdateCustomer;

/// <summary>
/// Wolverine handler for updating a customer.
/// </summary>
public class UpdateCustomerCommandHandler
{
    private readonly IRepository<Domain.Entities.Customer.Customer, Guid> _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCustomerCommandHandler(
        IRepository<Domain.Entities.Customer.Customer, Guid> customerRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CustomerDto>> Handle(
        UpdateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new CustomerByIdForUpdateSpec(command.Id);
        var customer = await _customerRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (customer is null)
        {
            return Result.Failure<CustomerDto>(
                Error.NotFound($"Customer with ID '{command.Id}' not found.", "NOIR-CUSTOMER-002"));
        }

        // Check email uniqueness if changed
        if (!string.Equals(customer.Email, command.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailSpec = new CustomerByEmailSpec(command.Email);
            var existingCustomer = await _customerRepository.FirstOrDefaultAsync(emailSpec, cancellationToken);

            if (existingCustomer is not null && existingCustomer.Id != command.Id)
            {
                return Result.Failure<CustomerDto>(
                    Error.Conflict($"A customer with email '{command.Email}' already exists.", "NOIR-CUSTOMER-001"));
            }
        }

        customer.UpdateProfile(command.FirstName, command.LastName, command.Email, command.Phone);

        if (command.Notes is not null)
        {
            customer.AddNote(command.Notes);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CustomerMapper.ToDto(customer));
    }
}
