namespace NOIR.Application.Features.Cart.Specifications;

/// <summary>
/// Specification to get active cart by user ID.
/// </summary>
public sealed class ActiveCartByUserIdSpec : Specification<Domain.Entities.Cart.Cart>
{
    public ActiveCartByUserIdSpec(string userId, bool forUpdate = false)
    {
        Query.Where(c => c.UserId == userId && c.Status == CartStatus.Active)
            .Include(c => c.Items)
            .TagWith("ActiveCartByUserId");

        if (forUpdate)
        {
            Query.AsTracking();
        }
    }
}

/// <summary>
/// Specification to get active cart by session ID.
/// </summary>
public sealed class ActiveCartBySessionIdSpec : Specification<Domain.Entities.Cart.Cart>
{
    public ActiveCartBySessionIdSpec(string sessionId, bool forUpdate = false)
    {
        Query.Where(c => c.SessionId == sessionId && c.Status == CartStatus.Active)
            .Include(c => c.Items)
            .TagWith("ActiveCartBySessionId");

        if (forUpdate)
        {
            Query.AsTracking();
        }
    }
}

/// <summary>
/// Specification to get cart by ID with items.
/// </summary>
public sealed class CartByIdSpec : Specification<Domain.Entities.Cart.Cart>
{
    public CartByIdSpec(Guid cartId, bool forUpdate = false)
    {
        Query.Where(c => c.Id == cartId)
            .Include(c => c.Items)
            .TagWith("CartById");

        if (forUpdate)
        {
            Query.AsTracking();
        }
    }
}

/// <summary>
/// Specification to find abandoned carts for cleanup job.
/// </summary>
public sealed class AbandonedCartsSpec : Specification<Domain.Entities.Cart.Cart>
{
    public AbandonedCartsSpec(DateTimeOffset abandonmentThreshold)
    {
        Query.Where(c => c.Status == CartStatus.Active &&
                         c.LastActivityAt < abandonmentThreshold)
            .TagWith("AbandonedCarts");
    }
}

/// <summary>
/// Specification to find expired guest carts for cleanup.
/// </summary>
public sealed class ExpiredGuestCartsSpec : Specification<Domain.Entities.Cart.Cart>
{
    public ExpiredGuestCartsSpec()
    {
        Query.Where(c => c.Status == CartStatus.Active &&
                         c.ExpiresAt.HasValue &&
                         c.ExpiresAt < DateTimeOffset.UtcNow)
            .TagWith("ExpiredGuestCarts");
    }
}
