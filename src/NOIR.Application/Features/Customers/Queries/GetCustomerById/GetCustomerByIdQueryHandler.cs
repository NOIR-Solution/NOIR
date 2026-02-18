namespace NOIR.Application.Features.Customers.Queries.GetCustomerById;

/// <summary>
/// Wolverine handler for getting a customer by ID.
/// </summary>
public class GetCustomerByIdQueryHandler
{
    private readonly IRepository<Domain.Entities.Customer.Customer, Guid> _customerRepository;

    public GetCustomerByIdQueryHandler(IRepository<Domain.Entities.Customer.Customer, Guid> customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<Result<CustomerDto>> Handle(
        GetCustomerByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new CustomerByIdSpec(query.Id);
        var customer = await _customerRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (customer is null)
        {
            return Result.Failure<CustomerDto>(
                Error.NotFound($"Customer with ID '{query.Id}' not found.", "NOIR-CUSTOMER-002"));
        }

        return Result.Success(CustomerMapper.ToDto(customer));
    }
}
