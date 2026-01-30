using NOIR.Domain.Entities.Product;

namespace NOIR.Infrastructure.Services.Inventory;

/// <summary>
/// Service for logging inventory movements to database for audit trail.
/// </summary>
public class InventoryMovementLogger : IInventoryMovementLogger, IScopedService
{
    private readonly IRepository<InventoryMovement, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<InventoryMovementLogger> _logger;

    public InventoryMovementLogger(
        IRepository<InventoryMovement, Guid> repository,
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        ILogger<InventoryMovementLogger> logger)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task LogMovementAsync(
        ProductVariant variant,
        InventoryMovementType movementType,
        int quantityBefore,
        int quantityMoved,
        string? reference = null,
        string? notes = null,
        string? userId = null,
        string? correlationId = null,
        CancellationToken cancellationToken = default)
    {
        correlationId ??= GetCorrelationId();

        var movement = InventoryMovement.Create(
            variant.Id,
            variant.ProductId,
            movementType,
            quantityBefore,
            quantityMoved,
            variant.TenantId,
            reference,
            notes,
            userId,
            correlationId);

        await _repository.AddAsync(movement, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Logged inventory movement: {MovementType}, Variant: {VariantId}, Qty: {QuantityMoved} ({Before} -> {After})",
            movementType,
            variant.Id,
            quantityMoved,
            quantityBefore,
            quantityBefore + quantityMoved);
    }

    private string GetCorrelationId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            if (httpContext.Request.Headers.TryGetValue("X-Correlation-ID", out var correlationId) &&
                !string.IsNullOrEmpty(correlationId))
            {
                return correlationId!;
            }
            return httpContext.TraceIdentifier;
        }
        return Guid.NewGuid().ToString("N");
    }
}
