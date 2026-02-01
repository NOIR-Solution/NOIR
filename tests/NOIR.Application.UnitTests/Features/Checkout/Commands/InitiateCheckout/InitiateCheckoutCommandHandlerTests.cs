using NOIR.Application.Features.Checkout.Commands.InitiateCheckout;
using NOIR.Application.Features.Checkout.DTOs;
using NOIR.Application.Features.Checkout.Specifications;

namespace NOIR.Application.UnitTests.Features.Checkout.Commands.InitiateCheckout;

/// <summary>
/// Unit tests for InitiateCheckoutCommandHandler.
/// Tests checkout session creation from an active cart.
/// </summary>
public class InitiateCheckoutCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Domain.Entities.Cart.Cart, Guid>> _cartRepositoryMock;
    private readonly Mock<IRepository<CheckoutSession, Guid>> _checkoutRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITenantInfo> _tenantInfoMock;
    private readonly InitiateCheckoutCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestCustomerEmail = "customer@test.com";
    private const string TestCustomerName = "Test Customer";
    private const string TestCustomerPhone = "0901234567";

    public InitiateCheckoutCommandHandlerTests()
    {
        _cartRepositoryMock = new Mock<IRepository<Domain.Entities.Cart.Cart, Guid>>();
        _checkoutRepositoryMock = new Mock<IRepository<CheckoutSession, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _tenantInfoMock = new Mock<ITenantInfo>();

        _tenantInfoMock.Setup(x => x.Id).Returns(TestTenantId);

        _handler = new InitiateCheckoutCommandHandler(
            _cartRepositoryMock.Object,
            _checkoutRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _tenantInfoMock.Object);
    }

    private static Domain.Entities.Cart.Cart CreateTestCart(
        Guid? cartId = null,
        CartStatus status = CartStatus.Active,
        bool isEmpty = false)
    {
        var id = cartId ?? Guid.NewGuid();
        var cart = Domain.Entities.Cart.Cart.CreateForUser("user-123", "VND", TestTenantId);

        // Use reflection to set the Id
        var idProperty = typeof(Domain.Entities.Cart.Cart).BaseType?.BaseType?.GetProperty("Id");
        idProperty?.SetValue(cart, id);

        if (!isEmpty)
        {
            cart.AddItem(
                productId: Guid.NewGuid(),
                productVariantId: Guid.NewGuid(),
                productName: "Test Product",
                variantName: "Default",
                unitPrice: 100000m,
                quantity: 2);
        }

        // Set status if not Active
        if (status != CartStatus.Active)
        {
            var statusProperty = typeof(Domain.Entities.Cart.Cart).GetProperty("Status");
            statusProperty?.SetValue(cart, status);
        }

        return cart;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidActiveCart_ShouldCreateCheckoutSession()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ActiveCheckoutSessionByCartIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CheckoutSession?)null);

        _checkoutRepositoryMock
            .Setup(x => x.AddAsync(
                It.IsAny<CheckoutSession>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CheckoutSession session, CancellationToken ct) => session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new InitiateCheckoutCommand(
            CartId: cartId,
            CustomerEmail: TestCustomerEmail,
            CustomerName: TestCustomerName,
            CustomerPhone: TestCustomerPhone);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CartId.Should().Be(cartId);
        result.Value.CustomerEmail.Should().Be(TestCustomerEmail);
        result.Value.Status.Should().Be(CheckoutSessionStatus.Started);

        _checkoutRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<CheckoutSession>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingActiveSession_ShouldReturnExistingSession()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);

        var existingSession = CheckoutSession.Create(
            cartId: cartId,
            customerEmail: TestCustomerEmail,
            subTotal: cart.Subtotal,
            currency: "VND",
            userId: "user-123",
            tenantId: TestTenantId);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ActiveCheckoutSessionByCartIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSession);

        var command = new InitiateCheckoutCommand(
            CartId: cartId,
            CustomerEmail: TestCustomerEmail);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(existingSession.Id);

        _checkoutRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<CheckoutSession>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithOptionalCustomerInfo_ShouldSetCustomerInfo()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ActiveCheckoutSessionByCartIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CheckoutSession?)null);

        _checkoutRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<CheckoutSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CheckoutSession session, CancellationToken ct) => session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new InitiateCheckoutCommand(
            CartId: cartId,
            CustomerEmail: TestCustomerEmail,
            CustomerName: TestCustomerName,
            CustomerPhone: TestCustomerPhone);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerName.Should().Be(TestCustomerName);
        result.Value.CustomerPhone.Should().Be(TestCustomerPhone);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenCartNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var cartId = Guid.NewGuid();

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Cart.Cart?)null);

        var command = new InitiateCheckoutCommand(
            CartId: cartId,
            CustomerEmail: TestCustomerEmail);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-001");
        result.Error.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenCartIsEmpty_ShouldReturnValidationError()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var emptyCart = CreateTestCart(cartId, isEmpty: true);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyCart);

        var command = new InitiateCheckoutCommand(
            CartId: cartId,
            CustomerEmail: TestCustomerEmail);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-002");
        result.Error.Message.Should().Contain("empty cart");
    }

    [Theory]
    [InlineData(CartStatus.Abandoned)]
    [InlineData(CartStatus.Converted)]
    [InlineData(CartStatus.Expired)]
    [InlineData(CartStatus.Merged)]
    public async Task Handle_WhenCartNotActive_ShouldReturnValidationError(CartStatus status)
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestCart(cartId, status: status);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        var command = new InitiateCheckoutCommand(
            CartId: cartId,
            CustomerEmail: TestCustomerEmail);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-003");
        result.Error.Message.Should().Contain(status.ToString());
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithExpiredExistingSession_ShouldCreateNewSession()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);

        // Create an expired session by setting ExpiresAt in the past
        var expiredSession = CheckoutSession.Create(
            cartId: cartId,
            customerEmail: TestCustomerEmail,
            subTotal: cart.Subtotal,
            currency: "VND",
            userId: "user-123",
            tenantId: TestTenantId);

        // Use reflection to set ExpiresAt to past
        var expiresAtProperty = typeof(CheckoutSession).GetProperty("ExpiresAt");
        expiresAtProperty?.SetValue(expiredSession, DateTimeOffset.UtcNow.AddMinutes(-5));

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ActiveCheckoutSessionByCartIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expiredSession);

        _checkoutRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<CheckoutSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CheckoutSession session, CancellationToken ct) => session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new InitiateCheckoutCommand(
            CartId: cartId,
            CustomerEmail: TestCustomerEmail);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().NotBe(expiredSession.Id);

        _checkoutRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<CheckoutSession>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var cart = CreateTestCart(cartId);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ActiveCheckoutSessionByCartIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CheckoutSession?)null);

        _checkoutRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<CheckoutSession>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CheckoutSession session, CancellationToken ct) => session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new InitiateCheckoutCommand(
            CartId: cartId,
            CustomerEmail: TestCustomerEmail);

        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _cartRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CartByIdWithItemsSpec>(), token),
            Times.Once);

        _checkoutRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ActiveCheckoutSessionByCartIdSpec>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}
