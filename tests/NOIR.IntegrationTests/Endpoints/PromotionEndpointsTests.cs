using NOIR.Application.Common.Models;
using NOIR.Application.Features.Promotions.Commands.CreatePromotion;
using NOIR.Application.Features.Promotions.Commands.UpdatePromotion;
using NOIR.Application.Features.Promotions.DTOs;
using NOIR.Domain.Enums;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for promotion management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class PromotionEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public PromotionEndpointsTests(CustomWebApplicationFactory factory)
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

    #region GET /api/promotions

    [Fact]
    public async Task GetPromotions_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/promotions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<PromotionDto>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPromotions_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/promotions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetPromotions_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/promotions?page=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<PromotionDto>>();
        result.Should().NotBeNull();
        result!.PageNumber.Should().Be(1);
        result.Items.Count.Should().BeLessThanOrEqualTo(5);
    }

    #endregion

    #region GET /api/promotions/{id}

    [Fact]
    public async Task GetPromotionById_ValidId_ShouldReturnPromotion()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var createRequest = CreateTestPromotionCommand();
        var createResponse = await adminClient.PostAsJsonAsync("/api/promotions", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonWithEnumsAsync<PromotionDto>();

        // Act
        var response = await adminClient.GetAsync($"/api/promotions/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var promotion = await response.Content.ReadFromJsonWithEnumsAsync<PromotionDto>();
        promotion.Should().NotBeNull();
        promotion!.Id.Should().Be(created.Id);
        promotion.Name.Should().Be(createRequest.Name);
    }

    [Fact]
    public async Task GetPromotionById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/promotions/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/promotions

    [Fact]
    public async Task CreatePromotion_ValidRequest_ShouldReturnCreatedPromotion()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestPromotionCommand();

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/promotions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var promotion = await response.Content.ReadFromJsonWithEnumsAsync<PromotionDto>();
        promotion.Should().NotBeNull();
        promotion!.Name.Should().Be(request.Name);
        promotion.Code.Should().Be(request.Code);
        promotion.DiscountValue.Should().Be(request.DiscountValue);
    }

    [Fact]
    public async Task CreatePromotion_EmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var request = new CreatePromotionCommand(
            Name: "",
            Code: $"TESTCODE{uniqueId}",
            Description: null,
            PromotionType: PromotionType.VoucherCode,
            DiscountType: DiscountType.Percentage,
            DiscountValue: 10m,
            StartDate: DateTimeOffset.UtcNow,
            EndDate: DateTimeOffset.UtcNow.AddDays(30));

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/promotions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePromotion_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = CreateTestPromotionCommand();

        // Act
        var response = await _client.PostAsJsonAsync("/api/promotions", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    #endregion

    #region PUT /api/promotions/{id}

    [Fact]
    public async Task UpdatePromotion_ValidRequest_ShouldReturnUpdatedPromotion()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Create first
        var createRequest = CreateTestPromotionCommand();
        var createResponse = await adminClient.PostAsJsonAsync("/api/promotions", createRequest);
        var created = await createResponse.Content.ReadFromJsonWithEnumsAsync<PromotionDto>();

        var updateRequest = new UpdatePromotionCommand(
            Id: created!.Id,
            Name: "Updated Promotion Name",
            Code: created.Code,
            Description: "Updated description",
            PromotionType: PromotionType.VoucherCode,
            DiscountType: DiscountType.Percentage,
            DiscountValue: 15m,
            StartDate: created.StartDate,
            EndDate: created.EndDate,
            ApplyLevel: PromotionApplyLevel.Cart);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/promotions/{created.Id}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonWithEnumsAsync<PromotionDto>();
        updated.Should().NotBeNull();
        updated!.Name.Should().Be("Updated Promotion Name");
        updated.DiscountValue.Should().Be(15m);
    }

    [Fact]
    public async Task UpdatePromotion_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var nonExistentId = Guid.NewGuid();
        var updateRequest = new UpdatePromotionCommand(
            Id: nonExistentId,
            Name: "Updated Name",
            Code: "NOEXIST",
            Description: null,
            PromotionType: PromotionType.VoucherCode,
            DiscountType: DiscountType.Percentage,
            DiscountValue: 10m,
            StartDate: DateTimeOffset.UtcNow,
            EndDate: DateTimeOffset.UtcNow.AddDays(30),
            ApplyLevel: PromotionApplyLevel.Cart);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/promotions/{nonExistentId}", updateRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/promotions/{id}

    [Fact]
    public async Task DeletePromotion_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var createRequest = CreateTestPromotionCommand();
        var createResponse = await adminClient.PostAsJsonAsync("/api/promotions", createRequest);
        var created = await createResponse.Content.ReadFromJsonWithEnumsAsync<PromotionDto>();

        // Act
        var response = await adminClient.DeleteAsync($"/api/promotions/{created!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify it's deleted (soft)
        var getResponse = await adminClient.GetAsync($"/api/promotions/{created.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePromotion_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/promotions/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/promotions/{id}/activate and /deactivate

    [Fact]
    public async Task ActivatePromotion_ValidId_ShouldReturnActivatedPromotion()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var createRequest = CreateTestPromotionCommand();
        var createResponse = await adminClient.PostAsJsonAsync("/api/promotions", createRequest);
        var created = await createResponse.Content.ReadFromJsonWithEnumsAsync<PromotionDto>();

        // Act
        var response = await adminClient.PostAsync($"/api/promotions/{created!.Id}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var activated = await response.Content.ReadFromJsonWithEnumsAsync<PromotionDto>();
        activated.Should().NotBeNull();
        activated!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ActivatePromotion_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.PostAsync($"/api/promotions/{Guid.NewGuid()}/activate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeactivatePromotion_ValidId_ShouldReturnDeactivatedPromotion()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var createRequest = CreateTestPromotionCommand();
        var createResponse = await adminClient.PostAsJsonAsync("/api/promotions", createRequest);
        var created = await createResponse.Content.ReadFromJsonWithEnumsAsync<PromotionDto>();

        // Activate first
        await adminClient.PostAsync($"/api/promotions/{created!.Id}/activate", null);

        // Act
        var response = await adminClient.PostAsync($"/api/promotions/{created.Id}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var deactivated = await response.Content.ReadFromJsonWithEnumsAsync<PromotionDto>();
        deactivated.Should().NotBeNull();
        deactivated!.IsActive.Should().BeFalse();
    }

    #endregion

    #region GET /api/promotions/validate/{code}

    [Fact]
    public async Task ValidatePromoCode_NonExistentCode_ShouldReturnValidationResult()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/promotions/validate/NONEXISTENT?orderTotal=100");

        // Assert
        // The validate endpoint returns 200 with IsValid=false rather than 404
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PromoCodeValidationDto>();
        result.Should().NotBeNull();
        result!.IsValid.Should().BeFalse();
    }

    #endregion

    #region Authorization Tests

    [Fact]
    public async Task PromotionEndpoints_WithoutRequiredPermission_ShouldReturnForbidden()
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

        // Act
        var response = await userClient.GetAsync("/api/promotions");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region Helper Methods

    private static CreatePromotionCommand CreateTestPromotionCommand()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return new CreatePromotionCommand(
            Name: $"Test Promo {uniqueId}",
            Code: $"TEST{uniqueId}".ToUpperInvariant(),
            Description: "Integration test promotion",
            PromotionType: PromotionType.VoucherCode,
            DiscountType: DiscountType.Percentage,
            DiscountValue: 10m,
            StartDate: DateTimeOffset.UtcNow.AddDays(-1),
            EndDate: DateTimeOffset.UtcNow.AddDays(30));
    }

    #endregion
}
