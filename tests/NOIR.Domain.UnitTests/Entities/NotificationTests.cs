namespace NOIR.Domain.UnitTests.Entities;

using NOIR.Domain.Enums;
using NOIR.Domain.ValueObjects;

/// <summary>
/// Unit tests for the Notification entity.
/// Tests factory methods, state transitions, and action management.
/// </summary>
public class NotificationTests
{
    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidNotification()
    {
        // Arrange
        var userId = "user-123";
        var type = NotificationType.Info;
        var category = NotificationCategory.System;
        var title = "System Update";
        var message = "A new version is available";

        // Act
        var notification = Notification.Create(userId, type, category, title, message);

        // Assert
        notification.Should().NotBeNull();
        notification.Id.Should().NotBe(Guid.Empty);
        notification.UserId.Should().Be(userId);
        notification.Type.Should().Be(type);
        notification.Category.Should().Be(category);
        notification.Title.Should().Be(title);
        notification.Message.Should().Be(message);
        notification.IsRead.Should().BeFalse();
        notification.EmailSent.Should().BeFalse();
        notification.IncludedInDigest.Should().BeFalse();
    }

    [Fact]
    public void Create_WithOptionalParameters_ShouldSetAllProperties()
    {
        // Arrange
        var iconClass = "bell";
        var actionUrl = "/dashboard/tasks";
        var metadata = "{\"taskId\": 123}";
        var tenantId = "tenant-abc";

        // Act
        var notification = Notification.Create(
            "user-123", NotificationType.Success, NotificationCategory.UserAction,
            "Task Completed", "Your task has been completed",
            iconClass, actionUrl, metadata, tenantId);

        // Assert
        notification.IconClass.Should().Be(iconClass);
        notification.ActionUrl.Should().Be(actionUrl);
        notification.Metadata.Should().Be(metadata);
        notification.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_WithNullUserId_ShouldThrowArgumentException()
    {
        // Act
        var act = () => Notification.Create(null!, NotificationType.Info, NotificationCategory.System, "Title", "Message");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyTitle_ShouldThrowArgumentException()
    {
        // Act
        var act = () => Notification.Create("user-123", NotificationType.Info, NotificationCategory.System, "", "Message");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithEmptyMessage_ShouldThrowArgumentException()
    {
        // Act
        var act = () => Notification.Create("user-123", NotificationType.Info, NotificationCategory.System, "Title", "");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(NotificationType.Info)]
    [InlineData(NotificationType.Success)]
    [InlineData(NotificationType.Warning)]
    [InlineData(NotificationType.Error)]
    public void Create_WithVariousTypes_ShouldSetCorrectType(NotificationType type)
    {
        // Act
        var notification = Notification.Create("user-123", type, NotificationCategory.System, "Title", "Message");

        // Assert
        notification.Type.Should().Be(type);
    }

    [Theory]
    [InlineData(NotificationCategory.System)]
    [InlineData(NotificationCategory.UserAction)]
    [InlineData(NotificationCategory.Workflow)]
    [InlineData(NotificationCategory.Security)]
    [InlineData(NotificationCategory.Integration)]
    public void Create_WithVariousCategories_ShouldSetCorrectCategory(NotificationCategory category)
    {
        // Act
        var notification = Notification.Create("user-123", NotificationType.Info, category, "Title", "Message");

        // Assert
        notification.Category.Should().Be(category);
    }

    #endregion

    #region MarkAsRead Tests

    [Fact]
    public void MarkAsRead_WhenUnread_ShouldSetIsReadAndReadAt()
    {
        // Arrange
        var notification = Notification.Create("user-123", NotificationType.Info, NotificationCategory.System, "Title", "Message");
        var beforeMark = DateTimeOffset.UtcNow;

        // Act
        notification.MarkAsRead();

        // Assert
        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().NotBeNull();
        notification.ReadAt.Should().BeOnOrAfter(beforeMark);
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_ShouldNotUpdateReadAt()
    {
        // Arrange
        var notification = Notification.Create("user-123", NotificationType.Info, NotificationCategory.System, "Title", "Message");
        notification.MarkAsRead();
        var firstReadAt = notification.ReadAt;
        Thread.Sleep(10);

        // Act
        notification.MarkAsRead();

        // Assert
        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().Be(firstReadAt);
    }

    #endregion

    #region MarkAsUnread Tests

    [Fact]
    public void MarkAsUnread_ShouldClearIsReadAndReadAt()
    {
        // Arrange
        var notification = Notification.Create("user-123", NotificationType.Info, NotificationCategory.System, "Title", "Message");
        notification.MarkAsRead();

        // Act
        notification.MarkAsUnread();

        // Assert
        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
    }

    [Fact]
    public void MarkAsUnread_WhenAlreadyUnread_ShouldRemainUnread()
    {
        // Arrange
        var notification = Notification.Create("user-123", NotificationType.Info, NotificationCategory.System, "Title", "Message");

        // Act
        notification.MarkAsUnread();

        // Assert
        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
    }

    #endregion

    #region Email Tracking Tests

    [Fact]
    public void MarkEmailSent_ShouldSetEmailSentToTrue()
    {
        // Arrange
        var notification = Notification.Create("user-123", NotificationType.Info, NotificationCategory.System, "Title", "Message");

        // Act
        notification.MarkEmailSent();

        // Assert
        notification.EmailSent.Should().BeTrue();
    }

    [Fact]
    public void MarkIncludedInDigest_ShouldSetIncludedInDigestToTrue()
    {
        // Arrange
        var notification = Notification.Create("user-123", NotificationType.Info, NotificationCategory.System, "Title", "Message");

        // Act
        notification.MarkIncludedInDigest();

        // Assert
        notification.IncludedInDigest.Should().BeTrue();
    }

    #endregion

    #region Action Tests

    [Fact]
    public void AddAction_ShouldAddActionToCollection()
    {
        // Arrange
        var notification = Notification.Create("user-123", NotificationType.Info, NotificationCategory.Workflow, "Approval Required", "Please review");
        var action = NotificationAction.Primary("Approve", "/api/approve");

        // Act
        notification.AddAction(action);

        // Assert
        notification.Actions.Should().HaveCount(1);
        notification.Actions.Should().Contain(action);
    }

    [Fact]
    public void AddAction_ShouldSupportFluentChaining()
    {
        // Arrange
        var notification = Notification.Create("user-123", NotificationType.Info, NotificationCategory.Workflow, "Approval Required", "Please review");

        // Act
        var result = notification
            .AddAction(NotificationAction.Primary("Approve", "/api/approve"))
            .AddAction(NotificationAction.Destructive("Reject", "/api/reject"));

        // Assert
        result.Should().BeSameAs(notification);
        notification.Actions.Should().HaveCount(2);
    }

    [Fact]
    public void AddAction_WithNullAction_ShouldThrowArgumentNullException()
    {
        // Arrange
        var notification = Notification.Create("user-123", NotificationType.Info, NotificationCategory.System, "Title", "Message");

        // Act
        var act = () => notification.AddAction(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddActions_ShouldAddMultipleActions()
    {
        // Arrange
        var notification = Notification.Create("user-123", NotificationType.Info, NotificationCategory.Workflow, "Review", "Review needed");
        var actions = new[]
        {
            NotificationAction.Primary("View", "/view"),
            NotificationAction.Secondary("Dismiss", "/dismiss")
        };

        // Act
        notification.AddActions(actions);

        // Assert
        notification.Actions.Should().HaveCount(2);
    }

    [Fact]
    public void AddActions_WithNullCollection_ShouldThrowArgumentNullException()
    {
        // Arrange
        var notification = Notification.Create("user-123", NotificationType.Info, NotificationCategory.System, "Title", "Message");

        // Act
        var act = () => notification.AddActions(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Actions_ShouldBeReadOnly()
    {
        // Arrange
        var notification = Notification.Create("user-123", NotificationType.Info, NotificationCategory.System, "Title", "Message");
        notification.AddAction(NotificationAction.Primary("Test", "/test"));

        // Assert
        notification.Actions.Should().BeAssignableTo<IReadOnlyCollection<NotificationAction>>();
    }

    #endregion

    #region Tenant Tests

    [Fact]
    public void Create_WithTenantId_ShouldBeAssociatedWithTenant()
    {
        // Arrange
        var tenantId = "tenant-xyz";

        // Act
        var notification = Notification.Create(
            "user-123", NotificationType.Info, NotificationCategory.System,
            "Title", "Message", tenantId: tenantId);

        // Assert
        notification.TenantId.Should().Be(tenantId);
    }

    #endregion
}
