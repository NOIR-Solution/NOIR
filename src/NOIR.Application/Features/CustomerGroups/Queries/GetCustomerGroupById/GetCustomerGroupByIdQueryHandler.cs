namespace NOIR.Application.Features.CustomerGroups.Queries.GetCustomerGroupById;

/// <summary>
/// Wolverine handler for getting a customer group by ID.
/// </summary>
public class GetCustomerGroupByIdQueryHandler
{
    private readonly IRepository<CustomerGroup, Guid> _repository;

    public GetCustomerGroupByIdQueryHandler(IRepository<CustomerGroup, Guid> repository)
    {
        _repository = repository;
    }

    public async Task<Result<CustomerGroupDto>> Handle(
        GetCustomerGroupByIdQuery query,
        CancellationToken cancellationToken)
    {
        var spec = new CustomerGroupByIdSpec(query.Id);
        var group = await _repository.FirstOrDefaultAsync(spec, cancellationToken);

        if (group == null)
        {
            return Result.Failure<CustomerGroupDto>(
                Error.NotFound($"Customer group with ID '{query.Id}' was not found.", ErrorCodes.CustomerGroup.NotFound));
        }

        return Result.Success(CustomerGroupMapper.ToDto(group));
    }
}
