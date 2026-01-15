namespace NOIR.Application.UnitTests.Features.Users;

/// <summary>
/// Unit tests for DeleteUserCommandHandler.
/// Tests user soft-deletion scenarios with mocked dependencies.
/// </summary>
public class DeleteUserCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly DeleteUserCommandHandler _handler;

    public DeleteUserCommandHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _currentUserMock = new Mock<ICurrentUser>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new DeleteUserCommandHandler(
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

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidUser_ShouldSucceed()
    {
        // Arrange
        const string targetUserId = "user-to-delete";
        const string currentUserId = "admin-user";

        _currentUserMock.Setup(x => x.UserId).Returns(currentUserId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(targetUserId));

        _userIdentityServiceMock
            .Setup(x => x.SoftDeleteUserAsync(targetUserId, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = new DeleteUserCommand(targetUserId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _userIdentityServiceMock.Verify(
            x => x.SoftDeleteUserAsync(targetUserId, currentUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCurrentUserIsNull_ShouldUseSystemAsDeletedBy()
    {
        // Arrange
        const string targetUserId = "user-to-delete";

        _currentUserMock.Setup(x => x.UserId).Returns((string?)null);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(targetUserId));

        _userIdentityServiceMock
            .Setup(x => x.SoftDeleteUserAsync(targetUserId, "system", It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = new DeleteUserCommand(targetUserId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userIdentityServiceMock.Verify(
            x => x.SoftDeleteUserAsync(targetUserId, "system", It.IsAny<CancellationToken>()),
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

        var command = new DeleteUserCommand(targetUserId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.UserNotFound);
        _userIdentityServiceMock.Verify(
            x => x.SoftDeleteUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Self-Deletion Prevention Scenarios

    [Fact]
    public async Task Handle_WhenDeletingSelf_ShouldReturnCannotDelete()
    {
        // Arrange
        const string userId = "user-123";

        _currentUserMock.Setup(x => x.UserId).Returns(userId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        var command = new DeleteUserCommand(userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Business.CannotDelete);
        _userIdentityServiceMock.Verify(
            x => x.SoftDeleteUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Deletion Failure Scenarios

    [Fact]
    public async Task Handle_WhenSoftDeleteFails_ShouldReturnValidationError()
    {
        // Arrange
        const string targetUserId = "user-to-delete";
        const string currentUserId = "admin-user";

        _currentUserMock.Setup(x => x.UserId).Returns(currentUserId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(targetUserId));

        _userIdentityServiceMock
            .Setup(x => x.SoftDeleteUserAsync(targetUserId, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Deletion failed"));

        var command = new DeleteUserCommand(targetUserId);

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
        const string targetUserId = "user-to-delete";
        const string currentUserId = "admin-user";

        _currentUserMock.Setup(x => x.UserId).Returns(currentUserId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(targetUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(targetUserId));

        _userIdentityServiceMock
            .Setup(x => x.SoftDeleteUserAsync(targetUserId, currentUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = new DeleteUserCommand(targetUserId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _userIdentityServiceMock.Verify(x => x.FindByIdAsync(targetUserId, token), Times.Once);
        _userIdentityServiceMock.Verify(x => x.SoftDeleteUserAsync(targetUserId, currentUserId, token), Times.Once);
    }

    #endregion
}
