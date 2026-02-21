using NOIR.Application.Features.FeatureManagement.DTOs;
using NOIR.Application.Modules;
using NOIR.Domain.Interfaces;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for feature management endpoints.
/// Tests the full HTTP request/response cycle for module catalog, tenant features, and toggling.
/// </summary>
[Collection("Integration")]
public class FeatureManagementEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FeatureManagementEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var loginCommand = new LoginCommand("admin@noir.local", "123qwe");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return _factory.CreateAuthenticatedClient(loginResponse!.Auth!.AccessToken);
    }

    #region GetCurrentTenantFeatures Tests

    [Fact]
    public async Task GetCurrentTenantFeatures_AsAdmin_ShouldReturnFeatureStates()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/features/current-tenant");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, EffectiveFeatureState>>();
        result.Should().NotBeNull();
        result.Should().NotBeEmpty("there should be feature states for the current tenant");

        // Core modules should be present
        result.Should().ContainKey(ModuleNames.Core.Auth);
        result.Should().ContainKey(ModuleNames.Core.Users);
        result.Should().ContainKey(ModuleNames.Core.Dashboard);

        // Core modules should always be effective
        result![ModuleNames.Core.Auth].IsCore.Should().BeTrue();
        result[ModuleNames.Core.Auth].IsEffective.Should().BeTrue();
    }

    [Fact]
    public async Task GetCurrentTenantFeatures_Unauthenticated_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/features/current-tenant");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetModuleCatalog Tests

    [Fact]
    public async Task GetModuleCatalog_AsAdmin_ShouldReturnModules()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/features/catalog");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ModuleCatalogDto>();
        result.Should().NotBeNull();
        result!.Modules.Should().NotBeEmpty("the catalog should contain module definitions");
        result.Modules.Count.Should().BeGreaterThanOrEqualTo(30,
            because: "there should be at least 30 module definitions");

        // Verify core modules are present
        result.Modules.Should().Contain(m => m.Name == ModuleNames.Core.Auth && m.IsCore);
        result.Modules.Should().Contain(m => m.Name == ModuleNames.Core.Users && m.IsCore);

        // Verify non-core modules are present
        result.Modules.Should().Contain(m => m.Name == ModuleNames.Content.Blog && !m.IsCore);
        result.Modules.Should().Contain(m => m.Name == ModuleNames.Ecommerce.Products && !m.IsCore);
    }

    [Fact]
    public async Task GetModuleCatalog_Unauthenticated_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/features/catalog");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region ToggleModule Tests

    [Fact]
    public async Task ToggleModule_AsAdmin_ShouldToggleFeature()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var toggleRequest = new { FeatureName = ModuleNames.Content.Blog, IsEnabled = false };

        // Act - Toggle the module off
        var toggleResponse = await adminClient.PutAsJsonAsync("/api/features/toggle", toggleRequest);

        // Assert
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggleResult = await toggleResponse.Content.ReadFromJsonAsync<TenantFeatureStateDto>();
        toggleResult.Should().NotBeNull();
        toggleResult!.FeatureName.Should().Be(ModuleNames.Content.Blog);
        toggleResult.IsEnabled.Should().BeFalse();

        // Verify via GetCurrentTenantFeatures that the state persisted
        var featuresResponse = await adminClient.GetAsync("/api/features/current-tenant");
        featuresResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var features = await featuresResponse.Content.ReadFromJsonAsync<Dictionary<string, EffectiveFeatureState>>();
        features.Should().ContainKey(ModuleNames.Content.Blog);
        features![ModuleNames.Content.Blog].IsEnabled.Should().BeFalse();

        // Act - Toggle back on for cleanup
        var toggleBackRequest = new { FeatureName = ModuleNames.Content.Blog, IsEnabled = true };
        var toggleBackResponse = await adminClient.PutAsJsonAsync("/api/features/toggle", toggleBackRequest);
        toggleBackResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ToggleModule_Unauthenticated_ShouldReturn401()
    {
        // Arrange
        var toggleRequest = new { FeatureName = ModuleNames.Content.Blog, IsEnabled = false };

        // Act
        var response = await _client.PutAsJsonAsync("/api/features/toggle", toggleRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ToggleModule_CoreModule_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var toggleRequest = new { FeatureName = ModuleNames.Core.Auth, IsEnabled = false };

        // Act - Attempt to toggle a core module
        var response = await adminClient.PutAsJsonAsync("/api/features/toggle", toggleRequest);

        // Assert - Core modules cannot be toggled
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ToggleModule_InvalidFeatureName_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var toggleRequest = new { FeatureName = "NonExistent.Module", IsEnabled = false };

        // Act
        var response = await adminClient.PutAsJsonAsync("/api/features/toggle", toggleRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
