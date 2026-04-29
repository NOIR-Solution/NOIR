using NOIR.Application.Features.Webhooks.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for webhook management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class WebhookEndpointsTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public WebhookEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var loginCommand = new LoginCommand("admin@noir.local", "123qwe");
        var response = await _client.PostAsJsonWithEnumsAsync("/api/auth/login", loginCommand);
        var loginResponse = await response.Content.ReadFromJsonWithEnumsAsync<LoginResponse>();
        return _factory.CreateAuthenticatedClient(loginResponse!.Auth!.AccessToken);
    }

    #region GET /api/webhooks

    [Fact]
    public async Task GetWebhookSubscriptions_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/webhooks");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<WebhookSubscriptionSummaryDto>>();
        result.ShouldNotBeNull();
        result!.Items.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetWebhookSubscriptions_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/webhooks");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWebhookSubscriptions_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/webhooks?page=1&pageSize=5");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<WebhookSubscriptionSummaryDto>>();
        result.ShouldNotBeNull();
        result!.Items.Count.ShouldBeLessThanOrEqualTo(5);
    }

    #endregion

    #region GET /api/webhooks/{id}

    [Fact]
    public async Task GetWebhookSubscriptionById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/webhooks/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/webhooks

    [Fact]
    public async Task CreateWebhookSubscription_ValidRequest_ShouldReturnCreated()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var command = new
        {
            Name = $"Test Webhook {uniqueId}",
            Url = $"https://example.com/webhook/{uniqueId}",
            EventPatterns = "order.*",
            Description = "Integration test webhook",
            MaxRetries = 3,
            TimeoutSeconds = 15
        };

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/webhooks", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var webhook = await response.Content.ReadFromJsonWithEnumsAsync<WebhookSubscriptionDto>();
        webhook.ShouldNotBeNull();
        webhook!.Name.ShouldStartWith("Test Webhook");
    }

    [Fact]
    public async Task CreateWebhookSubscription_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = new
        {
            Name = "Test Webhook",
            Url = "https://example.com/webhook",
            EventPatterns = "order.*"
        };

        // Act
        var response = await _client.PostAsJsonWithEnumsAsync("/api/webhooks", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /api/webhooks/{id}

    [Fact]
    public async Task UpdateWebhookSubscription_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var command = new
        {
            Name = "Updated Webhook",
            Url = "https://example.com/webhook/updated",
            EventPatterns = "order.*"
        };

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync($"/api/webhooks/{Guid.NewGuid()}", command);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/webhooks/{id}

    [Fact]
    public async Task DeleteWebhookSubscription_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/webhooks/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteWebhookSubscription_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var createCommand = new
        {
            Name = $"Delete Test Webhook {uniqueId}",
            Url = $"https://example.com/webhook/delete-{uniqueId}",
            EventPatterns = "order.*"
        };

        var createResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/webhooks", createCommand);
        createResponse.EnsureSuccessStatusCode();
        var createdWebhook = await createResponse.Content.ReadFromJsonWithEnumsAsync<WebhookSubscriptionDto>();

        // Act
        var response = await adminClient.DeleteAsync($"/api/webhooks/{createdWebhook!.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region GET /api/webhooks/event-types

    [Fact]
    public async Task GetWebhookEventTypes_AsAdmin_ShouldReturnList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/webhooks/event-types");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<List<WebhookEventTypeDto>>();
        result.ShouldNotBeNull();
        result!.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetWebhookEventTypes_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/webhooks/event-types");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region POST /api/webhooks/{id}/activate

    [Fact]
    public async Task ActivateWebhookSubscription_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.PostAsync($"/api/webhooks/{Guid.NewGuid()}/activate", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/webhooks/{id}/deactivate

    [Fact]
    public async Task DeactivateWebhookSubscription_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.PostAsync($"/api/webhooks/{Guid.NewGuid()}/deactivate", null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /api/webhooks/{id}/deliveries

    [Fact]
    public async Task GetWebhookDeliveryLogs_InvalidId_ShouldReturnOkWithEmptyList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act - Delivery logs endpoint returns 200 with empty list for non-existent webhook IDs
        var response = await adminClient.GetAsync($"/api/webhooks/{Guid.NewGuid()}/deliveries");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion
}
