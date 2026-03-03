namespace NOIR.Application.Features.Orders.Commands.AddOrderNote;

/// <summary>
/// Wolverine handler for adding a note to an order.
/// </summary>
public class AddOrderNoteCommandHandler
{
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserIdentityService _userIdentityService;
    private readonly IEntityUpdateHubContext _entityUpdateHub;

    public AddOrderNoteCommandHandler(
        IRepository<Order, Guid> orderRepository,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        IUserIdentityService userIdentityService,
        IEntityUpdateHubContext entityUpdateHub)
    {
        _orderRepository = orderRepository;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _userIdentityService = userIdentityService;
        _entityUpdateHub = entityUpdateHub;
    }

    public async Task<Result<OrderNoteDto>> Handle(
        AddOrderNoteCommand command,
        CancellationToken cancellationToken)
    {
        // Verify order exists
        var orderSpec = new OrderByIdSpec(command.OrderId);
        var order = await _orderRepository.FirstOrDefaultAsync(orderSpec, cancellationToken);

        if (order is null)
        {
            return Result.Failure<OrderNoteDto>(
                Error.NotFound($"Order with ID '{command.OrderId}' not found.", ErrorCodes.Order.NotFound));
        }

        // Get user display name
        var userName = "Unknown";
        if (!string.IsNullOrEmpty(command.UserId))
        {
            var user = await _userIdentityService.FindByIdAsync(command.UserId, cancellationToken);
            if (user is not null)
            {
                userName = user.DisplayName ?? user.FullName;
            }
        }

        var note = OrderNote.Create(
            command.OrderId,
            command.Content,
            command.UserId ?? string.Empty,
            userName,
            order.TenantId);

        await _dbContext.OrderNotes.AddAsync(note, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _entityUpdateHub.PublishEntityUpdatedAsync(
            entityType: "Order",
            entityId: order.Id,
            operation: EntityOperation.Updated,
            tenantId: order.TenantId!,
            ct: cancellationToken);

        return Result.Success(OrderMapper.ToDto(note));
    }
}
