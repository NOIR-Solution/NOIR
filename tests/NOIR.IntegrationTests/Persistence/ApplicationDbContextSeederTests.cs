namespace NOIR.IntegrationTests.Persistence;

/// <summary>
/// Integration tests for ApplicationDbContextSeeder.
/// Tests database initialization and seeding functionality.
/// </summary>
[Collection("Integration")]
public class ApplicationDbContextSeederTests : IAsyncLifetime
{
    private readonly CustomWebApplicationFactory _factory;
    private string _testEmail = null!;

    public ApplicationDbContextSeederTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        _testEmail = $"test-{Guid.NewGuid()}@noir.local";
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(_testEmail);
            if (user != null)
            {
                await userManager.DeleteAsync(user);
            }
        });
    }

    #region SeedDatabaseAsync Tests

    [Fact]
    public async Task SeedDatabaseAsync_ShouldCreateDefaultRoles()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

            // Assert - Default roles should exist
            foreach (var roleName in Roles.Defaults)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                role.Should().NotBeNull($"Role '{roleName}' should exist");
            }
        });
    }

    [Fact]
    public async Task SeedDatabaseAsync_ShouldCreateAdminUser()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Act
            var adminUser = await userManager.FindByEmailAsync("admin@noir.local");

            // Assert
            adminUser.Should().NotBeNull();
            adminUser!.EmailConfirmed.Should().BeTrue();
            adminUser.IsActive.Should().BeTrue();
        });
    }

    [Fact]
    public async Task SeedDatabaseAsync_ShouldAssignAdminUserToAdminRole()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Arrange
            var adminUser = await userManager.FindByEmailAsync("admin@noir.local");

            // Assert
            adminUser.Should().NotBeNull();
            var isInAdminRole = await userManager.IsInRoleAsync(adminUser!, Roles.Admin);
            isInAdminRole.Should().BeTrue();
        });
    }

    [Fact]
    public async Task SeedDatabaseAsync_ShouldSetAdminPermissions()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

            // Arrange
            var adminRole = await roleManager.FindByNameAsync(Roles.Admin);

            // Assert
            adminRole.Should().NotBeNull();

            var claims = await roleManager.GetClaimsAsync(adminRole!);
            var permissionClaims = claims
                .Where(c => c.Type == Permissions.ClaimType)
                .Select(c => c.Value)
                .ToList();

            // Admin should have admin permissions
            permissionClaims.Should().Contain(Permissions.AdminDefaults);
        });
    }

    [Fact]
    public async Task SeedDatabaseAsync_ShouldSetUserPermissions()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

            // Arrange
            var userRole = await roleManager.FindByNameAsync(Roles.User);

            // Assert
            userRole.Should().NotBeNull();

            var claims = await roleManager.GetClaimsAsync(userRole!);
            var permissionClaims = claims
                .Where(c => c.Type == Permissions.ClaimType)
                .Select(c => c.Value)
                .ToList();

            // User should have user permissions
            permissionClaims.Should().Contain(Permissions.UserDefaults);
        });
    }

    #endregion

    #region Role Seeding Tests

    [Fact]
    public async Task SeedRolesAsync_WhenRoleExists_ShouldNotDuplicate()
    {
        await _factory.ExecuteWithTenantAsync(services =>
        {
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

            // Assert - Should have exactly the default roles, no duplicates
            var roles = roleManager.Roles.ToList();
            roles.Should().HaveCount(Roles.Defaults.Count());

            // Each default role should exist exactly once
            foreach (var roleName in Roles.Defaults)
            {
                roles.Count(r => r.Name == roleName).Should().Be(1,
                    $"Role '{roleName}' should exist exactly once");
            }

            return Task.CompletedTask;
        });
    }

    [Fact]
    public async Task SeedRolePermissions_WhenPermissionExists_ShouldNotDuplicate()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

            // Arrange
            var adminRole = await roleManager.FindByNameAsync(Roles.Admin);
            adminRole.Should().NotBeNull();

            // Assert - Each admin permission should exist exactly once
            var claims = await roleManager.GetClaimsAsync(adminRole!);
            var permissionClaims = claims
                .Where(c => c.Type == Permissions.ClaimType)
                .ToList();

            // Check no duplicates exist
            var groupedClaims = permissionClaims.GroupBy(c => c.Value);
            foreach (var group in groupedClaims)
            {
                group.Count().Should().Be(1,
                    $"Permission '{group.Key}' should exist exactly once on Admin role");
            }
        });
    }

    #endregion

    #region Admin User Seeding Tests

    [Fact]
    public async Task SeedAdminUser_WhenUserExists_ShouldNotDuplicate()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Assert - Should have exactly one admin user
            var adminUsers = await userManager.Users
                .Where(u => u.Email == "admin@noir.local")
                .ToListAsync();

            adminUsers.Should().HaveCount(1);
            adminUsers[0].Should().NotBeNull();
        });
    }

    [Fact]
    public async Task SeedAdminUser_ShouldSetCorrectPassword()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Arrange
            var adminUser = await userManager.FindByEmailAsync("admin@noir.local");

            // Assert
            adminUser.Should().NotBeNull();
            var passwordValid = await userManager.CheckPasswordAsync(adminUser!, "123qwe");
            passwordValid.Should().BeTrue();
        });
    }

    [Fact]
    public async Task SeedAdminUser_ShouldHaveConfirmedEmail()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Arrange
            var adminUser = await userManager.FindByEmailAsync("admin@noir.local");

            // Assert - Admin user should have confirmed email set during seeding
            adminUser.Should().NotBeNull();
            adminUser!.EmailConfirmed.Should().BeTrue();
        });
    }

    #endregion

    #region Database Initialization Tests

    [Fact]
    public async Task SeedDatabaseAsync_ShouldEnsureTablesExist()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var context = services.GetRequiredService<ApplicationDbContext>();

            // Assert - Tables should exist and be queryable
            var canConnect = await context.Database.CanConnectAsync();
            canConnect.Should().BeTrue();

            // Query each DbSet to ensure tables exist
            _ = await context.RefreshTokens.Take(1).ToListAsync();
            _ = await context.EntityAuditLogs.Take(1).ToListAsync();
            _ = await context.Permissions.Take(1).ToListAsync();
            _ = await context.RolePermissions.Take(1).ToListAsync();
        });
    }

    [Fact]
    public async Task SeedDatabaseAsync_ShouldBeIdempotent_VerifyState()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Assert - Should have exactly the default roles (PlatformAdmin, Admin, User)
            var roles = roleManager.Roles.ToList();
            roles.Should().HaveCount(Roles.Defaults.Count);

            // Should have exactly 1 admin user
            var adminUsers = await userManager.Users
                .Where(u => u.Email == "admin@noir.local")
                .ToListAsync();
            adminUsers.Should().HaveCount(1);
        });
    }

    #endregion

    #region Role Configuration Tests

    [Fact]
    public async Task SeedDatabaseAsync_ShouldConfigureAllRolesWithPermissions()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

            // Assert - Both roles should have permissions configured
            foreach (var roleName in Roles.Defaults)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                role.Should().NotBeNull($"Role '{roleName}' should exist");

                var claims = await roleManager.GetClaimsAsync(role!);
                var permissionClaims = claims.Where(c => c.Type == Permissions.ClaimType);
                permissionClaims.Should().NotBeEmpty($"Role '{roleName}' should have permissions");
            }
        });
    }


    #endregion

    #region Default Tenant Seeding Tests

    [Fact]
    public async Task SeedDatabaseAsync_ShouldCreateDefaultTenant()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var tenantContext = services.GetRequiredService<TenantStoreDbContext>();

            // Assert - Default tenant should exist
            var defaultTenant = await tenantContext.TenantInfo
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Identifier == "default");

            defaultTenant.Should().NotBeNull();
            defaultTenant!.IsActive.Should().BeTrue();
            defaultTenant.IsDeleted.Should().BeFalse();
        });
    }

    [Fact]
    public async Task SeedDefaultTenantAsync_WhenTenantSoftDeleted_ShouldRestoreTenant()
    {
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var tenantContext = services.GetRequiredService<TenantStoreDbContext>();
            var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();

            // Arrange - Soft delete the default tenant
            var defaultTenant = await tenantContext.TenantInfo
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Identifier == "default");

            defaultTenant.Should().NotBeNull();

            // Soft delete by mutating tracked entity properties
            defaultTenant!.IsDeleted = true;
            defaultTenant.DeletedAt = DateTimeOffset.UtcNow;
            defaultTenant.DeletedBy = "test-soft-delete";
            await tenantContext.SaveChangesAsync();

            // Verify it's soft-deleted
            var verifyDeleted = await tenantContext.TenantInfo
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Identifier == "default");
            verifyDeleted!.IsDeleted.Should().BeTrue();

            // Act - Re-run the seeder which should restore the tenant
            var settings = new DefaultTenantSettings
            {
                Enabled = true,
                Identifier = "default",
                Name = "Default Tenant"
            };
            await TenantSeeder.SeedDefaultTenantAsync(tenantContext, settings, logger);

            // Assert - Tenant should be restored
            var restoredTenant = await tenantContext.TenantInfo
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Identifier == "default");

            restoredTenant.Should().NotBeNull();
            restoredTenant!.IsDeleted.Should().BeFalse();
            restoredTenant.DeletedAt.Should().BeNull();
            restoredTenant.DeletedBy.Should().BeNull();
            restoredTenant.IsActive.Should().BeTrue();
        });
    }

    #endregion
}
