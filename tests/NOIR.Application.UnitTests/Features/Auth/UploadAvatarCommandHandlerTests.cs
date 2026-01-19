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
    private readonly Mock<IImageProcessor> _imageProcessorMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly Mock<ILogger<UploadAvatarCommandHandler>> _loggerMock;
    private readonly UploadAvatarCommandHandler _handler;

    public UploadAvatarCommandHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _fileStorageMock = new Mock<IFileStorage>();
        _imageProcessorMock = new Mock<IImageProcessor>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _loggerMock = new Mock<ILogger<UploadAvatarCommandHandler>>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new UploadAvatarCommandHandler(
            _userIdentityServiceMock.Object,
            _fileStorageMock.Object,
            _imageProcessorMock.Object,
            _localizationServiceMock.Object,
            _loggerMock.Object);
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

    private ImageProcessingResult CreateSuccessfulProcessingResult(string slug = "avatar")
    {
        return new ImageProcessingResult
        {
            Success = true,
            Slug = slug,
            Variants =
            [
                new ImageVariantInfo
                {
                    Variant = ImageVariant.Thumb,
                    Format = OutputFormat.WebP,
                    Path = $"avatars/user-123/{slug}-thumb.webp",
                    Url = $"/media/avatars/user-123/{slug}-thumb.webp",
                    Width = 150,
                    Height = 150,
                    SizeBytes = 5000
                },
                new ImageVariantInfo
                {
                    Variant = ImageVariant.Medium,
                    Format = OutputFormat.WebP,
                    Path = $"avatars/user-123/{slug}-medium.webp",
                    Url = $"/media/avatars/user-123/{slug}-medium.webp",
                    Width = 640,
                    Height = 640,
                    SizeBytes = 50000
                }
            ],
            ProcessingTimeMs = 100
        };
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidJpgFile_ShouldSucceed()
    {
        // Arrange
        const string userId = "user-123";
        var user = CreateTestUserDto(userId);
        var processingResult = CreateSuccessfulProcessingResult();

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<ImageProcessingOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(processingResult);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = CreateTestCommand(userId, "avatar.jpg");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AvatarUrl.Should().Contain("medium.webp");
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
        var processingResult = CreateSuccessfulProcessingResult();

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<ImageProcessingOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(processingResult);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = CreateTestCommand(userId, fileName);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Old Avatar Deletion

    [Fact]
    public async Task Handle_WhenUserHasExistingAvatar_ShouldDeleteOldAvatarFiles()
    {
        // Arrange
        const string userId = "user-123";
        const string oldAvatarUrl = "/media/avatars/user-123/old-avatar-medium.webp";

        var user = CreateTestUserDto(userId, oldAvatarUrl);
        var processingResult = CreateSuccessfulProcessingResult();

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<ImageProcessingOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(processingResult);

        _fileStorageMock
            .Setup(x => x.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = CreateTestCommand(userId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Old avatar variants should be deleted
        _fileStorageMock.Verify(
            x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoAvatar_ShouldNotCallDelete()
    {
        // Arrange
        const string userId = "user-123";

        var user = CreateTestUserDto(userId, null); // No existing avatar
        var processingResult = CreateSuccessfulProcessingResult();

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<ImageProcessingOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(processingResult);

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
    public async Task Handle_WithInvalidFileFormat_ShouldReturnValidationError(string fileName)
    {
        // Arrange
        const string userId = "user-123";
        var user = CreateTestUserDto(userId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(false);

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
    public async Task Handle_WhenUpdateFails_ShouldRollbackUploadedFiles()
    {
        // Arrange
        const string userId = "user-123";
        var user = CreateTestUserDto(userId);
        var processingResult = CreateSuccessfulProcessingResult();

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<ImageProcessingOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(processingResult);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Update failed"));

        var command = CreateTestCommand(userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.UpdateFailed);

        // Verify rollback - all variant files should be deleted
        _fileStorageMock.Verify(
            x => x.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(processingResult.Variants.Count));
    }

    #endregion

    #region Image Processing Scenarios

    [Fact]
    public async Task Handle_ShouldProcessWithCorrectOptions()
    {
        // Arrange
        const string userId = "user-123";
        var user = CreateTestUserDto(userId);
        var processingResult = CreateSuccessfulProcessingResult();

        ImageProcessingOptions? capturedOptions = null;

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<ImageProcessingOptions>(), It.IsAny<CancellationToken>()))
            .Callback<Stream, string, ImageProcessingOptions, CancellationToken>((_, _, opts, _) => capturedOptions = opts)
            .ReturnsAsync(processingResult);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = CreateTestCommand(userId);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.Variants.Should().Contain(ImageVariant.Thumb);
        capturedOptions.Variants.Should().Contain(ImageVariant.Medium);
        capturedOptions.GenerateThumbHash.Should().BeFalse(); // Avatars don't need ThumbHash
        capturedOptions.ExtractDominantColor.Should().BeFalse();
        capturedOptions.StorageFolder.Should().Contain($"avatars/{userId}");
    }

    [Fact]
    public async Task Handle_WhenProcessingFails_ShouldReturnFailure()
    {
        // Arrange
        const string userId = "user-123";
        var user = CreateTestUserDto(userId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<ImageProcessingOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImageProcessingResult.Failure("Processing failed"));

        var command = CreateTestCommand(userId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.UpdateFailed);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        const string userId = "user-123";
        var user = CreateTestUserDto(userId);
        var processingResult = CreateSuccessfulProcessingResult();

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _imageProcessorMock
            .Setup(x => x.IsValidImageAsync(It.IsAny<Stream>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _imageProcessorMock
            .Setup(x => x.ProcessAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<ImageProcessingOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(processingResult);

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
        _imageProcessorMock.Verify(x => x.ProcessAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<ImageProcessingOptions>(), token), Times.Once);
        _userIdentityServiceMock.Verify(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), token), Times.Once);
    }

    #endregion
}
