using NOIR.Application.Features.Shipping.DTOs;
using NOIR.Application.Features.Shipping.Queries.GetActiveShippingProviders;
using NOIR.Application.Features.Shipping.Specifications;

namespace NOIR.Application.UnitTests.Features.Shipping;

/// <summary>
/// Unit tests for GetActiveShippingProvidersQueryHandler.
/// Tests active shipping provider retrieval scenarios for checkout.
/// </summary>
public class GetActiveShippingProvidersQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ShippingProvider, Guid>> _providerRepositoryMock;
    private readonly GetActiveShippingProvidersQueryHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestEncryptedCredentials = "encrypted_credentials_abc123";

    public GetActiveShippingProvidersQueryHandlerTests()
    {
        _providerRepositoryMock = new Mock<IRepository<ShippingProvider, Guid>>();
        _handler = new GetActiveShippingProvidersQueryHandler(_providerRepositoryMock.Object);
    }

    private static ShippingProvider CreateTestProvider(
        Guid? id = null,
        ShippingProviderCode providerCode = ShippingProviderCode.GHTK,
        string displayName = "Test Provider",
        int sortOrder = 1,
        bool isActive = true,
        bool supportsCod = true,
        bool supportsInsurance = false,
        string? tenantId = TestTenantId)
    {
        var provider = ShippingProvider.Create(
            providerCode,
            displayName,
            providerCode.ToString(),
            GatewayEnvironment.Sandbox,
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
    public async Task Handle_WithActiveProviders_ReturnsAllActiveProviders()
    {
        // Arrange
        var providers = new List<ShippingProvider>
        {
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHTK, "GHTK", sortOrder: 1),
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHN, "GHN", sortOrder: 2)
        };

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ActiveShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        var query = new GetActiveShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_EmptyProviderList_ReturnsEmptyList()
    {
        // Arrange
        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ActiveShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShippingProvider>());

        var query = new GetActiveShippingProvidersQuery();

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
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHTK, "GHTK", sortOrder: 2),
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHN, "GHN", sortOrder: 1)
        };

        // Pre-sort by SortOrder as the spec would do
        providers = providers.OrderBy(p => p.SortOrder).ToList();

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ActiveShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        var query = new GetActiveShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].DisplayName.Should().Be("GHN"); // SortOrder 1
        result.Value[1].DisplayName.Should().Be("GHTK"); // SortOrder 2
    }

    [Fact]
    public async Task Handle_ReturnsCheckoutDtoWithCorrectFields()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(
            providerId,
            ShippingProviderCode.GHTK,
            "Giao Hang Tiet Kiem",
            sortOrder: 1,
            supportsCod: true,
            supportsInsurance: true);

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ActiveShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShippingProvider> { provider });

        var query = new GetActiveShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        var dto = result.Value[0];
        dto.Id.Should().Be(providerId);
        dto.ProviderCode.Should().Be(ShippingProviderCode.GHTK);
        dto.DisplayName.Should().Be("Giao Hang Tiet Kiem");
        dto.SortOrder.Should().Be(1);
        dto.SupportsCod.Should().BeTrue();
        dto.SupportsInsurance.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ReturnsProviderWithSupportedServices()
    {
        // Arrange
        var provider = CreateTestProvider(Guid.NewGuid());
        provider.SetSupportedServices("[\"Standard\",\"Express\",\"SameDay\"]");

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ActiveShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShippingProvider> { provider });

        var query = new GetActiveShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[0].SupportedServices.Should().Contain("Standard");
        result.Value[0].SupportedServices.Should().Contain("Express");
        result.Value[0].SupportedServices.Should().Contain("SameDay");
    }

    [Fact]
    public async Task Handle_SingleProvider_ReturnsSingleProvider()
    {
        // Arrange
        var provider = CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHTK);

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ActiveShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShippingProvider> { provider });

        var query = new GetActiveShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
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
            .Setup(x => x.ListAsync(It.IsAny<ActiveShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShippingProvider>());

        var query = new GetActiveShippingProvidersQuery();

        // Act
        await _handler.Handle(query, token);

        // Assert
        _providerRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ActiveShippingProvidersSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_MultipleProvidersWithDifferentCodSupport_ReturnsCorrectCodSupport()
    {
        // Arrange
        var providers = new List<ShippingProvider>
        {
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHTK, "GHTK", supportsCod: true),
            CreateTestProvider(Guid.NewGuid(), ShippingProviderCode.GHN, "GHN", supportsCod: false)
        };

        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ActiveShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(providers);

        var query = new GetActiveShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.First(p => p.ProviderCode == ShippingProviderCode.GHTK).SupportsCod.Should().BeTrue();
        result.Value.First(p => p.ProviderCode == ShippingProviderCode.GHN).SupportsCod.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AlwaysReturnsSuccessResult()
    {
        // Arrange
        _providerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ActiveShippingProvidersSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShippingProvider>());

        var query = new GetActiveShippingProvidersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    #endregion
}
