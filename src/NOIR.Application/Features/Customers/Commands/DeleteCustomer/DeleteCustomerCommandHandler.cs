namespace NOIR.Application.Features.Customers.Commands.DeleteCustomer;

/// <summary>
/// Wolverine handler for soft-deleting a customer.
/// </summary>
public class DeleteCustomerCommandHandler
{
    private readonly IRepository<Domain.Entities.Customer.Customer, Guid> _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCustomerCommandHandler(
        IRepository<Domain.Entities.Customer.Customer, Guid> customerRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CustomerDto>> Handle(
        DeleteCustomerCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new CustomerByIdForUpdateSpec(command.Id);
        var customer = await _customerRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (customer is null)
        {
            return Result.Failure<CustomerDto>(
                Error.NotFound($"Customer with ID '{command.Id}' not found.", "NOIR-CUSTOMER-002"));
        }

        var dto = CustomerMapper.ToDto(customer);
        _customerRepository.Remove(customer);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(dto);
    }
}
