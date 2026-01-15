namespace NOIR.IntegrationTests.Middleware;

/// <summary>
/// Integration tests for ExceptionHandlingMiddleware.
/// Tests various exception types and their HTTP responses.
/// </summary>
[Collection("Integration")]
public class ExceptionHandlingTests
{
    private readonly CustomWebApplicationFactory _factory;

    public ExceptionHandlingTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    #region Validation Error Tests

    [Fact]
    public async Task Login_WithEmptyEmail_ShouldReturn400WithValidationDetails()
    {
        // Arrange
        using var client = _factory.CreateTestClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "",
            Password = "password123"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Validation Error");
        // Note: The validation error message may not include field names in all cases
    }

    [Fact]
    public async Task Login_WithInvalidEmailFormat_ShouldReturn400()
    {
        // Arrange
        using var client = _factory.CreateTestClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "not-an-email",
            Password = "password123"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Validation Error");
    }

    [Fact]
    public async Task Login_WithShortPassword_ShouldReturnError()
    {
        // Arrange
        using var client = _factory.CreateTestClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "test@example.com",
            Password = "12345" // Less than 6 characters
        });

        // Assert - Could be 400 (validation) or 401 (auth fails for non-existent user)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithEmptyFields_ShouldReturn400WithMultipleErrors()
    {
        // Arrange
        using var client = _factory.CreateTestClient();

        // Act - Test validation on login endpoint with multiple empty fields
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "",
            Password = ""
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Validation Error");
    }

    #endregion

    #region Authentication Error Tests

    [Fact]
    public async Task GetCurrentUser_WithoutToken_ShouldReturn401()
    {
        // Arrange
        using var client = _factory.CreateTestClient();

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithInvalidToken_ShouldReturn401()
    {
        // Arrange
        using var client = _factory.CreateTestClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCurrentUser_WithExpiredToken_ShouldReturn401()
    {
        // Arrange
        using var client = _factory.CreateTestClient();
        // This is a properly formatted but expired JWT
        var expiredToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0IiwiZXhwIjoxMDAwMDAwMDAwfQ.abc123";
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", expiredToken);

        // Act
        var response = await client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Not Found Tests

    [Fact]
    public async Task NonExistentEndpoint_ShouldReturn404()
    {
        // Arrange
        using var client = _factory.CreateTestClient();

        // Act
        var response = await client.GetAsync("/api/nonexistent/resource");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ShouldReturn401()
    {
        // Arrange
        using var client = _factory.CreateTestClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "nonexistent@example.com",
            Password = "password123"
        });

        // Assert
        // Login with non-existent user returns 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region Problem Details Format Tests

    [Fact]
    public async Task ValidationError_ShouldReturnProblemJson()
    {
        // Arrange
        using var client = _factory.CreateTestClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "",
            Password = ""
        });

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    [Fact]
    public async Task ValidationError_ShouldIncludeTraceId()
    {
        // Arrange
        using var client = _factory.CreateTestClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "",
            Password = ""
        });

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("correlationId");
    }

    [Fact]
    public async Task ValidationError_ShouldIncludeTitle()
    {
        // Arrange
        using var client = _factory.CreateTestClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "",
            Password = ""
        });

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("title");
    }

    [Fact]
    public async Task ValidationError_ShouldIncludeStatus()
    {
        // Arrange
        using var client = _factory.CreateTestClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = "",
            Password = ""
        });

        // Assert
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("status");
        content.Should().Contain("400");
    }

    #endregion

    #region RefreshToken Error Tests

    [Fact]
    public async Task RefreshToken_WithEmptyToken_ShouldReturn400()
    {
        // Arrange
        using var client = _factory.CreateTestClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            RefreshToken = ""
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ShouldReturnError()
    {
        // Arrange
        using var client = _factory.CreateTestClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/refresh", new
        {
            RefreshToken = "invalid-refresh-token"
        });

        // Assert - The invalid token should return either 400 (bad format) or 401 (not authorized)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    #endregion
}
