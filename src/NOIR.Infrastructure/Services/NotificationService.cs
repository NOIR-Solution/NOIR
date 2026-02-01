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
            // Fetch user to check platform admin status and get TenantId
            var targetUser = await _userIdentityService.FindByIdAsync(userId, ct);
            if (targetUser is null)
            {
                _logger.LogWarning("Cannot send notification to non-existent user {UserId}", userId);
                return Result.Failure<NotificationDto>(
                    Error.NotFound("User not found", "NOTIFICATION-001"));
            }

            // Delegate to internal overload that accepts pre-fetched user
            return await SendToUserInternalAsync(
                targetUser,
                type,
                category,
                title,
                message,
                iconClass,
                actionUrl,
                actions,
                metadata,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
            return Result.Failure<NotificationDto>(
                Error.Failure("NOTIFICATION_SEND_FAILED", $"Failed to send notification: {ex.Message}"));
        }
    }

    /// <summary>
    /// Internal overload accepting pre-fetched UserIdentityDto to avoid repeated lookups in batch operations.
    /// </summary>
    private async Task<Result<NotificationDto>> SendToUserInternalAsync(
        UserIdentityDto targetUser,
        NotificationType type,
        NotificationCategory category,
        string title,
        string message,
        string? iconClass,
        string? actionUrl,
        IEnumerable<NotificationActionDto>? actions,
        string? metadata,
        CancellationToken ct)
    {
        try
        {
            // Skip notifications for platform admins (they operate across all tenants)
            if (targetUser.IsSystemUser || targetUser.TenantId is null)
            {
                _logger.LogDebug("Skipping notification for platform admin user {UserId}", targetUser.Id);
                return Result.Success<NotificationDto>(null!);
            }

            // Check user preferences
            var prefSpec = new UserPreferencesByCategorySpec(targetUser.Id, category);
            var preference = await _preferenceRepository.FirstOrDefaultAsync(prefSpec, ct);

            // If no preference, create defaults and use them
            if (preference is null)
            {
                preference = NotificationPreference.Create(
                    targetUser.Id,
                    category,
                    inAppEnabled: true,
                    emailFrequency: category == NotificationCategory.Security
                        ? EmailFrequency.Immediate
                        : EmailFrequency.Daily,
                    targetUser.TenantId);
                await _preferenceRepository.AddAsync(preference, ct);
            }

            // Skip if in-app notifications are disabled for this category
            if (!preference.InAppEnabled)
            {
                _logger.LogDebug("In-app notifications disabled for user {UserId}, category {Category}",
                    targetUser.Id, category);

                // Still handle email if configured
                if (preference.EmailFrequency == EmailFrequency.Immediate)
                {
                    await SendImmediateEmailAsync(targetUser.Id, type, title, message, actionUrl, ct);
                }

                return Result.Success<NotificationDto>(null!);
            }

            // 1. PERSIST: Create and save notification
            var notification = Notification.Create(
                targetUser.Id,
                type,
                category,
                title,
                message,
                iconClass,
                actionUrl,
                metadata,
                targetUser.TenantId);

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
            await _hubContext.SendToUserAsync(targetUser.Id, dto, ct);

            // Update unread count
            var unreadCount = await _notificationRepository.CountAsync(
                new UnreadNotificationsCountSpec(targetUser.Id), ct);
            await _hubContext.UpdateUnreadCountAsync(targetUser.Id, unreadCount, ct);

            // 3. EMAIL: Handle immediate email if configured
            if (preference.EmailFrequency == EmailFrequency.Immediate)
            {
                // Queue email to avoid blocking
                _backgroundJobs.Enqueue(() => SendImmediateEmailAsync(targetUser.Id, type, title, message, actionUrl, ct));
                notification.MarkEmailSent();
                await _unitOfWork.SaveChangesAsync(ct);
            }

            _logger.LogInformation("Notification {NotificationId} sent to user {UserId}",
                notification.Id, targetUser.Id);

            return Result.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}", targetUser.Id);
            return Result.Failure<NotificationDto>(
                Error.Failure("NOTIFICATION_SEND_FAILED", $"Failed to send notification: {ex.Message}"));
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
            // Get users in role directly in a single query (fixes N+1)
            var usersInRole = await _userIdentityService.GetUsersInRoleAsync(_currentUser.TenantId, roleName, ct);
            var count = 0;

            foreach (var user in usersInRole)
            {
                // Skip platform admins (they operate across all tenants)
                if (user.IsSystemUser || user.TenantId is null)
                    continue;

                // Use internal method to avoid re-fetching user
                var result = await SendToUserInternalAsync(
                    user,
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
                Error.Failure("NOTIFICATION_ROLE_SEND_FAILED", $"Failed to send notifications to role: {ex.Message}"));
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
            var (users, _) = await _userIdentityService.GetUsersPaginatedAsync(
                _currentUser.TenantId, search: null, page: 1, pageSize: 10000, role: null, isLocked: null, ct);
            var count = 0;

            foreach (var user in users)
            {
                // Skip inactive/deleted users and platform admins
                if (!user.IsActive || user.IsDeleted || user.IsSystemUser || user.TenantId is null)
                    continue;

                // Use internal method to avoid re-fetching user
                var result = await SendToUserInternalAsync(
                    user,
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
                Error.Failure("NOTIFICATION_BROADCAST_FAILED", $"Failed to broadcast notification: {ex.Message}"));
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
