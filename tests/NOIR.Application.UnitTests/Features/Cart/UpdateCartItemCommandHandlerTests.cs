using NOIR.Application.Features.Cart.Commands.UpdateCartItem;
using NOIR.Application.Features.Cart.DTOs;
using NOIR.Application.Features.Cart.Specifications;

namespace NOIR.Application.UnitTests.Features.Cart;

/// <summary>
/// Unit tests for UpdateCartItemCommandHandler.
/// Tests cart item quantity update scenarios with mocked dependencies.
/// </summary>
public class UpdateCartItemCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Domain.Entities.Cart.Cart, Guid>> _cartRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<UpdateCartItemCommandHandler>> _loggerMock;
    private readonly UpdateCartItemCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "test-user-123";
    private const string TestSessionId = "test-session-456";

    public UpdateCartItemCommandHandlerTests()
    {
        _cartRepositoryMock = new Mock<IRepository<Domain.Entities.Cart.Cart, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<UpdateCartItemCommandHandler>>();

        _handler = new UpdateCartItemCommandHandler(
            _cartRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
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

    private static CartItem AddItemToCart(Domain.Entities.Cart.Cart cart, Guid? itemId = null, int quantity = 2)
    {
        var item = cart.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Product",
            "Test Variant",
            100m,
            quantity,
            "http://example.com/image.jpg");

        if (itemId.HasValue)
        {
            typeof(Entity<Guid>).GetProperty("Id")!.SetValue(item, itemId.Value);
        }

        return item;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_ValidQuantityUpdate_UpdatesItemQuantity()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart, itemId, quantity: 2);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateCartItemCommand(cartId, itemId, 5) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        var updatedItem = result.Value.Items.FirstOrDefault(i => i.Id == itemId);
        updatedItem.ShouldNotBeNull();
        updatedItem!.Quantity.ShouldBe(5);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_QuantityZero_RemovesItemFromCart()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart, itemId, quantity: 2);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateCartItemCommand(cartId, itemId, 0) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.ShouldNotContain(i => i.Id == itemId);
        result.Value.IsEmpty.ShouldBe(true);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_IncrementQuantity_UpdatesCorrectly()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart, itemId, quantity: 3);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateCartItemCommand(cartId, itemId, 10) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var updatedItem = result.Value.Items.First(i => i.Id == itemId);
        updatedItem.Quantity.ShouldBe(10);
        updatedItem.LineTotal.ShouldBe(1000m); // 10 * 100
    }

    [Fact]
    public async Task Handle_DecrementQuantity_UpdatesCorrectly()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart, itemId, quantity: 10);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateCartItemCommand(cartId, itemId, 3) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var updatedItem = result.Value.Items.First(i => i.Id == itemId);
        updatedItem.Quantity.ShouldBe(3);
    }

    [Fact]
    public async Task Handle_GuestCart_UpdatesQuantitySuccessfully()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cart = CreateTestCart(cartId, userId: null, sessionId: TestSessionId);
        AddItemToCart(cart, itemId, quantity: 2);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateCartItemCommand(cartId, itemId, 5) { SessionId = TestSessionId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var updatedItem = result.Value.Items.First(i => i.Id == itemId);
        updatedItem.Quantity.ShouldBe(5);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_CartNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Cart.Cart?)null);

        var command = new UpdateCartItemCommand(cartId, itemId, 5) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Message.ShouldContain("Cart");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnauthorizedUser_ReturnsForbiddenError()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cart = CreateTestCart(cartId, userId: "other-user");
        AddItemToCart(cart, itemId);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var command = new UpdateCartItemCommand(cartId, itemId, 5) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        result.Error.Message.ShouldContain("Cart");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ItemNotInCart_ReturnsValidationError()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var nonExistentItemId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart); // Add a different item

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var command = new UpdateCartItemCommand(cartId, nonExistentItemId, 5) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Type.ShouldBe(ErrorType.Validation);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InactiveCart_ReturnsValidationError()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart, itemId);
        cart.MarkAsAbandoned();

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var command = new UpdateCartItemCommand(cartId, itemId, 5) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Type.ShouldBe(ErrorType.Validation);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_PassesTokenToServices()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart, itemId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateCartItemCommand(cartId, itemId, 5) { UserId = TestUserId };

        // Act
        await _handler.Handle(command, token);

        // Assert
        _cartRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsCorrectDtoAfterUpdate()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart, itemId, quantity: 2);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateCartItemCommand(cartId, itemId, 5) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Id.ShouldBe(cartId);
        result.Value.UserId.ShouldBe(TestUserId);
        result.Value.Status.ShouldBe(CartStatus.Active);
        result.Value.Currency.ShouldBe("VND");
        result.Value.ItemCount.ShouldBe(5);
        result.Value.Subtotal.ShouldBe(500m); // 5 * 100
    }

    [Fact]
    public async Task Handle_MultipleItems_OnlyUpdatesTargetItem()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var targetItemId = Guid.NewGuid();
        var otherItemId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart, targetItemId, quantity: 2);
        AddItemToCart(cart, otherItemId, quantity: 3);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateCartItemCommand(cartId, targetItemId, 10) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var targetItem = result.Value.Items.First(i => i.Id == targetItemId);
        var otherItem = result.Value.Items.First(i => i.Id == otherItemId);

        targetItem.Quantity.ShouldBe(10);
        otherItem.Quantity.ShouldBe(3); // Unchanged
    }

    #endregion
}
