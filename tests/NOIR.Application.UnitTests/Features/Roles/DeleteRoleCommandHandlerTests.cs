namespace NOIR.Application.UnitTests.Features.Roles;

/// <summary>
/// Unit tests for DeleteRoleCommandHandler.
/// Tests role deletion scenarios with mocked dependencies.
/// </summary>
public class DeleteRoleCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRoleIdentityService> _roleIdentityServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly DeleteRoleCommandHandler _handler;

    public DeleteRoleCommandHandlerTests()
    {
        _roleIdentityServiceMock = new Mock<IRoleIdentityService>();
        _localizationServiceMock = new Mock<ILocalizationService>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new DeleteRoleCommandHandler(
            _roleIdentityServiceMock.Object,
            _localizationServiceMock.Object);
    }

    private static RoleIdentityDto CreateTestRoleDto(
        string id = "role-123",
        string name = "TestRole")
    {
        return new RoleIdentityDto(id, name, name.ToUpperInvariant());
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidRole_ShouldSucceed()
    {
        // Arrange
        const string roleId = "role-123";
        const string roleName = "CustomRole";

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, roleName));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _roleIdentityServiceMock
            .Setup(x => x.DeleteRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = new DeleteRoleCommand(roleId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
        _roleIdentityServiceMock.Verify(
            x => x.DeleteRoleAsync(roleId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Not Found Scenarios

    [Fact]
    public async Task Handle_WhenRoleNotFound_ShouldReturnNotFound()
    {
        // Arrange
        const string roleId = "non-existent-role";

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleIdentityDto?)null);

        var command = new DeleteRoleCommand(roleId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.RoleNotFound);
        _roleIdentityServiceMock.Verify(
            x => x.DeleteRoleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region System Role Protection Scenarios

    [Fact]
    public async Task Handle_WhenDeletingAdminRole_ShouldReturnCannotDelete()
    {
        // Arrange
        const string roleId = "admin-role-id";
        const string adminRoleName = "Admin";

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, adminRoleName));

        var command = new DeleteRoleCommand(roleId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Business.CannotDelete);
        _roleIdentityServiceMock.Verify(
            x => x.DeleteRoleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDeletingUserRole_ShouldReturnCannotDelete()
    {
        // Arrange
        const string roleId = "user-role-id";
        const string userRoleName = "User";

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, userRoleName));

        var command = new DeleteRoleCommand(roleId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Business.CannotDelete);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("ADMIN")]
    [InlineData("Admin")]
    [InlineData("user")]
    [InlineData("USER")]
    [InlineData("User")]
    public async Task Handle_WhenDeletingSystemRoleWithAnyCasing_ShouldReturnCannotDelete(string roleName)
    {
        // Arrange
        const string roleId = "system-role-id";

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, roleName));

        var command = new DeleteRoleCommand(roleId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Business.CannotDelete);
    }

    #endregion

    #region Users Assigned Scenarios

    [Fact]
    public async Task Handle_WhenRoleHasUsers_ShouldReturnCannotDelete()
    {
        // Arrange
        const string roleId = "role-123";
        const string roleName = "CustomRole";
        const int userCount = 5;

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, roleName));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCount);

        var command = new DeleteRoleCommand(roleId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Business.CannotDelete);
        _roleIdentityServiceMock.Verify(
            x => x.DeleteRoleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenRoleHasOneUser_ShouldReturnCannotDelete()
    {
        // Arrange
        const string roleId = "role-123";
        const string roleName = "CustomRole";

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, roleName));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteRoleCommand(roleId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Business.CannotDelete);
    }

    #endregion

    #region Deletion Failure Scenarios

    [Fact]
    public async Task Handle_WhenDeleteFails_ShouldReturnValidationError()
    {
        // Arrange
        const string roleId = "role-123";
        const string roleName = "CustomRole";

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, roleName));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _roleIdentityServiceMock
            .Setup(x => x.DeleteRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Failure("Deletion failed"));

        var command = new DeleteRoleCommand(roleId);

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
        const string roleId = "role-123";
        const string roleName = "CustomRole";

        _roleIdentityServiceMock
            .Setup(x => x.FindByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestRoleDto(roleId, roleName));

        _roleIdentityServiceMock
            .Setup(x => x.GetUserCountAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _roleIdentityServiceMock
            .Setup(x => x.DeleteRoleAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityOperationResult.Success());

        var command = new DeleteRoleCommand(roleId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _roleIdentityServiceMock.Verify(x => x.FindByIdAsync(roleId, token), Times.Once);
        _roleIdentityServiceMock.Verify(x => x.GetUserCountAsync(roleId, token), Times.Once);
        _roleIdentityServiceMock.Verify(x => x.DeleteRoleAsync(roleId, token), Times.Once);
    }

    #endregion
}
