using NOIR.Application.Features.ProductAttributes.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for product attribute management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class ProductAttributeEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProductAttributeEndpointsTests(CustomWebApplicationFactory factory)
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

    #region GET /api/product-attributes

    [Fact]
    public async Task GetProductAttributes_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/product-attributes");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<ProductAttributeListDto>>();
        result.ShouldNotBeNull();
        result!.Items.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetProductAttributes_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/product-attributes");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProductAttributes_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/product-attributes?pageNumber=1&pageSize=5");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<PagedResult<ProductAttributeListDto>>();
        result.ShouldNotBeNull();
        result!.Items.Count.ShouldBeLessThanOrEqualTo(5);
    }

    #endregion

    #region GET /api/product-attributes/{id}

    [Fact]
    public async Task GetProductAttributeById_ValidId_ShouldReturnAttribute()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create an attribute
        var createRequest = CreateTestAttributeRequest();
        var createResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/product-attributes", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdAttr = await createResponse.Content.ReadFromJsonWithEnumsAsync<ProductAttributeDto>();

        // Act
        var response = await adminClient.GetAsync($"/api/product-attributes/{createdAttr!.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var attr = await response.Content.ReadFromJsonWithEnumsAsync<ProductAttributeDto>();
        attr.ShouldNotBeNull();
        attr!.Id.ShouldBe(createdAttr.Id);
        attr.Name.ShouldBe(createRequest.Name);
    }

    [Fact]
    public async Task GetProductAttributeById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/product-attributes/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/product-attributes

    [Fact]
    public async Task CreateProductAttribute_ValidRequest_ShouldReturnCreatedAttribute()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestAttributeRequest();

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/product-attributes", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var attr = await response.Content.ReadFromJsonWithEnumsAsync<ProductAttributeDto>();
        attr.ShouldNotBeNull();
        attr!.Name.ShouldBe(request.Name);
        attr.Code.ShouldBe(request.Code);
    }

    [Fact]
    public async Task CreateProductAttribute_EmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new CreateProductAttributeRequest(
            Code: "test-code",
            Name: "",
            Type: "Text");

        // Act
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/product-attributes", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProductAttribute_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = CreateTestAttributeRequest();

        // Act
        var response = await _client.PostAsJsonWithEnumsAsync("/api/product-attributes", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProductAttribute_DuplicateCode_ShouldReturnConflict()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestAttributeRequest();

        // Create the first attribute
        var firstResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/product-attributes", request);
        firstResponse.EnsureSuccessStatusCode();

        // Act - Try to create with same code
        var response = await adminClient.PostAsJsonWithEnumsAsync("/api/product-attributes", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    #endregion

    #region PUT /api/product-attributes/{id}

    [Fact]
    public async Task UpdateProductAttribute_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        var updateRequest = new UpdateProductAttributeRequest(
            Code: $"upd_{uniqueId}".ToLowerInvariant(),
            Name: "Updated Attribute",
            IsFilterable: false,
            IsSearchable: false,
            IsRequired: false,
            IsVariantAttribute: false,
            ShowInProductCard: false,
            ShowInSpecifications: true,
            Unit: null,
            ValidationRegex: null,
            MinValue: null,
            MaxValue: null,
            MaxLength: null,
            DefaultValue: null,
            Placeholder: null,
            HelpText: null,
            SortOrder: 0,
            IsActive: true);

        // Act
        var response = await adminClient.PutAsJsonWithEnumsAsync($"/api/product-attributes/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/product-attributes/{id}

    [Fact]
    public async Task DeleteProductAttribute_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create an attribute
        var request = CreateTestAttributeRequest();
        var createResponse = await adminClient.PostAsJsonWithEnumsAsync("/api/product-attributes", request);
        createResponse.EnsureSuccessStatusCode();
        var createdAttr = await createResponse.Content.ReadFromJsonWithEnumsAsync<ProductAttributeDto>();

        // Act
        var response = await adminClient.DeleteAsync($"/api/product-attributes/{createdAttr!.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify it's deleted (soft delete - should return not found)
        var getResponse = await adminClient.GetAsync($"/api/product-attributes/{createdAttr.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProductAttribute_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/product-attributes/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Helper Methods

    private static CreateProductAttributeRequest CreateTestAttributeRequest()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8].ToLowerInvariant();
        return new CreateProductAttributeRequest(
            Code: $"attr_{uniqueId}",
            Name: $"Test Attribute {uniqueId}",
            Type: "Text");
    }

    #endregion
}
