namespace NOIR.Application.UnitTests.Features.Users;

/// <summary>
/// Unit tests for LockUserCommandHandler.
/// Tests user locking/unlocking scenarios with mocked dependencies.
/// </summary>
public class LockUserCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly LockUserCommandHandler _handler;

    public LockUserCommandHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _currentUserMock = new Mock<ICurrentUser>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new LockUserCommandHandler(
            _userIdentityServiceMock.Object,
            _currentUserMock.Object,
            _localizationServiceMock.Object);
    }

    private static UserIdentityDto CreateTestUserDto(
        string id = "user-123",
        string email = "test@example.com",
        bool isActive = true)
    {
        return new UserIdentityDto(
            Id: id,
            Email: email,
            TenantId: "default",
            FirstName: "Test",
            LastName: "User",
            DisplayName: null,
            FullName: "Test User",
            PhoneNumber: null,
            AvatarUrl: null,
            IsActive: isActive,
            IsDeleted: false,
            IsSystemUser: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: null);
    }

    #endregion

    #region Lock User Success Scenarios

    [Fact]
    public async Task Handle_LockUser_WithValidUser_ShouldSucceed()
    {
        // Arrange
        const string targetUserId = "user-to-lock";
        const string currentUserId = "admin-user";

        _currentUserMock.Setup(x => x.UserId).Returns(currentUserId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(targetUserId, isActive: true));

        _userIdentityServiceMock
            .Setup(x => x.SetUserLockoutAsync(targetUserId, true, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = new LockUserCommand(targetUserId, Lock: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _userIdentityServiceMock.Verify(
            x => x.SetUserLockoutAsync(targetUserId, true, currentUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LockUser_WhenCurrentUserIdIsNull_ShouldUseSystemAsLockedBy()
    {
        // Arrange
        const string targetUserId = "user-to-lock";

        _currentUserMock.Setup(x => x.UserId).Returns((string?)null);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(targetUserId, isActive: true));

        _userIdentityServiceMock
            .Setup(x => x.SetUserLockoutAsync(targetUserId, true, "system", It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = new LockUserCommand(targetUserId, Lock: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userIdentityServiceMock.Verify(
            x => x.SetUserLockoutAsync(targetUserId, true, "system", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Unlock User Success Scenarios

    [Fact]
    public async Task Handle_UnlockUser_WithValidUser_ShouldSucceed()
    {
        // Arrange
        const string targetUserId = "user-to-unlock";
        const string currentUserId = "admin-user";

        _currentUserMock.Setup(x => x.UserId).Returns(currentUserId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(targetUserId, isActive: false));

        _userIdentityServiceMock
            .Setup(x => x.SetUserLockoutAsync(targetUserId, false, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = new LockUserCommand(targetUserId, Lock: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _userIdentityServiceMock.Verify(
            x => x.SetUserLockoutAsync(targetUserId, false, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UnlockUser_ShouldPassNullAsLockedBy()
    {
        // Arrange
        const string targetUserId = "user-to-unlock";
        const string currentUserId = "admin-user";

        _currentUserMock.Setup(x => x.UserId).Returns(currentUserId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(targetUserId));

        _userIdentityServiceMock
            .Setup(x => x.SetUserLockoutAsync(targetUserId, false, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = new LockUserCommand(targetUserId, Lock: false);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - When unlocking, lockedBy should be null
        _userIdentityServiceMock.Verify(
            x => x.SetUserLockoutAsync(targetUserId, false, null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Not Found Scenarios

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        const string targetUserId = "non-existent-user";
        const string currentUserId = "admin-user";

        _currentUserMock.Setup(x => x.UserId).Returns(currentUserId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        var command = new LockUserCommand(targetUserId, Lock: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.UserNotFound);
        _userIdentityServiceMock.Verify(
            x => x.SetUserLockoutAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Self-Locking Prevention Scenarios

    [Fact]
    public async Task Handle_WhenLockingSelf_ShouldReturnCannotModify()
    {
        // Arrange
        const string userId = "user-123";

        _currentUserMock.Setup(x => x.UserId).Returns(userId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        var command = new LockUserCommand(userId, Lock: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Business.CannotModify);
        _userIdentityServiceMock.Verify(
            x => x.SetUserLockoutAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUnlockingSelf_ShouldReturnCannotModify()
    {
        // Arrange
        const string userId = "user-123";

        _currentUserMock.Setup(x => x.UserId).Returns(userId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId, isActive: false));

        var command = new LockUserCommand(userId, Lock: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Business.CannotModify);
    }

    #endregion

    #region Lockout Failure Scenarios

    [Fact]
    public async Task Handle_WhenSetLockoutFails_ShouldReturnValidationError()
    {
        // Arrange
        const string targetUserId = "user-to-lock";
        const string currentUserId = "admin-user";

        _currentUserMock.Setup(x => x.UserId).Returns(currentUserId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(targetUserId));

        _userIdentityServiceMock
            .Setup(x => x.SetUserLockoutAsync(targetUserId, true, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Lockout failed"));

        var command = new LockUserCommand(targetUserId, Lock: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Validation.General);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        const string targetUserId = "user-to-lock";
        const string currentUserId = "admin-user";

        _currentUserMock.Setup(x => x.UserId).Returns(currentUserId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(targetUserId));

        _userIdentityServiceMock
            .Setup(x => x.SetUserLockoutAsync(targetUserId, true, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = new LockUserCommand(targetUserId, Lock: true);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _userIdentityServiceMock.Verify(x => x.FindByIdAsync(targetUserId, token), Times.Once);
        _userIdentityServiceMock.Verify(x => x.SetUserLockoutAsync(targetUserId, true, currentUserId, token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithUserEmail_ShouldNotAffectLockingLogic()
    {
        // Arrange
        const string targetUserId = "user-to-lock";
        const string userEmail = "user@example.com";
        const string currentUserId = "admin-user";

        _currentUserMock.Setup(x => x.UserId).Returns(currentUserId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(targetUserId, userEmail));

        _userIdentityServiceMock
            .Setup(x => x.SetUserLockoutAsync(targetUserId, true, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        // UserEmail is optional metadata for audit, doesn't affect the locking logic
        var command = new LockUserCommand(targetUserId, Lock: true, UserEmail: userEmail);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion
}
