using NOIR.Application.Features.Checkout.Commands.CompleteCheckout;
using NOIR.Application.Features.Checkout.DTOs;
using NOIR.Application.Features.Checkout.Specifications;
using NOIR.Application.Features.Orders.Specifications;

namespace NOIR.Application.UnitTests.Features.Checkout.Commands.CompleteCheckout;

/// <summary>
/// Unit tests for CompleteCheckoutCommandHandler.
/// Tests completing checkout sessions and creating orders.
/// </summary>
public class CompleteCheckoutCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<CheckoutSession, Guid>> _checkoutRepositoryMock;
    private readonly Mock<IRepository<Domain.Entities.Cart.Cart, Guid>> _cartRepositoryMock;
    private readonly Mock<IRepository<Order, Guid>> _orderRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ITenantInfo> _tenantInfoMock;
    private readonly CompleteCheckoutCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestCustomerEmail = "customer@test.com";

    public CompleteCheckoutCommandHandlerTests()
    {
        _checkoutRepositoryMock = new Mock<IRepository<CheckoutSession, Guid>>();
        _cartRepositoryMock = new Mock<IRepository<Domain.Entities.Cart.Cart, Guid>>();
        _orderRepositoryMock = new Mock<IRepository<Order, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _tenantInfoMock = new Mock<ITenantInfo>();

        _tenantInfoMock.Setup(x => x.Id).Returns(TestTenantId);

        _handler = new CompleteCheckoutCommandHandler(
            _checkoutRepositoryMock.Object,
            _cartRepositoryMock.Object,
            _orderRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _tenantInfoMock.Object);
    }

    private static CheckoutSession CreateTestSession(
        Guid? sessionId = null,
        Guid? cartId = null,
        CheckoutSessionStatus status = CheckoutSessionStatus.PaymentPending,
        bool isExpired = false,
        bool hasShippingAddress = true,
        bool hasShippingMethod = true)
    {
        var id = sessionId ?? Guid.NewGuid();
        var cId = cartId ?? Guid.NewGuid();
        var session = CheckoutSession.Create(
            cartId: cId,
            customerEmail: TestCustomerEmail,
            subTotal: 200000m,
            currency: "VND",
            userId: "user-123",
            tenantId: TestTenantId);

        // Use reflection to set the Id
        var idProperty = typeof(CheckoutSession).BaseType?.BaseType?.GetProperty("Id");
        idProperty?.SetValue(session, id);

        // Set shipping address if needed
        if (hasShippingAddress)
        {
            var address = new NOIR.Domain.ValueObjects.Address
            {
                FullName = "Nguyen Van A",
                Phone = "0901234567",
                AddressLine1 = "123 Nguyen Hue",
                AddressLine2 = "Floor 5",
                Ward = "Ben Nghe",
                District = "District 1",
                Province = "Ho Chi Minh City",
                Country = "Vietnam",
                PostalCode = "70000",
                IsDefault = false
            };
            session.SetShippingAddress(address);
        }

        // Set shipping method if needed
        if (hasShippingMethod && hasShippingAddress)
        {
            session.SelectShippingMethod("Standard Delivery", 30000m, DateTimeOffset.UtcNow.AddDays(3));
        }

        // Set payment method to advance status
        if (status == CheckoutSessionStatus.PaymentPending ||
            status == CheckoutSessionStatus.PaymentProcessing)
        {
            session.SelectPaymentMethod(PaymentMethod.COD, null);
        }

        // Set final status if different
        if (status != CheckoutSessionStatus.PaymentPending &&
            status != CheckoutSessionStatus.ShippingSelected &&
            status != CheckoutSessionStatus.AddressComplete &&
            status != CheckoutSessionStatus.Started)
        {
            var statusProperty = typeof(CheckoutSession).GetProperty("Status");
            statusProperty?.SetValue(session, status);
        }

        // Set as expired if needed
        if (isExpired)
        {
            var expiresAtProperty = typeof(CheckoutSession).GetProperty("ExpiresAt");
            expiresAtProperty?.SetValue(session, DateTimeOffset.UtcNow.AddMinutes(-5));
        }

        return session;
    }

    private static Domain.Entities.Cart.Cart CreateTestCart(Guid? cartId = null, bool isEmpty = false)
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
                productName: "Test Product 1",
                variantName: "Default",
                unitPrice: 100000m,
                quantity: 1);

            cart.AddItem(
                productId: Guid.NewGuid(),
                productVariantId: Guid.NewGuid(),
                productName: "Test Product 2",
                variantName: "Large",
                unitPrice: 50000m,
                quantity: 2);
        }

        return cart;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidSession_ShouldCompleteCheckoutAndCreateOrder()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, cartId, CheckoutSessionStatus.PaymentPending);
        var cart = CreateTestCart(cartId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(
                It.IsAny<Order>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken ct) => order);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CompleteCheckoutCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CheckoutSessionStatus.Completed);
        result.Value.OrderId.Should().NotBeNull();
        result.Value.OrderNumber.Should().NotBeNullOrEmpty();

        _orderRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCustomerNotes_ShouldSetNotesOnSession()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, cartId, CheckoutSessionStatus.PaymentPending);
        var cart = CreateTestCart(cartId);
        var customerNotes = "Please deliver before 6 PM";

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken ct) => order);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CompleteCheckoutCommand(sessionId, customerNotes);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerNotes.Should().Be(customerNotes);
    }

    [Fact]
    public async Task Handle_ShouldGenerateCorrectOrderNumber()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, cartId, CheckoutSessionStatus.PaymentPending);
        var cart = CreateTestCart(cartId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null); // No existing orders today

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken ct) => order);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CompleteCheckoutCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrderNumber.Should().StartWith("ORD-");
        result.Value.OrderNumber.Should().EndWith("-0001"); // First order of the day
    }

    [Fact]
    public async Task Handle_ShouldMarkCartAsConverted()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, cartId, CheckoutSessionStatus.PaymentPending);
        var cart = CreateTestCart(cartId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken ct) => order);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CompleteCheckoutCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        cart.Status.Should().Be(CartStatus.Converted);
    }

    [Fact]
    public async Task Handle_FromShippingSelectedStatus_ShouldTransitionToProcessing()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, cartId, CheckoutSessionStatus.ShippingSelected);
        var cart = CreateTestCart(cartId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken ct) => order);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CompleteCheckoutCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CheckoutSessionStatus.Completed);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenSessionNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CheckoutSession?)null);

        var command = new CompleteCheckoutCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-014");
        result.Error.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenSessionExpired_ShouldReturnValidationError()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, isExpired: true);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CompleteCheckoutCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-015");
        result.Error.Message.Should().Contain("expired");
    }

    [Fact]
    public async Task Handle_WhenNoShippingAddress_ShouldReturnValidationError()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, hasShippingAddress: false, hasShippingMethod: false);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var command = new CompleteCheckoutCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-016");
        result.Error.Message.Should().Contain("Shipping address is required");
    }

    [Fact]
    public async Task Handle_WhenNoShippingMethod_ShouldReturnValidationError()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, hasShippingAddress: true, hasShippingMethod: false);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var command = new CompleteCheckoutCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-017");
        result.Error.Message.Should().Contain("Shipping method must be selected");
    }

    [Fact]
    public async Task Handle_WhenCartNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, cartId, CheckoutSessionStatus.PaymentPending);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Cart.Cart?)null);

        var command = new CompleteCheckoutCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-018");
        result.Error.Message.Should().Contain("Cart").And.Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenCartIsEmpty_ShouldReturnValidationError()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, cartId, CheckoutSessionStatus.PaymentPending);
        var emptyCart = CreateTestCart(cartId, isEmpty: true);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyCart);

        var command = new CompleteCheckoutCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-019");
        result.Error.Message.Should().Contain("empty cart");
    }

    [Theory]
    [InlineData(CheckoutSessionStatus.Completed)]
    [InlineData(CheckoutSessionStatus.Expired)]
    [InlineData(CheckoutSessionStatus.Abandoned)]
    public async Task Handle_WhenSessionInFinalStatus_ShouldReturnValidationError(CheckoutSessionStatus status)
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, cartId, status: status);
        var cart = CreateTestCart(cartId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var command = new CompleteCheckoutCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-020");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithNullCustomerNotes_ShouldSucceed()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, cartId, CheckoutSessionStatus.PaymentPending);
        var cart = CreateTestCart(cartId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken ct) => order);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CompleteCheckoutCommand(sessionId, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        var cartId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, cartId, CheckoutSessionStatus.PaymentPending);
        var cart = CreateTestCart(cartId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CartByIdWithItemsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cart);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken ct) => order);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CompleteCheckoutCommand(sessionId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _checkoutRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CheckoutSessionByIdForUpdateSpec>(), token),
            Times.Once);

        _cartRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CartByIdWithItemsForUpdateSpec>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}
