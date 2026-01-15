using NOIR.Application.Features.Auth.Commands.UpdateUserProfile;
using NOIR.Application.Features.Auth.Queries.GetUserById;
using NOIR.Application.Features.Users.Commands.CreateUser;

namespace NOIR.IntegrationTests.Auth;

/// <summary>
/// Integration tests for UpdateUserProfile endpoint.
/// Verifies full flow including audit logging with before/after diff tracking.
/// </summary>
[Collection("Integration")]
public class UpdateUserProfileTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public UpdateUserProfileTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    private async Task<HttpClient> GetAdminClientAsync()
    {
        var loginCommand = new LoginCommand("admin@noir.local", "123qwe");
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return _factory.CreateAuthenticatedClient(auth!.AccessToken);
    }

    private async Task<(string Email, string Password, AuthResponse Auth)> CreateTestUserAsync(
        string? firstName = null,
        string? lastName = null)
    {
        var adminClient = await GetAdminClientAsync();
        var email = $"test_{Guid.NewGuid():N}@example.com";
        var password = "TestPassword123!";

        var createCommand = new CreateUserCommand(
            Email: email,
            Password: password,
            FirstName: firstName,
            LastName: lastName,
            DisplayName: null,
            RoleNames: null);

        var createResponse = await adminClient.PostAsJsonAsync("/api/users", createCommand);
        createResponse.EnsureSuccessStatusCode();

        // Login as the created user
        var loginCommand = new LoginCommand(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", loginCommand);
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        return (email, password, auth!);
    }

    #region Endpoint Tests

    [Fact]
    public async Task UpdateUserProfile_ValidRequest_ShouldUpdateProfile()
    {
        // Arrange - Create user and login
        var (email, _, auth) = await CreateTestUserAsync("Original", "Name");
        var authenticatedClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act - Update profile
        var updateCommand = new { FirstName = "Updated", LastName = "User" };
        var response = await authenticatedClient.PutAsJsonAsync("/api/auth/me", updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var userProfile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        userProfile.Should().NotBeNull();
        userProfile!.FirstName.Should().Be("Updated");
        userProfile.LastName.Should().Be("User");
        userProfile.Email.Should().Be(email);
    }

    [Fact]
    public async Task UpdateUserProfile_OnlyFirstName_ShouldUpdateOnlyFirstName()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync("Original", "Lastname");
        var authenticatedClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act - Update only first name
        var updateCommand = new { FirstName = "NewFirst", LastName = (string?)null };
        var response = await authenticatedClient.PutAsJsonAsync("/api/auth/me", updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var userProfile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        userProfile!.FirstName.Should().Be("NewFirst");
        userProfile.LastName.Should().Be("Lastname"); // Unchanged
    }

    [Fact]
    public async Task UpdateUserProfile_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var updateCommand = new { FirstName = "Test", LastName = "User" };
        var response = await _client.PutAsJsonAsync("/api/auth/me", updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateUserProfile_TooLongFirstName_ShouldReturnBadRequest()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync("First", "Last");
        var authenticatedClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act - Try to update with too long first name
        var updateCommand = new { FirstName = new string('A', 101), LastName = "User" };
        var response = await authenticatedClient.PutAsJsonAsync("/api/auth/me", updateCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Audit Log Tests

    [Fact]
    public async Task UpdateUserProfile_ShouldCreateEntityAuditLog()
    {
        // Arrange - Create user and get user ID
        var (email, _, auth) = await CreateTestUserAsync("Original", "Name");
        var authenticatedClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act - Update profile
        var updateCommand = new { FirstName = "Audited", LastName = "Change" };
        await authenticatedClient.PutAsJsonAsync("/api/auth/me", updateCommand);

        // Assert - Check entity audit log was created
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();

            // Find the user first
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            user.Should().NotBeNull();

            // Check for entity audit logs for this user
            var auditLogs = await dbContext.EntityAuditLogs
                .Where(a => a.EntityType == "ApplicationUser" && a.EntityId == user!.Id)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();

            // Should have at least one audit log for the update
            auditLogs.Should().NotBeEmpty();

            var latestLog = auditLogs.First();
            latestLog.Operation.Should().Be("Modified");
            latestLog.EntityDiff.Should().NotBeNullOrEmpty();

            // Verify the diff contains the changes
            latestLog.EntityDiff.Should().Contain("FirstName");
        });
    }

    [Fact]
    public async Task UpdateUserProfile_ShouldCaptureBeforeAndAfterValues()
    {
        // Arrange
        var (email, _, auth) = await CreateTestUserAsync("Before", "Test");
        var authenticatedClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act - Update profile
        var updateCommand = new { FirstName = "After", LastName = (string?)null };
        await authenticatedClient.PutAsJsonAsync("/api/auth/me", updateCommand);

        // Assert - Check entity audit log contains before/after values
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();

            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            user.Should().NotBeNull();

            var auditLog = await dbContext.EntityAuditLogs
                .Where(a => a.EntityType == "ApplicationUser" && a.EntityId == user!.Id)
                .Where(a => a.Operation == "Modified")
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            auditLog.Should().NotBeNull();
            auditLog!.EntityDiff.Should().NotBeNullOrEmpty();

            // Parse the diff and verify it has the expected structure
            // Format: {"fieldName": {"from": oldValue, "to": newValue}}
            var diff = JsonSerializer.Deserialize<JsonElement>(auditLog.EntityDiff!);
            diff.ValueKind.Should().Be(JsonValueKind.Object);

            // Should contain FirstName field with from/to values
            diff.TryGetProperty("FirstName", out var firstNameChange).Should().BeTrue();
            firstNameChange.GetProperty("from").GetString().Should().Be("Before");
            firstNameChange.GetProperty("to").GetString().Should().Be("After");
        });
    }

    [Fact]
    public async Task UpdateUserProfile_NoChanges_ShouldNotCreateAuditLog()
    {
        // Arrange
        var (email, _, auth) = await CreateTestUserAsync("Same", "Name");
        var authenticatedClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Get current audit log count
        var initialCount = await _factory.ExecuteWithTenantAsync(async services =>
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            return await dbContext.EntityAuditLogs
                .Where(a => a.EntityType == "ApplicationUser" && a.EntityId == user!.Id)
                .CountAsync();
        });

        // Act - Update with same values
        var updateCommand = new { FirstName = "Same", LastName = "Name" };
        await authenticatedClient.PutAsJsonAsync("/api/auth/me", updateCommand);

        // Assert - No new audit log should be created
        var finalCount = await _factory.ExecuteWithTenantAsync(async services =>
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
            return await dbContext.EntityAuditLogs
                .Where(a => a.EntityType == "ApplicationUser" && a.EntityId == user!.Id)
                .CountAsync();
        });

        finalCount.Should().Be(initialCount);
    }

    [Fact]
    public async Task UpdateUserProfile_ShouldHaveCorrelationId()
    {
        // Arrange
        var (email, _, auth) = await CreateTestUserAsync("First", "Last");
        var authenticatedClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var updateCommand = new { FirstName = "Correlated", LastName = "User" };
        await authenticatedClient.PutAsJsonAsync("/api/auth/me", updateCommand);

        // Assert - Verify correlation ID is set
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);

            var auditLog = await dbContext.EntityAuditLogs
                .Where(a => a.EntityType == "ApplicationUser" && a.EntityId == user!.Id)
                .OrderByDescending(a => a.Timestamp)
                .FirstOrDefaultAsync();

            auditLog.Should().NotBeNull();
            auditLog!.CorrelationId.Should().NotBeNullOrEmpty();
        });
    }

    [Fact]
    public async Task UpdateUserProfile_ShouldCreateHandlerAuditLogWithDtoDiff()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync("Original", "Name");
        var authenticatedClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act - Update profile
        var updateCommand = new { FirstName = "Changed", LastName = "User" };
        await authenticatedClient.PutAsJsonAsync("/api/auth/me", updateCommand);

        // Assert - Check handler audit log has DTO diff
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();

            // Find handler audit log for UpdateUserProfileCommand
            var handlerLog = await dbContext.HandlerAuditLogs
                .Where(h => h.HandlerName == "UpdateUserProfileCommand")
                .OrderByDescending(h => h.StartTime)
                .FirstOrDefaultAsync();

            handlerLog.Should().NotBeNull();
            handlerLog!.IsSuccess.Should().BeTrue();
            handlerLog.OperationType.Should().Be(AuditOperationType.Update.ToString());
            handlerLog.TargetDtoType.Should().Be("UserProfileDto");
            handlerLog.TargetDtoId.Should().NotBeNullOrEmpty("the user ID should be set as target DTO ID");

            // DtoDiff should contain the before/after changes
            // Uses lazy initialization - resolvers are applied on first use
            handlerLog.DtoDiff.Should().NotBeNullOrEmpty("DTO diff should be computed for update operations");

            // Parse the diff to verify structure
            // Format: {"fieldName": {"from": oldValue, "to": newValue}}
            var diff = JsonSerializer.Deserialize<JsonElement>(handlerLog.DtoDiff!);
            diff.ValueKind.Should().Be(JsonValueKind.Object);

            // Should contain firstName field with from/to values (camelCase for DTOs)
            diff.TryGetProperty("firstName", out var firstNameChange).Should().BeTrue("FirstName change should be in the diff");
            firstNameChange.GetProperty("from").GetString().Should().Be("Original");
            firstNameChange.GetProperty("to").GetString().Should().Be("Changed");
        });
    }

    [Fact]
    public async Task UpdateUserProfile_NoChanges_HandlerAuditLogShouldHaveNullDiff()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync("Same", "Name");
        var authenticatedClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act - Update with same values (no actual changes)
        var updateCommand = new { FirstName = "Same", LastName = "Name" };
        await authenticatedClient.PutAsJsonAsync("/api/auth/me", updateCommand);

        // Assert - Handler audit log should have null or empty diff
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();

            var handlerLog = await dbContext.HandlerAuditLogs
                .Where(h => h.HandlerName == "UpdateUserProfileCommand")
                .OrderByDescending(h => h.StartTime)
                .FirstOrDefaultAsync();

            handlerLog.Should().NotBeNull();
            handlerLog!.IsSuccess.Should().BeTrue();

            // When there are no changes, the diff should be null
            // (the diff service returns null for identical objects)
            handlerLog.DtoDiff.Should().BeNull("no changes means no diff");
        });
    }

    #endregion

    #region Handler Audit Log Tests

    [Fact]
    public async Task UpdateUserProfile_ShouldCreateHandlerAuditLogWithCorrelationId()
    {
        // Arrange
        var (_, _, auth) = await CreateTestUserAsync("First", "Last");
        var authenticatedClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var updateCommand = new { FirstName = "Handler", LastName = "Test" };
        await authenticatedClient.PutAsJsonAsync("/api/auth/me", updateCommand);

        // Assert - Handler audit log should have correlation ID
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();

            var handlerLog = await dbContext.HandlerAuditLogs
                .Where(h => h.HandlerName == "UpdateUserProfileCommand")
                .OrderByDescending(h => h.StartTime)
                .FirstOrDefaultAsync();

            handlerLog.Should().NotBeNull();
            handlerLog!.CorrelationId.Should().NotBeNullOrEmpty();
            handlerLog.IsSuccess.Should().BeTrue();

            // Note: HttpRequestAuditLogId is null in Testing environment because
            // HttpRequestAuditMiddleware is disabled to avoid test interference
        });
    }

    #endregion

    #region Security Tests

    [Fact]
    public async Task UpdateUserProfile_ShouldNotAuditSensitiveFields()
    {
        // Arrange - This test verifies that password and security stamps are NOT in audit logs
        var (email, _, auth) = await CreateTestUserAsync("Test", "User");
        var authenticatedClient = _factory.CreateAuthenticatedClient(auth.AccessToken);

        // Act
        var updateCommand = new { FirstName = "Secure", LastName = "User" };
        await authenticatedClient.PutAsJsonAsync("/api/auth/me", updateCommand);

        // Assert - Verify no sensitive data in audit logs
        await _factory.ExecuteWithTenantAsync(async services =>
        {
            var dbContext = services.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);

            var auditLogs = await dbContext.EntityAuditLogs
                .Where(a => a.EntityType == "ApplicationUser" && a.EntityId == user!.Id)
                .ToListAsync();

            foreach (var log in auditLogs)
            {
                if (log.EntityDiff != null)
                {
                    log.EntityDiff.Should().NotContain("Password");
                    log.EntityDiff.Should().NotContain("PasswordHash");
                    log.EntityDiff.Should().NotContain("SecurityStamp");
                    log.EntityDiff.Should().NotContain("ConcurrencyStamp");
                }
            }
        });
    }

    #endregion
}
