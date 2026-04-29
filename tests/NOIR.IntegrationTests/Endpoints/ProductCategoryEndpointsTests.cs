namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for product category management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class ProductCategoryEndpointsTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProductCategoryEndpointsTests(CustomWebApplicationFactory factory)
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

    #region GET /api/products/categories

    [Fact]
    public async Task GetProductCategories_AsAdmin_ShouldReturnList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/products/categories");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<List<ProductCategoryListDto>>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetProductCategories_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/products/categories");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProductCategories_WithTopLevelFilter_ShouldReturn200()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/products/categories?topLevelOnly=true");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region GET /api/products/categories/{id}

    [Fact]
    public async Task GetProductCategoryById_ValidId_ShouldReturnCategory()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a category
        var createRequest = CreateTestCategoryRequest();
        var createResponse = await adminClient.PostAsJsonAsync("/api/products/categories", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdCategory = await createResponse.Content.ReadFromJsonAsync<ProductCategoryDto>();

        // Act
        var response = await adminClient.GetAsync($"/api/products/categories/{createdCategory!.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var category = await response.Content.ReadFromJsonAsync<ProductCategoryDto>();
        category.ShouldNotBeNull();
        category!.Id.ShouldBe(createdCategory.Id);
        category.Name.ShouldBe(createRequest.Name);
    }

    [Fact]
    public async Task GetProductCategoryById_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync($"/api/products/categories/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region POST /api/products/categories

    [Fact]
    public async Task CreateProductCategory_ValidRequest_ShouldReturnCreatedCategory()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestCategoryRequest();

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/products/categories", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var category = await response.Content.ReadFromJsonAsync<ProductCategoryDto>();
        category.ShouldNotBeNull();
        category!.Name.ShouldBe(request.Name);
        category.Slug.ShouldBe(request.Slug);
    }

    [Fact]
    public async Task CreateProductCategory_EmptyName_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = new CreateProductCategoryRequest(
            Name: "",
            Slug: "test-slug",
            Description: null,
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 0,
            ParentId: null);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/products/categories", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProductCategory_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = CreateTestCategoryRequest();

        // Act
        var response = await _client.PostAsJsonAsync("/api/products/categories", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateProductCategory_DuplicateSlug_ShouldReturnConflict()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var request = CreateTestCategoryRequest();

        // Create the first category
        var firstResponse = await adminClient.PostAsJsonAsync("/api/products/categories", request);
        firstResponse.EnsureSuccessStatusCode();

        // Act - Try to create with same slug
        var response = await adminClient.PostAsJsonAsync("/api/products/categories", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    #endregion

    #region PUT /api/products/categories/{id}

    [Fact]
    public async Task UpdateProductCategory_ValidRequest_ShouldReturnUpdatedCategory()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a category
        var createRequest = CreateTestCategoryRequest();
        var createResponse = await adminClient.PostAsJsonAsync("/api/products/categories", createRequest);
        createResponse.EnsureSuccessStatusCode();
        var createdCategory = await createResponse.Content.ReadFromJsonAsync<ProductCategoryDto>();

        var updateRequest = new UpdateProductCategoryRequest(
            Name: "Updated Category Name",
            Slug: createRequest.Slug,
            Description: "Updated description",
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 1,
            ParentId: null);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/products/categories/{createdCategory!.Id}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updatedCategory = await response.Content.ReadFromJsonAsync<ProductCategoryDto>();
        updatedCategory.ShouldNotBeNull();
        updatedCategory!.Name.ShouldBe("Updated Category Name");
        updatedCategory.Description.ShouldBe("Updated description");
    }

    [Fact]
    public async Task UpdateProductCategory_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var updateRequest = new UpdateProductCategoryRequest(
            Name: "Updated Category",
            Slug: "updated-category",
            Description: null,
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 0,
            ParentId: null);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/products/categories/{Guid.NewGuid()}", updateRequest);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/products/categories/{id}

    [Fact]
    public async Task DeleteProductCategory_ValidId_ShouldReturnSuccess()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // First create a category
        var request = CreateTestCategoryRequest();
        var createResponse = await adminClient.PostAsJsonAsync("/api/products/categories", request);
        createResponse.EnsureSuccessStatusCode();
        var createdCategory = await createResponse.Content.ReadFromJsonAsync<ProductCategoryDto>();

        // Act
        var response = await adminClient.DeleteAsync($"/api/products/categories/{createdCategory!.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Verify it's deleted (soft delete - should return not found)
        var getResponse = await adminClient.GetAsync($"/api/products/categories/{createdCategory.Id}");
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProductCategory_InvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.DeleteAsync($"/api/products/categories/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion

    #region Helper Methods

    private static CreateProductCategoryRequest CreateTestCategoryRequest()
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..8];
        return new CreateProductCategoryRequest(
            Name: $"Test Category {uniqueId}",
            Slug: $"test-category-{uniqueId}",
            Description: "Integration test category",
            MetaTitle: null,
            MetaDescription: null,
            ImageUrl: null,
            SortOrder: 0,
            ParentId: null);
    }

    #endregion
}
