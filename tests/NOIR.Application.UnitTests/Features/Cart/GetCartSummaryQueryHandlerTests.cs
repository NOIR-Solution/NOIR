using NOIR.Application.Features.Cart.DTOs;
using NOIR.Application.Features.Cart.Queries.GetCartSummary;
using NOIR.Application.Features.Cart.Specifications;

namespace NOIR.Application.UnitTests.Features.Cart;

/// <summary>
/// Unit tests for GetCartSummaryQueryHandler.
/// Tests cart summary retrieval scenarios with mocked dependencies.
/// </summary>
public class GetCartSummaryQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Domain.Entities.Cart.Cart, Guid>> _cartRepositoryMock;
    private readonly GetCartSummaryQueryHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "test-user-123";
    private const string TestSessionId = "test-session-456";

    public GetCartSummaryQueryHandlerTests()
    {
        _cartRepositoryMock = new Mock<IRepository<Domain.Entities.Cart.Cart, Guid>>();
        _handler = new GetCartSummaryQueryHandler(_cartRepositoryMock.Object);
    }

    private static Domain.Entities.Cart.Cart CreateTestCart(
        Guid? cartId = null,
        string? userId = TestUserId,
        string? sessionId = null,
        string? tenantId = TestTenantId)
    {
        var cart = userId != null
            ? Domain.Entities.Cart.Cart.CreateForUser(userId, "VND", tenantId)
            : Domain.Entities.Cart.Cart.CreateForGuest(sessionId ?? TestSessionId, "VND", tenantId);

        if (cartId.HasValue)
        {
            typeof(Entity<Guid>).GetProperty("Id")!.SetValue(cart, cartId.Value);
        }

        return cart;
    }

    private static void AddItemToCart(
        Domain.Entities.Cart.Cart cart,
        string productName = "Test Product",
        decimal price = 100m,
        int quantity = 1)
    {
        cart.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            productName,
            "Test Variant",
            price,
            quantity,
            "http://example.com/image.jpg");
    }

    #endregion

    #region Success Scenarios - User Cart

    [Fact]
    public async Task Handle_UserWithCart_ReturnsSummary()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart, "Product 1", 100m, 2);
        AddItemToCart(cart, "Product 2", 200m, 1);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var query = new GetCartSummaryQuery { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(cartId);
        result.Value.ItemCount.Should().Be(3); // 2 + 1
        result.Value.Subtotal.Should().Be(400m); // (100*2) + (200*1)
        result.Value.Currency.Should().Be("VND");
    }

    [Fact]
    public async Task Handle_UserWithEmptyCart_ReturnsZeroSummary()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var query = new GetCartSummaryQuery { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(cartId);
        result.Value.ItemCount.Should().Be(0);
        result.Value.Subtotal.Should().Be(0);
        result.Value.RecentItems.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UserWithNoCart_ReturnsEmptySummary()
    {
        // Arrange
        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Cart.Cart?)null);

        var query = new GetCartSummaryQuery { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(Guid.Empty);
        result.Value.ItemCount.Should().Be(0);
        result.Value.Subtotal.Should().Be(0);
        result.Value.Currency.Should().Be("VND");
        result.Value.RecentItems.Should().BeEmpty();
    }

    #endregion

    #region Success Scenarios - Guest Cart

    [Fact]
    public async Task Handle_GuestWithCart_ReturnsSummary()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestCart(cartId, userId: null, sessionId: TestSessionId);
        AddItemToCart(cart, "Guest Product", 150m, 3);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartBySessionIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var query = new GetCartSummaryQuery { SessionId = TestSessionId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(cartId);
        result.Value.ItemCount.Should().Be(3);
        result.Value.Subtotal.Should().Be(450m);
    }

    [Fact]
    public async Task Handle_GuestWithNoCart_ReturnsEmptySummary()
    {
        // Arrange
        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartBySessionIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Cart.Cart?)null);

        var query = new GetCartSummaryQuery { SessionId = TestSessionId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(Guid.Empty);
        result.Value.ItemCount.Should().Be(0);
        result.Value.Subtotal.Should().Be(0);
    }

    #endregion

    #region Priority - User over Session

    [Fact]
    public async Task Handle_BothUserAndSession_PrioritizesUserCart()
    {
        // Arrange
        var userCartId = Guid.NewGuid();
        var userCart = CreateTestCart(userCartId);
        AddItemToCart(userCart, "User Product", 100m, 1);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userCart);

        var query = new GetCartSummaryQuery { UserId = TestUserId, SessionId = TestSessionId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(userCartId);
        result.Value.ItemCount.Should().Be(1);

        // Session cart should NOT be queried since user cart was found
        _cartRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartBySessionIdSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UserCartNotFound_FallsBackToSessionCart()
    {
        // Arrange
        var sessionCartId = Guid.NewGuid();
        var sessionCart = CreateTestCart(sessionCartId, userId: null, sessionId: TestSessionId);
        AddItemToCart(sessionCart, "Session Product", 200m, 2);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Cart.Cart?)null);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartBySessionIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sessionCart);

        var query = new GetCartSummaryQuery { UserId = TestUserId, SessionId = TestSessionId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(sessionCartId);
        result.Value.ItemCount.Should().Be(2);
    }

    #endregion

    #region Recent Items

    [Fact]
    public async Task Handle_CartWithManyItems_ReturnsRecentItemsOnly()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);

        // Add 10 items
        for (int i = 1; i <= 10; i++)
        {
            AddItemToCart(cart, $"Product {i}", 100m, 1);
        }

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var query = new GetCartSummaryQuery { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ItemCount.Should().Be(10);
        result.Value.RecentItems.Should().HaveCount(5); // Default limit is 5
    }

    [Fact]
    public async Task Handle_CartWithFewerThanLimitItems_ReturnsAllItems()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart, "Product 1", 100m, 1);
        AddItemToCart(cart, "Product 2", 200m, 2);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var query = new GetCartSummaryQuery { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RecentItems.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_RecentItems_ContainsCorrectDetails()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart, "Test Product", 150m, 3);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var query = new GetCartSummaryQuery { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RecentItems.Should().HaveCount(1);
        var recentItem = result.Value.RecentItems[0];
        recentItem.ProductName.Should().Be("Test Product");
        recentItem.UnitPrice.Should().Be(150m);
        recentItem.Quantity.Should().Be(3);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var cart = CreateTestCart();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var query = new GetCartSummaryQuery { UserId = TestUserId };

        // Act
        await _handler.Handle(query, token);

        // Assert
        _cartRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NeitherUserNorSession_ReturnsEmptySummary()
    {
        // Arrange
        var query = new GetCartSummaryQuery(); // No UserId or SessionId

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(Guid.Empty);
        result.Value.ItemCount.Should().Be(0);
        result.Value.Subtotal.Should().Be(0);

        // No repository calls should be made
        _cartRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _cartRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartBySessionIdSpec>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}
