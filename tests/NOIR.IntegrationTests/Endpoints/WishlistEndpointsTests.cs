using NOIR.Application.Features.Wishlists.Commands.CreateWishlist;
using NOIR.Application.Features.Wishlists.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for wishlist management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class WishlistEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public WishlistEndpointsTests(CustomWebApplicationFactory factory)
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

    #region Authentication Tests

    [Fact]
    public async Task GetWishlists_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/wishlists");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateWishlist_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new CreateWishlistCommand("Test Wishlist");

        // Act
        var response = await _client.PostAsJsonAsync("/api/wishlists", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWishlistById_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/wishlists/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteWishlist_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.DeleteAsync($"/api/wishlists/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Get Wishlists Tests

    [Fact]
    public async Task GetWishlists_AsAdmin_ShouldReturnOk()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/wishlists");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<WishlistDto>>();
        result.Should().NotBeNull();
    }

    #endregion

    #region Create Wishlist Tests

    [Fact]
    public async Task CreateWishlist_ValidRequest_ShouldReturnCreatedWishlist()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var uniqueName = $"Test Wishlist {Guid.NewGuid():N}";
        var request = new CreateWishlistCommand(uniqueName);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/wishlists", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var wishlist = await response.Content.ReadFromJsonAsync<WishlistDto>();
        wishlist.Should().NotBeNull();
        wishlist!.Name.Should().Be(uniqueName);
    }

    [Fact]
    public async Task CreateWishlist_WithPublicFlag_ShouldReturnPublicWishlist()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var uniqueName = $"Public Wishlist {Guid.NewGuid():N}";
        var request = new CreateWishlistCommand(uniqueName, IsPublic: true);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/wishlists", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var wishlist = await response.Content.ReadFromJsonAsync<WishlistDto>();
        wishlist.Should().NotBeNull();
        wishlist!.Name.Should().Be(uniqueName);
        wishlist.IsPublic.Should().BeTrue();
    }

    #endregion

    #region Get Wishlist By ID Tests

    [Fact]
    public async Task GetWishlistById_ValidId_ShouldReturnWishlist()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a wishlist
        var uniqueName = $"Test Wishlist {Guid.NewGuid():N}";
        var createRequest = new CreateWishlistCommand(uniqueName);
        var createResponse = await adminClient.PostAsJsonAsync("/api/wishlists", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdWishlist = await createResponse.Content.ReadFromJsonAsync<WishlistDto>();

        // Act
        var response = await adminClient.GetAsync($"/api/wishlists/{createdWishlist!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var wishlist = await response.Content.ReadFromJsonAsync<WishlistDetailDto>();
        wishlist.Should().NotBeNull();
        wishlist!.Id.Should().Be(createdWishlist.Id);
        wishlist.Name.Should().Be(uniqueName);
    }

    [Fact]
    public async Task GetWishlistById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/wishlists/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Delete Wishlist Tests

    [Fact]
    public async Task DeleteWishlist_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a non-default wishlist
        var uniqueName = $"Deletable Wishlist {Guid.NewGuid():N}";
        var createRequest = new CreateWishlistCommand(uniqueName);
        var createResponse = await adminClient.PostAsJsonAsync("/api/wishlists", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdWishlist = await createResponse.Content.ReadFromJsonAsync<WishlistDto>();

        // Act
        var response = await adminClient.DeleteAsync($"/api/wishlists/{createdWishlist!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify it's deleted
        var getResponse = await adminClient.GetAsync($"/api/wishlists/{createdWishlist.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteWishlist_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/wishlists/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Analytics Tests

    [Fact]
    public async Task GetWishlistAnalytics_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/wishlists/analytics?topCount=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWishlistAnalytics_AsAdmin_ShouldReturnOk()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/wishlists/analytics?topCount=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<WishlistAnalyticsDto>();
        result.Should().NotBeNull();
    }

    #endregion

    #region Shared Wishlist Tests

    [Fact]
    public async Task GetSharedWishlist_InvalidToken_ShouldReturnNotFound()
    {
        // Act - shared endpoint allows anonymous access
        var response = await _client.GetAsync("/api/wishlists/shared/non-existent-token");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
