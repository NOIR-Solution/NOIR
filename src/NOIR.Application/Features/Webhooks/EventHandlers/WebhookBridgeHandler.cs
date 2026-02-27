namespace NOIR.Application.Features.Webhooks.EventHandlers;

/// <summary>
/// Bridges domain events to the webhook dispatch system.
/// Has a Handle() overload for every webhook-eligible domain event.
/// Each handler checks if the Webhooks feature is enabled before dispatching.
/// Wolverine auto-discovers these handlers via convention.
/// </summary>
public class WebhookBridgeHandler
{
    private readonly IFeatureChecker _featureChecker;
    private readonly IWebhookDispatcher _dispatcher;
    private readonly ILogger<WebhookBridgeHandler> _logger;

    public WebhookBridgeHandler(
        IFeatureChecker featureChecker,
        IWebhookDispatcher dispatcher,
        ILogger<WebhookBridgeHandler> logger)
    {
        _featureChecker = featureChecker;
        _dispatcher = dispatcher;
        _logger = logger;
    }

    // ─── Product Events ──────────────────────────────────────────────

    public async Task Handle(ProductCreatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(ProductPublishedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(ProductArchivedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(ProductUpdatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(ProductStockChangedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    // ─── Product Category Events ─────────────────────────────────────

    public async Task Handle(ProductCategoryCreatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(ProductCategoryUpdatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(ProductCategoryDeletedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    // ─── Brand Events ────────────────────────────────────────────────

    public async Task Handle(BrandCreatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(BrandUpdatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(BrandDeletedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    // ─── Order Events ────────────────────────────────────────────────

    public async Task Handle(OrderCreatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(OrderConfirmedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(OrderShippedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(OrderDeliveredEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(OrderCompletedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(OrderCancelledEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(OrderRefundedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(OrderReturnedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    // ─── Payment Events ──────────────────────────────────────────────

    public async Task Handle(PaymentCreatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(PaymentSucceededEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(PaymentFailedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(RefundCompletedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(CodCollectedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    // ─── Cart Events ─────────────────────────────────────────────────

    public async Task Handle(CartAbandonedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(CartConvertedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    // ─── Checkout Events ─────────────────────────────────────────────

    public async Task Handle(CheckoutCompletedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    // ─── Customer Events ─────────────────────────────────────────────

    public async Task Handle(CustomerCreatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(CustomerUpdatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(CustomerDeactivatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(CustomerTierChangedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(CustomerSegmentChangedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    // ─── Inventory Events ────────────────────────────────────────────

    public async Task Handle(InventoryReceiptCreatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(InventoryReceiptConfirmedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(InventoryReceiptCancelledEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    // ─── Review Events ───────────────────────────────────────────────

    public async Task Handle(ReviewCreatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(ReviewApprovedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(ReviewRejectedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    // ─── Wishlist Events ─────────────────────────────────────────────

    public async Task Handle(WishlistCreatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(WishlistItemAddedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(WishlistItemRemovedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    // ─── Promotion Events ────────────────────────────────────────────

    public async Task Handle(PromotionCreatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(PromotionActivatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(PromotionDeactivatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(PromotionAppliedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    // ─── Blog Events ─────────────────────────────────────────────────

    public async Task Handle(PostCreatedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(PostPublishedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    public async Task Handle(PostUnpublishedEvent evt, CancellationToken ct)
    {
        if (!await IsWebhooksEnabled(ct)) return;
        await _dispatcher.DispatchAsync(evt, ct);
    }

    // ─── Helper ──────────────────────────────────────────────────────

    private async Task<bool> IsWebhooksEnabled(CancellationToken ct)
    {
        var enabled = await _featureChecker.IsEnabledAsync(ModuleNames.Integrations.Webhooks, ct);
        if (!enabled)
        {
            _logger.LogDebug("Webhooks feature is disabled for current tenant, skipping dispatch");
        }
        return enabled;
    }
}
