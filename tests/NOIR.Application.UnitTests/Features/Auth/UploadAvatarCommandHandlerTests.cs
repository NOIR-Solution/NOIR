namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for UploadAvatarCommandHandler.
/// Tests avatar upload scenarios with mocked dependencies.
/// </summary>
public class UploadAvatarCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<IFileStorage> _fileStorageMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly UploadAvatarCommandHandler _handler;

    public UploadAvatarCommandHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _fileStorageMock = new Mock<IFileStorage>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new UploadAvatarCommandHandler(
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
            TenantId: null,
            IsActive: true,
            IsDeleted: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: null);
    }

    private static UploadAvatarCommand CreateTestCommand(
        string userId = "user-123",
        string fileName = "avatar.jpg",
        string contentType = "image/jpeg",
        long fileSize = 1024)
    {
        var stream = new MemoryStream([1, 2, 3, 4]);
        return new UploadAvatarCommand(fileName, stream, contentType, fileSize)
        {
            UserId = userId
        };
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidJpgFile_ShouldSucceed()
    {
        // Arrange
        const string userId = "user-123";
        const string storagePath = "avatars/user-123/abc123.jpg";
        const string publicUrl = "/api/files/avatars/user-123/abc123.jpg";

        var user = CreateTestUserDto(userId);
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _fileStorageMock
            .Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storagePath);

        _fileStorageMock
            .Setup(x => x.GetPublicUrl(storagePath))
            .Returns(publicUrl);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = CreateTestCommand(userId, "avatar.jpg");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AvatarUrl.Should().Be(publicUrl);
    }

    [Theory]
    [InlineData("avatar.jpg")]
    [InlineData("avatar.jpeg")]
    [InlineData("avatar.png")]
    [InlineData("avatar.gif")]
    [InlineData("avatar.webp")]
    public async Task Handle_WithAllowedExtensions_ShouldSucceed(string fileName)
    {
        // Arrange
        const string userId = "user-123";
        var user = CreateTestUserDto(userId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _fileStorageMock
            .Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("path");

        _fileStorageMock
            .Setup(x => x.GetPublicUrl(It.IsAny<string>()))
            .Returns("/api/files/path");

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = CreateTestCommand(userId, fileName);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithUppercaseExtension_ShouldSucceed()
    {
        // Arrange
        const string userId = "user-123";
        var user = CreateTestUserDto(userId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _fileStorageMock
            .Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("path");

        _fileStorageMock
            .Setup(x => x.GetPublicUrl(It.IsAny<string>()))
            .Returns("/api/files/path");

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = CreateTestCommand(userId, "avatar.JPG");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Old Avatar Deletion

    [Fact]
    public async Task Handle_WhenUserHasExistingAvatar_ShouldDeleteOldAvatar()
    {
        // Arrange
        const string userId = "user-123";
        const string oldAvatarUrl = "/api/files/avatars/user-123/old-avatar.jpg";

        var user = CreateTestUserDto(userId, oldAvatarUrl);
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _fileStorageMock
            .Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-path");

        _fileStorageMock
            .Setup(x => x.GetPublicUrl(It.IsAny<string>()))
            .Returns("/api/files/new-path");

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = CreateTestCommand(userId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Old avatar should be deleted
        _fileStorageMock.Verify(
            x => x.DeleteAsync("avatars/user-123/old-avatar.jpg", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoAvatar_ShouldNotCallDelete()
    {
        // Arrange
        const string userId = "user-123";

        var user = CreateTestUserDto(userId, null); // No existing avatar
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _fileStorageMock
            .Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("path");

        _fileStorageMock
            .Setup(x => x.GetPublicUrl(It.IsAny<string>()))
            .Returns("/api/files/path");

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = CreateTestCommand(userId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Delete should not be called
        _fileStorageMock.Verify(
            x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
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
        var stream = new MemoryStream([1, 2, 3, 4]);
        var command = new UploadAvatarCommand("avatar.jpg", stream, "image/jpeg", 1024)
        {
            UserId = null
        };

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

    #region Validation Failure Scenarios

    [Theory]
    [InlineData("avatar.pdf")]
    [InlineData("avatar.doc")]
    [InlineData("avatar.exe")]
    [InlineData("avatar.txt")]
    [InlineData("avatar.svg")]
    [InlineData("avatar")]
    public async Task Handle_WithInvalidFileExtension_ShouldReturnValidationError(string fileName)
    {
        // Arrange
        const string userId = "user-123";
        var user = CreateTestUserDto(userId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var command = CreateTestCommand(userId, fileName);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Validation.InvalidInput);
    }

    #endregion

    #region Update Failure Scenarios

    [Fact]
    public async Task Handle_WhenUpdateFails_ShouldRollbackUploadedFile()
    {
        // Arrange
        const string userId = "user-123";
        const string storagePath = "avatars/user-123/abc123.jpg";

        var user = CreateTestUserDto(userId);
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _fileStorageMock
            .Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storagePath);

        _fileStorageMock
            .Setup(x => x.GetPublicUrl(storagePath))
            .Returns("/api/files/" + storagePath);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Update failed"));

        var command = CreateTestCommand(userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.UpdateFailed);

        // Verify rollback - the uploaded file should be deleted
        _fileStorageMock.Verify(
            x => x.DeleteAsync(storagePath, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region File Upload Scenarios

    [Fact]
    public async Task Handle_ShouldUploadToCorrectFolder()
    {
        // Arrange
        const string userId = "user-123";
        var user = CreateTestUserDto(userId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _fileStorageMock
            .Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("path");

        _fileStorageMock
            .Setup(x => x.GetPublicUrl(It.IsAny<string>()))
            .Returns("/api/files/path");

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = CreateTestCommand(userId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Should upload to avatars/{userId} folder
        _fileStorageMock.Verify(
            x => x.UploadAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                $"avatars/{userId}",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldGenerateUniqueFileName()
    {
        // Arrange
        const string userId = "user-123";
        var user = CreateTestUserDto(userId);

        string? capturedFileName = null;
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _fileStorageMock
            .Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, Stream, string?, CancellationToken>((fileName, _, _, _) => capturedFileName = fileName)
            .ReturnsAsync("path");

        _fileStorageMock
            .Setup(x => x.GetPublicUrl(It.IsAny<string>()))
            .Returns("/api/files/path");

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = CreateTestCommand(userId, "my-avatar.jpg");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Should not use original filename, should be a GUID
        capturedFileName.Should().NotBeNull();
        capturedFileName.Should().NotBe("my-avatar.jpg");
        capturedFileName.Should().EndWith(".jpg");
        // Should be a 32-char GUID without dashes + extension
        capturedFileName!.Replace(".jpg", "").Length.Should().Be(32);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        const string userId = "user-123";
        var user = CreateTestUserDto(userId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _fileStorageMock
            .Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("path");

        _fileStorageMock
            .Setup(x => x.GetPublicUrl(It.IsAny<string>()))
            .Returns("/api/files/path");

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
        _fileStorageMock.Verify(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), token), Times.Once);
        _userIdentityServiceMock.Verify(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), token), Times.Once);
    }

    #endregion
}
