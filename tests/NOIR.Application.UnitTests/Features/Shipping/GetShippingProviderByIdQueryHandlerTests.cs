using NOIR.Application.Features.Shipping.DTOs;
using NOIR.Application.Features.Shipping.Queries.GetShippingProviderById;
using NOIR.Application.Features.Shipping.Specifications;

namespace NOIR.Application.UnitTests.Features.Shipping;

/// <summary>
/// Unit tests for GetShippingProviderByIdQueryHandler.
/// Tests shipping provider retrieval by ID scenarios.
/// </summary>
public class GetShippingProviderByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ShippingProvider, Guid>> _providerRepositoryMock;
    private readonly GetShippingProviderByIdQueryHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestEncryptedCredentials = "encrypted_credentials_abc123";

    public GetShippingProviderByIdQueryHandlerTests()
    {
        _providerRepositoryMock = new Mock<IRepository<ShippingProvider, Guid>>();
        _handler = new GetShippingProviderByIdQueryHandler(_providerRepositoryMock.Object);
    }

    private static ShippingProvider CreateTestProvider(
        Guid? id = null,
        ShippingProviderCode providerCode = ShippingProviderCode.GHTK,
        string displayName = "Test Provider",
        int sortOrder = 1,
        bool isActive = true,
        bool supportsCod = true,
        bool supportsInsurance = false,
        GatewayEnvironment environment = GatewayEnvironment.Sandbox,
        string? tenantId = TestTenantId)
    {
        var provider = ShippingProvider.Create(
            providerCode,
            displayName,
            providerCode.ToString(),
            environment,
            tenantId);

        if (isActive)
        {
            provider.Activate();
        }

        provider.Configure(TestEncryptedCredentials, null);
        provider.SetCodSupport(supportsCod);
        provider.SetInsuranceSupport(supportsInsurance);
        provider.SetSortOrder(sortOrder);
        provider.SetSupportedServices("[\"Standard\",\"Express\"]");
        provider.SetApiBaseUrl("https://api.example.com");
        provider.SetTrackingUrlTemplate("https://tracking.example.com/{trackingNumber}");
        provider.SetWeightLimits(100, 50000);
        provider.SetCodLimits(10000m, 10000000m);

        if (id.HasValue)
        {
            typeof(Entity<Guid>).GetProperty("Id")!.SetValue(provider, id.Value);
        }

        return provider;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_ValidProviderId_ReturnsProvider()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        var query = new GetShippingProviderByIdQuery(providerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldNotBeNull();
        result.Value.Id.ShouldBe(providerId);
    }

    [Fact]
    public async Task Handle_ReturnsProviderWithAllFields()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(
            providerId,
            ShippingProviderCode.GHTK,
            "Giao Hang Tiet Kiem",
            sortOrder: 1,
            isActive: true,
            supportsCod: true,
            supportsInsurance: true,
            environment: GatewayEnvironment.Production);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        var query = new GetShippingProviderByIdQuery(providerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        var dto = result.Value;

        dto.Id.ShouldBe(providerId);
        dto.ProviderCode.ShouldBe(ShippingProviderCode.GHTK);
        dto.DisplayName.ShouldBe("Giao Hang Tiet Kiem");
        dto.ProviderName.ShouldBe("GHTK");
        dto.IsActive.ShouldBe(true);
        dto.SortOrder.ShouldBe(1);
        dto.Environment.ShouldBe(GatewayEnvironment.Production);
        dto.HasCredentials.ShouldBe(true);
        dto.ApiBaseUrl.ShouldBe("https://api.example.com");
        dto.TrackingUrlTemplate.ShouldBe("https://tracking.example.com/{trackingNumber}");
        dto.SupportedServices.ShouldContain("Standard");
        dto.SupportedServices.ShouldContain("Express");
        dto.SupportsCod.ShouldBe(true);
        dto.SupportsInsurance.ShouldBe(true);
        dto.MinWeightGrams.ShouldBe(100);
        dto.MaxWeightGrams.ShouldBe(50000);
        dto.MinCodAmount.ShouldBe(10000m);
        dto.MaxCodAmount.ShouldBe(10000000m);
    }

    [Fact]
    public async Task Handle_InactiveProvider_ReturnsProviderWithIsActiveFalse()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId, isActive: false);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        var query = new GetShippingProviderByIdQuery(providerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.IsActive.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_GHNProvider_ReturnsGHNProvider()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId, ShippingProviderCode.GHN, "Giao Hang Nhanh");

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        var query = new GetShippingProviderByIdQuery(providerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ProviderCode.ShouldBe(ShippingProviderCode.GHN);
        result.Value.DisplayName.ShouldBe("Giao Hang Nhanh");
    }

    [Fact]
    public async Task Handle_SandboxEnvironment_ReturnsCorrectEnvironment()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId, environment: GatewayEnvironment.Sandbox);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        var query = new GetShippingProviderByIdQuery(providerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.Environment.ShouldBe(GatewayEnvironment.Sandbox);
    }

    [Fact]
    public async Task Handle_ProviderWithNoCodSupport_ReturnsCodSupportFalse()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId, supportsCod: false);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        var query = new GetShippingProviderByIdQuery(providerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.SupportsCod.ShouldBe(false);
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_ProviderNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider?)null);

        var query = new GetShippingProviderByIdQuery(nonExistentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(false);
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Message.ShouldContain("not found");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        var query = new GetShippingProviderByIdQuery(providerId);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _providerRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ProviderWithNoCredentials_ReturnsHasCredentialsFalse()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = ShippingProvider.Create(
            ShippingProviderCode.GHTK,
            "Test Provider",
            "GHTK",
            GatewayEnvironment.Sandbox,
            TestTenantId);

        typeof(Entity<Guid>).GetProperty("Id")!.SetValue(provider, providerId);
        // Note: Not calling Configure(), so EncryptedCredentials is null

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        var query = new GetShippingProviderByIdQuery(providerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.HasCredentials.ShouldBe(false);
    }

    [Fact]
    public async Task Handle_ProviderWithNoLimits_ReturnsNullLimits()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = ShippingProvider.Create(
            ShippingProviderCode.GHTK,
            "Test Provider",
            "GHTK",
            GatewayEnvironment.Sandbox,
            TestTenantId);

        typeof(Entity<Guid>).GetProperty("Id")!.SetValue(provider, providerId);
        // Note: Not setting weight or COD limits

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        var query = new GetShippingProviderByIdQuery(providerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.MinWeightGrams.ShouldBeNull();
        result.Value.MaxWeightGrams.ShouldBeNull();
        result.Value.MinCodAmount.ShouldBeNull();
        result.Value.MaxCodAmount.ShouldBeNull();
    }

    [Fact]
    public async Task Handle_IncludesCreatedAtAndModifiedAt()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        var query = new GetShippingProviderByIdQuery(providerId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.CreatedAt.ShouldNotBe(default);
    }

    #endregion
}
