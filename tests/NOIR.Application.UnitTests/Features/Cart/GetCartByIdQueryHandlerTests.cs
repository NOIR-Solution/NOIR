using NOIR.Application.Features.Cart.DTOs;
using NOIR.Application.Features.Cart.Queries.GetCartById;
using NOIR.Application.Features.Cart.Specifications;

namespace NOIR.Application.UnitTests.Features.Cart;

/// <summary>
/// Unit tests for GetCartByIdQueryHandler.
/// Tests retrieving a cart by its ID with various scenarios.
/// </summary>
public class GetCartByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Domain.Entities.Cart.Cart, Guid>> _cartRepositoryMock;
    private readonly GetCartByIdQueryHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "test-user-123";
    private const string TestSessionId = "test-session-456";

    public GetCartByIdQueryHandlerTests()
    {
        _cartRepositoryMock = new Mock<IRepository<Domain.Entities.Cart.Cart, Guid>>();
        _handler = new GetCartByIdQueryHandler(_cartRepositoryMock.Object);
    }

    private static Domain.Entities.Cart.Cart CreateTestUserCart(
        Guid? id = null,
        string userId = TestUserId,
        string currency = "VND")
    {
        var cart = Domain.Entities.Cart.Cart.CreateForUser(userId, currency, TestTenantId);

        if (id.HasValue)
        {
            typeof(Domain.Entities.Cart.Cart).GetProperty("Id")?.SetValue(cart, id.Value);
        }

        return cart;
    }

    private static Domain.Entities.Cart.Cart CreateTestGuestCart(
        Guid? id = null,
        string sessionId = TestSessionId,
        string currency = "VND")
    {
        var cart = Domain.Entities.Cart.Cart.CreateForGuest(sessionId, currency, TestTenantId);

        if (id.HasValue)
        {
            typeof(Domain.Entities.Cart.Cart).GetProperty("Id")?.SetValue(cart, id.Value);
        }

        return cart;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_CartExists_ReturnsCartDto()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestUserCart(id: cartId);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var query = new GetCartByIdQuery(cartId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(cartId);
        result.Value.UserId.ShouldBe(TestUserId);
        result.Value.Status.ShouldBe(CartStatus.Active);
    }

    [Fact]
    public async Task Handle_CartWithItems_ReturnsCartDtoWithItems()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestUserCart(id: cartId);

        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        cart.AddItem(productId, variantId, "Test Product", "Size M", 100_000m, 2, "https://example.com/img.jpg");

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var query = new GetCartByIdQuery(cartId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(1);
        result.Value.Items[0].ProductName.ShouldBe("Test Product");
        result.Value.Items[0].VariantName.ShouldBe("Size M");
        result.Value.Items[0].UnitPrice.ShouldBe(100_000m);
        result.Value.Items[0].Quantity.ShouldBe(2);
        result.Value.Items[0].ImageUrl.ShouldBe("https://example.com/img.jpg");
    }

    [Fact]
    public async Task Handle_EmptyCart_ReturnsCartDtoWithNoItems()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestUserCart(id: cartId);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var query = new GetCartByIdQuery(cartId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.ShouldBeEmpty();
        result.Value.IsEmpty.ShouldBe(true);
        result.Value.ItemCount.ShouldBe(0);
        result.Value.Subtotal.ShouldBe(0);
    }

    [Fact]
    public async Task Handle_GuestCart_ReturnsGuestCartDto()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestGuestCart(id: cartId);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var query = new GetCartByIdQuery(cartId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.SessionId.ShouldBe(TestSessionId);
        result.Value.UserId.ShouldBeNull();
        result.Value.IsGuest.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_CartWithMultipleItems_ReturnsAllItems()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestUserCart(id: cartId);

        cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product A", "Variant A", 50_000m, 1);
        cart.AddItem(Guid.NewGuid(), Guid.NewGuid(), "Product B", "Variant B", 75_000m, 3);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var query = new GetCartByIdQuery(cartId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(2);
        result.Value.IsEmpty.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_CartWithCurrency_ReturnsCurrencyInDto()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestUserCart(id: cartId, currency: "USD");

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var query = new GetCartByIdQuery(cartId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Currency.ShouldBe("USD");
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_CartNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Cart.Cart?)null);

        var query = new GetCartByIdQuery(nonExistentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-CART-001");
        result.Error.Message.ShouldContain(nonExistentId.ToString());
        result.Error.Type.ShouldBe(ErrorType.NotFound);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassToRepository()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestUserCart(id: cartId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdSpec>(),
                token))
            .ReturnsAsync(cart);

        var query = new GetCartByIdQuery(cartId);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _cartRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdSpec>(),
                token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CartWithEmptyGuid_ReturnsNotFound()
    {
        // Arrange
        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Cart.Cart?)null);

        var query = new GetCartByIdQuery(Guid.Empty);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-CART-001");
    }

    #endregion
}
