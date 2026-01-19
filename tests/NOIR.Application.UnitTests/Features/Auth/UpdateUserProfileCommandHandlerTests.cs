namespace NOIR.Application.UnitTests.Features.Auth;

using NOIR.Application.Features.Auth.Commands.UpdateUserProfile;
using NOIR.Application.Features.Auth.Queries.GetUserById;

/// <summary>
/// Unit tests for UpdateUserProfileCommandHandler.
/// Tests all profile update scenarios with mocked dependencies.
/// </summary>
public class UpdateUserProfileCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly UpdateUserProfileCommandHandler _handler;
    private const string TestUserId = "user-123";
    private const string TestTenantId = "tenant-abc";

    public UpdateUserProfileCommandHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _currentUserMock = new Mock<ICurrentUser>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new UpdateUserProfileCommandHandler(
            _userIdentityServiceMock.Object,
            _currentUserMock.Object,
            _localizationServiceMock.Object);
    }

    private void SetupAuthenticatedUser(string userId = TestUserId, string tenantId = TestTenantId)
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(userId);
        _currentUserMock.Setup(x => x.TenantId).Returns(tenantId);
    }

    private void SetupUnauthenticatedUser()
    {
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(false);
        _currentUserMock.Setup(x => x.UserId).Returns((string?)null);
    }

    private UserIdentityDto CreateTestUserDto(
        string id = TestUserId,
        string email = "test@example.com",
        string? firstName = "Test",
        string? lastName = "User",
        string? displayName = null,
        string? phoneNumber = null,
        bool isActive = true)
    {
        return new UserIdentityDto(
            Id: id,
            Email: email,
            TenantId: "default",
            FirstName: firstName,
            LastName: lastName,
            DisplayName: displayName,
            FullName: $"{firstName} {lastName}".Trim(),
            PhoneNumber: phoneNumber,
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
    public async Task Handle_ValidUpdateWithChanges_ShouldReturnSuccess()
    {
        // Arrange
        SetupAuthenticatedUser();
        var existingUser = CreateTestUserDto();
        var command = new UpdateUserProfileCommand(
            FirstName: "Updated",
            LastName: "Name",
            DisplayName: "New Display",
            PhoneNumber: "+1234567890");

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(TestUserId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityOperationResult(Succeeded: true));

        var updatedUser = CreateTestUserDto(
            firstName: "Updated",
            lastName: "Name",
            displayName: "New Display",
            phoneNumber: "+1234567890");

        _userIdentityServiceMock
            .SetupSequence(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser)
            .ReturnsAsync(updatedUser);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.FirstName.Should().Be("Updated");
        result.Value.LastName.Should().Be("Name");
        result.Value.DisplayName.Should().Be("New Display");
        result.Value.PhoneNumber.Should().Be("+1234567890");
    }

    [Fact]
    public async Task Handle_PartialUpdate_ShouldOnlyUpdateProvidedFields()
    {
        // Arrange
        SetupAuthenticatedUser();
        var existingUser = CreateTestUserDto(firstName: "Original", lastName: "User");
        var command = new UpdateUserProfileCommand(
            FirstName: "Updated",
            LastName: null,
            DisplayName: null,
            PhoneNumber: null);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(TestUserId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityOperationResult(Succeeded: true));

        var updatedUser = CreateTestUserDto(firstName: "Updated", lastName: "User");
        _userIdentityServiceMock
            .SetupSequence(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser)
            .ReturnsAsync(updatedUser);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userIdentityServiceMock.Verify(
            x => x.UpdateUserAsync(
                TestUserId,
                It.Is<UpdateUserDto>(dto => dto.FirstName == "Updated" && dto.LastName == null),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NoChanges_ShouldReturnSuccessWithoutCallingUpdate()
    {
        // Arrange
        SetupAuthenticatedUser();
        var existingUser = CreateTestUserDto(firstName: "Test", lastName: "User");
        var command = new UpdateUserProfileCommand(
            FirstName: "Test", // Same as existing
            LastName: "User",  // Same as existing
            DisplayName: null,
            PhoneNumber: null);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userIdentityServiceMock.Verify(
            x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_NullValuesInCommand_ShouldNotTriggerUpdate()
    {
        // Arrange
        SetupAuthenticatedUser();
        var existingUser = CreateTestUserDto();
        var command = new UpdateUserProfileCommand(
            FirstName: null,
            LastName: null,
            DisplayName: null,
            PhoneNumber: null);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _userIdentityServiceMock.Verify(
            x => x.UpdateUserAsync(It.IsAny<string>(), It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ValidUpdate_ShouldReturnCorrectUserProfile()
    {
        // Arrange
        SetupAuthenticatedUser();
        var existingUser = CreateTestUserDto();
        var command = new UpdateUserProfileCommand(
            FirstName: "New",
            LastName: "Name",
            DisplayName: null,
            PhoneNumber: null);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(TestUserId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityOperationResult(Succeeded: true));

        var updatedUser = CreateTestUserDto(firstName: "New", lastName: "Name");
        _userIdentityServiceMock
            .SetupSequence(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser)
            .ReturnsAsync(updatedUser);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "Admin", "User" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(TestUserId);
        result.Value.Email.Should().Be("test@example.com");
        result.Value.Roles.Should().Contain("Admin");
        result.Value.Roles.Should().Contain("User");
        result.Value.TenantId.Should().Be(TestTenantId);
    }

    #endregion

    #region Failure Scenarios - Unauthorized

    [Fact]
    public async Task Handle_NotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        SetupUnauthenticatedUser();
        var command = new UpdateUserProfileCommand(
            FirstName: "Test",
            LastName: "User",
            DisplayName: null,
            PhoneNumber: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Message.Should().Contain("auth.user.notAuthenticated");
    }

    [Fact]
    public async Task Handle_NullUserId_ShouldReturnUnauthorized()
    {
        // Arrange
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns((string?)null);

        var command = new UpdateUserProfileCommand(
            FirstName: "Test",
            LastName: "User",
            DisplayName: null,
            PhoneNumber: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_EmptyUserId_ShouldReturnUnauthorized()
    {
        // Arrange
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(string.Empty);

        var command = new UpdateUserProfileCommand(
            FirstName: "Test",
            LastName: "User",
            DisplayName: null,
            PhoneNumber: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    #endregion

    #region Failure Scenarios - User Not Found

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        SetupAuthenticatedUser();
        var command = new UpdateUserProfileCommand(
            FirstName: "Test",
            LastName: "User",
            DisplayName: null,
            PhoneNumber: null);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().Contain("auth.user.notFound");
    }

    [Fact]
    public async Task Handle_UserNotFoundAfterUpdate_ShouldReturnNotFound()
    {
        // Arrange
        SetupAuthenticatedUser();
        var existingUser = CreateTestUserDto();
        var command = new UpdateUserProfileCommand(
            FirstName: "New",
            LastName: "Name",
            DisplayName: null,
            PhoneNumber: null);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(TestUserId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityOperationResult(Succeeded: true));

        // User exists on first call, but not on refresh
        _userIdentityServiceMock
            .SetupSequence(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser)
            .ReturnsAsync((UserIdentityDto?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    #endregion

    #region Failure Scenarios - Update Failed

    [Fact]
    public async Task Handle_UpdateFailed_ShouldReturnFailure()
    {
        // Arrange
        SetupAuthenticatedUser();
        var existingUser = CreateTestUserDto();
        var command = new UpdateUserProfileCommand(
            FirstName: "New",
            LastName: "Name",
            DisplayName: null,
            PhoneNumber: null);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(TestUserId, It.IsAny<UpdateUserDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityOperationResult(Succeeded: false, Errors: new[] { "Update failed" }));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Failure);
        result.Error.Message.Should().Contain("auth.user.updateFailed");
    }

    #endregion

    #region CancellationToken Propagation

    [Fact]
    public async Task Handle_ShouldPropagateCancellationToken()
    {
        // Arrange
        SetupAuthenticatedUser();
        var existingUser = CreateTestUserDto();
        var command = new UpdateUserProfileCommand(
            FirstName: "Test",
            LastName: null,
            DisplayName: null,
            PhoneNumber: null);

        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.FindByIdAsync(TestUserId, cancellationToken),
            Times.Once);
        _userIdentityServiceMock.Verify(
            x => x.GetRolesAsync(TestUserId, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithChanges_ShouldPropagateCancellationToken()
    {
        // Arrange
        SetupAuthenticatedUser();
        var existingUser = CreateTestUserDto();
        var command = new UpdateUserProfileCommand(
            FirstName: "New",
            LastName: null,
            DisplayName: null,
            PhoneNumber: null);

        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        var updatedUser = CreateTestUserDto(firstName: "New");
        _userIdentityServiceMock
            .SetupSequence(x => x.FindByIdAsync(TestUserId, cancellationToken))
            .ReturnsAsync(existingUser)
            .ReturnsAsync(updatedUser);

        _userIdentityServiceMock
            .Setup(x => x.UpdateUserAsync(TestUserId, It.IsAny<UpdateUserDto>(), cancellationToken))
            .ReturnsAsync(new IdentityOperationResult(Succeeded: true));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(TestUserId, cancellationToken))
            .ReturnsAsync(new List<string>());

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.UpdateUserAsync(TestUserId, It.IsAny<UpdateUserDto>(), cancellationToken),
            Times.Once);
    }

    #endregion
}
