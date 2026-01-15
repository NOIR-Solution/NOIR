namespace NOIR.Application.UnitTests.Features.Users;

/// <summary>
/// Unit tests for AssignRolesToUserCommandHandler.
/// Tests role assignment scenarios with mocked dependencies.
/// </summary>
public class AssignRolesToUserCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<IRoleIdentityService> _roleIdentityServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly Mock<IPermissionCacheInvalidator> _cacheInvalidatorMock;
    private readonly AssignRolesToUserCommandHandler _handler;

    public AssignRolesToUserCommandHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _roleIdentityServiceMock = new Mock<IRoleIdentityService>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _cacheInvalidatorMock = new Mock<IPermissionCacheInvalidator>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new AssignRolesToUserCommandHandler(
            _userIdentityServiceMock.Object,
            _roleIdentityServiceMock.Object,
            _localizationServiceMock.Object,
            _cacheInvalidatorMock.Object);
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
    public async Task Handle_WithValidUserAndRoles_ShouldSucceed()
    {
        // Arrange
        const string userId = "user-123";
        var roleNames = new List<string> { "Admin", "User" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _roleIdentityServiceMock
            .Setup(x => x.RoleExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userIdentityServiceMock
            .Setup(x => x.AssignRolesAsync(userId, roleNames, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roleNames);

        var command = new AssignRolesToUserCommand(userId, roleNames);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Roles.Should().BeEquivalentTo(roleNames);
    }

    [Fact]
    public async Task Handle_WithSingleRole_ShouldSucceed()
    {
        // Arrange
        const string userId = "user-123";
        var roleNames = new List<string> { "User" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _roleIdentityServiceMock
            .Setup(x => x.RoleExistsAsync("User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userIdentityServiceMock
            .Setup(x => x.AssignRolesAsync(userId, roleNames, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roleNames);

        var command = new AssignRolesToUserCommand(userId, roleNames);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userIdentityServiceMock.Verify(
            x => x.AssignRolesAsync(userId, roleNames, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReplaceExistingRoles()
    {
        // Arrange
        const string userId = "user-123";
        var newRoles = new List<string> { "Admin" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _roleIdentityServiceMock
            .Setup(x => x.RoleExistsAsync("Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userIdentityServiceMock
            .Setup(x => x.AssignRolesAsync(userId, newRoles, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(newRoles);

        var command = new AssignRolesToUserCommand(userId, newRoles);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert - Should call with replaceExisting = true
        _userIdentityServiceMock.Verify(
            x => x.AssignRolesAsync(userId, newRoles, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Not Found Scenarios

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        const string userId = "non-existent-user";
        var roleNames = new List<string> { "User" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        var command = new AssignRolesToUserCommand(userId, roleNames);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.UserNotFound);
        _userIdentityServiceMock.Verify(
            x => x.AssignRolesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRoleNotFound_ShouldReturnNotFound()
    {
        // Arrange
        const string userId = "user-123";
        var roleNames = new List<string> { "NonExistentRole" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _roleIdentityServiceMock
            .Setup(x => x.RoleExistsAsync("NonExistentRole", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new AssignRolesToUserCommand(userId, roleNames);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.RoleNotFound);
        _userIdentityServiceMock.Verify(
            x => x.AssignRolesAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOneOfMultipleRolesNotFound_ShouldReturnNotFound()
    {
        // Arrange
        const string userId = "user-123";
        var roleNames = new List<string> { "Admin", "NonExistentRole" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _roleIdentityServiceMock
            .Setup(x => x.RoleExistsAsync("Admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _roleIdentityServiceMock
            .Setup(x => x.RoleExistsAsync("NonExistentRole", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new AssignRolesToUserCommand(userId, roleNames);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.RoleNotFound);
    }

    #endregion

    #region Assignment Failure Scenarios

    [Fact]
    public async Task Handle_WhenAssignmentFails_ShouldReturnValidationError()
    {
        // Arrange
        const string userId = "user-123";
        var roleNames = new List<string> { "User" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _roleIdentityServiceMock
            .Setup(x => x.RoleExistsAsync("User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userIdentityServiceMock
            .Setup(x => x.AssignRolesAsync(userId, roleNames, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Assignment failed"));

        var command = new AssignRolesToUserCommand(userId, roleNames);

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
        const string userId = "user-123";
        var roleNames = new List<string> { "User" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _roleIdentityServiceMock
            .Setup(x => x.RoleExistsAsync("User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userIdentityServiceMock
            .Setup(x => x.AssignRolesAsync(userId, roleNames, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roleNames);

        var command = new AssignRolesToUserCommand(userId, roleNames);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _userIdentityServiceMock.Verify(x => x.FindByIdAsync(userId, token), Times.Once);
        _roleIdentityServiceMock.Verify(x => x.RoleExistsAsync("User", token), Times.Once);
        _userIdentityServiceMock.Verify(x => x.AssignRolesAsync(userId, roleNames, true, token), Times.Once);
        _userIdentityServiceMock.Verify(x => x.GetRolesAsync(userId, token), Times.Once);
    }

    #endregion
}
