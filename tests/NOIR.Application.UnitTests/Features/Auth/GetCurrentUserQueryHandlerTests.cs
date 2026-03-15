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

    private const string TestTenantId = "tenant-123";

    private UserIdentityDto CreateTestUserDto(
        string id = "user-123",
        string email = "test@example.com",
        string? firstName = "John",
        string? lastName = "Doe",
        bool isActive = true)
    {
        return new UserIdentityDto(
            Id: id,
            Email: email,
            TenantId: "default",
            FirstName: firstName,
            LastName: lastName,
            DisplayName: null,
            FullName: $"{firstName ?? ""} {lastName ?? ""}".Trim(),
            PhoneNumber: null,
            AvatarUrl: null,
            IsActive: isActive,
            IsDeleted: false,
            IsSystemUser: false,
            CreatedAt: DateTimeOffset.UtcNow.AddDays(-30),
            ModifiedAt: null);
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
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(user.Id);
        result.Value.Email.ShouldBe(user.Email);
    }

    [Fact]
    public async Task Handle_AuthenticatedUser_ShouldReturnAllUserProperties()
    {
        // Arrange
        var user = CreateTestUserDto(
            firstName: "Jane",
            lastName: "Smith");
        var query = new GetCurrentUserQuery();

        SetupAuthenticatedUser(user.Id);
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "User", "Admin" });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.FirstName.ShouldBe("Jane");
        result.Value.LastName.ShouldBe("Smith");
        result.Value.FullName.ShouldBe("Jane Smith");
        result.Value.TenantId.ShouldBe("default"); // Handler returns user.TenantId from database, not _currentUser.TenantId
        result.Value.IsActive.ShouldBe(true);
        result.Value.CreatedAt.ShouldBe(user.CreatedAt, TimeSpan.FromSeconds(1));
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
        result.IsSuccess.ShouldBe(true);
        result.Value.Roles.ShouldBe(roles);
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
        result.IsSuccess.ShouldBe(true);
        result.Value.Roles.ShouldBeEmpty();
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
        result.IsSuccess.ShouldBe(true);
        result.Value.FirstName.ShouldBeNull();
        result.Value.LastName.ShouldBeNull();
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
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
        result.Error.Message.ShouldContain("notAuthenticated");
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
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Unauthorized);
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
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Message.ShouldContain("user.notFound");
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
        result.IsSuccess.ShouldBe(true);
        result.Value.IsActive.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_UserWithNoTenant_ShouldReturnNullTenantId()
    {
        // Arrange - Platform admin with no tenant
        var user = new UserIdentityDto(
            Id: "platform-user-123",
            Email: "platform@example.com",
            TenantId: null, // Platform admin has no tenant
            FirstName: "Platform",
            LastName: "Admin",
            DisplayName: null,
            FullName: "Platform Admin",
            PhoneNumber: null,
            AvatarUrl: null,
            IsActive: true,
            IsDeleted: false,
            IsSystemUser: true,
            CreatedAt: DateTimeOffset.UtcNow.AddDays(-30),
            ModifiedAt: null);

        var query = new GetCurrentUserQuery();

        SetupAuthenticatedUser(user.Id);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "PlatformAdmin" });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - Handler should return user.TenantId (null) from database, not _currentUser.TenantId
        result.IsSuccess.ShouldBe(true);
        result.Value.TenantId.ShouldBeNull();
    }

    #endregion
}
