namespace NOIR.Application.UnitTests.Features.Auth;

using NOIR.Application.Features.Auth.Queries.GetUserById;

/// <summary>
/// Unit tests for GetUserByIdQueryHandler.
/// Tests all user retrieval by ID scenarios with mocked dependencies.
/// </summary>
public class GetUserByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly GetUserByIdQueryHandler _handler;
    private const string TestUserId = "user-123";
    private const string TestTenantId = "tenant-abc";

    public GetUserByIdQueryHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _currentUserMock = new Mock<ICurrentUser>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        // Setup current user with default tenant
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new GetUserByIdQueryHandler(
            _userIdentityServiceMock.Object,
            _currentUserMock.Object,
            _localizationServiceMock.Object);
    }

    private UserIdentityDto CreateTestUserDto(
        string id = TestUserId,
        string email = "test@example.com",
        string? firstName = "Test",
        string? lastName = "User",
        string? displayName = null,
        string? phoneNumber = null,
        string? avatarUrl = null,
        bool isActive = true)
    {
        var fullName = $"{firstName} {lastName}".Trim();
        return new UserIdentityDto(
            Id: id,
            Email: email,
            TenantId: "default",
            FirstName: firstName,
            LastName: lastName,
            DisplayName: displayName,
            FullName: string.IsNullOrEmpty(fullName) ? email : fullName,
            PhoneNumber: phoneNumber,
            AvatarUrl: avatarUrl,
            IsActive: isActive,
            IsDeleted: false,
            IsSystemUser: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: DateTimeOffset.UtcNow.AddDays(-1));
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_ValidUserId_ShouldReturnUserProfile()
    {
        // Arrange
        var query = new GetUserByIdQuery(TestUserId);
        var user = CreateTestUserDto();
        var roles = new List<string> { "Admin", "User" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(TestUserId);
        result.Value.Email.ShouldBe("test@example.com");
        result.Value.FirstName.ShouldBe("Test");
        result.Value.LastName.ShouldBe("User");
        result.Value.Roles.ShouldContain("Admin");
        result.Value.Roles.ShouldContain("User");
    }

    [Fact]
    public async Task Handle_ValidUserId_ShouldMapAllProperties()
    {
        // Arrange
        var user = CreateTestUserDto(
            id: TestUserId,
            email: "full@example.com",
            firstName: "Full",
            lastName: "Name",
            displayName: "Display Name",
            phoneNumber: "+1234567890",
            avatarUrl: "https://example.com/avatar.jpg",
            isActive: true);

        var query = new GetUserByIdQuery(TestUserId);
        var roles = new List<string> { "Admin" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var profile = result.Value;
        profile.Id.ShouldBe(TestUserId);
        profile.Email.ShouldBe("full@example.com");
        profile.FirstName.ShouldBe("Full");
        profile.LastName.ShouldBe("Name");
        profile.DisplayName.ShouldBe("Display Name");
        profile.FullName.ShouldBe(user.FullName);
        profile.PhoneNumber.ShouldBe("+1234567890");
        profile.AvatarUrl.ShouldBe("https://example.com/avatar.jpg");
        profile.IsActive.ShouldBe(true);
        profile.TenantId.ShouldBe("default"); // Handler returns user.TenantId from database, not _currentUser.TenantId
        profile.CreatedAt.ShouldBe(user.CreatedAt, TimeSpan.FromSeconds(1));
        profile.ModifiedAt!.Value.ShouldBe(user.ModifiedAt!.Value, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_UserWithNoRoles_ShouldReturnEmptyRolesList()
    {
        // Arrange
        var query = new GetUserByIdQuery(TestUserId);
        var user = CreateTestUserDto();

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Roles.ShouldBeEmpty();
    }

    [Fact]
    public async Task Handle_UserWithNullOptionalFields_ShouldSucceed()
    {
        // Arrange
        var user = CreateTestUserDto(
            firstName: null,
            lastName: null,
            displayName: null,
            phoneNumber: null,
            avatarUrl: null);

        var query = new GetUserByIdQuery(TestUserId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.FirstName.ShouldBeNull();
        result.Value.LastName.ShouldBeNull();
        result.Value.DisplayName.ShouldBeNull();
        result.Value.PhoneNumber.ShouldBeNull();
        result.Value.AvatarUrl.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_InactiveUser_ShouldStillReturnProfile()
    {
        // Arrange
        var user = CreateTestUserDto(isActive: false);
        var query = new GetUserByIdQuery(TestUserId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.IsActive.ShouldBe(false);
    }

    #endregion

    #region Failure Scenarios - Validation

    [Fact]
    public async Task Handle_NullUserId_ShouldReturnValidationError()
    {
        // Arrange
        var query = new GetUserByIdQuery(null!);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Validation);
        result.Error.Message.ShouldContain("validation.userId.required");
    }

    [Fact]
    public async Task Handle_EmptyUserId_ShouldReturnValidationError()
    {
        // Arrange
        var query = new GetUserByIdQuery(string.Empty);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_WhitespaceUserId_ShouldReturnValidationError()
    {
        // Arrange
        var query = new GetUserByIdQuery("   ");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.Validation);
    }

    #endregion

    #region Failure Scenarios - User Not Found

    [Fact]
    public async Task Handle_UserNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetUserByIdQuery("nonexistent-user-id");

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync("nonexistent-user-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Message.ShouldContain("auth.user.notFound");
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldNotCallGetRoles()
    {
        // Arrange
        var query = new GetUserByIdQuery("nonexistent-user-id");

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync("nonexistent-user-id", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.GetRolesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region CancellationToken Propagation

    [Fact]
    public async Task Handle_ShouldPropagateCancellationToken()
    {
        // Arrange
        var query = new GetUserByIdQuery(TestUserId);
        var user = CreateTestUserDto();

        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(TestUserId, cancellationToken))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(TestUserId, cancellationToken))
            .ReturnsAsync(new List<string>());

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.FindByIdAsync(TestUserId, cancellationToken),
            Times.Once);
        _userIdentityServiceMock.Verify(
            x => x.GetRolesAsync(TestUserId, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithInvalidUserId_ShouldNotCallServices()
    {
        // Arrange
        var query = new GetUserByIdQuery("");

        using var cts = new CancellationTokenSource();
        var cancellationToken = cts.Token;

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Service Call Verification

    [Fact]
    public async Task Handle_ShouldCallFindByIdWithCorrectUserId()
    {
        // Arrange
        var specificUserId = "specific-user-789";
        var query = new GetUserByIdQuery(specificUserId);
        var user = CreateTestUserDto(id: specificUserId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(specificUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(specificUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.FindByIdAsync(specificUserId, It.IsAny<CancellationToken>()),
            Times.Once);
        _userIdentityServiceMock.Verify(
            x => x.GetRolesAsync(specificUserId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldIncludeTenantIdFromUserEntity()
    {
        // Arrange
        var customTenantId = "custom-tenant-xyz";

        // Create user with specific TenantId in database
        var user = new UserIdentityDto(
            Id: TestUserId,
            Email: "test@example.com",
            TenantId: customTenantId, // User's actual tenant from database
            FirstName: "Test",
            LastName: "User",
            DisplayName: null,
            FullName: "Test User",
            PhoneNumber: null,
            AvatarUrl: null,
            IsActive: true,
            IsDeleted: false,
            IsSystemUser: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: DateTimeOffset.UtcNow);

        var query = new GetUserByIdQuery(TestUserId);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - Handler should return user.TenantId from database, not _currentUser.TenantId
        result.IsSuccess.ShouldBe(true);
        result.Value.TenantId.ShouldBe(customTenantId);
    }

    [Fact]
    public async Task Handle_WhenTenantIdIsNull_ShouldReturnNullTenantId()
    {
        // Arrange
        var query = new GetUserByIdQuery(TestUserId);

        // Create user with null TenantId (platform admin/system user)
        var user = new UserIdentityDto(
            Id: TestUserId,
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
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: DateTimeOffset.UtcNow);

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(TestUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "PlatformAdmin" });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - Handler should return user.TenantId (null) from database, not _currentUser.TenantId
        result.IsSuccess.ShouldBe(true);
        result.Value.TenantId.ShouldBeNull();
    }

    #endregion
}
