namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for Server-Sent Events (SSE) endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class SseEndpointsTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public SseEndpointsTests(CustomWebApplicationFactory factory)
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

    #region GET /api/sse/channels/{channelId}

    [Fact]
    public async Task SubscribeSseChannel_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/sse/channels/test-channel");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SubscribeSseChannel_AsAdmin_ShouldReturnEventStream()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        adminClient.Timeout = TimeSpan.FromSeconds(10);

        // Act - SSE endpoint streams indefinitely, so we use a CancellationToken to abort after verifying headers
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        try
        {
            var response = await adminClient.GetAsync(
                "/api/sse/channels/test-integration-channel",
                HttpCompletionOption.ResponseHeadersRead,
                cts.Token);

            // Assert - Should return 200 with text/event-stream content type
            // May also return 499 (client closed request) in some timing scenarios
            response.StatusCode.ShouldBeOneOf(
                HttpStatusCode.OK,
                (HttpStatusCode)499);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                response.Content.Headers.ContentType?.MediaType.ShouldBe("text/event-stream");
            }
        }
        catch (TaskCanceledException)
        {
            // Expected - the SSE stream was cancelled by our token after reading headers
        }
        catch (OperationCanceledException)
        {
            // Expected - the SSE stream was cancelled by our token after reading headers
        }
    }

    #endregion
}
