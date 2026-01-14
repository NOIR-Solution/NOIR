namespace NOIR.Application.UnitTests.Features.Users;

/// <summary>
/// Unit tests for UpdateUserCommandHandler.
/// Tests admin user update scenarios with mocked dependencies.
/// </summary>
public class UpdateUserCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly UpdateUserCommandHandler _handler;

    public UpdateUserCommandHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new UpdateUserCommandHandler(
            _userIdentityServiceMock.Object,
            _localizationServiceMock.Object);
    }

    private static UserIdentityDto CreateTestUserDto(
        string id = "user-123",
        string email = "test@example.com",
        string? firstName = "Test",
        string? lastName = "User",
        string? displayName = null,
        bool isActive = true)
    {
        return new UserIdentityDto(
            Id: id,
            Email: email,
            FirstName: firstName,
            LastName: lastName,
            DisplayName: displayName,
            FullName: $"{firstName} {lastName}",
            PhoneNumber: null,
            AvatarUrl: null,
            IsActive: isActive,
            IsDeleted: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: null);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidData_ShouldSucceed()
    {
        // Arrange
        const string userId = "user-123";
        const string newDisplayName = "New Display Name";
        const string newFirstName = "NewFirst";
        const string newLastName = "NewLast";

        _userIdentityServiceMock
            .SetupSequence(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId))
            .ReturnsAsync(CreateTestUserDto(userId, displayName: newDisplayName, firstName: newFirstName, lastName: newLastName));

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "User" });

        var command = new UpdateUserCommand(userId, newDisplayName, newFirstName, newLastName, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(userId);
    }

    [Fact]
    public async Task Handle_WithLockoutEnabled_ShouldInvertToIsActive()
    {
        // Arrange
        const string userId = "user-123";

        _userIdentityServiceMock
            .SetupSequence(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId, isActive: true))
            .ReturnsAsync(CreateTestUserDto(userId, isActive: false));

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(
                userId,
                It.Is<UpdateUserDto>(dto => dto.IsActive == false), // LockoutEnabled=true means IsActive=false
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var command = new UpdateUserCommand(userId, null, null, null, true); // LockoutEnabled = true

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userIdentityServiceMock.Verify(
            x => x.UpdateUserAsync(userId, It.Is<UpdateUserDto>(dto => dto.IsActive == false), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullLockoutEnabled_ShouldNotSetIsActive()
    {
        // Arrange
        const string userId = "user-123";

        _userIdentityServiceMock
            .SetupSequence(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(
                userId,
                It.Is<UpdateUserDto>(dto => dto.IsActive == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var command = new UpdateUserCommand(userId, "Display", null, null, null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.UpdateUserAsync(userId, It.Is<UpdateUserDto>(dto => dto.IsActive == null), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Not Found Scenarios

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        const string userId = "non-existent-user";

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        var command = new UpdateUserCommand(userId, "Display", "First", "Last", null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.UserNotFound);
        _userIdentityServiceMock.Verify(
            x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Update Failure Scenarios

    [Fact]
    public async Task Handle_WhenUpdateFails_ShouldReturnValidationError()
    {
        // Arrange
        const string userId = "user-123";

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Update failed"));

        var command = new UpdateUserCommand(userId, "Display", null, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Validation.General);
    }

    [Fact]
    public async Task Handle_WhenRetrievalAfterUpdateFails_ShouldReturnUnknownError()
    {
        // Arrange
        const string userId = "user-123";

        _userIdentityServiceMock
            .SetupSequence(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId))
            .ReturnsAsync((UserIdentityDto?)null);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = new UpdateUserCommand(userId, "Display", null, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.System.UnknownError);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        const string userId = "user-123";

        _userIdentityServiceMock
            .SetupSequence(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var command = new UpdateUserCommand(userId, "Display", null, null, null);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _userIdentityServiceMock.Verify(x => x.FindByIdAsync(userId, token), Times.AtLeastOnce);
        _userIdentityServiceMock.Verify(x => x.UpdateUserAsync(userId, It.IsAny<UpdateUserDto>(), token), Times.Once);
        _userIdentityServiceMock.Verify(x => x.GetRolesAsync(userId, token), Times.Once);
    }

    #endregion
}
