using NOIR.Application.Features.Checkout.DTOs;
using NOIR.Application.Features.Checkout.Queries.GetCheckoutSession;
using NOIR.Application.Features.Checkout.Specifications;

namespace NOIR.Application.UnitTests.Features.Checkout.Queries.GetCheckoutSession;

/// <summary>
/// Unit tests for GetCheckoutSessionQueryHandler.
/// Tests retrieving checkout session by ID.
/// </summary>
public class GetCheckoutSessionQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<CheckoutSession, Guid>> _checkoutRepositoryMock;
    private readonly GetCheckoutSessionQueryHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestCustomerEmail = "customer@test.com";

    public GetCheckoutSessionQueryHandlerTests()
    {
        _checkoutRepositoryMock = new Mock<IRepository<CheckoutSession, Guid>>();

        _handler = new GetCheckoutSessionQueryHandler(
            _checkoutRepositoryMock.Object);
    }

    private static CheckoutSession CreateTestSession(
        Guid? sessionId = null,
        Guid? cartId = null,
        CheckoutSessionStatus status = CheckoutSessionStatus.Started,
        bool hasShippingAddress = false,
        bool hasShippingMethod = false,
        bool hasPaymentMethod = false)
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

        // Set customer info
        session.SetCustomerInfo("Nguyen Van A", "0901234567");

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

        // Set payment method if needed
        if (hasPaymentMethod && hasShippingMethod)
        {
            session.SelectPaymentMethod(PaymentMethod.COD, null);
        }

        // Set final status if different
        if (status != CheckoutSessionStatus.Started &&
            status != CheckoutSessionStatus.AddressComplete &&
            status != CheckoutSessionStatus.ShippingSelected &&
            status != CheckoutSessionStatus.PaymentPending)
        {
            var statusProperty = typeof(CheckoutSession).GetProperty("Status");
            statusProperty?.SetValue(session, status);
        }

        return session;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidSessionId_ShouldReturnSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var cartId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, cartId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var query = new GetCheckoutSessionQuery(sessionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Id.ShouldBe(sessionId);
        result.Value.CartId.ShouldBe(cartId);
        result.Value.CustomerEmail.ShouldBe(TestCustomerEmail);
    }

    [Fact]
    public async Task Handle_WithSessionHavingCustomerInfo_ShouldReturnCustomerInfo()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var query = new GetCheckoutSessionQuery(sessionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.CustomerName.ShouldBe("Nguyen Van A");
        result.Value.CustomerPhone.ShouldBe("0901234567");
    }

    [Fact]
    public async Task Handle_WithSessionHavingShippingAddress_ShouldReturnAddress()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, hasShippingAddress: true);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var query = new GetCheckoutSessionQuery(sessionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShippingAddress.ShouldNotBeNull();
        result.Value.ShippingAddress!.FullName.ShouldBe("Nguyen Van A");
        result.Value.ShippingAddress.Province.ShouldBe("Ho Chi Minh City");
        result.Value.Status.ShouldBe(CheckoutSessionStatus.AddressComplete);
    }

    [Fact]
    public async Task Handle_WithSessionHavingShippingMethod_ShouldReturnShippingInfo()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, hasShippingAddress: true, hasShippingMethod: true);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var query = new GetCheckoutSessionQuery(sessionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShippingMethod.ShouldBe("Standard Delivery");
        result.Value.ShippingCost.ShouldBe(30000m);
        result.Value.EstimatedDeliveryAt.ShouldNotBeNull();
        result.Value.Status.ShouldBe(CheckoutSessionStatus.ShippingSelected);
    }

    [Fact]
    public async Task Handle_WithSessionHavingPaymentMethod_ShouldReturnPaymentInfo()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(
            sessionId,
            hasShippingAddress: true,
            hasShippingMethod: true,
            hasPaymentMethod: true);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var query = new GetCheckoutSessionQuery(sessionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.PaymentMethod.ShouldBe(PaymentMethod.COD);
        result.Value.Status.ShouldBe(CheckoutSessionStatus.PaymentPending);
    }

    [Fact]
    public async Task Handle_WithCompletedSession_ShouldReturnOrderInfo()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var cartId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var orderNumber = "ORD-20260131-0001";

        var session = CreateTestSession(
            sessionId,
            cartId,
            hasShippingAddress: true,
            hasShippingMethod: true,
            hasPaymentMethod: true);

        // Use reflection to set completed state
        var statusProperty = typeof(CheckoutSession).GetProperty("Status");
        statusProperty?.SetValue(session, CheckoutSessionStatus.Completed);

        var orderIdProperty = typeof(CheckoutSession).GetProperty("OrderId");
        orderIdProperty?.SetValue(session, orderId);

        var orderNumberProperty = typeof(CheckoutSession).GetProperty("OrderNumber");
        orderNumberProperty?.SetValue(session, orderNumber);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var query = new GetCheckoutSessionQuery(sessionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Status.ShouldBe(CheckoutSessionStatus.Completed);
        result.Value.OrderId.ShouldBe(orderId);
        result.Value.OrderNumber.ShouldBe(orderNumber);
    }

    [Theory]
    [InlineData(CheckoutSessionStatus.Started)]
    [InlineData(CheckoutSessionStatus.AddressComplete)]
    [InlineData(CheckoutSessionStatus.ShippingSelected)]
    [InlineData(CheckoutSessionStatus.PaymentPending)]
    [InlineData(CheckoutSessionStatus.PaymentProcessing)]
    [InlineData(CheckoutSessionStatus.Completed)]
    [InlineData(CheckoutSessionStatus.Expired)]
    [InlineData(CheckoutSessionStatus.Abandoned)]
    public async Task Handle_WithDifferentStatuses_ShouldReturnCorrectStatus(CheckoutSessionStatus status)
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, hasShippingAddress: true, hasShippingMethod: true);

        // Force the status
        var statusProperty = typeof(CheckoutSession).GetProperty("Status");
        statusProperty?.SetValue(session, status);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var query = new GetCheckoutSessionQuery(sessionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Status.ShouldBe(status);
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
                It.IsAny<CheckoutSessionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CheckoutSession?)null);

        var query = new GetCheckoutSessionQuery(sessionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Code.ShouldBe("NOIR-CHECKOUT-021");
        result.Error.Message.ShouldContain("not found");
    }

    [Fact]
    public async Task Handle_WithNonExistentSessionId_ShouldReturnNotFoundError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CheckoutSession?)null);

        var query = new GetCheckoutSessionQuery(nonExistentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBe(true);
        result.Error.Message.ShouldContain(nonExistentId.ToString());
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var query = new GetCheckoutSessionQuery(sessionId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(query, token);

        // Assert
        _checkoutRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CheckoutSessionByIdSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSessionHavingNoOptionalFields_ShouldReturnNullFields()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var cartId = Guid.NewGuid();
        var session = CheckoutSession.Create(
            cartId: cartId,
            customerEmail: TestCustomerEmail,
            subTotal: 100000m,
            currency: "VND",
            userId: null, // Guest checkout
            tenantId: TestTenantId);

        // Set Id via reflection
        var idProperty = typeof(CheckoutSession).BaseType?.BaseType?.GetProperty("Id");
        idProperty?.SetValue(session, sessionId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var query = new GetCheckoutSessionQuery(sessionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.UserId.ShouldBeNull();
        result.Value.CustomerName.ShouldBeNull();
        result.Value.CustomerPhone.ShouldBeNull();
        result.Value.ShippingAddress.ShouldBeNull();
        result.Value.BillingAddress.ShouldBeNull();
        result.Value.ShippingMethod.ShouldBeNull();
        result.Value.PaymentMethod.ShouldBeNull();
        result.Value.OrderId.ShouldBeNull();
        result.Value.OrderNumber.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnCorrectDtoMapping()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var cartId = Guid.NewGuid();
        var session = CreateTestSession(
            sessionId,
            cartId,
            hasShippingAddress: true,
            hasShippingMethod: true,
            hasPaymentMethod: true);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var query = new GetCheckoutSessionQuery(sessionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value;

        // Verify all DTO fields are populated correctly
        dto.Id.ShouldBe(sessionId);
        dto.CartId.ShouldBe(cartId);
        dto.CustomerEmail.ShouldBe(TestCustomerEmail);
        dto.Currency.ShouldBe("VND");
        dto.SubTotal.ShouldBe(200000m);
        dto.BillingSameAsShipping.ShouldBe(true);
        dto.ExpiresAt.ShouldBeGreaterThan(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Handle_WithGrandTotalCalculated_ShouldReturnCorrectTotals()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(
            sessionId,
            hasShippingAddress: true,
            hasShippingMethod: true);

        // Grand total should be: SubTotal (200000) + ShippingCost (30000) = 230000
        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var query = new GetCheckoutSessionQuery(sessionId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.SubTotal.ShouldBe(200000m);
        result.Value.ShippingCost.ShouldBe(30000m);
        result.Value.GrandTotal.ShouldBe(230000m);
    }

    #endregion
}
