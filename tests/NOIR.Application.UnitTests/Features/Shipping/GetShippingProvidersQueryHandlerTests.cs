using NOIR.Application.Features.Shipping.DTOs;
using NOIR.Application.Features.Shipping.Queries.GetShippingProviders;
using NOIR.Application.Features.Shipping.Specifications;

namespace NOIR.Application.UnitTests.Features.Shipping;

/// <summary>
/// Unit tests for GetShippingProvidersQueryHandler.
/// Tests shipping provider list retrieval scenarios (admin view).
/// </summary>
public class GetShippingProvidersQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ShippingProvider, Guid>> _providerRepositoryMock;
    private readonly GetShippingProvidersQueryHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestEncryptedCredentials = "encrypted_credentials_abc123";

    public GetShippingProvidersQueryHandlerTests()
    {
        _providerRepositoryMock = new Mock<IRepository<ShippingProvider, Guid>>();
        _handler = new GetShippingProvidersQueryHandler(_providerRepositoryMock.Object);
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

        if (id.HasValue)
        {
            typeof(Entity<Guid>).GetProperty("Id")!.SetValue(provider, id.Value);
        }

        return provider;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithProviders_ReturnsAllProviders()
    {
        // Arrange
        var providers = new List<ShippingProvider>
        {
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHTK, "GHTK", sortOrder: 1),
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHN, "GHN", sortOrder: 2),
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.JTExpress, "J&T Express", sortOrder: 3)
        };

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        var query = new GetShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_EmptyProviderList_ReturnsEmptyList()
    {
        // Arrange
        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShippingProvider>());

        var query = new GetShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsProvidersOrderedBySortOrder()
    {
        // Arrange
        var providers = new List<ShippingProvider>
        {
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHTK, "GHTK", sortOrder: 3),
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHN, "GHN", sortOrder: 1),
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.JTExpress, "J&T Express", sortOrder: 2)
        };

        // Pre-sort by SortOrder as the spec would do
        providers = providers.OrderBy(p => p.SortOrder).ToList();

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        var query = new GetShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].DisplayName.Should().Be("GHN");
        result.Value[1].DisplayName.Should().Be("J&T Express");
        result.Value[2].DisplayName.Should().Be("GHTK");
    }

    [Fact]
    public async Task Handle_IncludesBothActiveAndInactiveProviders()
    {
        // Arrange
        var providers = new List<ShippingProvider>
        {
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHTK, "GHTK", isActive: true),
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHN, "GHN", isActive: false)
        };

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        var query = new GetShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().Contain(p => p.IsActive);
        result.Value.Should().Contain(p => !p.IsActive);
    }

    [Fact]
    public async Task Handle_ReturnsFullDtoWithAllFields()
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

        provider.SetApiBaseUrl("https://api.ghtk.vn");
        provider.SetTrackingUrlTemplate("https://khachhang.ghtk.vn/tracking/{trackingNumber}");
        provider.SetWeightLimits(50, 30000);
        provider.SetCodLimits(5000m, 20000000m);

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShippingProvider> { provider });

        var query = new GetShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        var dto = result.Value[0];
        dto.Id.Should().Be(providerId);
        dto.ProviderCode.Should().Be(ShippingProviderCode.GHTK);
        dto.DisplayName.Should().Be("Giao Hang Tiet Kiem");
        dto.ProviderName.Should().Be("GHTK");
        dto.IsActive.Should().BeTrue();
        dto.SortOrder.Should().Be(1);
        dto.Environment.Should().Be(GatewayEnvironment.Production);
        dto.HasCredentials.Should().BeTrue();
        dto.ApiBaseUrl.Should().Be("https://api.ghtk.vn");
        dto.TrackingUrlTemplate.Should().Be("https://khachhang.ghtk.vn/tracking/{trackingNumber}");
        dto.SupportedServices.Should().Contain("Standard");
        dto.SupportsCod.Should().BeTrue();
        dto.SupportsInsurance.Should().BeTrue();
        dto.MinWeightGrams.Should().Be(50);
        dto.MaxWeightGrams.Should().Be(30000);
        dto.MinCodAmount.Should().Be(5000m);
        dto.MaxCodAmount.Should().Be(20000000m);
    }

    [Fact]
    public async Task Handle_SingleProvider_ReturnsSingleProvider()
    {
        // Arrange
        var provider = CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHTK);

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShippingProvider> { provider });

        var query = new GetShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_IncludesAllProviderCodes()
    {
        // Arrange
        var providers = new List<ShippingProvider>
        {
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHTK, "GHTK", sortOrder: 1),
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHN, "GHN", sortOrder: 2),
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.JTExpress, "J&T", sortOrder: 3),
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.VNPost, "VNPost", sortOrder: 4),
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.ViettelPost, "Viettel Post", sortOrder: 5)
        };

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        var query = new GetShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(5);
        result.Value.Should().Contain(p => p.ProviderCode == ShippingProviderCode.GHTK);
        result.Value.Should().Contain(p => p.ProviderCode == ShippingProviderCode.GHN);
        result.Value.Should().Contain(p => p.ProviderCode == ShippingProviderCode.JTExpress);
        result.Value.Should().Contain(p => p.ProviderCode == ShippingProviderCode.VNPost);
        result.Value.Should().Contain(p => p.ProviderCode == ShippingProviderCode.ViettelPost);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_PassesTokenToRepository()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShippingProvider>());

        var query = new GetShippingProvidersQuery();

        // Act
        await _handler.Handle(query, token);

        // Assert
        _providerRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ShippingProvidersSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ProvidersWithDifferentEnvironments_ReturnsAllEnvironments()
    {
        // Arrange
        var providers = new List<ShippingProvider>
        {
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHTK, "GHTK Sandbox", environment: GatewayEnvironment.Sandbox),
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHN, "GHN Production", environment: GatewayEnvironment.Production)
        };

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        var query = new GetShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain(p => p.Environment == GatewayEnvironment.Sandbox);
        result.Value.Should().Contain(p => p.Environment == GatewayEnvironment.Production);
    }

    [Fact]
    public async Task Handle_AlwaysReturnsSuccessResult()
    {
        // Arrange
        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShippingProvider>());

        var query = new GetShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ProvidersWithNoCredentials_ReturnsHasCredentialsFalse()
    {
        // Arrange
        var provider = ShippingProvider.Create(
            ShippingProviderCode.GHTK,
            "Test Provider",
            "GHTK",
            GatewayEnvironment.Sandbox,
            TestTenantId);

        typeof(Entity<Guid>).GetProperty("Id")!.SetValue(provider, Guid.NewGuid());
        // Note: Not calling Configure(), so EncryptedCredentials is null

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShippingProvider> { provider });

        var query = new GetShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[0].HasCredentials.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_IncludesHealthStatusFields()
    {
        // Arrange
        var provider = CreateTestProvider(Guid.NewGuid());
        provider.UpdateHealthStatus(ShippingProviderHealthStatus.Healthy);

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShippingProvider> { provider });

        var query = new GetShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[0].HealthStatus.Should().Be(ShippingProviderHealthStatus.Healthy);
        result.Value[0].LastHealthCheck.Should().NotBeNull();
    }

    #endregion
}
