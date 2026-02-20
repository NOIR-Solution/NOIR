namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for product management endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// </summary>
[Collection("Integration")]
public class ProductEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProductEndpointsTests(CustomWebApplicationFactory factory)
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

    #region GetProducts Tests

    [Fact]
    public async Task GetProducts_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<Application.Features.Products.Queries.GetProducts.PagedResult<ProductListDto>>();
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetProducts_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProducts_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act - The endpoint uses 'page' and 'pageSize' query parameters
        var response = await adminClient.GetAsync("/api/products?page=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<Application.Features.Products.Queries.GetProducts.PagedResult<ProductListDto>>();
        result.Should().NotBeNull();
        result!.Page.Should().Be(1);
        result.Items.Count.Should().BeLessThanOrEqualTo(5);
    }

    #endregion

    #region GetProductById Tests

    [Fact]
    public async Task GetProductById_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await adminClient.GetAsync($"/api/products/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GetProductStats Tests

    [Fact]
    public async Task GetProductStats_AsAdmin_ShouldReturnStats()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/products/stats");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region CreateProduct Tests

    [Fact]
    public async Task CreateProduct_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var command = new CreateProductCommand(
            Name: $"Test Product {uniqueId}",
            Slug: $"test-product-{uniqueId}",
            ShortDescription: "Short desc",
            Description: "Test product description",
            DescriptionHtml: null,
            BasePrice: 99.99m,
            Currency: "VND",
            CategoryId: null,
            BrandId: null,
            Brand: null,
            Sku: $"SKU-{uniqueId}",
            Barcode: null,
            TrackInventory: true,
            MetaTitle: null,
            MetaDescription: null,
            SortOrder: 0,
            Weight: null,
            WeightUnit: null,
            Length: null,
            Width: null,
            Height: null,
            DimensionUnit: null,
            Variants: null,
            Images: null);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<ProductDto>();
        result.Should().NotBeNull();
        result!.Name.Should().StartWith("Test Product");
    }

    [Fact]
    public async Task CreateProduct_WithMissingName_ShouldReturnBadRequest()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var uniqueId = Guid.NewGuid().ToString("N").Substring(0, 8);
        var command = new CreateProductCommand(
            Name: "",
            Slug: $"test-product-{uniqueId}",
            ShortDescription: null,
            Description: "Test product description",
            DescriptionHtml: null,
            BasePrice: 99.99m,
            Currency: "VND",
            CategoryId: null,
            BrandId: null,
            Brand: null,
            Sku: $"SKU-{uniqueId}",
            Barcode: null,
            TrackInventory: true,
            MetaTitle: null,
            MetaDescription: null,
            SortOrder: 0,
            Weight: null,
            WeightUnit: null,
            Length: null,
            Width: null,
            Height: null,
            DimensionUnit: null,
            Variants: null,
            Images: null);

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/products", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region UpdateProduct Tests

    [Fact]
    public async Task UpdateProduct_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();
        var command = new UpdateProductCommand(
            Id: invalidId,
            Name: "Updated Product",
            Slug: "updated-product",
            ShortDescription: null,
            Description: "Updated description",
            DescriptionHtml: null,
            BasePrice: 149.99m,
            Currency: "VND",
            CategoryId: null,
            BrandId: null,
            Brand: null,
            Sku: "UPDATED-SKU",
            Barcode: null,
            TrackInventory: true,
            MetaTitle: null,
            MetaDescription: null,
            SortOrder: 0,
            Weight: null,
            WeightUnit: null,
            Length: null,
            Width: null,
            Height: null,
            DimensionUnit: null);

        // Act
        var response = await adminClient.PutAsJsonAsync($"/api/products/{invalidId}", command);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DeleteProduct Tests

    [Fact]
    public async Task DeleteProduct_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var invalidId = Guid.NewGuid();

        // Act
        var response = await adminClient.DeleteAsync($"/api/products/{invalidId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region SearchProductVariants Tests

    [Fact]
    public async Task SearchProductVariants_AsAdmin_ShouldReturnPaginatedList()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/products/variants/search");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchProductVariants_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Act
        var response = await _client.GetAsync("/api/products/variants/search");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SearchProductVariants_WithSearchFilter_ShouldReturn200()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/products/variants/search?search=laptop");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchProductVariants_WithPagination_ShouldRespectParameters()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();

        // Act
        var response = await adminClient.GetAsync("/api/products/variants/search?page=1&pageSize=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SearchProductVariants_WithCategoryFilter_ShouldReturn200()
    {
        // Arrange
        var adminClient = await GetAdminClientAsync();
        var categoryId = Guid.NewGuid();

        // Act
        var response = await adminClient.GetAsync($"/api/products/variants/search?categoryId={categoryId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
