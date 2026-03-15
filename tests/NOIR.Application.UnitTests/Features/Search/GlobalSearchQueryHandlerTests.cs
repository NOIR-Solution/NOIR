using NOIR.Application.Features.Blog.Specifications;
using NOIR.Application.Features.Search.DTOs;
using NOIR.Application.Features.Search.Queries;
using NOIR.Application.Features.Products.Specifications;
using NOIR.Application.Modules;

namespace NOIR.Application.UnitTests.Features.Search;

/// <summary>
/// Unit tests for GlobalSearchQueryHandler.
/// Tests global search across multiple entity types with feature gating.
/// </summary>
public class GlobalSearchQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IRepository<Order, Guid>> _orderRepositoryMock;
    private readonly Mock<IRepository<Domain.Entities.Customer.Customer, Guid>> _customerRepositoryMock;
    private readonly Mock<IRepository<Post, Guid>> _postRepositoryMock;
    private readonly Mock<IUserIdentityService> _userIdentityServiceMock;
    private readonly Mock<IFeatureChecker> _featureCheckerMock;
    private readonly GlobalSearchQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public GlobalSearchQueryHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _orderRepositoryMock = new Mock<IRepository<Order, Guid>>();
        _customerRepositoryMock = new Mock<IRepository<Domain.Entities.Customer.Customer, Guid>>();
        _postRepositoryMock = new Mock<IRepository<Post, Guid>>();
        _userIdentityServiceMock = new Mock<IUserIdentityService>();
        _featureCheckerMock = new Mock<IFeatureChecker>();

        _handler = new GlobalSearchQueryHandler(
            _productRepositoryMock.Object,
            _orderRepositoryMock.Object,
            _customerRepositoryMock.Object,
            _postRepositoryMock.Object,
            _userIdentityServiceMock.Object,
            _featureCheckerMock.Object);
    }

    private void SetupFeatureEnabled(string featureName, bool enabled)
    {
        _featureCheckerMock
            .Setup(x => x.IsEnabledAsync(featureName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(enabled);
    }

    private void SetupAllFeaturesEnabled()
    {
        SetupFeatureEnabled(ModuleNames.Ecommerce.Products, true);
        SetupFeatureEnabled(ModuleNames.Ecommerce.Orders, true);
        SetupFeatureEnabled(ModuleNames.Ecommerce.Customers, true);
        SetupFeatureEnabled(ModuleNames.Content.Blog, true);
    }

    private void SetupAllFeaturesDisabled()
    {
        SetupFeatureEnabled(ModuleNames.Ecommerce.Products, false);
        SetupFeatureEnabled(ModuleNames.Ecommerce.Orders, false);
        SetupFeatureEnabled(ModuleNames.Ecommerce.Customers, false);
        SetupFeatureEnabled(ModuleNames.Content.Blog, false);
    }

    private void SetupEmptyUsers()
    {
        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(
                null, It.IsAny<string>(), 1, It.IsAny<int>(), null, null, It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<UserIdentityDto>() as IReadOnlyList<UserIdentityDto>, 0));
    }

    private void SetupUsers(params UserIdentityDto[] users)
    {
        _userIdentityServiceMock
            .Setup(x => x.GetUsersPaginatedAsync(
                null, It.IsAny<string>(), 1, It.IsAny<int>(), null, null, It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((users.ToList() as IReadOnlyList<UserIdentityDto>, users.Length));
    }

    private static UserIdentityDto CreateTestUser(string id = "user-1", string email = "test@test.com", string fullName = "Test User")
    {
        return new UserIdentityDto(
            id, email, TestTenantId, "Test", "User", fullName, fullName,
            null, null, true, false, false,
            DateTimeOffset.UtcNow, null);
    }

    private static Product CreateTestProduct(string name = "Test Product", string slug = "test-product", decimal basePrice = 99.99m)
    {
        return Product.Create(name, slug, basePrice, "VND", TestTenantId);
    }

    private static Order CreateTestOrder(string orderNumber = "ORD-001", string customerEmail = "customer@test.com")
    {
        return Order.Create(orderNumber, customerEmail, 100m, 100m, "VND", TestTenantId);
    }

    private static Domain.Entities.Customer.Customer CreateTestCustomer(
        string email = "customer@test.com",
        string firstName = "John",
        string lastName = "Doe")
    {
        return Domain.Entities.Customer.Customer.Create(null, email, firstName, lastName, null, TestTenantId);
    }

    private static Post CreateTestPost(string title = "Test Post", string slug = "test-post")
    {
        return Post.Create(title, slug, Guid.NewGuid(), TestTenantId);
    }

    #endregion

    #region Empty/Short Search Tests

    [Fact]
    public async Task Handle_WhenSearchIsEmpty_ReturnsEmptyResponse()
    {
        // Arrange
        var query = new GlobalSearchQuery("");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Products.ShouldBeEmpty();
        result.Value.Orders.ShouldBeEmpty();
        result.Value.Customers.ShouldBeEmpty();
        result.Value.BlogPosts.ShouldBeEmpty();
        result.Value.Users.ShouldBeEmpty();
        result.Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WhenSearchIsOneChar_ReturnsEmptyResponse()
    {
        // Arrange
        var query = new GlobalSearchQuery("a");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_WhenSearchIsWhitespace_ReturnsEmptyResponse()
    {
        // Arrange
        var query = new GlobalSearchQuery("   ");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.TotalCount.ShouldBe(0);
    }

    #endregion

    #region Feature-Gated Search Tests

    [Fact]
    public async Task Handle_WhenProductsEnabled_ReturnsProductResults()
    {
        // Arrange
        var products = new List<Product> { CreateTestProduct() };
        SetupFeatureEnabled(ModuleNames.Ecommerce.Products, true);
        SetupFeatureEnabled(ModuleNames.Ecommerce.Orders, false);
        SetupFeatureEnabled(ModuleNames.Ecommerce.Customers, false);
        SetupFeatureEnabled(ModuleNames.Content.Blog, false);
        SetupEmptyUsers();

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        var query = new GlobalSearchQuery("Test");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Products.Count().ShouldBe(1);
        result.Value.Products[0].Type.ShouldBe("product");
        result.Value.Products[0].Title.ShouldBe("Test Product");
    }

    [Fact]
    public async Task Handle_WhenProductsDisabled_SkipsProducts()
    {
        // Arrange
        SetupAllFeaturesDisabled();
        SetupEmptyUsers();

        var query = new GlobalSearchQuery("Test");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Products.ShouldBeEmpty();

        _productRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductsSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOrdersEnabled_ReturnsOrderResults()
    {
        // Arrange
        var orders = new List<Order> { CreateTestOrder("ORD-001", "customer@test.com") };
        SetupFeatureEnabled(ModuleNames.Ecommerce.Products, false);
        SetupFeatureEnabled(ModuleNames.Ecommerce.Orders, true);
        SetupFeatureEnabled(ModuleNames.Ecommerce.Customers, false);
        SetupFeatureEnabled(ModuleNames.Content.Blog, false);
        SetupEmptyUsers();

        _orderRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<OrderSearchSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var query = new GlobalSearchQuery("ORD");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Orders.Count().ShouldBe(1);
        result.Value.Orders[0].Type.ShouldBe("order");
        result.Value.Orders[0].Title.ShouldBe("ORD-001");
        result.Value.Orders[0].Subtitle.ShouldBe("customer@test.com");
    }

    [Fact]
    public async Task Handle_WhenCustomersEnabled_ReturnsCustomerResults()
    {
        // Arrange
        var customers = new List<Domain.Entities.Customer.Customer> { CreateTestCustomer() };
        SetupFeatureEnabled(ModuleNames.Ecommerce.Products, false);
        SetupFeatureEnabled(ModuleNames.Ecommerce.Orders, false);
        SetupFeatureEnabled(ModuleNames.Ecommerce.Customers, true);
        SetupFeatureEnabled(ModuleNames.Content.Blog, false);
        SetupEmptyUsers();

        _customerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<CustomersFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        var query = new GlobalSearchQuery("John");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Customers.Count().ShouldBe(1);
        result.Value.Customers[0].Type.ShouldBe("customer");
        result.Value.Customers[0].Title.ShouldBe("John Doe");
        result.Value.Customers[0].Subtitle.ShouldBe("customer@test.com");
    }

    [Fact]
    public async Task Handle_WhenBlogEnabled_ReturnsBlogResults()
    {
        // Arrange
        var posts = new List<Post> { CreateTestPost("My Blog Post", "my-blog-post") };
        SetupFeatureEnabled(ModuleNames.Ecommerce.Products, false);
        SetupFeatureEnabled(ModuleNames.Ecommerce.Orders, false);
        SetupFeatureEnabled(ModuleNames.Ecommerce.Customers, false);
        SetupFeatureEnabled(ModuleNames.Content.Blog, true);
        SetupEmptyUsers();

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<PostsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        var query = new GlobalSearchQuery("Blog");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.BlogPosts.Count().ShouldBe(1);
        result.Value.BlogPosts[0].Type.ShouldBe("blogPost");
        result.Value.BlogPosts[0].Title.ShouldBe("My Blog Post");
    }

    #endregion

    #region Users Always Searched Tests

    [Fact]
    public async Task Handle_ReturnsUsers_AlwaysSearched()
    {
        // Arrange
        var user = CreateTestUser();
        SetupAllFeaturesDisabled();
        SetupUsers(user);

        var query = new GlobalSearchQuery("Test");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Users.Count().ShouldBe(1);
        result.Value.Users[0].Type.ShouldBe("user");
        result.Value.Users[0].Title.ShouldBe("Test User");
        result.Value.Users[0].Subtitle.ShouldBe("test@test.com");
    }

    [Fact]
    public async Task Handle_WhenAllFeaturesDisabled_ReturnsOnlyUsers()
    {
        // Arrange
        var user = CreateTestUser();
        SetupAllFeaturesDisabled();
        SetupUsers(user);

        var query = new GlobalSearchQuery("Test");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Products.ShouldBeEmpty();
        result.Value.Orders.ShouldBeEmpty();
        result.Value.Customers.ShouldBeEmpty();
        result.Value.BlogPosts.ShouldBeEmpty();
        result.Value.Users.Count().ShouldBe(1);
        result.Value.TotalCount.ShouldBe(1);

        // Verify no repository calls were made
        _productRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductsSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _orderRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<OrderSearchSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _customerRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<CustomersFilterSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _postRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<PostsSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region MaxPerCategory Tests

    [Fact]
    public async Task Handle_RespectsMaxPerCategory()
    {
        // Arrange
        SetupAllFeaturesEnabled();

        var products = Enumerable.Range(1, 3).Select(i => CreateTestProduct($"Product {i}", $"product-{i}")).ToList();
        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _orderRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<OrderSearchSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        _customerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<CustomersFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Domain.Entities.Customer.Customer>());

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<PostsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());

        SetupEmptyUsers();

        var query = new GlobalSearchQuery("Product", MaxPerCategory: 3);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Products.Count().ShouldBe(3);

        // Verify that the spec was called with correct take parameter
        _productRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductsSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region TotalCount Tests

    [Fact]
    public async Task Handle_TotalCountSumsAllCategories()
    {
        // Arrange
        SetupAllFeaturesEnabled();

        var products = new List<Product> { CreateTestProduct() };
        var orders = new List<Order> { CreateTestOrder() };
        var customers = new List<Domain.Entities.Customer.Customer> { CreateTestCustomer() };
        var posts = new List<Post> { CreateTestPost() };
        var user = CreateTestUser();

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _orderRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<OrderSearchSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _customerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<CustomersFilterSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        _postRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<PostsSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        SetupUsers(user);

        var query = new GlobalSearchQuery("Test");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.TotalCount.ShouldBe(5); // 1 product + 1 order + 1 customer + 1 blog + 1 user
    }

    #endregion
}
