using NOIR.Application.Features.Cart.EventHandlers;
using NOIR.Domain.Events.Cart;

namespace NOIR.Application.UnitTests.Features.Cart.EventHandlers;

/// <summary>
/// Unit tests for CartRecoveryHandler.
/// Verifies abandoned cart recovery emails are sent for authenticated users and skipped for guests.
/// </summary>
public class CartRecoveryHandlerTests
{
    private readonly Mock<IEmailService> _emailService = new();
    private readonly Mock<IUserIdentityService> _userIdentityService = new();
    private readonly Mock<ILogger<CartRecoveryHandler>> _logger = new();
    private readonly CartRecoveryHandler _sut;

    public CartRecoveryHandlerTests()
    {
        _emailService
            .Setup(x => x.SendTemplateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _sut = new CartRecoveryHandler(
            _emailService.Object,
            _userIdentityService.Object,
            _logger.Object);
    }

    private static UserIdentityDto CreateUserDto(string userId, string email)
        => new(
            Id: userId,
            Email: email,
            TenantId: "tenant-default",
            FirstName: "Test",
            LastName: "User",
            DisplayName: null,
            FullName: "Test User",
            PhoneNumber: null,
            AvatarUrl: null,
            IsActive: true,
            IsDeleted: false,
            IsSystemUser: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: null);

    #region Authenticated user — should send recovery email

    [Fact]
    public async Task Handle_CartAbandoned_WhenUserIdIsSet_ShouldSendRecoveryEmail()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var userId = "user-recovery";
        var evt = new CartAbandonedEvent(cartId, userId, null, 3, 450_000m);

        var user = CreateUserDto(userId, "recover@example.com");
        _userIdentityService.Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _emailService.Verify(x => x.SendTemplateAsync(
            "recover@example.com",
            "You left something behind!",
            "cart_abandoned_recovery",
            It.IsAny<object>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CartAbandoned_WhenUserIdIsSet_ShouldLookupUserById()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var userId = "user-lookup";
        var evt = new CartAbandonedEvent(cartId, userId, null, 1, 100_000m);

        var user = CreateUserDto(userId, "lookup@example.com");
        _userIdentityService.Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _userIdentityService.Verify(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_CartAbandoned_WhenUserNotFound_ShouldLogWarningAndSkipEmail()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var userId = "user-ghost";
        var evt = new CartAbandonedEvent(cartId, userId, null, 2, 200_000m);

        _userIdentityService.Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert
        _emailService.Verify(x => x.SendTemplateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_CartAbandoned_WhenEmailThrows_ShouldLogWarningAndNotRethrow()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var userId = "user-email-fail";
        var evt = new CartAbandonedEvent(cartId, userId, null, 4, 800_000m);

        var user = CreateUserDto(userId, "emailfail@example.com");
        _userIdentityService.Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _emailService
            .Setup(x => x.SendTemplateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        // Act & Assert
        await _sut.Handle(evt, CancellationToken.None);
        _logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Guest cart — should skip

    [Fact]
    public async Task Handle_CartAbandoned_WhenUserIdIsNull_ShouldSkipRecoveryEmail()
    {
        // Arrange — guest cart has UserId = null, only SessionId is set
        var evt = new CartAbandonedEvent(Guid.NewGuid(), null, "session-abc-123", 2, 350_000m);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert — must not lookup user or send email
        _userIdentityService.Verify(
            x => x.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _emailService.Verify(x => x.SendTemplateAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<object>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_CartAbandoned_WhenUserIdIsNull_ShouldLogDebugAndReturn()
    {
        // Arrange
        var evt = new CartAbandonedEvent(Guid.NewGuid(), null, "sess-xyz", 1, 50_000m);

        // Act
        await _sut.Handle(evt, CancellationToken.None);

        // Assert — debug message for guest skip (not a warning — this is expected)
        _logger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion
}
