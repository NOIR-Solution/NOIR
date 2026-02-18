namespace NOIR.Application.Features.Customers.Commands.RedeemLoyaltyPoints;

/// <summary>
/// Wolverine handler for redeeming loyalty points.
/// </summary>
public class RedeemLoyaltyPointsCommandHandler
{
    private readonly IRepository<Domain.Entities.Customer.Customer, Guid> _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RedeemLoyaltyPointsCommandHandler(
        IRepository<Domain.Entities.Customer.Customer, Guid> customerRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CustomerDto>> Handle(
        RedeemLoyaltyPointsCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new CustomerByIdForUpdateSpec(command.CustomerId);
        var customer = await _customerRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (customer is null)
        {
            return Result.Failure<CustomerDto>(
                Error.NotFound($"Customer with ID '{command.CustomerId}' not found.", "NOIR-CUSTOMER-002"));
        }

        try
        {
            customer.RedeemLoyaltyPoints(command.Points);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<CustomerDto>(
                Error.Validation("Points", ex.Message, "NOIR-CUSTOMER-004"));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(CustomerMapper.ToDto(customer));
    }
}
