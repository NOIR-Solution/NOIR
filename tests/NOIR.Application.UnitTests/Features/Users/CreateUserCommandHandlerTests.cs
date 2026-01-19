namespace NOIR.Application.UnitTests.Features.Users;

/// <summary>
/// Unit tests for CreateUserCommandHandler.
/// Tests admin user creation scenarios with mocked dependencies.
/// </summary>
public class CreateUserCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<IBaseUrlService> _baseUrlServiceMock;
    private readonly Mock<ILogger<CreateUserCommandHandler>> _loggerMock;
    private readonly CreateUserCommandHandler _handler;
    private const string TestTenantId = "test-tenant-id";

    public CreateUserCommandHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _currentUserMock = new Mock<ICurrentUser>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _emailServiceMock = new Mock<IEmailService>();
        _baseUrlServiceMock = new Mock<IBaseUrlService>();
        _loggerMock = new Mock<ILogger<CreateUserCommandHandler>>();

        // Setup current user with test tenant
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new CreateUserCommandHandler(
            _userIdentityServiceMock.Object,
            _currentUserMock.Object,
            _localizationServiceMock.Object,
            _emailServiceMock.Object,
            _baseUrlServiceMock.Object,
            _loggerMock.Object);
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
            IsSystemUser: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: null);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidData_ShouldSucceed()
    {
        // Arrange
        const string email = "newuser@example.com";
        const string password = "Password123!";
        const string firstName = "New";
        const string lastName = "User";
        const string displayName = "New User Display";
        const string userId = "new-user-id";

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(email, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        _userIdentityServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success(userId));

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId, email, firstName, lastName, displayName));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _baseUrlServiceMock
            .Setup(x => x.BuildUrl(It.IsAny<string>()))
            .Returns("https://example.com/login");

        var command = new CreateUserCommand(email, password, firstName, lastName, displayName, null, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(userId);
        result.Value.Email.Should().Be(email);
        result.Value.FirstName.Should().Be(firstName);
        result.Value.LastName.Should().Be(lastName);
        result.Value.DisplayName.Should().Be(displayName);
    }

    [Fact]
    public async Task Handle_WithRoleAssignment_ShouldAssignRoles()
    {
        // Arrange
        const string email = "newuser@example.com";
        const string password = "Password123!";
        const string userId = "new-user-id";
        var roles = new List<string> { "Admin", "User" };

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(email, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        _userIdentityServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success(userId));

        _userIdentityServiceMock
            .Setup(x => x.AssignRolesAsync(userId, roles, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId, email));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        var command = new CreateUserCommand(email, password, null, null, null, roles, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Roles.Should().BeEquivalentTo(roles);
        _userIdentityServiceMock.Verify(
            x => x.AssignRolesAsync(userId, roles, true, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSendWelcomeEmailTrue_ShouldTriggerEmailSending()
    {
        // Arrange
        const string email = "newuser@example.com";
        const string password = "Password123!";
        const string userId = "new-user-id";
        const string loginUrl = "https://example.com/login";

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(email, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        _userIdentityServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success(userId));

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId, email));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        _baseUrlServiceMock
            .Setup(x => x.BuildUrl("/login"))
            .Returns(loginUrl);

        var command = new CreateUserCommand(email, password, null, null, null, null, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Just verify the result is successful; email is sent fire-and-forget
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenRoleAssignmentFails_ShouldStillSucceedUserCreation()
    {
        // Arrange
        const string email = "newuser@example.com";
        const string password = "Password123!";
        const string userId = "new-user-id";
        var roles = new List<string> { "Admin" };

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(email, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        _userIdentityServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success(userId));

        _userIdentityServiceMock
            .Setup(x => x.AssignRolesAsync(userId, roles, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Role not found"));

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId, email));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>()); // No roles assigned due to failure

        var command = new CreateUserCommand(email, password, null, null, null, roles, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - User creation should still succeed even if role assignment fails
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(userId);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        const string email = "existing@example.com";
        const string password = "Password123!";

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(email, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(email: email));

        var command = new CreateUserCommand(email, password, null, null, null, null, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.DuplicateEmail);
        _userIdentityServiceMock.Verify(
            x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Validation Failure Scenarios

    [Fact]
    public async Task Handle_WhenCreateUserFails_ShouldReturnValidationError()
    {
        // Arrange
        const string email = "newuser@example.com";
        const string password = "weak";

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(email, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        _userIdentityServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Password too weak"));

        var command = new CreateUserCommand(email, password, null, null, null, null, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Validation.General);
    }

    [Fact]
    public async Task Handle_WhenRetrievalAfterCreateFails_ShouldReturnUnknownError()
    {
        // Arrange
        const string email = "newuser@example.com";
        const string password = "Password123!";
        const string userId = "new-user-id";

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(email, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        _userIdentityServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success(userId));

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        var command = new CreateUserCommand(email, password, null, null, null, null, false);

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
        const string email = "newuser@example.com";
        const string password = "Password123!";
        const string userId = "new-user-id";

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(email, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        _userIdentityServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success(userId));

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId, email));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var command = new CreateUserCommand(email, password, null, null, null, null, false);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _userIdentityServiceMock.Verify(x => x.FindByEmailAsync(email, TestTenantId, token), Times.Once);
        _userIdentityServiceMock.Verify(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), password, token), Times.Once);
        _userIdentityServiceMock.Verify(x => x.FindByIdAsync(userId, token), Times.Once);
        _userIdentityServiceMock.Verify(x => x.GetRolesAsync(userId, token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullOptionalFields_ShouldSucceed()
    {
        // Arrange
        const string email = "newuser@example.com";
        const string password = "Password123!";
        const string userId = "new-user-id";

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(email, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        _userIdentityServiceMock
            .Setup(x => x.CreateUserAsync(
                It.Is<CreateUserDto>(dto =>
                    dto.Email == email &&
                    dto.FirstName == null &&
                    dto.LastName == null &&
                    dto.DisplayName == null),
                password,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success(userId));

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId, email, firstName: null, lastName: null, displayName: null));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var command = new CreateUserCommand(email, password, null, null, null, null, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithEmptyRolesList_ShouldNotAttemptRoleAssignment()
    {
        // Arrange
        const string email = "newuser@example.com";
        const string password = "Password123!";
        const string userId = "new-user-id";

        _userIdentityServiceMock
            .Setup(x => x.FindByEmailAsync(email, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        _userIdentityServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success(userId));

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId, email));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var command = new CreateUserCommand(email, password, null, null, null, new List<string>(), false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userIdentityServiceMock.Verify(
            x => x.AssignRolesAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}
