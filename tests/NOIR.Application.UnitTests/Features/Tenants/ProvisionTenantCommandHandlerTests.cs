using NOIR.Application.Features.Tenants.Commands.ProvisionTenant;

namespace NOIR.Application.UnitTests.Features.Tenants;

/// <summary>
/// Unit tests for ProvisionTenantCommandHandler.
/// Tests tenant provisioning with optional admin user creation.
/// </summary>
public class ProvisionTenantCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IMultiTenantStore<Tenant>> _tenantStoreMock;
    private readonly Mock<IUserIdentityService> _identityServiceMock;
    private readonly Mock<ILocalizationService> _localizationServiceMock;
    private readonly Mock<IWelcomeEmailService> _welcomeEmailServiceMock;
    private readonly Mock<ILogger<ProvisionTenantCommandHandler>> _loggerMock;
    private readonly ProvisionTenantCommandHandler _handler;

    public ProvisionTenantCommandHandlerTests()
    {
        _tenantStoreMock = new Mock<IMultiTenantStore<Tenant>>();
        _identityServiceMock = new Mock<IUserIdentityService>();
        _localizationServiceMock = new Mock<ILocalizationService>();
        _welcomeEmailServiceMock = new Mock<IWelcomeEmailService>();
        _loggerMock = new Mock<ILogger<ProvisionTenantCommandHandler>>();

        // Setup localization to return the key (pass-through for testing)
        _localizationServiceMock
            .Setup(x => x[It.IsAny<string>()])
            .Returns<string>(key => key);

        _handler = new ProvisionTenantCommandHandler(
            _tenantStoreMock.Object,
            _identityServiceMock.Object,
            _localizationServiceMock.Object,
            _welcomeEmailServiceMock.Object,
            _loggerMock.Object);
    }

    private void SetupSuccessfulTenantCreation()
    {
        _tenantStoreMock
            .Setup(x => x.GetByIdentifierAsync(It.IsAny<string>()))
            .ReturnsAsync((Tenant?)null);

        _tenantStoreMock
            .Setup(x => x.AddAsync(It.IsAny<Tenant>()))
            .ReturnsAsync(true);

        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Tenant>());
    }

    private void SetupSuccessfulAdminCreation()
    {
        _identityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        _identityServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityOperationResult(true, "admin-user-id", null));

        _identityServiceMock
            .Setup(x => x.AssignRolesAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityOperationResult(true, null, null));
    }

    #endregion

    #region Success Scenarios - Without Admin

    [Fact]
    public async Task Handle_WithoutAdminUser_ShouldCreateTenantOnly()
    {
        // Arrange
        SetupSuccessfulTenantCreation();

        var command = new ProvisionTenantCommand(
            Identifier: "new-tenant",
            Name: "New Tenant",
            CreateAdminUser: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Identifier.Should().Be("new-tenant");
        result.Value.Name.Should().Be("New Tenant");
        result.Value.IsActive.Should().BeTrue();
        result.Value.AdminUserCreated.Should().BeFalse();
        result.Value.AdminUserId.Should().BeNull();
        result.Value.AdminEmail.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithDomainAndDescription_ShouldPreserveFields()
    {
        // Arrange
        SetupSuccessfulTenantCreation();

        var command = new ProvisionTenantCommand(
            Identifier: "acme",
            Name: "ACME Corp",
            Domain: "acme.example.com",
            Description: "ACME Corporation tenant",
            Note: "Internal note",
            CreateAdminUser: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Domain.Should().Be("acme.example.com");
    }

    #endregion

    #region Success Scenarios - With Admin

    [Fact]
    public async Task Handle_WithAdminUser_ShouldCreateTenantAndAdmin()
    {
        // Arrange
        SetupSuccessfulTenantCreation();
        SetupSuccessfulAdminCreation();

        var command = new ProvisionTenantCommand(
            Identifier: "new-tenant",
            Name: "New Tenant",
            CreateAdminUser: true,
            AdminEmail: "admin@newtenant.com",
            AdminPassword: "SecurePass123!",
            AdminFirstName: "John",
            AdminLastName: "Doe");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AdminUserCreated.Should().BeTrue();
        result.Value.AdminUserId.Should().Be("admin-user-id");
        result.Value.AdminEmail.Should().Be("admin@newtenant.com");
        result.Value.AdminCreationError.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithAdminUser_ShouldAssignAdminRole()
    {
        // Arrange
        SetupSuccessfulTenantCreation();
        SetupSuccessfulAdminCreation();

        var command = new ProvisionTenantCommand(
            Identifier: "new-tenant",
            Name: "New Tenant",
            CreateAdminUser: true,
            AdminEmail: "admin@newtenant.com",
            AdminPassword: "SecurePass123!");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _identityServiceMock.Verify(
            x => x.AssignRolesAsync(
                "admin-user-id",
                It.Is<IEnumerable<string>>(roles => roles.Contains(Domain.Common.Roles.Admin)),
                false,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithAdminUser_ShouldSendWelcomeEmail()
    {
        // Arrange
        SetupSuccessfulTenantCreation();
        SetupSuccessfulAdminCreation();

        var command = new ProvisionTenantCommand(
            Identifier: "new-tenant",
            Name: "New Tenant",
            CreateAdminUser: true,
            AdminEmail: "admin@newtenant.com",
            AdminPassword: "SecurePass123!",
            AdminFirstName: "John",
            AdminLastName: "Doe");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _welcomeEmailServiceMock.Verify(
            x => x.SendWelcomeEmailAsync(
                "admin@newtenant.com",
                "John Doe",
                "SecurePass123!",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenIdentifierExists_ShouldReturnConflict()
    {
        // Arrange
        _tenantStoreMock
            .Setup(x => x.GetByIdentifierAsync("existing"))
            .ReturnsAsync(Tenant.Create("existing", "Existing Tenant"));

        var command = new ProvisionTenantCommand(
            Identifier: "existing",
            Name: "New Tenant",
            CreateAdminUser: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Business.AlreadyExists);
        _tenantStoreMock.Verify(x => x.AddAsync(It.IsAny<Tenant>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDomainAlreadyInUse_ShouldReturnConflict()
    {
        // Arrange
        _tenantStoreMock
            .Setup(x => x.GetByIdentifierAsync(It.IsAny<string>()))
            .ReturnsAsync((Tenant?)null);

        var existingTenant = Tenant.Create("other", "Other Tenant", "shared.example.com");
        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Tenant> { existingTenant });

        var command = new ProvisionTenantCommand(
            Identifier: "new-tenant",
            Name: "New Tenant",
            Domain: "shared.example.com",
            CreateAdminUser: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Business.AlreadyExists);
    }

    #endregion

    #region Tenant Creation Failure

    [Fact]
    public async Task Handle_WhenTenantAddFails_ShouldReturnInternalError()
    {
        // Arrange
        _tenantStoreMock
            .Setup(x => x.GetByIdentifierAsync(It.IsAny<string>()))
            .ReturnsAsync((Tenant?)null);
        _tenantStoreMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Tenant>());
        _tenantStoreMock
            .Setup(x => x.AddAsync(It.IsAny<Tenant>()))
            .ReturnsAsync(false);

        var command = new ProvisionTenantCommand(
            Identifier: "new-tenant",
            Name: "New Tenant",
            CreateAdminUser: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.System.InternalError);
    }

    #endregion

    #region Admin User Creation Failure - Rollback

    [Fact]
    public async Task Handle_WhenAdminEmailExists_ShouldRollbackTenantAndReturnConflict()
    {
        // Arrange
        SetupSuccessfulTenantCreation();

        var existingUser = new UserIdentityDto(
            Id: "existing-user",
            Email: "admin@newtenant.com",
            TenantId: null,
            FirstName: "Existing",
            LastName: "User",
            DisplayName: null,
            FullName: "Existing User",
            PhoneNumber: null,
            AvatarUrl: null,
            IsActive: true,
            IsDeleted: false,
            IsSystemUser: false,
            CreatedAt: DateTimeOffset.UtcNow,
            ModifiedAt: null);

        _identityServiceMock
            .Setup(x => x.FindByEmailAsync("admin@newtenant.com", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser);

        var command = new ProvisionTenantCommand(
            Identifier: "new-tenant",
            Name: "New Tenant",
            CreateAdminUser: true,
            AdminEmail: "admin@newtenant.com",
            AdminPassword: "SecurePass123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.DuplicateEmail);

        // Verify rollback
        _tenantStoreMock.Verify(
            x => x.RemoveAsync(It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserCreationFails_ShouldRollbackTenant()
    {
        // Arrange
        SetupSuccessfulTenantCreation();

        _identityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        _identityServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityOperationResult(false, null, new[] { "Password too weak" }));

        var command = new ProvisionTenantCommand(
            Identifier: "new-tenant",
            Name: "New Tenant",
            CreateAdminUser: true,
            AdminEmail: "admin@newtenant.com",
            AdminPassword: "weak");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.UserCreationFailed);

        // Verify rollback
        _tenantStoreMock.Verify(
            x => x.RemoveAsync(It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRoleAssignmentFails_ShouldRollbackTenantAndDeleteUser()
    {
        // Arrange
        SetupSuccessfulTenantCreation();

        _identityServiceMock
            .Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserIdentityDto?)null);

        _identityServiceMock
            .Setup(x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityOperationResult(true, "admin-user-id", null));

        _identityServiceMock
            .Setup(x => x.AssignRolesAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IdentityOperationResult(false, null, new[] { "Role not found" }));

        var command = new ProvisionTenantCommand(
            Identifier: "new-tenant",
            Name: "New Tenant",
            CreateAdminUser: true,
            AdminEmail: "admin@newtenant.com",
            AdminPassword: "SecurePass123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Auth.RoleAssignmentFailed);

        // Verify user deletion
        _identityServiceMock.Verify(
            x => x.SoftDeleteUserAsync("admin-user-id", "SYSTEM", It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify tenant rollback
        _tenantStoreMock.Verify(
            x => x.RemoveAsync(It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    #region CreateAdminUser Flag

    [Fact]
    public async Task Handle_WithCreateAdminFalse_ShouldNotAttemptUserCreation()
    {
        // Arrange
        SetupSuccessfulTenantCreation();

        var command = new ProvisionTenantCommand(
            Identifier: "new-tenant",
            Name: "New Tenant",
            CreateAdminUser: false,
            AdminEmail: "admin@newtenant.com",
            AdminPassword: "SecurePass123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _identityServiceMock.Verify(
            x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithCreateAdminTrueButNoEmail_ShouldSkipAdminCreation()
    {
        // Arrange
        SetupSuccessfulTenantCreation();

        var command = new ProvisionTenantCommand(
            Identifier: "new-tenant",
            Name: "New Tenant",
            CreateAdminUser: true,
            AdminEmail: null,
            AdminPassword: "SecurePass123!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AdminUserCreated.Should().BeFalse();
        _identityServiceMock.Verify(
            x => x.CreateUserAsync(It.IsAny<CreateUserDto>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Default Admin Name

    [Fact]
    public async Task Handle_WithNoAdminName_ShouldUseDefaultNames()
    {
        // Arrange
        SetupSuccessfulTenantCreation();
        SetupSuccessfulAdminCreation();

        var command = new ProvisionTenantCommand(
            Identifier: "new-tenant",
            Name: "New Tenant",
            CreateAdminUser: true,
            AdminEmail: "admin@newtenant.com",
            AdminPassword: "SecurePass123!",
            AdminFirstName: null,
            AdminLastName: null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _identityServiceMock.Verify(
            x => x.CreateUserAsync(
                It.Is<CreateUserDto>(dto => dto.FirstName == "Admin" && dto.LastName == "User"),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
