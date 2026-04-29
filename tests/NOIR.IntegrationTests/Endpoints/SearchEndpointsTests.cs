using NOIR.Application.Features.Search.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for global search endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class SearchEndpointsTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SearchEndpointsTests(CustomWebApplicationFactory factory)
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

    #region GET /api/search

    [Fact]
    public async Task GlobalSearch_AsAdmin_ShouldReturnResults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/search?q=test");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GlobalSearchResponseDto>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GlobalSearch_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/search?q=test");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GlobalSearch_EmptyQuery_ShouldReturnEmptyResults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/search?q=");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GlobalSearchResponseDto>();
        result.ShouldNotBeNull();
        result!.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GlobalSearch_ShortQuery_ShouldReturnEmptyResults()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act - Query too short (less than 2 chars)
        var response = await adminClient.GetAsync("/api/search?q=a");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<GlobalSearchResponseDto>();
        result.ShouldNotBeNull();
        result!.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task GlobalSearch_WithValidQuery_ShouldReturn200()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/search?q=admin");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion
}
