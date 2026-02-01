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
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(sessionId);
        result.Value.CartId.Should().Be(cartId);
        result.Value.CustomerEmail.Should().Be(TestCustomerEmail);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerName.Should().Be("Nguyen Van A");
        result.Value.CustomerPhone.Should().Be("0901234567");
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
        result.IsSuccess.Should().BeTrue();
        result.Value.ShippingAddress.Should().NotBeNull();
        result.Value.ShippingAddress!.FullName.Should().Be("Nguyen Van A");
        result.Value.ShippingAddress.Province.Should().Be("Ho Chi Minh City");
        result.Value.Status.Should().Be(CheckoutSessionStatus.AddressComplete);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.ShippingMethod.Should().Be("Standard Delivery");
        result.Value.ShippingCost.Should().Be(30000m);
        result.Value.EstimatedDeliveryAt.Should().NotBeNull();
        result.Value.Status.Should().Be(CheckoutSessionStatus.ShippingSelected);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentMethod.Should().Be(PaymentMethod.COD);
        result.Value.Status.Should().Be(CheckoutSessionStatus.PaymentPending);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CheckoutSessionStatus.Completed);
        result.Value.OrderId.Should().Be(orderId);
        result.Value.OrderNumber.Should().Be(orderNumber);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(status);
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
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-021");
        result.Error.Message.Should().Contain("not found");
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
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Contain(nonExistentId.ToString());
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
        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().BeNull();
        result.Value.CustomerName.Should().BeNull();
        result.Value.CustomerPhone.Should().BeNull();
        result.Value.ShippingAddress.Should().BeNull();
        result.Value.BillingAddress.Should().BeNull();
        result.Value.ShippingMethod.Should().BeNull();
        result.Value.PaymentMethod.Should().BeNull();
        result.Value.OrderId.Should().BeNull();
        result.Value.OrderNumber.Should().BeNull();
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
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;

        // Verify all DTO fields are populated correctly
        dto.Id.Should().Be(sessionId);
        dto.CartId.Should().Be(cartId);
        dto.CustomerEmail.Should().Be(TestCustomerEmail);
        dto.Currency.Should().Be("VND");
        dto.SubTotal.Should().Be(200000m);
        dto.BillingSameAsShipping.Should().BeTrue();
        dto.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.SubTotal.Should().Be(200000m);
        result.Value.ShippingCost.Should().Be(30000m);
        result.Value.GrandTotal.Should().Be(230000m);
    }

    #endregion
}
