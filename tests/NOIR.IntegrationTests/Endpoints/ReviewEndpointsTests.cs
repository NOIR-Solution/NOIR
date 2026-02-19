using NOIR.Application.Common.Models;
using NOIR.Application.Features.Reviews.DTOs;
using NOIR.Web.Endpoints;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for review management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class ReviewEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ReviewEndpointsTests(CustomWebApplicationFactory factory)
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

    #region GET /api/reviews (Admin moderation queue)

    [Fact]
    public async Task GetReviews_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/reviews");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<ReviewDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetReviews_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/reviews");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetReviews_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/reviews?page=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<ReviewDto>>();
        result.Should().NotBeNull();
        result!.PageNumber.Should().Be(1);
        result.Items.Count.Should().BeLessThanOrEqualTo(5);
    }

    #endregion

    #region GET /api/reviews/{id}

    [Fact]
    public async Task GetReviewById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/reviews/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/reviews/{id}/approve

    [Fact]
    public async Task ApproveReview_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.PostAsync($"/api/reviews/{Guid.NewGuid()}/approve", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/reviews/{id}/reject

    [Fact]
    public async Task RejectReview_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new RejectReviewRequest("Test reason");

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/reviews/{Guid.NewGuid()}/reject", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/reviews/{id}/respond

    [Fact]
    public async Task AddAdminResponse_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new AdminResponseRequest("Thank you for your feedback");

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/reviews/{Guid.NewGuid()}/respond", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/reviews/{id}/vote

    [Fact]
    public async Task VoteReview_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new VoteReviewRequest(true);

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/reviews/{Guid.NewGuid()}/vote", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /api/products/{productId}/reviews

    [Fact]
    public async Task GetProductReviews_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var productId = Guid.NewGuid(); // Non-existent product returns empty list, not 404

        // Act
        var response = await adminClient.GetAsync($"/api/products/{productId}/reviews");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<ReviewDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetProductReviews_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}/reviews");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region GET /api/products/{productId}/reviews/stats

    [Fact]
    public async Task GetReviewStats_AsAdmin_ShouldReturnStats()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var productId = Guid.NewGuid(); // Non-existent product returns zero stats

        // Act
        var response = await adminClient.GetAsync($"/api/products/{productId}/reviews/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var stats = await response.Content.ReadFromJsonAsync<ReviewStatsDto>();
        stats.Should().NotBeNull();
        stats!.TotalReviews.Should().Be(0);
    }

    [Fact]
    public async Task GetReviewStats_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}/reviews/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task ReviewEndpoints_WithoutRequiredPermission_ShouldReturnForbidden()
    {
        // Arrange - Create a user without admin permissions
        var adminClient = await GetAdminClientAsync();
        var email = $"test_{Guid.NewGuid():N}@example.com";
        var password = "TestPassword123!";
        var createCommand = new CreateUserCommand(
            Email: email,
            Password: password,
            FirstName: "Test",
            LastName: "User",
            DisplayName: null,
            RoleNames: null);
        var createResponse = await adminClient.PostAsJsonAsync("/api/users", createCommand);
        createResponse.EnsureSuccessStatusCode();

        // Login as unprivileged user
        var loginCommand = new LoginCommand(email, password);
        var loginResult = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var loginResponse = await loginResult.Content.ReadFromJsonAsync<LoginResponse>();
        var userClient = _factory.CreateAuthenticatedClient(loginResponse!.Auth!.AccessToken);

        // Act - Admin moderation queue requires ReviewsRead permission
        var response = await userClient.GetAsync("/api/reviews");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Bulk Operations

    [Fact]
    public async Task BulkApproveReviews_EmptyList_ShouldReturnOk()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new { ReviewIds = Array.Empty<Guid>() };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/reviews/bulk-approve", request);

        // Assert
        // Empty list should either return 200 with 0 count or 400
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BulkRejectReviews_EmptyList_ShouldReturnOk()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new { ReviewIds = Array.Empty<Guid>(), Reason = "Test bulk reject" };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/reviews/bulk-reject", request);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    #endregion
}
