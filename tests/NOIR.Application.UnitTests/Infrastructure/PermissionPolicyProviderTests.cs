namespace NOIR.Application.UnitTests.Infrastructure;

/// <summary>
/// Unit tests for PermissionPolicyProvider.
/// Tests dynamic policy creation for permission-based authorization.
/// </summary>
public class PermissionPolicyProviderTests
{
    private readonly PermissionPolicyProvider _sut;

    public PermissionPolicyProviderTests()
    {
        var options = Options.Create(new AuthorizationOptions());
        _sut = new PermissionPolicyProvider(options);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldAcceptOptions()
    {
        // Arrange
        var options = Options.Create(new AuthorizationOptions());

        // Act
        var provider = new PermissionPolicyProvider(options);

        // Assert
        provider.Should().NotBeNull();
    }

    [Fact]
    public void Service_ShouldImplementIAuthorizationPolicyProvider()
    {
        // Assert
        _sut.Should().BeAssignableTo<IAuthorizationPolicyProvider>();
    }

    #endregion

    #region GetPolicyAsync Tests

    [Fact]
    public async Task GetPolicyAsync_WithPermissionPrefix_ShouldReturnPolicy()
    {
        // Act
        var policy = await _sut.GetPolicyAsync("Permission:users.read");

        // Assert
        policy.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPolicyAsync_WithPermissionPrefix_PolicyShouldRequireAuthenticatedUser()
    {
        // Act
        var policy = await _sut.GetPolicyAsync("Permission:users.read");

        // Assert
        policy!.Requirements.Should().Contain(r => r is DenyAnonymousAuthorizationRequirement);
    }

    [Fact]
    public async Task GetPolicyAsync_WithPermissionPrefix_PolicyShouldHavePermissionRequirement()
    {
        // Act
        var policy = await _sut.GetPolicyAsync("Permission:users.write");

        // Assert
        policy!.Requirements.Should().Contain(r => r is PermissionRequirement);
    }

    [Fact]
    public async Task GetPolicyAsync_WithPermissionPrefix_PermissionShouldMatchInput()
    {
        // Arrange
        var expectedPermission = "roles.manage";

        // Act
        var policy = await _sut.GetPolicyAsync($"Permission:{expectedPermission}");

        // Assert
        var requirement = policy!.Requirements.OfType<PermissionRequirement>().First();
        requirement.Permission.Should().Be(expectedPermission);
    }

    [Fact]
    public async Task GetPolicyAsync_WithUppercasePrefix_ShouldReturnPolicy()
    {
        // Act (case-insensitive match)
        var policy = await _sut.GetPolicyAsync("PERMISSION:users.read");

        // Assert
        policy.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPolicyAsync_WithMixedCasePrefix_ShouldReturnPolicy()
    {
        // Act
        var policy = await _sut.GetPolicyAsync("PeRmIsSiOn:users.read");

        // Assert
        policy.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPolicyAsync_WithoutPermissionPrefix_ShouldFallbackToDefault()
    {
        // Act
        var policy = await _sut.GetPolicyAsync("SomeOtherPolicy");

        // Assert - Should return null for non-existent fallback policies
        policy.Should().BeNull();
    }

    [Fact]
    public async Task GetPolicyAsync_WithEmptyPermission_ShouldReturnPolicy()
    {
        // Act
        var policy = await _sut.GetPolicyAsync("Permission:");

        // Assert - Should still return a policy even with empty permission
        policy.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPolicyAsync_WithComplexPermission_ShouldExtractCorrectPermission()
    {
        // Arrange
        var complexPermission = "users.admin.super.special.access";

        // Act
        var policy = await _sut.GetPolicyAsync($"Permission:{complexPermission}");

        // Assert
        var requirement = policy!.Requirements.OfType<PermissionRequirement>().First();
        requirement.Permission.Should().Be(complexPermission);
    }

    #endregion

    #region GetDefaultPolicyAsync Tests

    [Fact]
    public async Task GetDefaultPolicyAsync_ShouldReturnPolicy()
    {
        // Act
        var policy = await _sut.GetDefaultPolicyAsync();

        // Assert
        policy.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDefaultPolicyAsync_ShouldReturnPolicyWithRequirements()
    {
        // Act
        var policy = await _sut.GetDefaultPolicyAsync();

        // Assert
        policy.Requirements.Should().NotBeEmpty();
    }

    #endregion

    #region GetFallbackPolicyAsync Tests

    [Fact]
    public async Task GetFallbackPolicyAsync_ShouldNotThrow()
    {
        // Act
        var act = async () => await _sut.GetFallbackPolicyAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetFallbackPolicyAsync_DefaultShouldReturnNull()
    {
        // Act (default AuthorizationOptions has no fallback policy)
        var policy = await _sut.GetFallbackPolicyAsync();

        // Assert
        policy.Should().BeNull();
    }

    #endregion

    #region Method Existence Tests

    [Fact]
    public void GetPolicyAsync_MethodShouldExist()
    {
        // Assert
        var method = typeof(PermissionPolicyProvider).GetMethod("GetPolicyAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void GetDefaultPolicyAsync_MethodShouldExist()
    {
        // Assert
        var method = typeof(PermissionPolicyProvider).GetMethod("GetDefaultPolicyAsync");
        method.Should().NotBeNull();
    }

    [Fact]
    public void GetFallbackPolicyAsync_MethodShouldExist()
    {
        // Assert
        var method = typeof(PermissionPolicyProvider).GetMethod("GetFallbackPolicyAsync");
        method.Should().NotBeNull();
    }

    #endregion

    #region Multiple Permission Scenarios

    [Theory]
    [InlineData("Permission:users.read")]
    [InlineData("Permission:users.write")]
    [InlineData("Permission:roles.manage")]
    [InlineData("Permission:admin.full")]
    [InlineData("Permission:system.config")]
    public async Task GetPolicyAsync_WithVariousPermissions_ShouldReturnValidPolicies(string policyName)
    {
        // Act
        var policy = await _sut.GetPolicyAsync(policyName);

        // Assert
        policy.Should().NotBeNull();
        policy!.Requirements.Should().HaveCountGreaterThanOrEqualTo(2); // DenyAnonymous + PermissionRequirement
    }

    #endregion
}
