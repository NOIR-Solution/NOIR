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
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(providerId);
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
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;

        dto.Id.Should().Be(providerId);
        dto.ProviderCode.Should().Be(ShippingProviderCode.GHTK);
        dto.DisplayName.Should().Be("Giao Hang Tiet Kiem");
        dto.ProviderName.Should().Be("GHTK");
        dto.IsActive.Should().BeTrue();
        dto.SortOrder.Should().Be(1);
        dto.Environment.Should().Be(GatewayEnvironment.Production);
        dto.HasCredentials.Should().BeTrue();
        dto.ApiBaseUrl.Should().Be("https://api.example.com");
        dto.TrackingUrlTemplate.Should().Be("https://tracking.example.com/{trackingNumber}");
        dto.SupportedServices.Should().Contain("Standard");
        dto.SupportedServices.Should().Contain("Express");
        dto.SupportsCod.Should().BeTrue();
        dto.SupportsInsurance.Should().BeTrue();
        dto.MinWeightGrams.Should().Be(100);
        dto.MaxWeightGrams.Should().Be(50000);
        dto.MinCodAmount.Should().Be(10000m);
        dto.MaxCodAmount.Should().Be(10000000m);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
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
        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderCode.Should().Be(ShippingProviderCode.GHN);
        result.Value.DisplayName.Should().Be("Giao Hang Nhanh");
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
        result.IsSuccess.Should().BeTrue();
        result.Value.Environment.Should().Be(GatewayEnvironment.Sandbox);
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
        result.IsSuccess.Should().BeTrue();
        result.Value.SupportsCod.Should().BeFalse();
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
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().Contain("not found");
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
        result.IsSuccess.Should().BeTrue();
        result.Value.HasCredentials.Should().BeFalse();
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
        result.IsSuccess.Should().BeTrue();
        result.Value.MinWeightGrams.Should().BeNull();
        result.Value.MaxWeightGrams.Should().BeNull();
        result.Value.MinCodAmount.Should().BeNull();
        result.Value.MaxCodAmount.Should().BeNull();
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
        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedAt.Should().NotBe(default);
    }

    #endregion
}
