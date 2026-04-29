using NOIR.Application.Features.ProductFilter.DTOs;

namespace NOIR.IntegrationTests.Endpoints;

/// <summary>
/// Integration tests for product filter endpoints.
/// Tests the full HTTP request/response cycle with real middleware and handlers.
/// Note: Product filter endpoints are public (AllowAnonymous) for storefront use.
/// </summary>
[Collection("Integration")]
public class ProductFilterEndpointsTests
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public ProductFilterEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateTestClient();
    }

    #region GET /api/products/filter

    [Fact]
    public async Task FilterProducts_NoParams_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/api/products/filter");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonWithEnumsAsync<FilteredProductsResult>();
        result.ShouldNotBeNull();
    }

    [Fact]
    public async Task FilterProducts_WithSearchQuery_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/api/products/filter?q=test");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FilterProducts_WithPriceRange_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/api/products/filter?priceMin=10&priceMax=100");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FilterProducts_WithPagination_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/api/products/filter?page=1&pageSize=5");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FilterProducts_WithInStockFilter_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/api/products/filter?inStock=true");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FilterProducts_WithSortOrder_ShouldReturn200()
    {
        // Act
        var response = await _client.GetAsync("/api/products/filter?sort=price&order=asc");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region POST /api/products/filter

    [Fact]
    public async Task FilterProductsPost_ValidRequest_ShouldReturn200()
    {
        // Arrange
        var request = new ProductFilterRequest
        {
            Page = 1,
            PageSize = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products/filter", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FilterProductsPost_WithSearchQuery_ShouldReturn200()
    {
        // Arrange
        var request = new ProductFilterRequest
        {
            SearchQuery = "test",
            Page = 1,
            PageSize = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/products/filter", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    #endregion

    #region GET /api/categories/{slug}/filters

    [Fact]
    public async Task GetCategoryFilters_NonExistentSlug_ShouldReturnNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/categories/non-existent-category/filters");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    #endregion
}
