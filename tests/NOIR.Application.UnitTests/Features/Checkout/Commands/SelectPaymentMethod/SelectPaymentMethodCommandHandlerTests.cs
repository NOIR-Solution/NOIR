using NOIR.Application.Features.Checkout.Commands.SelectPaymentMethod;
using NOIR.Application.Features.Checkout.DTOs;
using NOIR.Application.Features.Checkout.Specifications;

namespace NOIR.Application.UnitTests.Features.Checkout.Commands.SelectPaymentMethod;

/// <summary>
/// Unit tests for SelectPaymentMethodCommandHandler.
/// Tests selecting payment methods on checkout sessions.
/// </summary>
public class SelectPaymentMethodCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<CheckoutSession, Guid>> _checkoutRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly SelectPaymentMethodCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestCustomerEmail = "customer@test.com";

    public SelectPaymentMethodCommandHandlerTests()
    {
        _checkoutRepositoryMock = new Mock<IRepository<CheckoutSession, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new SelectPaymentMethodCommandHandler(
            _checkoutRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static CheckoutSession CreateTestSession(
        Guid? sessionId = null,
        CheckoutSessionStatus status = CheckoutSessionStatus.ShippingSelected,
        bool isExpired = false)
    {
        var id = sessionId ?? Guid.NewGuid();
        var session = CheckoutSession.Create(
            cartId: Guid.NewGuid(),
            customerEmail: TestCustomerEmail,
            subTotal: 200000m,
            currency: "VND",
            userId: "user-123",
            tenantId: TestTenantId);

        // Use reflection to set the Id
        var idProperty = typeof(CheckoutSession).BaseType?.BaseType?.GetProperty("Id");
        idProperty?.SetValue(session, id);

        // Set shipping address (required for shipping method selection)
        var address = new NOIR.Domain.ValueObjects.Address
        {
            FullName = "Test Customer",
            Phone = "0901234567",
            AddressLine1 = "123 Test Street",
            AddressLine2 = null,
            Ward = "Ward 1",
            District = "District 1",
            Province = "Ho Chi Minh City",
            Country = "Vietnam",
            PostalCode = "70000",
            IsDefault = false
        };
        session.SetShippingAddress(address);

        // Select shipping method to advance to ShippingSelected status
        if (status == CheckoutSessionStatus.ShippingSelected ||
            status == CheckoutSessionStatus.PaymentPending ||
            status == CheckoutSessionStatus.PaymentProcessing)
        {
            session.SelectShippingMethod("Standard Delivery", 30000m, DateTimeOffset.UtcNow.AddDays(3));
        }

        // Set status if different from ShippingSelected
        if (status != CheckoutSessionStatus.ShippingSelected &&
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

    private static SelectPaymentMethodCommand CreateTestCommand(
        Guid sessionId,
        PaymentMethod paymentMethod = PaymentMethod.COD,
        Guid? paymentGatewayId = null)
    {
        return new SelectPaymentMethodCommand(
            SessionId: sessionId,
            PaymentMethod: paymentMethod,
            PaymentGatewayId: paymentGatewayId);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCODPaymentMethod_ShouldSetMethodAndReturnSuccess()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateTestCommand(sessionId, PaymentMethod.COD);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentMethod.Should().Be(PaymentMethod.COD);
        result.Value.Status.Should().Be(CheckoutSessionStatus.PaymentPending);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithPaymentGateway_ShouldSetGatewayId()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var gatewayId = Guid.NewGuid();
        var session = CreateTestSession(sessionId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateTestCommand(sessionId, PaymentMethod.BankTransfer, gatewayId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentMethod.Should().Be(PaymentMethod.BankTransfer);
        result.Value.PaymentGatewayId.Should().Be(gatewayId);
    }

    [Theory]
    [InlineData(PaymentMethod.COD)]
    [InlineData(PaymentMethod.BankTransfer)]
    [InlineData(PaymentMethod.CreditCard)]
    [InlineData(PaymentMethod.EWallet)]
    public async Task Handle_WithDifferentPaymentMethods_ShouldSetCorrectly(PaymentMethod paymentMethod)
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateTestCommand(sessionId, paymentMethod);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentMethod.Should().Be(paymentMethod);
    }

    [Fact]
    public async Task Handle_ShouldTransitionToPaymentPendingStatus()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, CheckoutSessionStatus.ShippingSelected);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateTestCommand(sessionId, PaymentMethod.COD);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CheckoutSessionStatus.PaymentPending);
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

        var command = CreateTestCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-011");
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

        var command = CreateTestCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-012");
        result.Error.Message.Should().Contain("expired");
    }

    [Theory]
    [InlineData(CheckoutSessionStatus.Completed)]
    [InlineData(CheckoutSessionStatus.Expired)]
    [InlineData(CheckoutSessionStatus.Abandoned)]
    public async Task Handle_WhenSessionInFinalStatus_ShouldReturnValidationError(CheckoutSessionStatus status)
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, status: status);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var command = CreateTestCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-013");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithNullPaymentGatewayId_ShouldSucceed()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateTestCommand(sessionId, PaymentMethod.COD, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentGatewayId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_CanUpdateExistingPaymentMethod_ShouldSucceed()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, status: CheckoutSessionStatus.PaymentPending);

        // Set initial payment method
        session.SelectPaymentMethod(PaymentMethod.COD, null);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var newGatewayId = Guid.NewGuid();
        var command = CreateTestCommand(sessionId, PaymentMethod.BankTransfer, newGatewayId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentMethod.Should().Be(PaymentMethod.BankTransfer);
        result.Value.PaymentGatewayId.Should().Be(newGatewayId);
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateTestCommand(sessionId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _checkoutRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CheckoutSessionByIdForUpdateSpec>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_FromAddressCompleteStatus_ShouldNotTransitionToPaymentPending()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, status: CheckoutSessionStatus.AddressComplete);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateTestCommand(sessionId, PaymentMethod.COD);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Status should remain AddressComplete since it's not ShippingSelected
        result.Value.Status.Should().Be(CheckoutSessionStatus.AddressComplete);
    }

    #endregion
}
