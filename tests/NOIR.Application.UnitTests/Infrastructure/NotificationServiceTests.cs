namespace NOIR.Application.UnitTests.Infrastructure;

using NOIR.Application.Specifications.Notifications;
using NOIR.Domain.Enums;
using NOIR.Domain.ValueObjects;

/// <summary>
/// Unit tests for NotificationService.
/// Tests notification creation, delivery via SignalR, and email handling.
/// </summary>
public class NotificationServiceTests
{
    private readonly Mock<IRepository<Notification, Guid>> _notificationRepoMock;
    private readonly Mock<IRepository<NotificationPreference, Guid>> _preferenceRepoMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<INotificationHubContext> _hubContextMock;
    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IBackgroundJobs> _backgroundJobsMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly NotificationService _sut;

    private const string TestUserId = "test-user-id";
    private const string TestTenantId = "test-tenant-id";

    public NotificationServiceTests()
    {
        _notificationRepoMock = new Mock<IRepository<Notification, Guid>>();
        _preferenceRepoMock = new Mock<IRepository<NotificationPreference, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _hubContextMock = new Mock<INotificationHubContext>();
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _emailServiceMock = new Mock<IEmailService>();
        _backgroundJobsMock = new Mock<IBackgroundJobs>();
        _currentUserMock = new Mock<ICurrentUser>();
        _loggerMock = new Mock<ILogger<NotificationService>>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _sut = new NotificationService(
            _notificationRepoMock.Object,
            _preferenceRepoMock.Object,
            _unitOfWorkMock.Object,
            _hubContextMock.Object,
            _userIdentityServiceMock.Object,
            _emailServiceMock.Object,
            _backgroundJobsMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    #region SendToUserAsync Tests

    [Fact]
    public async Task SendToUserAsync_WithNoExistingPreference_ShouldCreateDefaultPreferenceAndSend()
    {
        // Arrange
        _preferenceRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<UserPreferencesByCategorySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationPreference?)null);

        _notificationRepoMock
            .Setup(x => x.CountAsync(It.IsAny<UnreadNotificationsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.SendToUserAsync(
            TestUserId,
            NotificationType.Info,
            NotificationCategory.System,
            "Test Title",
            "Test Message");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _preferenceRepoMock.Verify(x => x.AddAsync(It.IsAny<NotificationPreference>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationRepoMock.Verify(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _hubContextMock.Verify(x => x.SendToUserAsync(TestUserId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendToUserAsync_WithExistingPreferenceInAppEnabled_ShouldSendNotification()
    {
        // Arrange
        var preference = NotificationPreference.Create(
            TestUserId,
            NotificationCategory.System,
            inAppEnabled: true,
            emailFrequency: EmailFrequency.None,
            TestTenantId);

        _preferenceRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<UserPreferencesByCategorySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        _notificationRepoMock
            .Setup(x => x.CountAsync(It.IsAny<UnreadNotificationsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        var result = await _sut.SendToUserAsync(
            TestUserId,
            NotificationType.Success,
            NotificationCategory.System,
            "Success Title",
            "Success Message");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _notificationRepoMock.Verify(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Once);
        _hubContextMock.Verify(x => x.SendToUserAsync(TestUserId, It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _hubContextMock.Verify(x => x.UpdateUnreadCountAsync(TestUserId, 5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendToUserAsync_WithInAppDisabled_ShouldSkipInAppNotification()
    {
        // Arrange
        var preference = NotificationPreference.Create(
            TestUserId,
            NotificationCategory.Integration, // Changed from Marketing
            inAppEnabled: false,
            emailFrequency: EmailFrequency.None,
            TestTenantId);

        _preferenceRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<UserPreferencesByCategorySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        // Act
        var result = await _sut.SendToUserAsync(
            TestUserId,
            NotificationType.Info,
            NotificationCategory.Integration, // Changed from Marketing
            "Integration Title",
            "Integration Message");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _notificationRepoMock.Verify(x => x.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
        _hubContextMock.Verify(x => x.SendToUserAsync(It.IsAny<string>(), It.IsAny<NotificationDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendToUserAsync_WithImmediateEmail_ShouldQueueEmail()
    {
        // Arrange
        var preference = NotificationPreference.Create(
            TestUserId,
            NotificationCategory.Security,
            inAppEnabled: true,
            emailFrequency: EmailFrequency.Immediate,
            TestTenantId);

        _preferenceRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<UserPreferencesByCategorySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        _notificationRepoMock
            .Setup(x => x.CountAsync(It.IsAny<UnreadNotificationsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.SendToUserAsync(
            TestUserId,
            NotificationType.Warning,
            NotificationCategory.Security,
            "Security Alert",
            "Your account was accessed");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _backgroundJobsMock.Verify(x => x.Enqueue(It.IsAny<Expression<Func<Task>>>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeast(2)); // Once for notification, once for email sent flag
    }

    [Fact]
    public async Task SendToUserAsync_WithActions_ShouldAddActionsToNotification()
    {
        // Arrange
        var preference = NotificationPreference.Create(
            TestUserId,
            NotificationCategory.System,
            inAppEnabled: true,
            emailFrequency: EmailFrequency.None,
            TestTenantId);

        _preferenceRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<UserPreferencesByCategorySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        _notificationRepoMock
            .Setup(x => x.CountAsync(It.IsAny<UnreadNotificationsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var actions = new[]
        {
            new NotificationActionDto("Approve", "/approve/123", "primary", "POST"),
            new NotificationActionDto("Deny", "/deny/123", "destructive", "POST")
        };

        // Act
        var result = await _sut.SendToUserAsync(
            TestUserId,
            NotificationType.Info,
            NotificationCategory.System,
            "Approval Required",
            "Please review this request",
            actions: actions);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _notificationRepoMock.Verify(x => x.AddAsync(
            It.Is<Notification>(n => n.Actions.Count == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendToUserAsync_WithOptionalParameters_ShouldPassThrough()
    {
        // Arrange
        var preference = NotificationPreference.Create(
            TestUserId,
            NotificationCategory.System,
            inAppEnabled: true,
            emailFrequency: EmailFrequency.None,
            TestTenantId);

        _preferenceRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<UserPreferencesByCategorySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(preference);

        _notificationRepoMock
            .Setup(x => x.CountAsync(It.IsAny<UnreadNotificationsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _sut.SendToUserAsync(
            TestUserId,
            NotificationType.Success,
            NotificationCategory.System,
            "Order Complete",
            "Your order has been processed",
            iconClass: "check-circle",
            actionUrl: "/orders/123",
            metadata: "{\"orderId\": \"123\"}");

        // Assert
        result.IsSuccess.Should().BeTrue();
        _notificationRepoMock.Verify(x => x.AddAsync(
            It.Is<Notification>(n =>
                n.IconClass == "check-circle" &&
                n.ActionUrl == "/orders/123" &&
                n.Metadata == "{\"orderId\": \"123\"}"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendToUserAsync_WhenExceptionOccurs_ShouldReturnFailure()
    {
        // Arrange
        _preferenceRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<UserPreferencesByCategorySpec>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _sut.SendToUserAsync(
            TestUserId,
            NotificationType.Error,
            NotificationCategory.System,
            "Test",
            "Test message");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Be("NOTIFICATION_SEND_FAILED");
    }

    [Fact]
    public async Task SendToUserAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _preferenceRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<UserPreferencesByCategorySpec>(), cts.Token))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var result = await _sut.SendToUserAsync(
            TestUserId,
            NotificationType.Info,
            NotificationCategory.System,
            "Test",
            "Test message",
            ct: cts.Token);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region SendToRoleAsync Tests

    [Fact]
    public async Task SendToRoleAsync_WithUsersInRole_ShouldSendToAllRoleMembers()
    {
        // Arrange
        var users = new List<UserIdentityDto>
        {
            CreateTestUser("user1"),
            CreateTestUser("user2"),
            CreateTestUser("user3")
        };

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(null, 1, 1000, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users.AsReadOnly(), 3));

        _userIdentityServiceMock
            .Setup(x => x.IsInRoleAsync(It.IsAny<string>(), "Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string userId, string roleName, CancellationToken ct) =>
                userId == "user1" || userId == "user2");

        SetupSuccessfulNotificationSend();

        // Act
        var result = await _sut.SendToRoleAsync(
            "Admin",
            NotificationType.Info,
            NotificationCategory.System,
            "Role Alert",
            "Message for admins");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2); // Only user1 and user2 are in the Admin role
    }

    [Fact]
    public async Task SendToRoleAsync_WithNoUsersInRole_ShouldReturnZero()
    {
        // Arrange
        var users = new List<UserIdentityDto>
        {
            CreateTestUser("user1"),
            CreateTestUser("user2")
        };

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(null, 1, 1000, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users.AsReadOnly(), 2));

        _userIdentityServiceMock
            .Setup(x => x.IsInRoleAsync(It.IsAny<string>(), "SuperAdmin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.SendToRoleAsync(
            "SuperAdmin",
            NotificationType.Info,
            NotificationCategory.System,
            "Super Admin Alert",
            "Message");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task SendToRoleAsync_WhenExceptionOccurs_ShouldReturnFailure()
    {
        // Arrange
        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(null, 1, 1000, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act
        var result = await _sut.SendToRoleAsync(
            "Admin",
            NotificationType.Info,
            NotificationCategory.System,
            "Test",
            "Test message");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Be("NOTIFICATION_ROLE_SEND_FAILED");
    }

    #endregion

    #region BroadcastAsync Tests

    [Fact]
    public async Task BroadcastAsync_WithActiveUsers_ShouldSendToAll()
    {
        // Arrange
        var users = new List<UserIdentityDto>
        {
            CreateTestUser("user1", isActive: true, isDeleted: false),
            CreateTestUser("user2", isActive: true, isDeleted: false),
            CreateTestUser("user3", isActive: false, isDeleted: false), // Inactive
            CreateTestUser("user4", isActive: true, isDeleted: true)   // Deleted
        };

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(null, 1, 10000, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users.AsReadOnly(), 4));

        SetupSuccessfulNotificationSend();

        // Act
        var result = await _sut.BroadcastAsync(
            NotificationType.Warning,
            NotificationCategory.System,
            "System Maintenance",
            "System will be down for maintenance");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(2); // Only user1 and user2 are active and not deleted
    }

    [Fact]
    public async Task BroadcastAsync_WithNoActiveUsers_ShouldReturnZero()
    {
        // Arrange
        var users = new List<UserIdentityDto>
        {
            CreateTestUser("user1", isActive: false, isDeleted: false),
            CreateTestUser("user2", isActive: true, isDeleted: true)
        };

        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(null, 1, 10000, It.IsAny<CancellationToken>()))
            .ReturnsAsync((users.AsReadOnly(), 2));

        // Act
        var result = await _sut.BroadcastAsync(
            NotificationType.Info,
            NotificationCategory.System,
            "Broadcast",
            "Message");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(0);
    }

    [Fact]
    public async Task BroadcastAsync_WhenExceptionOccurs_ShouldReturnFailure()
    {
        // Arrange
        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(null, 1, 10000, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act
        var result = await _sut.BroadcastAsync(
            NotificationType.Info,
            NotificationCategory.System,
            "Test",
            "Test message");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Be("NOTIFICATION_BROADCAST_FAILED");
    }

    #endregion

    #region Service Registration Tests

    [Fact]
    public void Service_ShouldImplementINotificationService()
    {
        // Assert
        _sut.Should().BeAssignableTo<INotificationService>();
    }

    [Fact]
    public void Service_ShouldImplementIScopedService()
    {
        // Assert
        _sut.Should().BeAssignableTo<IScopedService>();
    }

    #endregion

    #region Helper Methods

    private static UserIdentityDto CreateTestUser(string id, bool isActive = true, bool isDeleted = false)
    {
        return new UserIdentityDto(
            Id: id,
            Email: $"{id}@test.com",
            FirstName: "Test",
            LastName: $"User {id}",
            DisplayName: null,
            FullName: $"Test User {id}",
            PhoneNumber: null,
            AvatarUrl: null,
            IsActive: isActive,
            IsDeleted: isDeleted,
            IsSystemUser: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: null);
    }

    private void SetupSuccessfulNotificationSend()
    {
        _preferenceRepoMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<UserPreferencesByCategorySpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserPreferencesByCategorySpec spec, CancellationToken ct) =>
                NotificationPreference.Create(
                    "test-user",
                    NotificationCategory.System,
                    inAppEnabled: true,
                    emailFrequency: EmailFrequency.None,
                    TestTenantId));

        _notificationRepoMock
            .Setup(x => x.CountAsync(It.IsAny<UnreadNotificationsCountSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    #endregion
}
