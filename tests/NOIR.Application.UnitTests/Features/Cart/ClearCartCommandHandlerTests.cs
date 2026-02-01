using NOIR.Application.Features.Cart.Commands.ClearCart;
using NOIR.Application.Features.Cart.DTOs;
using NOIR.Application.Features.Cart.Specifications;

namespace NOIR.Application.UnitTests.Features.Cart;

/// <summary>
/// Unit tests for ClearCartCommandHandler.
/// Tests cart clearing scenarios with mocked dependencies.
/// </summary>
public class ClearCartCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Domain.Entities.Cart.Cart, Guid>> _cartRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<ClearCartCommandHandler>> _loggerMock;
    private readonly ClearCartCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "test-user-123";
    private const string TestSessionId = "test-session-456";

    public ClearCartCommandHandlerTests()
    {
        _cartRepositoryMock = new Mock<IRepository<Domain.Entities.Cart.Cart, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<ClearCartCommandHandler>>();

        _handler = new ClearCartCommandHandler(
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

    private static void AddItemToCart(Domain.Entities.Cart.Cart cart)
    {
        cart.AddItem(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test Product",
            "Test Variant",
            100m,
            2,
            "http://example.com/image.jpg");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_ValidCartWithItems_ClearsAllItems()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart);
        AddItemToCart(cart);

        cart.ItemCount.Should().Be(4); // 2 items x 2 quantity each

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ClearCartCommand(cartId) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.ItemCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyCart_ReturnsSuccessWithEmptyCart()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ClearCartCommand(cartId) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_GuestCart_ClearsItemsSuccessfully()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestCart(cartId, userId: null, sessionId: TestSessionId);
        AddItemToCart(cart);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ClearCartCommand(cartId) { SessionId = TestSessionId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ItemCount.Should().Be(0);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_CartNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var cartId = Guid.NewGuid();

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Cart.Cart?)null);

        var command = new ClearCartCommand(cartId) { UserId = TestUserId };

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
        var cart = CreateTestCart(cartId, userId: "other-user");

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var command = new ClearCartCommand(cartId) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
        result.Error.Message.Should().Contain("Cart");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InactiveCart_ReturnsValidationError()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        cart.MarkAsAbandoned();

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var command = new ClearCartCommand(cartId) { UserId = TestUserId };

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
        var cart = CreateTestCart(cartId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ClearCartCommand(cartId) { UserId = TestUserId };

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
    public async Task Handle_ReturnsCorrectDtoWithAllFields()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);
        AddItemToCart(cart);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<CartByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ClearCartCommand(cartId) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(cartId);
        result.Value.UserId.Should().Be(TestUserId);
        result.Value.Status.Should().Be(CartStatus.Active);
        result.Value.Currency.Should().Be("VND");
        result.Value.Items.Should().BeEmpty();
        result.Value.IsEmpty.Should().BeTrue();
    }

    #endregion
}
