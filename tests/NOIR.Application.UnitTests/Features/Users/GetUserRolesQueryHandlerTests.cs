namespace NOIR.Application.UnitTests.Features.Users;

/// <summary>
/// Unit tests for GetUserRolesQueryHandler.
/// Tests retrieving roles assigned to a user with mocked dependencies.
/// </summary>
public class GetUserRolesQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly GetUserRolesQueryHandler _handler;

    public GetUserRolesQueryHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new GetUserRolesQueryHandler(
            _userIdentityServiceMock.Object,
            _localizationServiceMock.Object);
    }

    private static UserIdentityDto CreateTestUserDto(
        string id = "user-123",
        string email = "test@example.com")
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
            IsActive: true,
            IsDeleted: false,
            IsSystemUser: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: null);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidUser_ShouldReturnRoles()
    {
        // Arrange
        const string userId = "user-123";
        var expectedRoles = new List<string> { "Admin", "User", "Manager" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRoles);

        var query = new GetUserRolesQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedRoles);
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithUserHavingNoRoles_ShouldReturnEmptyList()
    {
        // Arrange
        const string userId = "user-no-roles";

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        var query = new GetUserRolesQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithUserHavingSingleRole_ShouldReturnSingleRole()
    {
        // Arrange
        const string userId = "user-single-role";
        var expectedRoles = new List<string> { "User" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRoles);

        var query = new GetUserRolesQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.Should().Be("User");
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

        var query = new GetUserRolesQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.UserNotFound);
        _userIdentityServiceMock.Verify(
            x => x.GetRolesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        const string userId = "user-123";

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "User" });

        var query = new GetUserRolesQuery(userId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(query, token);

        // Assert
        _userIdentityServiceMock.Verify(x => x.FindByIdAsync(userId, token), Times.Once);
        _userIdentityServiceMock.Verify(x => x.GetRolesAsync(userId, token), Times.Once);
    }

    [Theory]
    [InlineData("user-1")]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    [InlineData("long-user-id-with-many-characters")]
    public async Task Handle_WithVariousUserIdFormats_ShouldWork(string userId)
    {
        // Arrange
        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string> { "User" });

        var query = new GetUserRolesQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnIReadOnlyList()
    {
        // Arrange
        const string userId = "user-123";
        var roles = new List<string> { "Admin", "User" };

        _userIdentityServiceMock
            .Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestUserDto(userId));

        _userIdentityServiceMock
            .Setup(x => x.GetRolesAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        var query = new GetUserRolesQuery(userId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeAssignableTo<IReadOnlyList<string>>();
    }

    #endregion
}
