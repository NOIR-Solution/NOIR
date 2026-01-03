namespace NOIR.Application.UnitTests.Features.Auth;

/// <summary>
/// Unit tests for GetCurrentUserQueryHandler.
/// Tests current user profile retrieval with authentication checks.
/// </summary>
public class GetCurrentUserQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly GetCurrentUserQueryHandler _handler;

    public GetCurrentUserQueryHandlerTests()
    {
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _currentUserMock = new Mock<ICurrentUser>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _localizationServiceMock.Setup(x => x[It.IsAny<string>()]).Returns<string>(key => key);

        _handler = new GetCurrentUserQueryHandler(
            _userManagerMock.Object,
            _currentUserMock.Object,
            _localizationServiceMock.Object);
    }

    private ApplicationUser CreateTestUser(
        string id = "user-123",
        string email = "test@example.com",
        string? firstName = "John",
        string? lastName = "Doe",
        bool isActive = true,
        string? tenantId = null)
    {
        return new ApplicationUser
        {
            Id = id,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = email,
            FirstName = firstName,
            LastName = lastName,
            IsActive = isActive,
            TenantId = tenantId,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-30)
        };
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
        var user = CreateTestUser();
        var query = new GetCurrentUserQuery();

        SetupAuthenticatedUser(user.Id);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
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
        var user = CreateTestUser(
            firstName: "Jane",
            lastName: "Smith",
            tenantId: "tenant-123");
        var query = new GetCurrentUserQuery();

        SetupAuthenticatedUser(user.Id);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
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
        var user = CreateTestUser();
        var query = new GetCurrentUserQuery();
        var roles = new List<string> { "User", "Admin", "Manager" };

        SetupAuthenticatedUser(user.Id);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
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
        var user = CreateTestUser();
        var query = new GetCurrentUserQuery();

        SetupAuthenticatedUser(user.Id);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
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
        var user = CreateTestUser(firstName: null, lastName: null);
        var query = new GetCurrentUserQuery();

        SetupAuthenticatedUser(user.Id);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
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
    public async Task Handle_UnauthenticatedUser_ShouldNotCallUserManager()
    {
        // Arrange
        var query = new GetCurrentUserQuery();
        SetupUnauthenticatedUser();

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(
            x => x.FindByIdAsync(It.IsAny<string>()),
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

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

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

        _userManagerMock
            .Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(
            x => x.GetRolesAsync(It.IsAny<ApplicationUser>()),
            Times.Never);
    }

    #endregion

    #region ICurrentUser Usage Tests

    [Fact]
    public async Task Handle_ShouldUseCurrentUserUserId()
    {
        // Arrange
        var user = CreateTestUser(id: "specific-user-456");
        var query = new GetCurrentUserQuery();

        SetupAuthenticatedUser("specific-user-456");

        _userManagerMock
            .Setup(x => x.FindByIdAsync("specific-user-456"))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _userManagerMock.Verify(
            x => x.FindByIdAsync("specific-user-456"),
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
        var user = CreateTestUser(isActive: false);
        var query = new GetCurrentUserQuery();

        SetupAuthenticatedUser(user.Id);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
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
        var user = CreateTestUser(tenantId: null);
        var query = new GetCurrentUserQuery();

        SetupAuthenticatedUser(user.Id);

        _userManagerMock
            .Setup(x => x.FindByIdAsync(user.Id))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(x => x.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TenantId.Should().BeNull();
    }

    #endregion
}
