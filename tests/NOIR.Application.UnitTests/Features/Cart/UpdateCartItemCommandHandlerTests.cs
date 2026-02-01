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
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        var updatedItem = result.Value.Items.FirstOrDefault(i => i.Id == itemId);
        updatedItem.Should().NotBeNull();
        updatedItem!.Quantity.Should().Be(5);

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
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().NotContain(i => i.Id == itemId);
        result.Value.IsEmpty.Should().BeTrue();

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
        result.IsSuccess.Should().BeTrue();
        var updatedItem = result.Value.Items.First(i => i.Id == itemId);
        updatedItem.Quantity.Should().Be(10);
        updatedItem.LineTotal.Should().Be(1000m); // 10 * 100
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
        result.IsSuccess.Should().BeTrue();
        var updatedItem = result.Value.Items.First(i => i.Id == itemId);
        updatedItem.Quantity.Should().Be(3);
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
        result.IsSuccess.Should().BeTrue();
        var updatedItem = result.Value.Items.First(i => i.Id == itemId);
        updatedItem.Quantity.Should().Be(5);
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
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().Contain("Cart");

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
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
        result.Error.Message.Should().Contain("Cart");

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
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);

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
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Validation);

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
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(cartId);
        result.Value.UserId.Should().Be(TestUserId);
        result.Value.Status.Should().Be(CartStatus.Active);
        result.Value.Currency.Should().Be("VND");
        result.Value.ItemCount.Should().Be(5);
        result.Value.Subtotal.Should().Be(500m); // 5 * 100
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
        result.IsSuccess.Should().BeTrue();
        var targetItem = result.Value.Items.First(i => i.Id == targetItemId);
        var otherItem = result.Value.Items.First(i => i.Id == otherItemId);

        targetItem.Quantity.Should().Be(10);
        otherItem.Quantity.Should().Be(3); // Unchanged
    }

    #endregion
}
