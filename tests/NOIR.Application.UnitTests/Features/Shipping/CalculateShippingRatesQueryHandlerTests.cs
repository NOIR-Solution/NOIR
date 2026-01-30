namespace NOIR.Application.UnitTests.Features.Shipping;

/// <summary>
/// Unit tests for CalculateShippingRatesQueryHandler.
/// Tests shipping rate calculation scenarios with mocked providers.
/// </summary>
public class CalculateShippingRatesQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ShippingProvider, Guid>> _providerRepositoryMock;
    private readonly Mock<IShippingProviderFactory> _providerFactoryMock;
    private readonly Mock<IShippingProvider> _ghtkProviderMock;
    private readonly Mock<IShippingProvider> _ghnProviderMock;
    private readonly Mock<ILogger<CalculateShippingRatesQueryHandler>> _loggerMock;
    private readonly CalculateShippingRatesQueryHandler _handler;

    private const string TestTenantId = "test-tenant";

    public CalculateShippingRatesQueryHandlerTests()
    {
        _providerRepositoryMock = new Mock<IRepository<ShippingProvider, Guid>>();
        _providerFactoryMock = new Mock<IShippingProviderFactory>();
        _ghtkProviderMock = new Mock<IShippingProvider>();
        _ghnProviderMock = new Mock<IShippingProvider>();
        _loggerMock = new Mock<ILogger<CalculateShippingRatesQueryHandler>>();

        _handler = new CalculateShippingRatesQueryHandler(
            _providerRepositoryMock.Object,
            _providerFactoryMock.Object,
            _loggerMock.Object);
    }

    private static CalculateShippingRatesQuery CreateTestQuery(
        decimal weightGrams = 1000m,
        decimal? codAmount = null,
        List<ShippingProviderCode>? preferredProviders = null)
    {
        return new CalculateShippingRatesQuery(
            CreateTestAddress("Origin"),
            CreateTestAddress("Destination"),
            weightGrams,
            30m, // LengthCm
            20m, // WidthCm
            15m, // HeightCm
            500000m, // DeclaredValue
            codAmount,
            false, // RequireInsurance
            preferredProviders);
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

    private static ShippingProvider CreateTestProvider(
        ShippingProviderCode code,
        bool isActive = true)
    {
        var provider = ShippingProvider.Create(
            code,
            $"{code} Provider",
            $"{code} Official Name",
            GatewayEnvironment.Sandbox,
            TestTenantId);

        provider.SetSortOrder(code == ShippingProviderCode.GHTK ? 1 : 2);

        // Use reflection to set Id for testing
        typeof(ShippingProvider).GetProperty("Id")?.SetValue(provider, Guid.NewGuid());

        if (isActive)
        {
            provider.Activate();
        }

        return provider;
    }

    private static List<ShippingRateDto> CreateTestRates(
        ShippingProviderCode provider,
        decimal baseRate,
        string serviceType)
    {
        return new List<ShippingRateDto>
        {
            new ShippingRateDto(
                provider,
                $"{provider} Provider",
                serviceType.ToUpperInvariant(),
                $"{provider} {serviceType}",
                baseRate,
                2000m, // CodFee
                2500m, // InsuranceFee
                baseRate + 2000m + 2500m, // TotalRate
                2, // EstimatedDaysMin
                5, // EstimatedDaysMax
                "VND",
                null)
        };
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithActiveProviders_ShouldReturnRates()
    {
        // Arrange
        var query = CreateTestQuery();
        var ghtkProvider = CreateTestProvider(ShippingProviderCode.GHTK);
        var ghnProvider = CreateTestProvider(ShippingProviderCode.GHN);
        var providers = new List<ShippingProvider> { ghtkProvider, ghnProvider };

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ActiveShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns(_ghtkProviderMock.Object);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHN))
            .Returns(_ghnProviderMock.Object);

        var ghtkRates = CreateTestRates(ShippingProviderCode.GHTK, 45000m, "Standard");
        var ghnRates = CreateTestRates(ShippingProviderCode.GHN, 50000m, "Express");

        _ghtkProviderMock
            .Setup(x => x.CalculateRatesAsync(
                It.IsAny<CalculateShippingRatesRequest>(),
                It.IsAny<ShippingProvider>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ghtkRates));

        _ghnProviderMock
            .Setup(x => x.CalculateRatesAsync(
                It.IsAny<CalculateShippingRatesRequest>(),
                It.IsAny<ShippingProvider>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ghnRates));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Rates.Should().HaveCount(2);
        result.Value.RecommendedRate.Should().NotBeNull();
        result.Value.RecommendedRate!.ProviderCode.Should().Be(ShippingProviderCode.GHTK); // Cheapest
    }

    [Fact]
    public async Task Handle_WithPreferredProviders_ShouldFilterProviders()
    {
        // Arrange
        var query = CreateTestQuery(preferredProviders: new List<ShippingProviderCode> { ShippingProviderCode.GHTK });
        var ghtkProvider = CreateTestProvider(ShippingProviderCode.GHTK);
        var ghnProvider = CreateTestProvider(ShippingProviderCode.GHN);
        var providers = new List<ShippingProvider> { ghtkProvider, ghnProvider };

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ActiveShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns(_ghtkProviderMock.Object);

        var ghtkRates = CreateTestRates(ShippingProviderCode.GHTK, 45000m, "Standard");

        _ghtkProviderMock
            .Setup(x => x.CalculateRatesAsync(
                It.IsAny<CalculateShippingRatesRequest>(),
                It.IsAny<ShippingProvider>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ghtkRates));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rates.Should().HaveCount(1);
        result.Value.Rates.All(r => r.ProviderCode == ShippingProviderCode.GHTK).Should().BeTrue();

        // GHN should not be called
        _providerFactoryMock.Verify(x => x.GetProvider(ShippingProviderCode.GHN), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSortByTotalRate()
    {
        // Arrange
        var query = CreateTestQuery();
        var ghtkProvider = CreateTestProvider(ShippingProviderCode.GHTK);
        var ghnProvider = CreateTestProvider(ShippingProviderCode.GHN);
        var providers = new List<ShippingProvider> { ghtkProvider, ghnProvider };

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ActiveShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns(_ghtkProviderMock.Object);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHN))
            .Returns(_ghnProviderMock.Object);

        // GHN is cheaper this time
        var ghtkRates = CreateTestRates(ShippingProviderCode.GHTK, 60000m, "Standard");
        var ghnRates = CreateTestRates(ShippingProviderCode.GHN, 40000m, "Express");

        _ghtkProviderMock
            .Setup(x => x.CalculateRatesAsync(
                It.IsAny<CalculateShippingRatesRequest>(),
                It.IsAny<ShippingProvider>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ghtkRates));

        _ghnProviderMock
            .Setup(x => x.CalculateRatesAsync(
                It.IsAny<CalculateShippingRatesRequest>(),
                It.IsAny<ShippingProvider>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ghnRates));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rates.First().ProviderCode.Should().Be(ShippingProviderCode.GHN); // Cheapest first
        result.Value.RecommendedRate!.ProviderCode.Should().Be(ShippingProviderCode.GHN);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenNoActiveProviders_ShouldReturnFailure()
    {
        // Arrange
        var query = CreateTestQuery();

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ActiveShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShippingProvider>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_WhenAllProvidersFailToReturnRates_ShouldReturnFailure()
    {
        // Arrange
        var query = CreateTestQuery();
        var ghtkProvider = CreateTestProvider(ShippingProviderCode.GHTK);
        var providers = new List<ShippingProvider> { ghtkProvider };

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ActiveShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns(_ghtkProviderMock.Object);

        _ghtkProviderMock
            .Setup(x => x.CalculateRatesAsync(
                It.IsAny<CalculateShippingRatesRequest>(),
                It.IsAny<ShippingProvider>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<List<ShippingRateDto>>(
                Error.Failure("API_ERROR", "Provider API unavailable")));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Shipping.RateCalculationFailed);
    }

    [Fact]
    public async Task Handle_WhenProviderThrowsException_ShouldContinueWithOthers()
    {
        // Arrange
        var query = CreateTestQuery();
        var ghtkProvider = CreateTestProvider(ShippingProviderCode.GHTK);
        var ghnProvider = CreateTestProvider(ShippingProviderCode.GHN);
        var providers = new List<ShippingProvider> { ghtkProvider, ghnProvider };

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ActiveShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns(_ghtkProviderMock.Object);

        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHN))
            .Returns(_ghnProviderMock.Object);

        // GHTK throws an exception
        _ghtkProviderMock
            .Setup(x => x.CalculateRatesAsync(
                It.IsAny<CalculateShippingRatesRequest>(),
                It.IsAny<ShippingProvider>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Network error"));

        // GHN returns successfully
        var ghnRates = CreateTestRates(ShippingProviderCode.GHN, 50000m, "Express");
        _ghnProviderMock
            .Setup(x => x.CalculateRatesAsync(
                It.IsAny<CalculateShippingRatesRequest>(),
                It.IsAny<ShippingProvider>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ghnRates));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rates.Should().HaveCount(1);
        result.Value.Rates.First().ProviderCode.Should().Be(ShippingProviderCode.GHN);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WhenNoProviderImplementation_ShouldLogAndContinue()
    {
        // Arrange
        var query = CreateTestQuery();
        var ghtkProvider = CreateTestProvider(ShippingProviderCode.GHTK);
        var ghnProvider = CreateTestProvider(ShippingProviderCode.GHN);
        var providers = new List<ShippingProvider> { ghtkProvider, ghnProvider };

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ActiveShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        // GHTK has no implementation
        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHTK))
            .Returns((IShippingProvider?)null);

        // GHN has implementation
        _providerFactoryMock
            .Setup(x => x.GetProvider(ShippingProviderCode.GHN))
            .Returns(_ghnProviderMock.Object);

        var ghnRates = CreateTestRates(ShippingProviderCode.GHN, 50000m, "Express");
        _ghnProviderMock
            .Setup(x => x.CalculateRatesAsync(
                It.IsAny<CalculateShippingRatesRequest>(),
                It.IsAny<ShippingProvider>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(ghnRates));

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rates.Should().HaveCount(1);
        result.Value.Rates.First().ProviderCode.Should().Be(ShippingProviderCode.GHN);
    }

    #endregion
}
