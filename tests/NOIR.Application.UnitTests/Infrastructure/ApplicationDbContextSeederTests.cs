namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for ApplicationDbContextSeeder.
/// Tests seeding logic with mocked dependencies.
/// </summary>
public class ApplicationDbContextSeederTests
{
    private readonly Mock<ILogger<ApplicationDbContext>> _loggerMock;

    public ApplicationDbContextSeederTests()
    {
        _loggerMock = new Mock<ILogger<ApplicationDbContext>>();
    }

    #region SeedSystemRolesAsync Tests

    [Fact]
    public async Task SeedSystemRolesAsync_WhenRolesDoNotExist_ShouldCreateRoles()
    {
        // Arrange
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        var roleManager = CreateRoleManager(roleStore);

        roleStore.Setup(x => x.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationRole?)null);

        roleStore.Setup(x => x.CreateAsync(It.IsAny<ApplicationRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);

        // Need to setup claims store for permission seeding
        SetupRoleClaimsStore(roleStore, []);

        // Act
        await ApplicationDbContextSeeder.SeedSystemRolesAsync(roleManager, _loggerMock.Object);

        // Assert - Should create each system role
        roleStore.Verify(
            x => x.CreateAsync(It.IsAny<ApplicationRole>(), It.IsAny<CancellationToken>()),
            Times.Exactly(Roles.SystemRoles.Count));
    }

    [Fact]
    public async Task SeedTenantRolesAsync_WhenRolesDoNotExist_ShouldCreateRoles()
    {
        // Arrange
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        var roleManager = CreateRoleManager(roleStore);

        roleStore.Setup(x => x.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationRole?)null);

        roleStore.Setup(x => x.CreateAsync(It.IsAny<ApplicationRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);

        // Need to setup claims store for permission seeding
        SetupRoleClaimsStore(roleStore, []);

        // Act
        await ApplicationDbContextSeeder.SeedTenantRolesAsync(roleManager, _loggerMock.Object);

        // Assert - Should create each tenant role
        roleStore.Verify(
            x => x.CreateAsync(It.IsAny<ApplicationRole>(), It.IsAny<CancellationToken>()),
            Times.Exactly(Roles.TenantRoles.Count));
    }

    [Fact]
    public async Task SeedTenantRolesAsync_WhenRolesExist_ShouldNotCreateRoles()
    {
        // Arrange
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        var roleManager = CreateRoleManager(roleStore);

        // Return existing role for each lookup
        roleStore.Setup(x => x.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string name, CancellationToken _) => ApplicationRole.Create(name));

        SetupRoleClaimsStore(roleStore, []);

        // Act
        await ApplicationDbContextSeeder.SeedTenantRolesAsync(roleManager, _loggerMock.Object);

        // Assert - Should not create any roles
        roleStore.Verify(
            x => x.CreateAsync(It.IsAny<ApplicationRole>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region SeedRolePermissionsAsync Tests

    [Fact]
    public async Task SeedRolePermissionsAsync_WhenPermissionsDoNotExist_ShouldAddPermissions()
    {
        // Arrange
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        var roleManager = CreateRoleManager(roleStore);
        var role = ApplicationRole.Create(Roles.Admin);
        var permissions = new List<string> { "Users.View", "Users.Create" };

        SetupRoleClaimsStore(roleStore, []);

        // Act
        await ApplicationDbContextSeeder.SeedRolePermissionsAsync(
            roleManager, role, permissions, _loggerMock.Object);

        // Assert - Should add each permission
        var claimsStore = roleStore.As<IRoleClaimStore<ApplicationRole>>();
        claimsStore.Verify(
            x => x.AddClaimAsync(role, It.Is<Claim>(c => c.Type == Permissions.ClaimType), It.IsAny<CancellationToken>()),
            Times.Exactly(permissions.Count));
    }

    [Fact]
    public async Task SeedRolePermissionsAsync_WhenPermissionsExist_ShouldNotDuplicate()
    {
        // Arrange
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        var roleManager = CreateRoleManager(roleStore);
        var role = ApplicationRole.Create(Roles.Admin);
        var permissions = new List<string> { "Users.View", "Users.Create" };

        // Setup existing claims that match the permissions
        var existingClaims = permissions
            .Select(p => new Claim(Permissions.ClaimType, p))
            .ToList();
        SetupRoleClaimsStore(roleStore, existingClaims);

        // Act
        await ApplicationDbContextSeeder.SeedRolePermissionsAsync(
            roleManager, role, permissions, _loggerMock.Object);

        // Assert - Should not add any claims
        var claimsStore = roleStore.As<IRoleClaimStore<ApplicationRole>>();
        claimsStore.Verify(
            x => x.AddClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SeedRolePermissionsAsync_WithPartialExisting_ShouldAddOnlyNew()
    {
        // Arrange
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        var roleManager = CreateRoleManager(roleStore);
        var role = ApplicationRole.Create(Roles.Admin);
        var permissions = new List<string> { "Users.View", "Users.Create", "Users.Delete" };

        // Only first permission exists
        var existingClaims = new List<Claim>
        {
            new(Permissions.ClaimType, "Users.View")
        };
        SetupRoleClaimsStore(roleStore, existingClaims);

        // Act
        await ApplicationDbContextSeeder.SeedRolePermissionsAsync(
            roleManager, role, permissions, _loggerMock.Object);

        // Assert - Should add only 2 new permissions
        var claimsStore = roleStore.As<IRoleClaimStore<ApplicationRole>>();
        claimsStore.Verify(
            x => x.AddClaimAsync(role, It.Is<Claim>(c => c.Type == Permissions.ClaimType), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    #endregion

    #region SeedTenantAdminUserAsync Tests

    [Fact]
    public async Task SeedTenantAdminUserAsync_WhenAdminDoesNotExist_ShouldCreateAdmin()
    {
        // Arrange
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userManager = CreateUserManager(userStore);
        var settings = new TenantAdminSettings
        {
            Email = "admin@noir.local",
            Password = "Admin123!",
            FirstName = "Tenant",
            LastName = "Administrator"
        };

        userStore.Setup(x => x.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        SetupUserEmailStore(userStore, null);
        SetupUserPasswordStore(userStore);
        SetupUserRoleStore(userStore);

        userStore.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);

        userStore.Setup(x => x.GetUserIdAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid().ToString());

        userStore.Setup(x => x.GetUserNameAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("admin@noir.local");

        // Act
        await ApplicationDbContextSeeder.SeedTenantAdminUserAsync(userManager, "test-tenant-id", settings, _loggerMock.Object);

        // Assert - Should create admin user
        userStore.Verify(
            x => x.CreateAsync(
                It.Is<ApplicationUser>(u => u.Email == "admin@noir.local"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SeedTenantAdminUserAsync_WhenAdminExists_WithCorrectPassword_ShouldDoNothing()
    {
        // Arrange
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userManager = CreateUserManager(userStore);
        var settings = new TenantAdminSettings
        {
            Email = "admin@noir.local",
            Password = "Admin123!",
            FirstName = "Tenant",
            LastName = "Administrator"
        };

        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "admin@noir.local",
            UserName = "admin@noir.local",
            TenantId = "test-tenant-id"
        };

        SetupUserEmailStore(userStore, existingUser);
        SetupUserPasswordStore(userStore, hasCorrectPassword: true, correctPassword: settings.Password);
        SetupUserRoleStore(userStore, new List<string> { Roles.Admin });

        userStore.Setup(x => x.GetUserIdAsync(existingUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser.Id);

        // Act
        await ApplicationDbContextSeeder.SeedTenantAdminUserAsync(userManager, "test-tenant-id", settings, _loggerMock.Object);

        // Assert - Should not create or modify user
        userStore.Verify(
            x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SeedTenantAdminUserAsync_WhenAdminExists_WithWrongPassword_ShouldResetPassword()
    {
        // Arrange
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userManager = CreateUserManager(userStore, withTokenProvider: true);
        var settings = new TenantAdminSettings
        {
            Email = "admin@noir.local",
            Password = "Admin123!",
            FirstName = "Tenant",
            LastName = "Administrator"
        };

        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "admin@noir.local",
            UserName = "admin@noir.local",
            TenantId = "test-tenant-id"
        };

        SetupUserEmailStore(userStore, existingUser);
        SetupUserPasswordStoreWithDifferentPassword(userStore);
        SetupUserTwoFactorStore(userStore);
        SetupUserRoleStore(userStore, new List<string> { Roles.Admin });

        userStore.Setup(x => x.GetUserIdAsync(existingUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser.Id);

        userStore.Setup(x => x.GetUserNameAsync(existingUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser.UserName);

        userStore.Setup(x => x.UpdateAsync(existingUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        await ApplicationDbContextSeeder.SeedTenantAdminUserAsync(userManager, "test-tenant-id", settings, _loggerMock.Object);

        // Assert - Should update user (password reset)
        userStore.Verify(
            x => x.UpdateAsync(existingUser, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task SeedTenantAdminUserAsync_WhenCreationFails_ShouldLogError()
    {
        // Arrange
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userManager = CreateUserManager(userStore);
        var settings = new TenantAdminSettings
        {
            Email = "admin@noir.local",
            Password = "Admin123!",
            FirstName = "Tenant",
            LastName = "Administrator"
        };

        userStore.Setup(x => x.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        SetupUserEmailStore(userStore, null);
        SetupUserPasswordStore(userStore);

        userStore.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Test error" }));

        // Act
        await ApplicationDbContextSeeder.SeedTenantAdminUserAsync(userManager, "test-tenant-id", settings, _loggerMock.Object);

        // Assert - Should log error
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to create tenant admin user")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task SeedTenantAdminUserAsync_WhenCreated_ShouldAddToAdminRole()
    {
        // Arrange
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userManager = CreateUserManager(userStore);
        var settings = new TenantAdminSettings
        {
            Email = "admin@noir.local",
            Password = "Admin123!",
            FirstName = "Tenant",
            LastName = "Administrator"
        };

        userStore.Setup(x => x.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        SetupUserEmailStore(userStore, null);
        SetupUserPasswordStore(userStore);
        SetupUserRoleStore(userStore);

        userStore.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);

        userStore.Setup(x => x.GetUserIdAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid().ToString());

        userStore.Setup(x => x.GetUserNameAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("admin@noir.local");

        // Act
        await ApplicationDbContextSeeder.SeedTenantAdminUserAsync(userManager, "test-tenant-id", settings, _loggerMock.Object);

        // Assert - Should add to Admin role (normalized to uppercase)
        var roleStore = userStore.As<IUserRoleStore<ApplicationUser>>();
        roleStore.Verify(
            x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), "ADMIN", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Error Path Tests

    [Fact]
    public async Task SeedTenantAdminUserAsync_WhenPasswordResetFails_ShouldNotThrow()
    {
        // Arrange
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userManager = CreateUserManager(userStore, withTokenProvider: true);
        var settings = new TenantAdminSettings
        {
            Email = "admin@noir.local",
            Password = "Admin123!",
            FirstName = "Tenant",
            LastName = "Administrator"
        };

        var existingUser = new ApplicationUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "admin@noir.local",
            UserName = "admin@noir.local",
            TenantId = "test-tenant-id"
        };

        SetupUserEmailStore(userStore, existingUser);
        SetupUserPasswordStoreWithDifferentPassword(userStore);
        SetupUserTwoFactorStore(userStore);
        SetupUserRoleStore(userStore, new List<string> { Roles.Admin });

        userStore.Setup(x => x.GetUserIdAsync(existingUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser.Id);

        userStore.Setup(x => x.GetUserNameAsync(existingUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingUser.UserName);

        // Password reset fails
        userStore.Setup(x => x.UpdateAsync(existingUser, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Reset failed" }));

        // Act - Should not throw even if reset fails
        var act = () => ApplicationDbContextSeeder.SeedTenantAdminUserAsync(userManager, "test-tenant-id", settings, _loggerMock.Object);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SeedTenantRolesAsync_WhenRoleCreationFails_ShouldContinueWithNextRole()
    {
        // Arrange
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        var roleManager = CreateRoleManager(roleStore);

        roleStore.Setup(x => x.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationRole?)null);

        // First role fails, second succeeds
        var callCount = 0;
        roleStore.Setup(x => x.CreateAsync(It.IsAny<ApplicationRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? IdentityResult.Failed(new IdentityError { Description = "First failed" })
                    : IdentityResult.Success;
            });

        SetupRoleClaimsStore(roleStore, []);

        // Act - Should not throw, should continue to next role
        await ApplicationDbContextSeeder.SeedTenantRolesAsync(roleManager, _loggerMock.Object);

        // Assert - Should attempt to create all tenant roles
        roleStore.Verify(
            x => x.CreateAsync(It.IsAny<ApplicationRole>(), It.IsAny<CancellationToken>()),
            Times.Exactly(Roles.TenantRoles.Count));
    }

    [Fact]
    public async Task SeedRolePermissionsAsync_WhenAddClaimFails_ShouldContinueWithNextPermission()
    {
        // Arrange
        var roleStore = new Mock<IRoleStore<ApplicationRole>>();
        var roleManager = CreateRoleManager(roleStore);
        var role = ApplicationRole.Create(Roles.Admin);
        var permissions = new List<string> { "Perm1", "Perm2", "Perm3" };

        var claimsStore = roleStore.As<IRoleClaimStore<ApplicationRole>>();
        claimsStore.Setup(x => x.GetClaimsAsync(It.IsAny<ApplicationRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Claim>());

        // First add fails, rest succeed
        var addCallCount = 0;
        claimsStore.Setup(x => x.AddClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                addCallCount++;
                if (addCallCount == 1)
                    throw new InvalidOperationException("Add claim failed");
                return Task.CompletedTask;
            });

        // Act & Assert - Should throw on first failure (unlike roles, permissions don't have built-in error handling)
        var act = () => ApplicationDbContextSeeder.SeedRolePermissionsAsync(
            roleManager, role, permissions, _loggerMock.Object);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SeedTenantAdminUserAsync_WhenAddToRoleFails_ShouldNotThrow()
    {
        // Arrange
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userManager = CreateUserManager(userStore);
        var settings = new TenantAdminSettings
        {
            Email = "admin@noir.local",
            Password = "Admin123!",
            FirstName = "Tenant",
            LastName = "Administrator"
        };

        userStore.Setup(x => x.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        SetupUserEmailStore(userStore, null);
        SetupUserPasswordStore(userStore);

        var roleStore = userStore.As<IUserRoleStore<ApplicationUser>>();
        roleStore.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Role assignment failed"));
        roleStore.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        userStore.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);

        userStore.Setup(x => x.GetUserIdAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid().ToString());

        userStore.Setup(x => x.GetUserNameAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("admin@noir.local");

        // Act - Adding to role throws, but creation succeeded
        var act = () => ApplicationDbContextSeeder.SeedTenantAdminUserAsync(userManager, "test-tenant-id", settings, _loggerMock.Object);

        // Assert - Should throw since role assignment is part of the flow
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task SeedTenantAdminUserAsync_WhenMultipleCreationErrors_ShouldLogAllErrors()
    {
        // Arrange
        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userManager = CreateUserManager(userStore);
        var settings = new TenantAdminSettings
        {
            Email = "admin@noir.local",
            Password = "Admin123!",
            FirstName = "Tenant",
            LastName = "Administrator"
        };

        userStore.Setup(x => x.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser?)null);

        SetupUserEmailStore(userStore, null);
        SetupUserPasswordStore(userStore);

        userStore.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Failed(
                new IdentityError { Code = "E1", Description = "Password too weak" },
                new IdentityError { Code = "E2", Description = "Email already taken" },
                new IdentityError { Code = "E3", Description = "Username invalid" }));

        // Act
        await ApplicationDbContextSeeder.SeedTenantAdminUserAsync(userManager, "test-tenant-id", settings, _loggerMock.Object);

        // Assert - Should log error containing all error descriptions
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) =>
                    v.ToString()!.Contains("Password too weak") ||
                    v.ToString()!.Contains("Email already taken")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private static RoleManager<ApplicationRole> CreateRoleManager(Mock<IRoleStore<ApplicationRole>> store)
    {
        store.As<IRoleClaimStore<ApplicationRole>>();

        return new RoleManager<ApplicationRole>(
            store.Object,
            Array.Empty<IRoleValidator<ApplicationRole>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            new Mock<ILogger<RoleManager<ApplicationRole>>>().Object);
    }

    private static void SetupRoleClaimsStore(Mock<IRoleStore<ApplicationRole>> store, List<Claim> existingClaims)
    {
        var claimsStore = store.As<IRoleClaimStore<ApplicationRole>>();

        claimsStore.Setup(x => x.GetClaimsAsync(It.IsAny<ApplicationRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingClaims);

        claimsStore.Setup(x => x.AddClaimAsync(It.IsAny<ApplicationRole>(), It.IsAny<Claim>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static UserManager<ApplicationUser> CreateUserManager(Mock<IUserStore<ApplicationUser>> store, bool withTokenProvider = false)
    {
        store.As<IUserEmailStore<ApplicationUser>>();
        store.As<IUserPasswordStore<ApplicationUser>>();
        store.As<IUserRoleStore<ApplicationUser>>();
        store.As<IUserTwoFactorStore<ApplicationUser>>();
        store.As<IUserAuthenticatorKeyStore<ApplicationUser>>();
        store.As<IUserTwoFactorRecoveryCodeStore<ApplicationUser>>();

        var identityOptions = new IdentityOptions();
        var options = new Mock<IOptions<IdentityOptions>>();
        options.Setup(x => x.Value).Returns(identityOptions);

        var passwordHasher = new PasswordHasher<ApplicationUser>();

        // Setup service provider for token providers
        var serviceProviderMock = new Mock<IServiceProvider>();

        if (withTokenProvider)
        {
            // Setup token provider for password reset
            var tokenProviderMock = new Mock<IUserTwoFactorTokenProvider<ApplicationUser>>();
            tokenProviderMock.Setup(x => x.GenerateAsync(It.IsAny<string>(), It.IsAny<UserManager<ApplicationUser>>(), It.IsAny<ApplicationUser>()))
                .ReturnsAsync("test-reset-token");
            tokenProviderMock.Setup(x => x.ValidateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UserManager<ApplicationUser>>(), It.IsAny<ApplicationUser>()))
                .ReturnsAsync(true);

            // Register the token provider in options using ProviderMap
            // UserManager looks up providers via: Options.Tokens.ProviderMap[providerName] then ServiceProvider.GetService(ProviderType)
            identityOptions.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
            identityOptions.Tokens.ProviderMap[TokenOptions.DefaultProvider] = new TokenProviderDescriptor(typeof(IUserTwoFactorTokenProvider<ApplicationUser>));

            // Service provider returns the token provider when requested
            serviceProviderMock.Setup(x => x.GetService(typeof(IUserTwoFactorTokenProvider<ApplicationUser>)))
                .Returns(tokenProviderMock.Object);
        }

        return new UserManager<ApplicationUser>(
            store.Object,
            options.Object,
            passwordHasher,
            Array.Empty<IUserValidator<ApplicationUser>>(),
            Array.Empty<IPasswordValidator<ApplicationUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            serviceProviderMock.Object,
            new Mock<ILogger<UserManager<ApplicationUser>>>().Object);
    }

    private static void SetupUserEmailStore(Mock<IUserStore<ApplicationUser>> store, ApplicationUser? user)
    {
        var emailStore = store.As<IUserEmailStore<ApplicationUser>>();

        emailStore.Setup(x => x.FindByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        if (user != null)
        {
            emailStore.Setup(x => x.GetEmailAsync(user, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user.Email);
        }
    }

    private static void SetupUserPasswordStore(Mock<IUserStore<ApplicationUser>> store, bool hasCorrectPassword = false, string correctPassword = "Admin123!")
    {
        var passwordStore = store.As<IUserPasswordStore<ApplicationUser>>();

        if (hasCorrectPassword)
        {
            var hasher = new PasswordHasher<ApplicationUser>();
            var user = new ApplicationUser();
            var hash = hasher.HashPassword(user, correctPassword);

            passwordStore.Setup(x => x.GetPasswordHashAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(hash);
        }
        else
        {
            // Return null to indicate no password set yet
            passwordStore.Setup(x => x.GetPasswordHashAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null);
        }

        passwordStore.Setup(x => x.SetPasswordHashAsync(It.IsAny<ApplicationUser>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static void SetupUserPasswordStoreWithDifferentPassword(Mock<IUserStore<ApplicationUser>> store)
    {
        var passwordStore = store.As<IUserPasswordStore<ApplicationUser>>();

        // Hash a different password so CheckPasswordAsync returns false
        var hasher = new PasswordHasher<ApplicationUser>();
        var user = new ApplicationUser();
        var hash = hasher.HashPassword(user, "different-password");

        passwordStore.Setup(x => x.GetPasswordHashAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(hash);

        passwordStore.Setup(x => x.SetPasswordHashAsync(It.IsAny<ApplicationUser>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private static void SetupUserRoleStore(Mock<IUserStore<ApplicationUser>> store, List<string>? existingRoles = null)
    {
        var roleStore = store.As<IUserRoleStore<ApplicationUser>>();

        roleStore.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        roleStore.Setup(x => x.GetRolesAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRoles ?? new List<string>());

        roleStore.Setup(x => x.IsInRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ApplicationUser _, string role, CancellationToken _) =>
                existingRoles != null && existingRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    private static void SetupUserTwoFactorStore(Mock<IUserStore<ApplicationUser>> store)
    {
        var twoFactorStore = store.As<IUserTwoFactorStore<ApplicationUser>>();

        twoFactorStore.Setup(x => x.GetTwoFactorEnabledAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var authenticatorStore = store.As<IUserAuthenticatorKeyStore<ApplicationUser>>();
        authenticatorStore.Setup(x => x.GetAuthenticatorKeyAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var recoveryStore = store.As<IUserTwoFactorRecoveryCodeStore<ApplicationUser>>();
        recoveryStore.Setup(x => x.CountCodesAsync(It.IsAny<ApplicationUser>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
    }

    #endregion
}
