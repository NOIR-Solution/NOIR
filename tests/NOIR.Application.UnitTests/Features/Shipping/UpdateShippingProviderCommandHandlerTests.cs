using NOIR.Application.Features.Shipping.Commands.UpdateShippingProvider;
using NOIR.Application.Features.Shipping.DTOs;
using NOIR.Application.Features.Shipping.Specifications;

namespace NOIR.Application.UnitTests.Features.Shipping;

/// <summary>
/// Unit tests for UpdateShippingProviderCommandHandler.
/// Tests shipping provider update scenarios with mocked dependencies.
/// </summary>
public class UpdateShippingProviderCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ShippingProvider, Guid>> _providerRepositoryMock;
    private readonly Mock<ICredentialEncryptionService> _encryptionServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateShippingProviderCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestEncryptedCredentials = "encrypted_credentials_abc123";

    public UpdateShippingProviderCommandHandlerTests()
    {
        _providerRepositoryMock = new Mock<IRepository<ShippingProvider, Guid>>();
        _encryptionServiceMock = new Mock<ICredentialEncryptionService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdateShippingProviderCommandHandler(
            _providerRepositoryMock.Object,
            _encryptionServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    private static ShippingProvider CreateTestProvider(
        Guid? id = null,
        ShippingProviderCode providerCode = ShippingProviderCode.GHTK,
        bool isActive = true,
        string? tenantId = TestTenantId)
    {
        var provider = ShippingProvider.Create(
            providerCode,
            "Test Provider",
            "Test Provider Name",
            GatewayEnvironment.Sandbox,
            tenantId);

        if (isActive)
        {
            provider.Activate();
        }

        provider.Configure(TestEncryptedCredentials, null);
        provider.SetCodSupport(true);
        provider.SetInsuranceSupport(false);
        provider.SetSortOrder(1);

        if (id.HasValue)
        {
            typeof(Entity<Guid>).GetProperty("Id")!.SetValue(provider, id.Value);
        }

        return provider;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_UpdateDisplayName_UpdatesDisplayNameOnly()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateShippingProviderCommand(providerId, DisplayName: "Updated Display Name");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayName.Should().Be("Updated Display Name");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UpdateEnvironment_UpdatesEnvironment()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateShippingProviderCommand(providerId, Environment: GatewayEnvironment.Production);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Environment.Should().Be(GatewayEnvironment.Production);
    }

    [Fact]
    public async Task Handle_UpdateCredentials_EncryptsAndUpdatesCredentials()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId);
        var newCredentials = new Dictionary<string, string>
        {
            { "ApiKey", "new-api-key" },
            { "SecretKey", "new-secret" }
        };
        var newEncryptedCredentials = "new_encrypted_credentials";

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _encryptionServiceMock
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns(newEncryptedCredentials);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateShippingProviderCommand(providerId, Credentials: newCredentials);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasCredentials.Should().BeTrue();

        _encryptionServiceMock.Verify(
            x => x.Encrypt(It.Is<string>(s => s.Contains("new-api-key"))),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ActivateProvider_ActivatesProvider()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId, isActive: false);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateShippingProviderCommand(providerId, IsActive: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_DeactivateProvider_DeactivatesProvider()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId, isActive: true);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateShippingProviderCommand(providerId, IsActive: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UpdateSortOrder_UpdatesSortOrder()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateShippingProviderCommand(providerId, SortOrder: 10);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SortOrder.Should().Be(10);
    }

    [Fact]
    public async Task Handle_UpdateCodSupport_UpdatesCodSupport()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateShippingProviderCommand(providerId, SupportsCod: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SupportsCod.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_UpdateInsuranceSupport_UpdatesInsuranceSupport()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateShippingProviderCommand(providerId, SupportsInsurance: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SupportsInsurance.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UpdateWeightLimits_UpdatesWeightLimits()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateShippingProviderCommand(providerId, MinWeightGrams: 100, MaxWeightGrams: 50000);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MinWeightGrams.Should().Be(100);
        result.Value.MaxWeightGrams.Should().Be(50000);
    }

    [Fact]
    public async Task Handle_UpdateCodLimits_UpdatesCodLimits()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateShippingProviderCommand(providerId, MinCodAmount: 10000m, MaxCodAmount: 10000000m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MinCodAmount.Should().Be(10000m);
        result.Value.MaxCodAmount.Should().Be(10000000m);
    }

    [Fact]
    public async Task Handle_MultipleUpdates_UpdatesAllFields()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateShippingProviderCommand(
            providerId,
            DisplayName: "New Name",
            Environment: GatewayEnvironment.Production,
            SortOrder: 5,
            IsActive: false,
            SupportsCod: false,
            SupportsInsurance: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayName.Should().Be("New Name");
        result.Value.Environment.Should().Be(GatewayEnvironment.Production);
        result.Value.SortOrder.Should().Be(5);
        result.Value.IsActive.Should().BeFalse();
        result.Value.SupportsCod.Should().BeFalse();
        result.Value.SupportsInsurance.Should().BeTrue();
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_ProviderNotFound_ReturnsNotFoundError()
    {
        // Arrange
        var providerId = Guid.NewGuid();

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider?)null);

        var command = new UpdateShippingProviderCommand(providerId, DisplayName: "Updated");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Message.Should().Contain("not found");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_PassesTokenToServices()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateShippingProviderCommand(providerId, DisplayName: "Updated");

        // Act
        await _handler.Handle(command, token);

        // Assert
        _providerRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdForUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyCredentials_DoesNotUpdateCredentials()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateShippingProviderCommand(
            providerId,
            DisplayName: "Updated",
            Credentials: new Dictionary<string, string>()); // Empty credentials

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Empty credentials should not trigger encryption
        _encryptionServiceMock.Verify(
            x => x.Encrypt(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_UpdateApiBaseUrl_UpdatesApiBaseUrl()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateShippingProviderCommand(providerId, ApiBaseUrl: "https://new-api.example.com");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ApiBaseUrl.Should().Be("https://new-api.example.com");
    }

    [Fact]
    public async Task Handle_UpdateTrackingUrlTemplate_UpdatesTrackingUrlTemplate()
    {
        // Arrange
        var providerId = Guid.NewGuid();
        var provider = CreateTestProvider(providerId);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(provider);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateShippingProviderCommand(
            providerId,
            TrackingUrlTemplate: "https://tracking.example.com/{trackingNumber}");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TrackingUrlTemplate.Should().Be("https://tracking.example.com/{trackingNumber}");
    }

    #endregion
}
