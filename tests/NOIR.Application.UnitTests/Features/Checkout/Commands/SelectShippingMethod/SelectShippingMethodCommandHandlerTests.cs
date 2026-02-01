using NOIR.Application.Features.Checkout.Commands.SelectShippingMethod;
using NOIR.Application.Features.Checkout.DTOs;
using NOIR.Application.Features.Checkout.Specifications;

namespace NOIR.Application.UnitTests.Features.Checkout.Commands.SelectShippingMethod;

/// <summary>
/// Unit tests for SelectShippingMethodCommandHandler.
/// Tests selecting shipping methods on checkout sessions.
/// </summary>
public class SelectShippingMethodCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<CheckoutSession, Guid>> _checkoutRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly SelectShippingMethodCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestCustomerEmail = "customer@test.com";
    private const string TestShippingMethod = "Standard Delivery";
    private const decimal TestShippingCost = 30000m;

    public SelectShippingMethodCommandHandlerTests()
    {
        _checkoutRepositoryMock = new Mock<IRepository<CheckoutSession, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new SelectShippingMethodCommandHandler(
            _checkoutRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static CheckoutSession CreateTestSession(
        Guid? sessionId = null,
        CheckoutSessionStatus status = CheckoutSessionStatus.AddressComplete,
        bool isExpired = false,
        bool hasShippingAddress = true)
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

        // Set shipping address if needed (required for selecting shipping method)
        if (hasShippingAddress)
        {
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
        }

        // Set status if different from AddressComplete
        if (status != CheckoutSessionStatus.AddressComplete && status != CheckoutSessionStatus.Started)
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

    private static SelectShippingMethodCommand CreateTestCommand(
        Guid sessionId,
        string shippingMethod = TestShippingMethod,
        decimal shippingCost = TestShippingCost,
        DateTimeOffset? estimatedDeliveryAt = null)
    {
        return new SelectShippingMethodCommand(
            SessionId: sessionId,
            ShippingMethod: shippingMethod,
            ShippingCost: shippingCost,
            EstimatedDeliveryAt: estimatedDeliveryAt);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidShippingMethod_ShouldSetMethodAndReturnSuccess()
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

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ShippingMethod.Should().Be(TestShippingMethod);
        result.Value.ShippingCost.Should().Be(TestShippingCost);
        result.Value.Status.Should().Be(CheckoutSessionStatus.ShippingSelected);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithEstimatedDeliveryDate_ShouldSetDeliveryDate()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId);
        var estimatedDelivery = DateTimeOffset.UtcNow.AddDays(3);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateTestCommand(sessionId, estimatedDeliveryAt: estimatedDelivery);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EstimatedDeliveryAt.Should().BeCloseTo(estimatedDelivery, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Handle_ShouldRecalculateGrandTotal()
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

        var command = CreateTestCommand(sessionId, shippingCost: 50000m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GrandTotal.Should().Be(result.Value.SubTotal + 50000m);
    }

    [Theory]
    [InlineData("Express Delivery", 50000)]
    [InlineData("Standard Delivery", 30000)]
    [InlineData("Free Shipping", 0)]
    [InlineData("Premium Same Day", 100000)]
    public async Task Handle_WithDifferentShippingOptions_ShouldSetCorrectly(
        string shippingMethod,
        decimal shippingCost)
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

        var command = CreateTestCommand(sessionId, shippingMethod, shippingCost);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ShippingMethod.Should().Be(shippingMethod);
        result.Value.ShippingCost.Should().Be(shippingCost);
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
        result.Error.Code.Should().Be("NOIR-CHECKOUT-008");
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
        result.Error.Code.Should().Be("NOIR-CHECKOUT-009");
        result.Error.Message.Should().Contain("expired");
    }

    [Fact]
    public async Task Handle_WhenNoShippingAddress_ShouldReturnValidationError()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, hasShippingAddress: false);

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
        result.Error.Code.Should().Be("NOIR-CHECKOUT-010");
        result.Error.Message.Should().Contain("Shipping address must be set");
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
        result.Error.Code.Should().Be("NOIR-CHECKOUT-010");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithZeroShippingCost_ShouldSucceed()
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

        var command = CreateTestCommand(sessionId, "Free Shipping", 0m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ShippingCost.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithNullEstimatedDelivery_ShouldSucceed()
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

        var command = CreateTestCommand(sessionId, estimatedDeliveryAt: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EstimatedDeliveryAt.Should().BeNull();
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
    public async Task Handle_CanUpdateExistingShippingMethod_ShouldSucceed()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, status: CheckoutSessionStatus.ShippingSelected);

        // Set initial shipping method
        session.SelectShippingMethod("Standard", 20000m, null);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateTestCommand(sessionId, "Express", 50000m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ShippingMethod.Should().Be("Express");
        result.Value.ShippingCost.Should().Be(50000m);
    }

    #endregion
}
