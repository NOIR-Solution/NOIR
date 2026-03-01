namespace NOIR.Application.Features.Webhooks;

/// <summary>
/// Singleton registry that maps domain event types to webhook-friendly string names.
/// Used by the WebhookDispatcher to determine which events are webhook-eligible
/// and by the API to expose available event types.
/// </summary>
public sealed class WebhookEventTypeRegistry : ISingletonService
{
    private static readonly Dictionary<Type, string> _eventTypeMap = new()
    {
        // Product
        { typeof(ProductCreatedEvent), "product.created" },
        { typeof(ProductPublishedEvent), "product.published" },
        { typeof(ProductArchivedEvent), "product.archived" },
        { typeof(ProductUpdatedEvent), "product.updated" },
        { typeof(ProductStockChangedEvent), "product.stock_changed" },

        // Product Category
        { typeof(ProductCategoryCreatedEvent), "product_category.created" },
        { typeof(ProductCategoryUpdatedEvent), "product_category.updated" },
        { typeof(ProductCategoryDeletedEvent), "product_category.deleted" },

        // Brand
        { typeof(BrandCreatedEvent), "brand.created" },
        { typeof(BrandUpdatedEvent), "brand.updated" },
        { typeof(BrandDeletedEvent), "brand.deleted" },

        // Order
        { typeof(OrderCreatedEvent), "order.created" },
        { typeof(OrderConfirmedEvent), "order.confirmed" },
        { typeof(OrderShippedEvent), "order.shipped" },
        { typeof(OrderDeliveredEvent), "order.delivered" },
        { typeof(OrderCompletedEvent), "order.completed" },
        { typeof(OrderCancelledEvent), "order.cancelled" },
        { typeof(OrderRefundedEvent), "order.refunded" },
        { typeof(OrderReturnedEvent), "order.returned" },

        // Payment
        { typeof(PaymentCreatedEvent), "payment.created" },
        { typeof(PaymentSucceededEvent), "payment.succeeded" },
        { typeof(PaymentFailedEvent), "payment.failed" },
        { typeof(RefundCompletedEvent), "payment.refund_completed" },
        { typeof(CodCollectedEvent), "payment.cod_collected" },

        // Cart
        { typeof(CartAbandonedEvent), "cart.abandoned" },
        { typeof(CartConvertedEvent), "cart.converted" },

        // Checkout
        { typeof(CheckoutCompletedEvent), "checkout.completed" },

        // Customer
        { typeof(CustomerCreatedEvent), "customer.created" },
        { typeof(CustomerUpdatedEvent), "customer.updated" },
        { typeof(CustomerDeactivatedEvent), "customer.deactivated" },
        { typeof(CustomerTierChangedEvent), "customer.tier_changed" },
        { typeof(CustomerSegmentChangedEvent), "customer.segment_changed" },

        // Inventory
        { typeof(InventoryReceiptCreatedEvent), "inventory.receipt_created" },
        { typeof(InventoryReceiptConfirmedEvent), "inventory.receipt_confirmed" },
        { typeof(InventoryReceiptCancelledEvent), "inventory.receipt_cancelled" },

        // Review
        { typeof(ReviewCreatedEvent), "review.created" },
        { typeof(ReviewApprovedEvent), "review.approved" },
        { typeof(ReviewRejectedEvent), "review.rejected" },

        // Wishlist
        { typeof(WishlistCreatedEvent), "wishlist.created" },
        { typeof(WishlistItemAddedEvent), "wishlist.item_added" },
        { typeof(WishlistItemRemovedEvent), "wishlist.item_removed" },

        // Promotion
        { typeof(PromotionCreatedEvent), "promotion.created" },
        { typeof(PromotionActivatedEvent), "promotion.activated" },
        { typeof(PromotionDeactivatedEvent), "promotion.deactivated" },
        { typeof(PromotionAppliedEvent), "promotion.applied" },

        // Blog
        { typeof(PostCreatedEvent), "blog.post_created" },
        { typeof(PostPublishedEvent), "blog.post_published" },
        { typeof(PostUnpublishedEvent), "blog.post_unpublished" },

        // CRM
        { typeof(ContactCreatedEvent), "crm.contact_created" },
        { typeof(CompanyCreatedEvent), "crm.company_created" },
        { typeof(LeadCreatedEvent), "crm.lead_created" },
        { typeof(LeadWonEvent), "crm.lead_won" },
        { typeof(LeadLostEvent), "crm.lead_lost" },
        { typeof(LeadReopenedEvent), "crm.lead_reopened" },

        // HR
        { typeof(EmployeeCreatedEvent), "hr.employee_created" },
        { typeof(EmployeeUpdatedEvent), "hr.employee_updated" },
        { typeof(EmployeeDeactivatedEvent), "hr.employee_deactivated" },
        { typeof(EmployeeDepartmentChangedEvent), "hr.employee_department_changed" },
        { typeof(DepartmentCreatedEvent), "hr.department_created" },
        { typeof(DepartmentUpdatedEvent), "hr.department_updated" },

        // PM
        { typeof(ProjectCreatedEvent), "pm.project_created" },
        { typeof(ProjectArchivedEvent), "pm.project_archived" },
        { typeof(ProjectCompletedEvent), "pm.project_completed" },
        { typeof(TaskMovedEvent), "pm.task_moved" },
        { typeof(TaskCompletedEvent), "pm.task_completed" },
        { typeof(TaskAssignedEvent), "pm.task_assigned" },
    };

    /// <summary>
    /// Gets the webhook event type string for a given domain event instance.
    /// Returns null if the event is not webhook-eligible.
    /// </summary>
    public string? GetEventType(IDomainEvent domainEvent) =>
        _eventTypeMap.TryGetValue(domainEvent.GetType(), out var eventType) ? eventType : null;

    /// <summary>
    /// Gets the webhook event type string for a given CLR type.
    /// Returns null if the type is not registered.
    /// </summary>
    public string? GetEventType(Type eventType) =>
        _eventTypeMap.TryGetValue(eventType, out var name) ? name : null;

    /// <summary>
    /// Returns all registered webhook event types as DTOs, ordered by event type name.
    /// </summary>
    public IReadOnlyList<WebhookEventTypeDto> GetAllEventTypes() =>
        _eventTypeMap.Select(kvp => new WebhookEventTypeDto
        {
            EventType = kvp.Value,
            Category = kvp.Value.Split('.')[0],
            Description = $"Fired when {kvp.Value.Replace('.', ' ').Replace('_', ' ')} occurs"
        }).OrderBy(e => e.EventType).ToList();
}
