namespace NOIR.Infrastructure.Services;

using NOIR.Application.Features.Notifications.DTOs;
using NOIR.Application.Specifications.Notifications;
using NOIR.Domain.Enums;
using NOIR.Domain.ValueObjects;

/// <summary>
/// Implementation of INotificationService.
/// Follows "Persist then Notify" pattern for reliable notification delivery.
/// </summary>
public class NotificationService : INotificationService, IScopedService
{
    private readonly IRepository<Notification, Guid> _notificationRepository;
    private readonly IRepository<NotificationPreference, Guid> _preferenceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationHubContext _hubContext;
    private readonly IUserIdentityService _userIdentityService;
    private readonly IEmailService _emailService;
    private readonly IBackgroundJobs _backgroundJobs;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IRepository<Notification, Guid> notificationRepository,
        IRepository<NotificationPreference, Guid> preferenceRepository,
        IUnitOfWork unitOfWork,
        INotificationHubContext hubContext,
        IUserIdentityService userIdentityService,
        IEmailService emailService,
        IBackgroundJobs backgroundJobs,
        ICurrentUser currentUser,
        ILogger<NotificationService> logger)
    {
        _notificationRepository = notificationRepository;
        _preferenceRepository = preferenceRepository;
        _unitOfWork = unitOfWork;
        _hubContext = hubContext;
        _userIdentityService = userIdentityService;
        _emailService = emailService;
        _backgroundJobs = backgroundJobs;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<Result<NotificationDto>> SendToUserAsync(
        string userId,
        NotificationType type,
        NotificationCategory category,
        string title,
        string message,
        string? iconClass = null,
        string? actionUrl = null,
        IEnumerable<NotificationActionDto>? actions = null,
        string? metadata = null,
        CancellationToken ct = default)
    {
        try
        {
            // Skip notifications for platform admins (they don't belong to a tenant)
            if (_currentUser.TenantId == null)
            {
                _logger.LogDebug("Skipping notification for platform admin user {UserId}", userId);
                return Result.Success<NotificationDto>(null!);
            }

            // Check user preferences
            var prefSpec = new UserPreferencesByCategorySpec(userId, category);
            var preference = await _preferenceRepository.FirstOrDefaultAsync(prefSpec, ct);

            // If no preference, create defaults and use them
            if (preference is null)
            {
                preference = NotificationPreference.Create(
                    userId,
                    category,
                    inAppEnabled: true,
                    emailFrequency: category == NotificationCategory.Security
                        ? EmailFrequency.Immediate
                        : EmailFrequency.Daily,
                    _currentUser.TenantId);
                await _preferenceRepository.AddAsync(preference, ct);
            }

            // Skip if in-app notifications are disabled for this category
            if (!preference.InAppEnabled)
            {
                _logger.LogDebug("In-app notifications disabled for user {UserId}, category {Category}",
                    userId, category);

                // Still handle email if configured
                if (preference.EmailFrequency == EmailFrequency.Immediate)
                {
                    await SendImmediateEmailAsync(userId, type, title, message, actionUrl, ct);
                }

                return Result.Success<NotificationDto>(null!);
            }

            // 1. PERSIST: Create and save notification
            var notification = Notification.Create(
                userId,
                type,
                category,
                title,
                message,
                iconClass,
                actionUrl,
                metadata,
                _currentUser.TenantId);

            // Add actions if provided
            if (actions != null)
            {
                foreach (var action in actions)
                {
                    notification.AddAction(NotificationAction.Create(
                        action.Label,
                        action.Url,
                        action.Style,
                        action.Method));
                }
            }

            await _notificationRepository.AddAsync(notification, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            // 2. NOTIFY: Push via SignalR
            var dto = MapToDto(notification);
            await _hubContext.SendToUserAsync(userId, dto, ct);

            // Update unread count
            var unreadCount = await _notificationRepository.CountAsync(
                new UnreadNotificationsCountSpec(userId), ct);
            await _hubContext.UpdateUnreadCountAsync(userId, unreadCount, ct);

            // 3. EMAIL: Handle immediate email if configured
            if (preference.EmailFrequency == EmailFrequency.Immediate)
            {
                // Queue email to avoid blocking
                _backgroundJobs.Enqueue(() => SendImmediateEmailAsync(userId, type, title, message, actionUrl, ct));
                notification.MarkEmailSent();
                await _unitOfWork.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Notification {NotificationId} sent to user {UserId}",
                notification.Id, userId);

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
            return Result.Failure<NotificationDto>(
                Error.Failure($"Failed to send notification: {ex.Message}", "NOTIFICATION_SEND_FAILED"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<int>> SendToRoleAsync(
        string roleName,
        NotificationType type,
        NotificationCategory category,
        string title,
        string message,
        string? iconClass = null,
        string? actionUrl = null,
        IEnumerable<NotificationActionDto>? actions = null,
        string? metadata = null,
        CancellationToken ct = default)
    {
        try
        {
            // Get users from current tenant and filter by role
            // This is a simplified approach - for large user bases, consider a dedicated endpoint
            var (users, _) = await _userIdentityService.GetUsersPaginatedAsync(_currentUser.TenantId, null, 1, 1000, ct);
            var count = 0;

            foreach (var user in users)
            {
                // Check if user is in role
                var isInRole = await _userIdentityService.IsInRoleAsync(user.Id, roleName, ct);
                if (!isInRole) continue;

                var result = await SendToUserAsync(
                    user.Id,
                    type,
                    category,
                    title,
                    message,
                    iconClass,
                    actionUrl,
                    actions,
                    metadata,
                    ct);

                if (result.IsSuccess)
                    count++;
            }

            _logger.LogInformation("Sent {Count} notifications to role {RoleName}", count, roleName);
            return Result.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notifications to role {RoleName}", roleName);
            return Result.Failure<int>(
                Error.Failure($"Failed to send notifications to role: {ex.Message}", "NOTIFICATION_ROLE_SEND_FAILED"));
        }
    }

    /// <inheritdoc />
    public async Task<Result<int>> BroadcastAsync(
        NotificationType type,
        NotificationCategory category,
        string title,
        string message,
        string? iconClass = null,
        string? actionUrl = null,
        IEnumerable<NotificationActionDto>? actions = null,
        string? metadata = null,
        CancellationToken ct = default)
    {
        try
        {
            // Get all users in current tenant - for large user bases, consider background job batching
            var (users, _) = await _userIdentityService.GetUsersPaginatedAsync(_currentUser.TenantId, null, 1, 10000, ct);
            var count = 0;

            foreach (var user in users)
            {
                if (!user.IsActive || user.IsDeleted) continue;

                var result = await SendToUserAsync(
                    user.Id,
                    type,
                    category,
                    title,
                    message,
                    iconClass,
                    actionUrl,
                    actions,
                    metadata,
                    ct);

                if (result.IsSuccess)
                    count++;
            }

            _logger.LogInformation("Broadcast notification to {Count} users", count);
            return Result.Success(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast notification");
            return Result.Failure<int>(
                Error.Failure($"Failed to broadcast notification: {ex.Message}", "NOTIFICATION_BROADCAST_FAILED"));
        }
    }

    private async Task SendImmediateEmailAsync(
        string userId,
        NotificationType type,
        string title,
        string message,
        string? actionUrl,
        CancellationToken ct)
    {
        try
        {
            var user = await _userIdentityService.FindByIdAsync(userId, ct);
            if (user?.Email is null) return;

            var subject = $"[{type}] {title}";
            var body = $@"
                <h2>{title}</h2>
                <p>{message}</p>
                {(actionUrl != null ? $"<p><a href=\"{actionUrl}\">View Details</a></p>" : "")}
            ";

            await _emailService.SendAsync(user.Email, subject, body, isHtml: true, ct);
            _logger.LogDebug("Sent immediate email notification to {Email}", user.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send immediate email to user {UserId}", userId);
        }
    }

    private static NotificationDto MapToDto(Notification n) => new(
        n.Id,
        n.Type,
        n.Category,
        n.Title,
        n.Message,
        n.IconClass,
        n.IsRead,
        n.ReadAt,
        n.ActionUrl,
        n.Actions.Select(a => new NotificationActionDto(a.Label, a.Url, a.Style, a.Method)),
        n.CreatedAt);
}
