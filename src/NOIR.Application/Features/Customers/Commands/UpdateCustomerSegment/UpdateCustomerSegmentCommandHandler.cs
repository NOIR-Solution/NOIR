namespace NOIR.Application.Features.Customers.Commands.UpdateCustomerSegment;

/// <summary>
/// Wolverine handler for updating customer segment.
/// </summary>
public class UpdateCustomerSegmentCommandHandler
{
    private readonly IRepository<Domain.Entities.Customer.Customer, Guid> _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public UpdateCustomerSegmentCommandHandler(
        IRepository<Domain.Entities.Customer.Customer, Guid> customerRepository,
        IUnitOfWork unitOfWork,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<CustomerDto>> Handle(
        UpdateCustomerSegmentCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new CustomerByIdForUpdateSpec(command.Id);
        var customer = await _customerRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (customer is null)
        {
            return Result.Failure<CustomerDto>(
                Error.NotFound($"Customer with ID '{command.Id}' not found.", "NOIR-CUSTOMER-002"));
        }

        customer.SetSegment(command.Segment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Customer",
            entityId: customer.Id,
            operation: EntityOperation.Updated,
            tenantId: customer.TenantId!,
            ct: cancellationToken);

        return Result.Success(CustomerMapper.ToDto(customer));
    }
}
