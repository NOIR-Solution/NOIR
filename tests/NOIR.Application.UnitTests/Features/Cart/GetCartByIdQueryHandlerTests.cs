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
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(cartId);
        result.Value.UserId.Should().Be(TestUserId);
        result.Value.Status.Should().Be(CartStatus.Active);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].ProductName.Should().Be("Test Product");
        result.Value.Items[0].VariantName.Should().Be("Size M");
        result.Value.Items[0].UnitPrice.Should().Be(100_000m);
        result.Value.Items[0].Quantity.Should().Be(2);
        result.Value.Items[0].ImageUrl.Should().Be("https://example.com/img.jpg");
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.IsEmpty.Should().BeTrue();
        result.Value.ItemCount.Should().Be(0);
        result.Value.Subtotal.Should().Be(0);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.SessionId.Should().Be(TestSessionId);
        result.Value.UserId.Should().BeNull();
        result.Value.IsGuest.Should().BeTrue();
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.IsEmpty.Should().BeFalse();
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("USD");
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
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CART-001");
        result.Error.Message.Should().Contain(nonExistentId.ToString());
        result.Error.Type.Should().Be(ErrorType.NotFound);
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
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CART-001");
    }

    #endregion
}
