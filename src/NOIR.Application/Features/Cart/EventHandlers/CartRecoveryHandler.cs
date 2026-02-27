namespace NOIR.Application.Features.Cart.EventHandlers;

/// <summary>
/// Handles cart abandonment events by sending recovery emails.
/// </summary>
public class CartRecoveryHandler
{
    private readonly IEmailService _emailService;
    private readonly IUserIdentityService _userIdentityService;
    private readonly ILogger<CartRecoveryHandler> _logger;

    public CartRecoveryHandler(
        IEmailService emailService,
        IUserIdentityService userIdentityService,
        ILogger<CartRecoveryHandler> logger)
    {
        _emailService = emailService;
        _userIdentityService = userIdentityService;
        _logger = logger;
    }

    public async Task Handle(CartAbandonedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Processing abandoned cart recovery for cart {CartId}", evt.CartId);

        if (evt.UserId is null)
        {
            _logger.LogDebug("Abandoned cart {CartId} is a guest cart, skipping recovery email", evt.CartId);
            return;
        }

        var user = await _userIdentityService.FindByIdAsync(evt.UserId, ct);
        if (user is null)
        {
            _logger.LogWarning("User {UserId} not found for cart recovery email", evt.UserId);
            return;
        }

        try
        {
            await _emailService.SendTemplateAsync(
                user.Email,
                "You left something behind!",
                "cart_abandoned_recovery",
                new { CartId = evt.CartId, evt.ItemCount, evt.Subtotal },
                ct);

            _logger.LogDebug("Sent cart recovery email to {UserId} for cart {CartId}", evt.UserId, evt.CartId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send cart recovery email for cart {CartId}", evt.CartId);
        }
    }
}
