using NOIR.Application.Features.Cart.Commands.RemoveCartItem;
using NOIR.Application.Features.Cart.DTOs;
using NOIR.Application.Features.Cart.Specifications;

namespace NOIR.Application.UnitTests.Features.Cart;

/// <summary>
/// Unit tests for RemoveCartItemCommandHandler.
/// Tests cart item removal scenarios with mocked dependencies.
/// </summary>
public class RemoveCartItemCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Domain.Entities.Cart.Cart, Guid>> _cartRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<RemoveCartItemCommandHandler>> _loggerMock;
    private readonly RemoveCartItemCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "test-user-123";
    private const string TestSessionId = "test-session-456";

    public RemoveCartItemCommandHandlerTests()
    {
        _cartRepositoryMock = new Mock<IRepository<Domain.Entities.Cart.Cart, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<RemoveCartItemCommandHandler>>();

        _handler = new RemoveCartItemCommandHandler(
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

    private static CartItem AddItemToCart(Domain.Entities.Cart.Cart cart, Guid? itemId = null)
    {
        var item = cart.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Product",
            "Test Variant",
            100m,
            2,
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
    public async Task Handle_ValidItemId_RemovesItemFromCart()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart, itemId);
        AddItemToCart(cart); // Add another item

        var initialItemCount = cart.Items.Count;

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RemoveCartItemCommand(cartId, itemId) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.Items.Count().ShouldBe(initialItemCount - 1);
        result.Value.Items.ShouldNotContain(i => i.Id == itemId);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_LastItemInCart_ReturnsEmptyCart()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart, itemId);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RemoveCartItemCommand(cartId, itemId) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.ShouldBeEmpty();
        result.Value.IsEmpty.ShouldBe(true);
    }

    [Fact]
    public async Task Handle_GuestCart_RemovesItemSuccessfully()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cart = CreateTestCart(cartId, userId: null, sessionId: TestSessionId);
        AddItemToCart(cart, itemId);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RemoveCartItemCommand(cartId, itemId) { SessionId = TestSessionId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.ShouldBeEmpty();
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

        var command = new RemoveCartItemCommand(cartId, itemId) { UserId = TestUserId };

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

        var command = new RemoveCartItemCommand(cartId, itemId) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        result.Error.Message.ShouldContain("Cart");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ItemNotInCart_DoesNotThrow()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var nonExistentItemId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart); // Add a different item

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RemoveCartItemCommand(cartId, nonExistentItemId) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        // RemoveItem doesn't throw if item doesn't exist
        result.IsSuccess.ShouldBe(true);
        result.Value.Items.Count().ShouldBe(1); // Original item still there
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

        var command = new RemoveCartItemCommand(cartId, itemId) { UserId = TestUserId };

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

        var command = new RemoveCartItemCommand(cartId, itemId) { UserId = TestUserId };

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
    public async Task Handle_ReturnsCorrectDtoAfterRemoval()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart, itemId);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RemoveCartItemCommand(cartId, itemId) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Id.ShouldBe(cartId);
        result.Value.UserId.ShouldBe(TestUserId);
        result.Value.Status.ShouldBe(CartStatus.Active);
        result.Value.Currency.ShouldBe("VND");
    }

    #endregion
}
