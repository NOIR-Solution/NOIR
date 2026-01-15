namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for DeleteAvatarCommandHandler.
/// Tests avatar deletion scenarios with mocked dependencies.
/// </summary>
public class DeleteAvatarCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<IFileStorage> _fileStorageMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly DeleteAvatarCommandHandler _handler;

    public DeleteAvatarCommandHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _fileStorageMock = new Mock<IFileStorage>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new DeleteAvatarCommandHandler(
            _userIdentityServiceMock.Object,
            _fileStorageMock.Object,
            _localizationServiceMock.Object);
    }

    private UserIdentityDto CreateTestUserDto(
        string id = "user-123",
        string? avatarUrl = null)
    {
        return new UserIdentityDto(
            Id: id,
            Email: "test@example.com",
            FirstName: "Test",
            LastName: "User",
            DisplayName: null,
            FullName: "Test User",
            PhoneNumber: null,
            AvatarUrl: avatarUrl,
            IsActive: true,
            IsDeleted: false,
            IsSystemUser: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: null);
    }

    private static DeleteAvatarCommand CreateTestCommand(string? userId = "user-123")
    {
        return new DeleteAvatarCommand { UserId = userId };
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithExistingAvatar_ShouldSucceed()
    {
        // Arrange
        const string userId = "user-123";
        const string avatarUrl = "/api/files/avatars/user-123/avatar.jpg";

        var user = CreateTestUserDto(userId, avatarUrl);
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _fileStorageMock
            .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = CreateTestCommand(userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithExistingAvatar_ShouldDeleteFileFromStorage()
    {
        // Arrange
        const string userId = "user-123";
        const string avatarUrl = "/api/files/avatars/user-123/avatar.jpg";
        const string expectedStoragePath = "avatars/user-123/avatar.jpg";

        var user = CreateTestUserDto(userId, avatarUrl);
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _fileStorageMock
            .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = CreateTestCommand(userId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Should strip /api/files/ prefix and delete
        _fileStorageMock.Verify(
            x => x.DeleteAsync(expectedStoragePath, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingAvatar_ShouldClearUserAvatarUrl()
    {
        // Arrange
        const string userId = "user-123";
        const string avatarUrl = "/api/files/avatars/user-123/avatar.jpg";

        var user = CreateTestUserDto(userId, avatarUrl);
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _fileStorageMock
            .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = CreateTestCommand(userId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - AvatarUrl should be set to empty string
        _userIdentityServiceMock.Verify(
            x => x.UpdateUserAsync(
                userId,
                It.Is<UpdateUserDto>(dto => dto.AvatarUrl == string.Empty),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region No Avatar Scenarios

    [Fact]
    public async Task Handle_WhenUserHasNoAvatar_ShouldSucceedWithMessage()
    {
        // Arrange
        const string userId = "user-123";

        var user = CreateTestUserDto(userId, null); // No avatar
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = CreateTestCommand(userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.Message.Should().Be("profile.avatar.noAvatar");
    }

    [Fact]
    public async Task Handle_WhenUserHasNoAvatar_ShouldNotCallDeleteOrUpdate()
    {
        // Arrange
        const string userId = "user-123";

        var user = CreateTestUserDto(userId, null); // No avatar
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = CreateTestCommand(userId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Should not call delete or update
        _fileStorageMock.Verify(
            x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _userIdentityServiceMock.Verify(
            x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserHasEmptyAvatarUrl_ShouldSucceedWithMessage()
    {
        // Arrange
        const string userId = "user-123";

        var user = CreateTestUserDto(userId, string.Empty); // Empty avatar URL
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = CreateTestCommand(userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
    }

    #endregion

    #region Authentication Failure Scenarios

    [Fact]
    public async Task Handle_WhenUserIdIsEmpty_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = CreateTestCommand(string.Empty);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenUserIdIsNull_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = CreateTestCommand(null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        const string userId = "non-existent-user";

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        var command = CreateTestCommand(userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.UserNotFound);
    }

    #endregion

    #region Update Failure Scenarios

    [Fact]
    public async Task Handle_WhenUpdateFails_ShouldReturnFailure()
    {
        // Arrange
        const string userId = "user-123";
        const string avatarUrl = "/api/files/avatars/user-123/avatar.jpg";

        var user = CreateTestUserDto(userId, avatarUrl);
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _fileStorageMock
            .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Update failed"));

        var command = CreateTestCommand(userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.UpdateFailed);
    }

    #endregion

    #region URL Path Processing

    [Fact]
    public async Task Handle_WithUrlContainingApiFilesPrefix_ShouldStripPrefix()
    {
        // Arrange
        const string userId = "user-123";
        const string avatarUrl = "/api/files/avatars/user-123/avatar.jpg";

        var user = CreateTestUserDto(userId, avatarUrl);
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _fileStorageMock
            .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = CreateTestCommand(userId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Should delete without /api/files/ prefix
        _fileStorageMock.Verify(
            x => x.DeleteAsync("avatars/user-123/avatar.jpg", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithUrlWithoutApiFilesPrefix_ShouldUseAsIs()
    {
        // Arrange
        const string userId = "user-123";
        const string avatarUrl = "avatars/user-123/avatar.jpg"; // No prefix

        var user = CreateTestUserDto(userId, avatarUrl);
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _fileStorageMock
            .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = CreateTestCommand(userId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Should use path as-is
        _fileStorageMock.Verify(
            x => x.DeleteAsync("avatars/user-123/avatar.jpg", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        const string userId = "user-123";
        const string avatarUrl = "/api/files/avatars/user-123/avatar.jpg";

        var user = CreateTestUserDto(userId, avatarUrl);
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _fileStorageMock
            .Setup(x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = CreateTestCommand(userId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _userIdentityServiceMock.Verify(x => x.FindByIdAsync(userId, token), Times.Once);
        _fileStorageMock.Verify(x => x.DeleteAsync(It.IsAny<string>(), token), Times.Once);
        _userIdentityServiceMock.Verify(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), token), Times.Once);
    }

    #endregion
}
