namespace NOIR.Application.UnitTests.Features.Shipping;

/// <summary>
/// Unit tests for CancelShippingOrderCommandHandler.
/// Tests shipping order cancellation scenarios.
/// </summary>
public class CancelShippingOrderCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ShippingOrder, Guid>> _orderRepositoryMock;
    private readonly Mock<IRepository<ShippingProvider, Guid>> _providerRepositoryMock;
    private readonly Mock<IShippingProviderFactory> _providerFactoryMock;
    private readonly Mock<IShippingProvider> _shippingProviderMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<CancelShippingOrderCommandHandler>> _loggerMock;
    private readonly CancelShippingOrderCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestTrackingNumber = "GHTK123456789";
    private static readonly Guid TestOrderId = Guid.NewGuid();
    private static readonly Guid TestProviderId = Guid.NewGuid();

    public CancelShippingOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IRepository<ShippingOrder, Guid>>();
        _providerRepositoryMock = new Mock<IRepository<ShippingProvider, Guid>>();
        _providerFactoryMock = new Mock<IShippingProviderFactory>();
        _shippingProviderMock = new Mock<IShippingProvider>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<CancelShippingOrderCommandHandler>>();

        _handler = new CancelShippingOrderCommandHandler(
            _orderRepositoryMock.Object,
            _providerRepositoryMock.Object,
            _providerFactoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    private static ShippingOrder CreateTestOrder(
        ShippingStatus status = ShippingStatus.AwaitingPickup,
        ShippingProviderCode providerCode = ShippingProviderCode.GHTK)
    {
        var order = ShippingOrder.Create(
            TestOrderId,
            TestProviderId,
            providerCode,
            "STANDARD",
            "Standard Delivery",
            "{}",
            "{}",
            "{}",
            "{}",
            "[]",
            1000m,
            500000m,
            200000m,
            false,
            "Test order",
            TestTenantId);

        // Set tracking number and status via reflection
        typeof(ShippingOrder).GetProperty("Id")?.SetValue(order, Guid.NewGuid());
        typeof(ShippingOrder).GetProperty("TrackingNumber")?.SetValue(order, TestTrackingNumber);
        typeof(ShippingOrder).GetProperty("Status")?.SetValue(order, status);

        return order;
    }

    private static ShippingProvider CreateTestProvider()
    {
        var provider = ShippingProvider.Create(
            ShippingProviderCode.GHTK,
            "Test Provider",
            "GHTK Official",
            GatewayEnvironment.Sandbox,
            TestTenantId);

        provider.Activate();
        typeof(ShippingProvider).GetProperty("Id")?.SetValue(provider, TestProviderId);

        return provider;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidDraftOrder_ShouldCancelWithoutProviderCall()
    {
        // Arrange
        var command = new CancelShippingOrderCommand(TestTrackingNumber, "Customer cancelled");
        var order = CreateTestOrder(status: ShippingStatus.Draft);
        var provider = CreateTestProvider();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ShippingStatus.Cancelled);

        // Provider should not be called for Draft orders
        _providerFactoryMock.Verify(x => x.GetProvider(It.IsAny<ShippingProviderCode>()), Times.Once);
        _shippingProviderMock.Verify(x => x.CancelOrderAsync(It.IsAny<string>(), It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithSubmittedOrder_ShouldCancelWithProvider()
    {
        // Arrange
        var command = new CancelShippingOrderCommand(TestTrackingNumber, "Customer cancelled");
        var order = CreateTestOrder(status: ShippingStatus.AwaitingPickup);
        var provider = CreateTestProvider();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns(_shippingProviderMock.Object);

        _shippingProviderMock
            .Setup(x => x.CancelOrderAsync(TestTrackingNumber, It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ShippingStatus.Cancelled);

        _shippingProviderMock.Verify(x => x.CancelOrderAsync(TestTrackingNumber, It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenProviderCancelFails_ShouldStillCancelLocally()
    {
        // Arrange
        var command = new CancelShippingOrderCommand(TestTrackingNumber, "Customer cancelled");
        var order = CreateTestOrder(status: ShippingStatus.InTransit);
        var provider = CreateTestProvider();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns(_shippingProviderMock.Object);

        // Provider cancellation fails
        _shippingProviderMock
            .Setup(x => x.CancelOrderAsync(TestTrackingNumber, It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Failure("PROVIDER_ERROR", "Cannot cancel - already shipped")));

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - Should still succeed locally even if provider fails
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ShippingStatus.Cancelled);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenOrderNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new CancelShippingOrderCommand("NONEXISTENT123", "Test reason");

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingOrder?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().Contain("NONEXISTENT123");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(ShippingStatus.Delivered)]
    [InlineData(ShippingStatus.Cancelled)]
    [InlineData(ShippingStatus.Returned)]
    public async Task Handle_WhenOrderInFinalStatus_ShouldReturnFailure(ShippingStatus status)
    {
        // Arrange
        var command = new CancelShippingOrderCommand(TestTrackingNumber, "Test reason");
        var order = CreateTestOrder(status: status);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Message.Should().Contain(status.ToString());

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProviderConfigNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new CancelShippingOrderCommand(TestTrackingNumber, "Test reason");
        var order = CreateTestOrder(status: ShippingStatus.AwaitingPickup);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().Contain("Provider configuration not found");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithNoProviderImplementation_ShouldStillCancelLocally()
    {
        // Arrange
        var command = new CancelShippingOrderCommand(TestTrackingNumber, "Customer cancelled");
        var order = CreateTestOrder(status: ShippingStatus.AwaitingPickup);
        var provider = CreateTestProvider();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        // No provider implementation
        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns((IShippingProvider?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Status.Should().Be(ShippingStatus.Cancelled);
    }

    [Fact]
    public async Task Handle_CancellationReasonShouldBeIncludedInNotes()
    {
        // Arrange
        var cancellationReason = "Customer changed their mind";
        var command = new CancelShippingOrderCommand(TestTrackingNumber, cancellationReason);
        var order = CreateTestOrder(status: ShippingStatus.AwaitingPickup);
        var provider = CreateTestProvider();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingOrderByTrackingNumberForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns(_shippingProviderMock.Object);

        _shippingProviderMock
            .Setup(x => x.CancelOrderAsync(It.IsAny<string>(), It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        order.Notes.Should().Contain(cancellationReason);
    }

    #endregion
}
