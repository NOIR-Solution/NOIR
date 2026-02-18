namespace NOIR.Application.Features.Customers.Commands.CreateCustomer;

/// <summary>
/// Wolverine handler for creating a new customer.
/// </summary>
public class CreateCustomerCommandHandler
{
    private readonly IRepository<Domain.Entities.Customer.Customer, Guid> _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateCustomerCommandHandler(
        IRepository<Domain.Entities.Customer.Customer, Guid> customerRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<CustomerDto>> Handle(
        CreateCustomerCommand command,
        CancellationToken cancellationToken)
    {
        // Check if email already exists
        var emailSpec = new CustomerByEmailSpec(command.Email);
        var existingCustomer = await _customerRepository.FirstOrDefaultAsync(emailSpec, cancellationToken);

        if (existingCustomer is not null)
        {
            return Result.Failure<CustomerDto>(
                Error.Conflict($"A customer with email '{command.Email}' already exists.", "NOIR-CUSTOMER-001"));
        }

        var customer = Domain.Entities.Customer.Customer.Create(
            command.UserId,
            command.Email,
            command.FirstName,
            command.LastName,
            command.Phone,
            _currentUser.TenantId);

        if (!string.IsNullOrEmpty(command.Tags))
        {
            foreach (var tag in command.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                customer.AddTag(tag);
            }
        }

        if (!string.IsNullOrEmpty(command.Notes))
        {
            customer.AddNote(command.Notes);
        }

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CustomerMapper.ToDto(customer));
    }
}
