using NOIR.Application.Features.ApiKeys.Commands.CreateApiKey;
using NOIR.Application.Features.ApiKeys.Commands.UpdateApiKey;
using NOIR.Application.Features.ApiKeys.Commands.RevokeApiKey;
using NOIR.Application.Features.ApiKeys.DTOs;
using NOIR.Web.Authentication;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for API Key management endpoints.
/// Tests all 7 endpoints: 5 user self-service + 2 admin management.
/// Also tests API Key authentication flow (X-API-Key + X-API-Secret headers).
/// </summary>
[Collection("Integration")]
public class ApiKeyEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ApiKeyEndpointsTests(CustomWebApplicationFactory factory)
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

    // =========================================================================
    // GET /api/auth/me/api-keys — List My API Keys
    // =========================================================================
    #region GET /api/auth/me/api-keys

    [Fact]
    public async Task GetMyApiKeys_AsAuthenticated_ShouldReturnList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/auth/me/api-keys");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ApiKeyDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetMyApiKeys_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me/api-keys");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyApiKeys_AfterCreating_ShouldIncludeNewKey()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var created = await CreateTestApiKeyAsync(adminClient);

        // Act
        var response = await adminClient.GetAsync("/api/auth/me/api-keys");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var keys = await response.Content.ReadFromJsonAsync<List<ApiKeyDto>>();
        keys.Should().Contain(k => k.Id == created.Id);
    }

    #endregion

    // =========================================================================
    // POST /api/auth/me/api-keys — Create API Key
    // =========================================================================
    #region POST /api/auth/me/api-keys

    [Fact]
    public async Task CreateApiKey_ValidRequest_ShouldReturnKeyAndSecret()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var command = CreateTestApiKeyCommand();

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/auth/me/api-keys", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiKeyCreatedDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be(command.Name);
        result.KeyIdentifier.Should().StartWith("noir_key_");
        result.Secret.Should().StartWith("noir_secret_");
        result.Secret.Should().NotBeNullOrEmpty();
        result.Permissions.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateApiKey_WithPermissions_ShouldStorePermissions()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var permissions = new List<string> { "products:read", "orders:read" };
        var command = new CreateApiKeyCommand(
            $"Scoped Key {Guid.NewGuid().ToString("N")[..8]}",
            "Key with limited permissions",
            permissions,
            null);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/auth/me/api-keys", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiKeyCreatedDto>();
        result!.Permissions.Should().BeEquivalentTo(permissions);
    }

    [Fact]
    public async Task CreateApiKey_WithExpiration_ShouldSetExpiry()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(30);
        var command = new CreateApiKeyCommand(
            $"Expiring Key {Guid.NewGuid().ToString("N")[..8]}",
            null,
            new List<string> { "products:read" },
            expiresAt);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/auth/me/api-keys", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiKeyCreatedDto>();
        result!.ExpiresAt.Should().NotBeNull();
        result.ExpiresAt!.Value.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CreateApiKey_EmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var command = new CreateApiKeyCommand("", null, new List<string>(), null);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/auth/me/api-keys", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateApiKey_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var command = CreateTestApiKeyCommand();

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/me/api-keys", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    // =========================================================================
    // PUT /api/auth/me/api-keys/{id} — Update API Key
    // =========================================================================
    #region PUT /api/auth/me/api-keys/{id}

    [Fact]
    public async Task UpdateApiKey_ValidRequest_ShouldReturnUpdated()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var created = await CreateTestApiKeyAsync(adminClient);
        var updateCommand = new UpdateApiKeyCommand(
            created.Id,
            "Updated Key Name",
            "Updated description",
            new List<string> { "products:read" });

        // Act
        var response = await adminClient.PutAsJsonAsync(
            $"/api/auth/me/api-keys/{created.Id}", updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiKeyDto>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Key Name");
        result.Description.Should().Be("Updated description");
    }

    [Fact]
    public async Task UpdateApiKey_NonexistentId_ShouldReturnNotFoundOrBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var fakeId = Guid.NewGuid();
        var updateCommand = new UpdateApiKeyCommand(
            fakeId, "Name", null, new List<string>());

        // Act
        var response = await adminClient.PutAsJsonAsync(
            $"/api/auth/me/api-keys/{fakeId}", updateCommand);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateApiKey_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.PutAsJsonAsync(
            $"/api/auth/me/api-keys/{Guid.NewGuid()}",
            new UpdateApiKeyCommand(Guid.NewGuid(), "Name", null, new List<string>()));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    // =========================================================================
    // POST /api/auth/me/api-keys/{id}/rotate — Rotate Secret
    // =========================================================================
    #region POST /api/auth/me/api-keys/{id}/rotate

    [Fact]
    public async Task RotateApiKey_ValidKey_ShouldReturnNewSecret()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var created = await CreateTestApiKeyAsync(adminClient);
        var originalSecret = created.Secret;

        // Act
        var response = await adminClient.PostAsync(
            $"/api/auth/me/api-keys/{created.Id}/rotate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiKeyRotatedDto>();
        result.Should().NotBeNull();
        result!.Secret.Should().NotBeNullOrEmpty();
        result.Secret.Should().NotBe(originalSecret);
        result.Secret.Should().StartWith("noir_secret_");
        result.KeyIdentifier.Should().Be(created.KeyIdentifier);
    }

    [Fact]
    public async Task RotateApiKey_NonexistentId_ShouldReturnNotFoundOrBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.PostAsync(
            $"/api/auth/me/api-keys/{Guid.NewGuid()}/rotate", null);

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    // =========================================================================
    // POST /api/auth/me/api-keys/{id}/revoke — Revoke API Key (User)
    // =========================================================================
    #region POST /api/auth/me/api-keys/{id}/revoke

    [Fact]
    public async Task RevokeApiKey_ValidKey_ShouldMarkRevoked()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var created = await CreateTestApiKeyAsync(adminClient);

        // Act
        var response = await adminClient.PostAsJsonAsync(
            $"/api/auth/me/api-keys/{created.Id}/revoke",
            new RevokeApiKeyCommand(created.Id, "No longer needed"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiKeyDto>();
        result.Should().NotBeNull();
        result!.IsRevoked.Should().BeTrue();
        result.RevokedAt.Should().NotBeNull();
        result.RevokedReason.Should().Be("No longer needed");
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task RevokeApiKey_WithoutReason_ShouldStillWork()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var created = await CreateTestApiKeyAsync(adminClient);

        // Act
        var response = await adminClient.PostAsJsonAsync(
            $"/api/auth/me/api-keys/{created.Id}/revoke",
            new RevokeApiKeyCommand(created.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiKeyDto>();
        result!.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeApiKey_AlreadyRevoked_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var created = await CreateTestApiKeyAsync(adminClient);
        await adminClient.PostAsJsonAsync(
            $"/api/auth/me/api-keys/{created.Id}/revoke",
            new RevokeApiKeyCommand(created.Id));

        // Act — revoke again
        var response = await adminClient.PostAsJsonAsync(
            $"/api/auth/me/api-keys/{created.Id}/revoke",
            new RevokeApiKeyCommand(created.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    // =========================================================================
    // GET /api/admin/api-keys — Admin: List All Tenant Keys
    // =========================================================================
    #region GET /api/admin/api-keys

    [Fact]
    public async Task GetTenantApiKeys_AsAdmin_ShouldReturnAllKeys()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var created = await CreateTestApiKeyAsync(adminClient);

        // Act
        var response = await adminClient.GetAsync("/api/admin/api-keys");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var keys = await response.Content.ReadFromJsonAsync<List<ApiKeyDto>>();
        keys.Should().NotBeNull();
        keys.Should().Contain(k => k.Id == created.Id);
    }

    [Fact]
    public async Task GetTenantApiKeys_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/admin/api-keys");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    // =========================================================================
    // POST /api/admin/api-keys/{id}/revoke — Admin: Revoke Any Key
    // =========================================================================
    #region POST /api/admin/api-keys/{id}/revoke

    [Fact]
    public async Task AdminRevokeApiKey_ValidKey_ShouldRevoke()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var created = await CreateTestApiKeyAsync(adminClient);

        // Act
        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/api-keys/{created.Id}/revoke",
            new RevokeApiKeyCommand(created.Id, "Admin revocation"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ApiKeyDto>();
        result!.IsRevoked.Should().BeTrue();
        result.RevokedReason.Should().Be("Admin revocation");
    }

    [Fact]
    public async Task AdminRevokeApiKey_NonexistentId_ShouldReturnNotFoundOrBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.PostAsJsonAsync(
            $"/api/admin/api-keys/{Guid.NewGuid()}/revoke",
            new RevokeApiKeyCommand(Guid.NewGuid()));

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }

    #endregion

    // =========================================================================
    // API Key Authentication Flow (X-API-Key + X-API-Secret headers)
    // =========================================================================
    #region API Key Authentication

    [Fact]
    public async Task ApiKeyAuth_ValidKeyAndSecret_ShouldAuthenticate()
    {
        // Arrange — create key and capture secret
        var adminClient = await GetAdminClientAsync();
        var created = await CreateTestApiKeyAsync(adminClient);

        // Act — use API key headers to call a protected endpoint
        var apiKeyClient = _factory.CreateTestClient();
        apiKeyClient.DefaultRequestHeaders.Add(
            ApiKeyAuthenticationHandler.ApiKeyHeaderName, created.KeyIdentifier);
        apiKeyClient.DefaultRequestHeaders.Add(
            ApiKeyAuthenticationHandler.ApiSecretHeaderName, created.Secret);
        var response = await apiKeyClient.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApiKeyAuth_InvalidSecret_ShouldReturnUnauthorized()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var created = await CreateTestApiKeyAsync(adminClient);

        // Act
        var apiKeyClient = _factory.CreateTestClient();
        apiKeyClient.DefaultRequestHeaders.Add(
            ApiKeyAuthenticationHandler.ApiKeyHeaderName, created.KeyIdentifier);
        apiKeyClient.DefaultRequestHeaders.Add(
            ApiKeyAuthenticationHandler.ApiSecretHeaderName, "noir_secret_invalid");
        var response = await apiKeyClient.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApiKeyAuth_RevokedKey_ShouldReturnUnauthorized()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var created = await CreateTestApiKeyAsync(adminClient);
        var secret = created.Secret;

        // Revoke the key
        await adminClient.PostAsJsonAsync(
            $"/api/auth/me/api-keys/{created.Id}/revoke",
            new RevokeApiKeyCommand(created.Id));

        // Act — try to authenticate with revoked key
        var apiKeyClient = _factory.CreateTestClient();
        apiKeyClient.DefaultRequestHeaders.Add(
            ApiKeyAuthenticationHandler.ApiKeyHeaderName, created.KeyIdentifier);
        apiKeyClient.DefaultRequestHeaders.Add(
            ApiKeyAuthenticationHandler.ApiSecretHeaderName, secret);
        var response = await apiKeyClient.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApiKeyAuth_RotatedSecret_OldSecretShouldFail()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var created = await CreateTestApiKeyAsync(adminClient);
        var oldSecret = created.Secret;

        // Rotate the secret
        var rotateResponse = await adminClient.PostAsync(
            $"/api/auth/me/api-keys/{created.Id}/rotate", null);
        rotateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act — try old secret
        var apiKeyClient = _factory.CreateTestClient();
        apiKeyClient.DefaultRequestHeaders.Add(
            ApiKeyAuthenticationHandler.ApiKeyHeaderName, created.KeyIdentifier);
        apiKeyClient.DefaultRequestHeaders.Add(
            ApiKeyAuthenticationHandler.ApiSecretHeaderName, oldSecret);
        var response = await apiKeyClient.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApiKeyAuth_RotatedSecret_NewSecretShouldWork()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var created = await CreateTestApiKeyAsync(adminClient);

        // Rotate
        var rotateResponse = await adminClient.PostAsync(
            $"/api/auth/me/api-keys/{created.Id}/rotate", null);
        var rotated = await rotateResponse.Content.ReadFromJsonAsync<ApiKeyRotatedDto>();

        // Act — use new secret
        var apiKeyClient = _factory.CreateTestClient();
        apiKeyClient.DefaultRequestHeaders.Add(
            ApiKeyAuthenticationHandler.ApiKeyHeaderName, rotated!.KeyIdentifier);
        apiKeyClient.DefaultRequestHeaders.Add(
            ApiKeyAuthenticationHandler.ApiSecretHeaderName, rotated.Secret);
        var response = await apiKeyClient.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ApiKeyAuth_MissingHeaders_ShouldReturnUnauthorized()
    {
        // Act — no API key headers, no JWT
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ApiKeyAuth_OnlyKeyNoSecret_ShouldReturnUnauthorized()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var created = await CreateTestApiKeyAsync(adminClient);

        // Act
        var apiKeyClient = _factory.CreateTestClient();
        apiKeyClient.DefaultRequestHeaders.Add(
            ApiKeyAuthenticationHandler.ApiKeyHeaderName, created.KeyIdentifier);
        var response = await apiKeyClient.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    // =========================================================================
    // Full CRUD Lifecycle
    // =========================================================================
    #region Lifecycle

    [Fact]
    public async Task ApiKey_FullLifecycle_CreateUpdateRotateRevokeVerify()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // 1. CREATE
        var createCommand = CreateTestApiKeyCommand();
        var createResponse = await adminClient.PostAsJsonAsync("/api/auth/me/api-keys", createCommand);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiKeyCreatedDto>();
        created!.KeyIdentifier.Should().StartWith("noir_key_");

        // 2. READ — verify in list
        var listResponse = await adminClient.GetAsync("/api/auth/me/api-keys");
        var keys = await listResponse.Content.ReadFromJsonAsync<List<ApiKeyDto>>();
        keys.Should().Contain(k => k.Id == created.Id);

        // 3. UPDATE — change name and permissions
        var updateCommand = new UpdateApiKeyCommand(
            created.Id, "Lifecycle Updated", "Updated in lifecycle test",
            new List<string> { "products:read" });
        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/auth/me/api-keys/{created.Id}", updateCommand);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiKeyDto>();
        updated!.Name.Should().Be("Lifecycle Updated");

        // 4. ROTATE — new secret
        var rotateResponse = await adminClient.PostAsync(
            $"/api/auth/me/api-keys/{created.Id}/rotate", null);
        rotateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotated = await rotateResponse.Content.ReadFromJsonAsync<ApiKeyRotatedDto>();
        rotated!.Secret.Should().NotBe(created.Secret);

        // 5. REVOKE
        var revokeResponse = await adminClient.PostAsJsonAsync(
            $"/api/auth/me/api-keys/{created.Id}/revoke",
            new RevokeApiKeyCommand(created.Id, "Lifecycle test complete"));
        revokeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var revoked = await revokeResponse.Content.ReadFromJsonAsync<ApiKeyDto>();
        revoked!.IsRevoked.Should().BeTrue();
        revoked.IsActive.Should().BeFalse();

        // 6. VERIFY — revoked key appears in admin list as revoked
        var adminListResponse = await adminClient.GetAsync("/api/admin/api-keys");
        var adminKeys = await adminListResponse.Content.ReadFromJsonAsync<List<ApiKeyDto>>();
        var revokedKey = adminKeys!.FirstOrDefault(k => k.Id == created.Id);
        revokedKey.Should().NotBeNull();
        revokedKey!.IsRevoked.Should().BeTrue();
    }

    #endregion

    // =========================================================================
    // DI Verification (CLAUDE.md Rule #21)
    // =========================================================================
    #region DI Verification

    [Fact]
    public async Task IRepository_ApiKey_ShouldResolveFromDI()
    {
        await _factory.ExecuteWithTenantAsync(sp =>
        {
            var repository = sp.GetRequiredService<IRepository<ApiKey, Guid>>();
            repository.Should().NotBeNull();
            return Task.CompletedTask;
        });
    }

    #endregion

    // =========================================================================
    // Helpers
    // =========================================================================
    #region Helpers

    /// <summary>
    /// Creates a test API key and returns the full creation response (with plaintext secret).
    /// Automatically revokes excess keys to stay within the max active limit.
    /// </summary>
    private async Task<ApiKeyCreatedDto> CreateTestApiKeyAsync(HttpClient adminClient)
    {
        // Revoke old test keys to avoid hitting the max active API keys limit (10)
        var listResponse = await adminClient.GetAsync("/api/auth/me/api-keys");
        var existingKeys = await listResponse.Content.ReadFromJsonAsync<List<ApiKeyDto>>();
        if (existingKeys is { Count: >= 9 })
        {
            foreach (var key in existingKeys.Where(k => k.IsActive && k.Name.StartsWith("Test Key")))
            {
                await adminClient.PostAsJsonAsync(
                    $"/api/auth/me/api-keys/{key.Id}/revoke",
                    new RevokeApiKeyCommand(key.Id, "Test cleanup"));
            }
        }

        var command = CreateTestApiKeyCommand();
        var response = await adminClient.PostAsJsonAsync("/api/auth/me/api-keys", command);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"CreateTestApiKeyAsync failed ({response.StatusCode}): {body}");
        }
        return (await response.Content.ReadFromJsonAsync<ApiKeyCreatedDto>())!;
    }

    private static CreateApiKeyCommand CreateTestApiKeyCommand()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return new CreateApiKeyCommand(
            $"Test Key {uniqueId}",
            "Integration test API key",
            new List<string> { "products:read" },
            null);
    }

    #endregion
}
