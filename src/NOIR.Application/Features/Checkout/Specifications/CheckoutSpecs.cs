namespace NOIR.Application.Features.Checkout.Specifications;

/// <summary>
/// Specification for getting a checkout session by ID.
/// </summary>
public class CheckoutSessionByIdSpec : Specification<CheckoutSession>
{
    public CheckoutSessionByIdSpec(Guid sessionId)
    {
        Query.Where(s => s.Id == sessionId)
             .TagWith("CheckoutSessionById");
    }
}

/// <summary>
/// Specification for getting a checkout session by ID for update (with tracking).
/// </summary>
public class CheckoutSessionByIdForUpdateSpec : Specification<CheckoutSession>
{
    public CheckoutSessionByIdForUpdateSpec(Guid sessionId)
    {
        Query.Where(s => s.Id == sessionId)
             .AsTracking()
             .TagWith("CheckoutSessionByIdForUpdate");
    }
}

/// <summary>
/// Specification for getting active checkout session for a cart.
/// </summary>
public class ActiveCheckoutSessionByCartIdSpec : Specification<CheckoutSession>
{
    public ActiveCheckoutSessionByCartIdSpec(Guid cartId)
    {
        Query.Where(s => s.CartId == cartId)
             .Where(s => s.Status != CheckoutSessionStatus.Completed)
             .Where(s => s.Status != CheckoutSessionStatus.Expired)
             .Where(s => s.Status != CheckoutSessionStatus.Abandoned)
             .OrderByDescending(s => s.CreatedAt)
             .TagWith("ActiveCheckoutSessionByCartId");
    }
}

/// <summary>
/// Specification for getting active checkout session for a user.
/// </summary>
public class ActiveCheckoutSessionByUserIdSpec : Specification<CheckoutSession>
{
    public ActiveCheckoutSessionByUserIdSpec(string userId)
    {
        Query.Where(s => s.UserId == userId)
             .Where(s => s.Status != CheckoutSessionStatus.Completed)
             .Where(s => s.Status != CheckoutSessionStatus.Expired)
             .Where(s => s.Status != CheckoutSessionStatus.Abandoned)
             .OrderByDescending(s => s.CreatedAt)
             .TagWith("ActiveCheckoutSessionByUserId");
    }
}

/// <summary>
/// Specification for getting expired checkout sessions for cleanup.
/// </summary>
public class ExpiredCheckoutSessionsSpec : Specification<CheckoutSession>
{
    public ExpiredCheckoutSessionsSpec()
    {
        var now = DateTimeOffset.UtcNow;
        Query.Where(s => s.ExpiresAt < now)
             .Where(s => s.Status != CheckoutSessionStatus.Completed)
             .Where(s => s.Status != CheckoutSessionStatus.Expired)
             .Where(s => s.Status != CheckoutSessionStatus.Abandoned)
             .AsTracking()
             .TagWith("ExpiredCheckoutSessions");
    }
}

/// <summary>
/// Specification for getting a cart by ID with items for checkout.
/// </summary>
public class CartByIdWithItemsSpec : Specification<Domain.Entities.Cart.Cart>
{
    public CartByIdWithItemsSpec(Guid cartId)
    {
        Query.Where(c => c.Id == cartId)
             .Include(c => c.Items)
             .TagWith("CartByIdWithItems");
    }
}

/// <summary>
/// Specification for getting a cart by ID with items for update (with tracking).
/// </summary>
public class CartByIdWithItemsForUpdateSpec : Specification<Domain.Entities.Cart.Cart>
{
    public CartByIdWithItemsForUpdateSpec(Guid cartId)
    {
        Query.Where(c => c.Id == cartId)
             .Include(c => c.Items)
             .AsTracking()
             .TagWith("CartByIdWithItemsForUpdate");
    }
}
