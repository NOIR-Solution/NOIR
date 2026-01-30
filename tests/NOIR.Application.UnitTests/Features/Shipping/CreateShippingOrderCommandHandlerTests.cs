namespace NOIR.Application.UnitTests.Features.Shipping;

/// <summary>
/// Unit tests for CreateShippingOrderCommandHandler.
/// Tests shipping order creation scenarios with mocked dependencies.
/// </summary>
public class CreateShippingOrderCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ShippingProvider, Guid>> _providerRepositoryMock;
    private readonly Mock<IRepository<ShippingOrder, Guid>> _orderRepositoryMock;
    private readonly Mock<IShippingProviderFactory> _providerFactoryMock;
    private readonly Mock<IShippingProvider> _shippingProviderMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly Mock<ILogger<CreateShippingOrderCommandHandler>> _loggerMock;
    private readonly CreateShippingOrderCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private static readonly Guid TestOrderId = Guid.NewGuid();
    private static readonly Guid TestProviderId = Guid.NewGuid();

    public CreateShippingOrderCommandHandlerTests()
    {
        _providerRepositoryMock = new Mock<IRepository<ShippingProvider, Guid>>();
        _orderRepositoryMock = new Mock<IRepository<ShippingOrder, Guid>>();
        _providerFactoryMock = new Mock<IShippingProviderFactory>();
        _shippingProviderMock = new Mock<IShippingProvider>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();
        _loggerMock = new Mock<ILogger<CreateShippingOrderCommandHandler>>();

        // Setup default current user
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new CreateShippingOrderCommandHandler(
            _providerRepositoryMock.Object,
            _orderRepositoryMock.Object,
            _providerFactoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object,
            _loggerMock.Object);
    }

    private static CreateShippingOrderCommand CreateTestCommand(
        Guid? orderId = null,
        ShippingProviderCode providerCode = ShippingProviderCode.GHTK,
        string serviceTypeCode = "STANDARD")
    {
        return new CreateShippingOrderCommand(
            orderId ?? TestOrderId,
            providerCode,
            serviceTypeCode,
            CreateTestAddress("Pickup"),
            CreateTestAddress("Delivery"),
            CreateTestContact("Sender"),
            CreateTestContact("Recipient"),
            new List<ShippingItemDto> { CreateTestItem() },
            1000m, // TotalWeightGrams
            500000m, // DeclaredValue
            200000m, // CodAmount
            false, // IsFreeship
            false, // RequireInsurance
            "Test notes");
    }

    private static ShippingAddressDto CreateTestAddress(string name) =>
        new ShippingAddressDto(
            name,                  // FullName
            "0901234567",          // Phone
            $"{name.ToLower()}@test.com",  // Email
            "123 Test Street",     // AddressLine1
            null,                  // AddressLine2
            "Ward 1",              // Ward
            "W001",                // WardCode
            "District 1",          // District
            "D001",                // DistrictCode
            "Ho Chi Minh City",    // Province
            "79",                  // ProvinceCode
            "70000",               // PostalCode
            "VN");                 // CountryCode

    private static ShippingContactDto CreateTestContact(string name) =>
        new ShippingContactDto(name, "0901234567", $"{name.ToLower()}@test.com");

    private static ShippingItemDto CreateTestItem() =>
        new ShippingItemDto("Test Product", 1, 500m, 100000m, "SKU001");

    private static ShippingProvider CreateTestProvider(
        bool isActive = true,
        ShippingProviderCode code = ShippingProviderCode.GHTK)
    {
        var provider = ShippingProvider.Create(
            code,
            "Test Provider",
            $"{code} Official Name",
            GatewayEnvironment.Sandbox,
            TestTenantId);

        provider.SetSortOrder(1);
        provider.SetApiBaseUrl("https://api.test.com");
        provider.SetTrackingUrlTemplate("https://track.test.com/{trackingNumber}");
        provider.Configure("encrypted-credentials", "webhook-secret");
        provider.SetCodSupport(true);
        provider.SetCodLimits(0, 10000000m);

        // Use reflection to set Id
        typeof(ShippingProvider).GetProperty("Id")?.SetValue(provider, TestProviderId);

        if (isActive)
        {
            provider.Activate();
        }

        return provider;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateShippingOrder()
    {
        // Arrange
        var command = CreateTestCommand();
        var providerConfig = CreateTestProvider();
        var trackingNumber = "GHTK123456789";

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerConfig);

        _providerFactoryMock
            .Setup(x => x.GetProvider(command.ProviderCode))
            .Returns(_shippingProviderMock.Object);

        _shippingProviderMock
            .Setup(x => x.CreateOrderAsync(
                It.IsAny<CreateShippingOrderRequest>(),
                It.IsAny<ShippingProvider>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new ProviderShippingOrderResult(
                trackingNumber,
                "PROVIDER-123",
                null,
                50000m,
                2000m,
                2500m,
                DateTimeOffset.UtcNow.AddDays(3),
                "{}")));

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ShippingOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingOrder entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.TrackingNumber.Should().Be(trackingNumber);
        result.Value.ProviderCode.Should().Be(command.ProviderCode);
        result.Value.Status.Should().Be(ShippingStatus.AwaitingPickup);

        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ShippingOrder>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenProviderNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = CreateTestCommand();

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().Contain("not configured or inactive");

        _providerFactoryMock.Verify(x => x.GetProvider(It.IsAny<ShippingProviderCode>()), Times.Never);
        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ShippingOrder>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProviderNotActive_ShouldReturnFailure()
    {
        // Arrange
        var command = CreateTestCommand();
        var inactiveProvider = CreateTestProvider(isActive: false);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveProvider);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().Contain("not configured or inactive");
    }

    [Fact]
    public async Task Handle_WhenProviderImplementationNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = CreateTestCommand();
        var providerConfig = CreateTestProvider();

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerConfig);

        _providerFactoryMock
            .Setup(x => x.GetProvider(command.ProviderCode))
            .Returns((IShippingProvider?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Shipping.ProviderNotConfigured);
        result.Error.Message.Should().Contain("No implementation available");
    }

    [Fact]
    public async Task Handle_WhenProviderRejectsOrder_ShouldCancelDraftAndReturnFailure()
    {
        // Arrange
        var command = CreateTestCommand();
        var providerConfig = CreateTestProvider();
        var providerError = Error.Failure("PROVIDER_ERROR", "Invalid address");

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerConfig);

        _providerFactoryMock
            .Setup(x => x.GetProvider(command.ProviderCode))
            .Returns(_shippingProviderMock.Object);

        _shippingProviderMock
            .Setup(x => x.CreateOrderAsync(
                It.IsAny<CreateShippingOrderRequest>(),
                It.IsAny<ShippingProvider>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<ProviderShippingOrderResult>(providerError));

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ShippingOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingOrder entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Message.Should().Be(providerError.Message);

        // Verify draft order was created then cancelled
        _orderRepositoryMock.Verify(x => x.AddAsync(It.IsAny<ShippingOrder>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2)); // Once for draft, once for cancel
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithDifferentProviders_ShouldUseCorrectProvider()
    {
        // Arrange
        var command = CreateTestCommand(providerCode: ShippingProviderCode.GHN, serviceTypeCode: "1");
        var providerConfig = CreateTestProvider(code: ShippingProviderCode.GHN);
        var trackingNumber = "GHN987654321";

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerConfig);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHN))
            .Returns(_shippingProviderMock.Object);

        _shippingProviderMock
            .Setup(x => x.CreateOrderAsync(
                It.IsAny<CreateShippingOrderRequest>(),
                It.IsAny<ShippingProvider>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new ProviderShippingOrderResult(
                trackingNumber,
                "GHN-ORDER-123",
                null,
                45000m,
                1500m,
                2000m,
                DateTimeOffset.UtcNow.AddDays(2),
                "{}")));

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ShippingOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingOrder entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderCode.Should().Be(ShippingProviderCode.GHN);
        result.Value.TrackingNumber.Should().Be(trackingNumber);

        _providerFactoryMock.Verify(x => x.GetProvider(ShippingProviderCode.GHN), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCodOrder_ShouldIncludeCodAmount()
    {
        // Arrange
        var codAmount = 350000m;
        var command = new CreateShippingOrderCommand(
            TestOrderId,
            ShippingProviderCode.GHTK,
            "STANDARD",
            CreateTestAddress("Pickup"),
            CreateTestAddress("Delivery"),
            CreateTestContact("Sender"),
            CreateTestContact("Recipient"),
            new List<ShippingItemDto> { CreateTestItem() },
            1000m,
            500000m,
            codAmount,
            false,
            false,
            null);

        var providerConfig = CreateTestProvider();

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providerConfig);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns(_shippingProviderMock.Object);

        _shippingProviderMock
            .Setup(x => x.CreateOrderAsync(
                It.Is<CreateShippingOrderRequest>(r => r.CodAmount == codAmount),
                It.IsAny<ShippingProvider>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new ProviderShippingOrderResult(
                "GHTK-COD-123",
                "PROVIDER-COD",
                null,
                50000m,
                3500m, // COD fee
                0m,
                DateTimeOffset.UtcNow.AddDays(3),
                "{}")));

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ShippingOrder>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingOrder entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CodAmount.Should().Be(codAmount);
        result.Value.CodFee.Should().Be(3500m);

        _shippingProviderMock.Verify(x => x.CreateOrderAsync(
            It.Is<CreateShippingOrderRequest>(r => r.CodAmount == codAmount),
            It.IsAny<ShippingProvider>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
