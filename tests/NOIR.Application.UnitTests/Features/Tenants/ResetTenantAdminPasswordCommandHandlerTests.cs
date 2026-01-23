using NOIR.Application.Features.Tenants.Commands.ResetTenantAdminPassword;

namespace NOIR.Application.UnitTests.Features.Tenants;

/// <summary>
/// Unit tests for ResetTenantAdminPasswordCommandHandler.
/// Tests admin password reset scenarios for tenants.
/// </summary>
public class ResetTenantAdminPasswordCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<IRoleIdentityService> _roleIdentityServiceMock;
    private readonly Mock<IMultiTenantStore<Tenant>> _tenantStoreMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly Mock<ILogger<ResetTenantAdminPasswordCommandHandler>> _loggerMock;
    private readonly ResetTenantAdminPasswordCommandHandler _handler;

    private const string TestTenantId = "tenant-abc";

    public ResetTenantAdminPasswordCommandHandlerTests()
    {
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _roleIdentityServiceMock = new Mock<IRoleIdentityService>();
        _tenantStoreMock = new Mock<IMultiTenantStore<Tenant>>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _loggerMock = new Mock<ILogger<ResetTenantAdminPasswordCommandHandler>>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new ResetTenantAdminPasswordCommandHandler(
            _userIdentityServiceMock.Object,
            _roleIdentityServiceMock.Object,
            _tenantStoreMock.Object,
            _localizationServiceMock.Object,
            _loggerMock.Object);
    }

    private static UserIdentityDto CreateTestAdminUser(
        string id = "admin-user-id",
        string email = "admin@tenant.com")
    {
        return new UserIdentityDto(
            Id: id,
            Email: email,
            TenantId: TestTenantId,
            FirstName: "Admin",
            LastName: "User",
            DisplayName: null,
            FullName: "Admin User",
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
    public async Task Handle_WithValidTenantAndAdmin_ShouldResetPasswordSuccessfully()
    {
        // Arrange
        var tenant = Tenant.Create("test-tenant", "Test Tenant");
        _tenantStoreMock
            .Setup(x => x.GetAsync(TestTenantId))
            .ReturnsAsync(tenant);

        var adminUser = CreateTestAdminUser();
        _roleIdentityServiceMock
            .Setup(x => x.GetUsersInRoleAsync(Domain.Common.Roles.Admin, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserIdentityDto> { adminUser });

        _userIdentityServiceMock
            .Setup(x => x.ResetPasswordAsync("admin-user-id", "NewSecurePass123!", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityOperationResult(true, null, null));

        var command = new ResetTenantAdminPasswordCommand(TestTenantId, "NewSecurePass123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Success.Should().BeTrue();
        result.Value.AdminUserId.Should().Be("admin-user-id");
        result.Value.AdminEmail.Should().Be("admin@tenant.com");
    }

    [Fact]
    public async Task Handle_WithMultipleAdmins_ShouldResetFirstAdminPassword()
    {
        // Arrange
        var tenant = Tenant.Create("test-tenant", "Test Tenant");
        _tenantStoreMock
            .Setup(x => x.GetAsync(TestTenantId))
            .ReturnsAsync(tenant);

        var adminUsers = new List<UserIdentityDto>
        {
            CreateTestAdminUser(id: "first-admin", email: "first@tenant.com"),
            CreateTestAdminUser(id: "second-admin", email: "second@tenant.com")
        };
        _roleIdentityServiceMock
            .Setup(x => x.GetUsersInRoleAsync(Domain.Common.Roles.Admin, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminUsers);

        _userIdentityServiceMock
            .Setup(x => x.ResetPasswordAsync("first-admin", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityOperationResult(true, null, null));

        var command = new ResetTenantAdminPasswordCommand(TestTenantId, "NewPass123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AdminUserId.Should().Be("first-admin");
        result.Value.AdminEmail.Should().Be("first@tenant.com");
    }

    #endregion

    #region Tenant Not Found

    [Fact]
    public async Task Handle_WithNonExistentTenant_ShouldReturnNotFound()
    {
        // Arrange
        _tenantStoreMock
            .Setup(x => x.GetAsync("non-existent"))
            .ReturnsAsync((Tenant?)null);

        var command = new ResetTenantAdminPasswordCommand("non-existent", "NewPass123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Business.NotFound);
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ShouldNotAttemptPasswordReset()
    {
        // Arrange
        _tenantStoreMock
            .Setup(x => x.GetAsync(It.IsAny<string>()))
            .ReturnsAsync((Tenant?)null);

        var command = new ResetTenantAdminPasswordCommand("non-existent", "NewPass123!");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region No Admin Users Found

    [Fact]
    public async Task Handle_WithNoAdminUsers_ShouldReturnNotFound()
    {
        // Arrange
        var tenant = Tenant.Create("test-tenant", "Test Tenant");
        _tenantStoreMock
            .Setup(x => x.GetAsync(TestTenantId))
            .ReturnsAsync(tenant);

        _roleIdentityServiceMock
            .Setup(x => x.GetUsersInRoleAsync(Domain.Common.Roles.Admin, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserIdentityDto>());

        var command = new ResetTenantAdminPasswordCommand(TestTenantId, "NewPass123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Business.NotFound);
    }

    #endregion

    #region Password Reset Failure

    [Fact]
    public async Task Handle_WhenResetPasswordFails_ShouldReturnInternalError()
    {
        // Arrange
        var tenant = Tenant.Create("test-tenant", "Test Tenant");
        _tenantStoreMock
            .Setup(x => x.GetAsync(TestTenantId))
            .ReturnsAsync(tenant);

        var adminUser = CreateTestAdminUser();
        _roleIdentityServiceMock
            .Setup(x => x.GetUsersInRoleAsync(Domain.Common.Roles.Admin, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserIdentityDto> { adminUser });

        _userIdentityServiceMock
            .Setup(x => x.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityOperationResult(false, null, new[] { "Password does not meet requirements" }));

        var command = new ResetTenantAdminPasswordCommand(TestTenantId, "weak");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.System.InternalError);
    }

    [Fact]
    public async Task Handle_WhenResetPasswordFailsWithNoErrors_ShouldReturnInternalError()
    {
        // Arrange
        var tenant = Tenant.Create("test-tenant", "Test Tenant");
        _tenantStoreMock
            .Setup(x => x.GetAsync(TestTenantId))
            .ReturnsAsync(tenant);

        var adminUser = CreateTestAdminUser();
        _roleIdentityServiceMock
            .Setup(x => x.GetUsersInRoleAsync(Domain.Common.Roles.Admin, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserIdentityDto> { adminUser });

        _userIdentityServiceMock
            .Setup(x => x.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityOperationResult(false, null, null));

        var command = new ResetTenantAdminPasswordCommand(TestTenantId, "SomePass123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.System.InternalError);
    }

    #endregion

    #region Service Call Verification

    [Fact]
    public async Task Handle_ShouldLookUpAdminRole()
    {
        // Arrange
        var tenant = Tenant.Create("test-tenant", "Test Tenant");
        _tenantStoreMock
            .Setup(x => x.GetAsync(TestTenantId))
            .ReturnsAsync(tenant);

        var adminUser = CreateTestAdminUser();
        _roleIdentityServiceMock
            .Setup(x => x.GetUsersInRoleAsync(Domain.Common.Roles.Admin, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserIdentityDto> { adminUser });

        _userIdentityServiceMock
            .Setup(x => x.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityOperationResult(true, null, null));

        var command = new ResetTenantAdminPasswordCommand(TestTenantId, "NewPass123!");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _roleIdentityServiceMock.Verify(
            x => x.GetUsersInRoleAsync(Domain.Common.Roles.Admin, TestTenantId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPassCorrectPasswordToResetService()
    {
        // Arrange
        var tenant = Tenant.Create("test-tenant", "Test Tenant");
        _tenantStoreMock
            .Setup(x => x.GetAsync(TestTenantId))
            .ReturnsAsync(tenant);

        var adminUser = CreateTestAdminUser();
        _roleIdentityServiceMock
            .Setup(x => x.GetUsersInRoleAsync(Domain.Common.Roles.Admin, TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserIdentityDto> { adminUser });

        _userIdentityServiceMock
            .Setup(x => x.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityOperationResult(true, null, null));

        const string newPassword = "VerySecureNewPassword123!";
        var command = new ResetTenantAdminPasswordCommand(TestTenantId, newPassword);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _userIdentityServiceMock.Verify(
            x => x.ResetPasswordAsync("admin-user-id", newPassword, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
