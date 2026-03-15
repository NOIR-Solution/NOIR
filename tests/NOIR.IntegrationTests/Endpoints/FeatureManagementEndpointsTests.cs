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

    private async Task<HttpClient> GetPlatformAdminClientAsync()
    {
        // Platform admin is a system user (TenantId = null) — no X-Tenant header needed
        var loginCommand = new LoginCommand("platform@noir.local", "123qwe");
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<Dictionary<string, EffectiveFeatureState>>();
        result.ShouldNotBeNull();
        result.ShouldNotBeEmpty("there should be feature states for the current tenant");

        // Core modules should be present
        result.ShouldContainKey(ModuleNames.Core.Auth);
        result.ShouldContainKey(ModuleNames.Core.Users);
        result.ShouldContainKey(ModuleNames.Core.Dashboard);

        // Core modules should always be effective
        result![ModuleNames.Core.Auth].IsCore.ShouldBeTrue();
        result[ModuleNames.Core.Auth].IsEffective.ShouldBeTrue();
    }

    [Fact]
    public async Task GetCurrentTenantFeatures_Unauthenticated_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/features/current-tenant");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ModuleCatalogDto>();
        result.ShouldNotBeNull();
        result!.Modules.ShouldNotBeEmpty("the catalog should contain module definitions");
        result.Modules.Count.ShouldBeGreaterThanOrEqualTo(30);

        // Verify core modules are present
        result.Modules.ShouldContain(m => m.Name == ModuleNames.Core.Auth && m.IsCore);
        result.Modules.ShouldContain(m => m.Name == ModuleNames.Core.Users && m.IsCore);

        // Verify non-core modules are present
        result.Modules.ShouldContain(m => m.Name == ModuleNames.Content.Blog && !m.IsCore);
        result.Modules.ShouldContain(m => m.Name == ModuleNames.Ecommerce.Products && !m.IsCore);
    }

    [Fact]
    public async Task GetModuleCatalog_Unauthenticated_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/features/catalog");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region ToggleModule Tests

    [Fact]
    public async Task ToggleModule_AsAdmin_ShouldToggleFeature()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var toggleRequest = new { FeatureName = ModuleNames.Content.Blog, IsEnabled = false };

        try
        {
            // Act - Toggle the module off
            var toggleResponse = await adminClient.PutAsJsonAsync("/api/features/toggle", toggleRequest);

            // Assert - Verify the toggle command response is correct
            toggleResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
            var toggleResult = await toggleResponse.Content.ReadFromJsonAsync<TenantFeatureStateDto>();
            toggleResult.ShouldNotBeNull();
            toggleResult!.FeatureName.ShouldBe(ModuleNames.Content.Blog);
            toggleResult.IsEnabled.ShouldBeFalse();

            // Note: We cannot verify via GetCurrentTenantFeatures because the test infrastructure
            // uses AllFeaturesEnabledFeatureChecker which always reports features as enabled.
            // The toggle command itself writes to the DB correctly (verified by the response above).
        }
        finally
        {
            // Always restore to enabled state, even if assertions fail
            var toggleBackRequest = new { FeatureName = ModuleNames.Content.Blog, IsEnabled = true };
            await adminClient.PutAsJsonAsync("/api/features/toggle", toggleBackRequest);
        }
    }

    [Fact]
    public async Task ToggleModule_Unauthenticated_ShouldReturn401()
    {
        // Arrange
        var toggleRequest = new { FeatureName = ModuleNames.Content.Blog, IsEnabled = false };

        // Act
        var response = await _client.PutAsJsonAsync("/api/features/toggle", toggleRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    #endregion

    #region SetModuleAvailability Tests

    [Fact]
    public async Task SetModuleAvailability_AsPlatformAdmin_ShouldSetAvailability()
    {
        // Arrange
        var platformClient = await GetPlatformAdminClientAsync();
        var request = new { FeatureName = ModuleNames.Content.Blog, IsAvailable = false };

        try
        {
            // Act - Set module unavailable
            var response = await platformClient.PutAsJsonAsync("/api/features/tenant/default/availability", request);

            // Assert
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<TenantFeatureStateDto>();
            result.ShouldNotBeNull();
            result!.FeatureName.ShouldBe(ModuleNames.Content.Blog);
            result.IsAvailable.ShouldBeFalse();
        }
        finally
        {
            // Always restore to available state, even if assertions fail
            var cleanupRequest = new { FeatureName = ModuleNames.Content.Blog, IsAvailable = true };
            await platformClient.PutAsJsonAsync("/api/features/tenant/default/availability", cleanupRequest);
        }
    }

    [Fact]
    public async Task SetModuleAvailability_Unauthenticated_ShouldReturn401()
    {
        // Arrange
        var request = new { FeatureName = ModuleNames.Content.Blog, IsAvailable = false };

        // Act
        var response = await _client.PutAsJsonAsync("/api/features/tenant/default/availability", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GetTenantFeatureStates Tests

    [Fact]
    public async Task GetTenantFeatureStates_AsPlatformAdmin_ShouldReturnStates()
    {
        // Arrange
        var platformClient = await GetPlatformAdminClientAsync();

        // Act
        var response = await platformClient.GetAsync("/api/features/tenant/default");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ModuleCatalogDto>();
        result.ShouldNotBeNull();
        result!.Modules.ShouldNotBeEmpty("the catalog should contain modules with tenant-specific states");

        // Core modules should be present
        result.Modules.ShouldContain(m => m.Name == ModuleNames.Core.Auth && m.IsCore);
        result.Modules.ShouldContain(m => m.Name == ModuleNames.Core.Users && m.IsCore);
    }

    [Fact]
    public async Task GetTenantFeatureStates_Unauthenticated_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/features/tenant/default");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion
}
