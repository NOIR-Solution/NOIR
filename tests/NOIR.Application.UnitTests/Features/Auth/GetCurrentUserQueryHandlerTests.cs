namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for GetCurrentUserQueryHandler.
/// Tests current user profile retrieval with authentication checks.
/// </summary>
public class GetCurrentUserQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly GetCurrentUserQueryHandler _handler;

    public GetCurrentUserQueryHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _currentUserMock = new Mock<ICurrentUser>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _localizationServiceMock.Setup(x => x[It.IsAny<string>()]).Returns<string>(key => key);

        _handler = new GetCurrentUserQueryHandler(
            _userIdentityServiceMock.Object,
            _currentUserMock.Object,
            _localizationServiceMock.Object);
    }

    private UserIdentityDto CreateTestUserDto(
        string id = "user-123",
        string email = "test@example.com",
        string? firstName = "John",
        string? lastName = "Doe",
        bool isActive = true,
        string? tenantId = null)
    {
        return new UserIdentityDto(
            id,
            email,
            firstName,
            lastName,
            null,
            $"{firstName ?? ""} {lastName ?? ""}".Trim(),
            tenantId,
            isActive,
            false,
            DateTimeOffset.UtcNow.AddDays(-30),
            null);
    }

    private void SetupAuthenticatedUser(string userId)
    {
        _currentUserMock
            .Setup(x => x.IsAuthenticated)
            .Returns(true);

        _currentUserMock
            .Setup(x => x.UserId)
            .Returns(userId);
    }

    private void SetupUnauthenticatedUser()
    {
        _currentUserMock
            .Setup(x => x.IsAuthenticated)
            .Returns(false);

        _currentUserMock
            .Setup(x => x.UserId)
            .Returns((string?)null);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_AuthenticatedUser_ShouldReturnSuccess()
    {
        // Arrange
        var user = CreateTestUserDto();
        var query = new GetCurrentUserQuery();

        SetupAuthenticatedUser(user.Id);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(user.Id);
        result.Value.Email.Should().Be(user.Email);
    }

    [Fact]
    public async Task Handle_AuthenticatedUser_ShouldReturnAllUserProperties()
    {
        // Arrange
        var user = CreateTestUserDto(
            firstName: "Jane",
            lastName: "Smith",
            tenantId: "tenant-123");
        var query = new GetCurrentUserQuery();

        SetupAuthenticatedUser(user.Id);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "User", "Admin" });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().Be("Jane");
        result.Value.LastName.Should().Be("Smith");
        result.Value.FullName.Should().Be("Jane Smith");
        result.Value.TenantId.Should().Be("tenant-123");
        result.Value.IsActive.Should().BeTrue();
        result.Value.CreatedAt.Should().BeCloseTo(user.CreatedAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_AuthenticatedUser_ShouldReturnRoles()
    {
        // Arrange
        var user = CreateTestUserDto();
        var query = new GetCurrentUserQuery();
        var roles = new List<string> { "User", "Admin", "Manager" };

        SetupAuthenticatedUser(user.Id);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Roles.Should().BeEquivalentTo(roles);
    }

    [Fact]
    public async Task Handle_UserWithNoRoles_ShouldReturnEmptyRoles()
    {
        // Arrange
        var user = CreateTestUserDto();
        var query = new GetCurrentUserQuery();

        SetupAuthenticatedUser(user.Id);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UserWithNullNames_ShouldHandleGracefully()
    {
        // Arrange
        var user = CreateTestUserDto(firstName: null, lastName: null);
        var query = new GetCurrentUserQuery();

        SetupAuthenticatedUser(user.Id);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FirstName.Should().BeNull();
        result.Value.LastName.Should().BeNull();
    }

    #endregion

    #region Failure Scenarios - Not Authenticated

    [Fact]
    public async Task Handle_UnauthenticatedUser_ShouldReturnUnauthorized()
    {
        // Arrange
        var query = new GetCurrentUserQuery();
        SetupUnauthenticatedUser();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        result.Error.Message.Should().Contain("notAuthenticated");
    }

    [Fact]
    public async Task Handle_AuthenticatedButNoUserId_ShouldReturnUnauthorized()
    {
        // Arrange
        var query = new GetCurrentUserQuery();

        _currentUserMock
            .Setup(x => x.IsAuthenticated)
            .Returns(true);

        _currentUserMock
            .Setup(x => x.UserId)
            .Returns(string.Empty);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_UnauthenticatedUser_ShouldNotCallUserIdentityService()
    {
        // Arrange
        var query = new GetCurrentUserQuery();
        SetupUnauthenticatedUser();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Failure Scenarios - User Not Found

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetCurrentUserQuery();
        var userId = "deleted-user-123";

        SetupAuthenticatedUser(userId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().Contain("user.notFound");
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldNotCallGetRoles()
    {
        // Arrange
        var query = new GetCurrentUserQuery();
        var userId = "deleted-user-123";

        SetupAuthenticatedUser(userId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.GetRolesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region ICurrentUser Usage Tests

    [Fact]
    public async Task Handle_ShouldUseCurrentUserUserId()
    {
        // Arrange
        var user = CreateTestUserDto(id: "specific-user-456");
        var query = new GetCurrentUserQuery();

        SetupAuthenticatedUser("specific-user-456");

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync("specific-user-456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync("specific-user-456", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.FindByIdAsync("specific-user-456", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCheckIsAuthenticatedFirst()
    {
        // Arrange
        var query = new GetCurrentUserQuery();
        SetupUnauthenticatedUser();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _currentUserMock.Verify(x => x.IsAuthenticated, Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_InactiveUser_ShouldStillReturnProfile()
    {
        // Arrange - Inactive users can still view their profile
        var user = CreateTestUserDto(isActive: false);
        var query = new GetCurrentUserQuery();

        SetupAuthenticatedUser(user.Id);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UserWithNoTenant_ShouldReturnNullTenantId()
    {
        // Arrange
        var user = CreateTestUserDto(tenantId: null);
        var query = new GetCurrentUserQuery();

        SetupAuthenticatedUser(user.Id);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().BeNull();
    }

    #endregion
}
