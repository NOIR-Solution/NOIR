namespace NOIR.Application.Features.Customers.EventHandlers;

/// <summary>
/// Handles customer domain events by sending welcome emails and tier change notifications.
/// </summary>
public class CustomerNotificationHandler
{
    private readonly IEmailService _emailService;
    private readonly INotificationService _notificationService;
    private readonly IRepository<Customer, Guid> _customerRepository;
    private readonly ILogger<CustomerNotificationHandler> _logger;

    public CustomerNotificationHandler(
        IEmailService emailService,
        INotificationService notificationService,
        IRepository<Customer, Guid> customerRepository,
        ILogger<CustomerNotificationHandler> logger)
    {
        _emailService = emailService;
        _notificationService = notificationService;
        _customerRepository = customerRepository;
        _logger = logger;
    }

    public async Task Handle(CustomerCreatedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Sending welcome email for new customer {CustomerId}", evt.CustomerId);

        try
        {
            await _emailService.SendTemplateAsync(
                evt.Email,
                "Welcome to NOIR",
                "customer_welcome",
                new { evt.FirstName, evt.LastName, evt.Email },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send welcome email for customer {CustomerId}", evt.CustomerId);
        }
    }

    public async Task Handle(CustomerTierChangedEvent evt, CancellationToken ct)
    {
        _logger.LogDebug("Sending tier change notification for customer {CustomerId}: {OldTier} -> {NewTier}",
            evt.CustomerId, evt.OldTier, evt.NewTier);

        var customer = await _customerRepository.GetByIdAsync(evt.CustomerId, ct);
        if (customer is null)
        {
            _logger.LogWarning("Customer {CustomerId} not found for tier change notification", evt.CustomerId);
            return;
        }

        try
        {
            await _emailService.SendTemplateAsync(
                customer.Email,
                $"Congratulations! You've reached {evt.NewTier} tier",
                "customer_tier_upgrade",
                new { customer.FirstName, OldTier = evt.OldTier.ToString(), NewTier = evt.NewTier.ToString() },
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send tier change email for customer {CustomerId}", evt.CustomerId);
        }

        try
        {
            await _notificationService.SendToRoleAsync(
                "admin",
                NotificationType.Info,
                NotificationCategory.Workflow,
                "Customer Tier Changed",
                $"Customer {customer.FirstName} {customer.LastName} moved from {evt.OldTier} to {evt.NewTier}.",
                actionUrl: $"/customers/{evt.CustomerId}",
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send admin notification for tier change of customer {CustomerId}", evt.CustomerId);
        }
    }
}
