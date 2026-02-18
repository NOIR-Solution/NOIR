namespace NOIR.Application.Features.Orders.Commands.ReturnOrder;

/// <summary>
/// Wolverine handler for returning an order.
/// Creates inventory return movements for each order item.
/// </summary>
public class ReturnOrderCommandHandler
{
    private readonly IRepository<Order, Guid> _orderRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IInventoryMovementLogger _movementLogger;
    private readonly IUnitOfWork _unitOfWork;

    public ReturnOrderCommandHandler(
        IRepository<Order, Guid> orderRepository,
        IRepository<Product, Guid> productRepository,
        IInventoryMovementLogger movementLogger,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _movementLogger = movementLogger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<OrderDto>> Handle(
        ReturnOrderCommand command,
        CancellationToken cancellationToken)
    {
        var spec = new OrderByIdForUpdateSpec(command.OrderId);
        var order = await _orderRepository.FirstOrDefaultAsync(spec, cancellationToken);

        if (order is null)
        {
            return Result.Failure<OrderDto>(
                Error.NotFound($"Order with ID '{command.OrderId}' not found.", "NOIR-ORDER-002"));
        }

        try
        {
            order.Return(command.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure<OrderDto>(
                Error.Validation("Status", ex.Message, "NOIR-ORDER-008"));
        }

        // Release inventory for each order item
        foreach (var item in order.Items)
        {
            var productSpec = new ProductByIdForUpdateSpec(item.ProductId);
            var product = await _productRepository.FirstOrDefaultAsync(productSpec, cancellationToken);

            if (product is null) continue;

            var variant = product.Variants.FirstOrDefault(v => v.Id == item.ProductVariantId);
            if (variant is null) continue;

            var quantityBefore = variant.StockQuantity;
            variant.ReleaseStock(item.Quantity);

            await _movementLogger.LogMovementAsync(
                variant,
                InventoryMovementType.Return,
                quantityBefore,
                item.Quantity,
                reference: order.OrderNumber,
                notes: $"Return from order {order.OrderNumber}. Reason: {command.Reason ?? "Not specified"}",
                userId: command.UserId,
                cancellationToken: cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(OrderMapper.ToDto(order));
    }
}
