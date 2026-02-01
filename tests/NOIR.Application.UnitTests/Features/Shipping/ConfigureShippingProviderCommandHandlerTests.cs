using NOIR.Application.Features.Shipping.Commands.ConfigureShippingProvider;
using NOIR.Application.Features.Shipping.DTOs;
using NOIR.Application.Features.Shipping.Specifications;

namespace NOIR.Application.UnitTests.Features.Shipping;

/// <summary>
/// Unit tests for ConfigureShippingProviderCommandHandler.
/// Tests shipping provider configuration scenarios with mocked dependencies.
/// </summary>
public class ConfigureShippingProviderCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ShippingProvider, Guid>> _providerRepositoryMock;
    private readonly Mock<ICredentialEncryptionService> _encryptionServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly ConfigureShippingProviderCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "test-user-123";
    private const string TestEncryptedCredentials = "encrypted_credentials_abc123";

    public ConfigureShippingProviderCommandHandlerTests()
    {
        _providerRepositoryMock = new Mock<IRepository<ShippingProvider, Guid>>();
        _encryptionServiceMock = new Mock<ICredentialEncryptionService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new ConfigureShippingProviderCommandHandler(
            _providerRepositoryMock.Object,
            _encryptionServiceMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static ConfigureShippingProviderCommand CreateTestCommand(
        ShippingProviderCode providerCode = ShippingProviderCode.GHTK,
        string displayName = "Giao Hang Tiet Kiem",
        GatewayEnvironment environment = GatewayEnvironment.Sandbox,
        bool isActive = true)
    {
        return new ConfigureShippingProviderCommand(
            providerCode,
            displayName,
            environment,
            new Dictionary<string, string>
            {
                { "ApiKey", "test-api-key" },
                { "SecretKey", "test-secret-key" }
            },
            new List<ShippingServiceType> { ShippingServiceType.Standard, ShippingServiceType.Express },
            SortOrder: 1,
            IsActive: isActive,
            SupportsCod: true,
            SupportsInsurance: false)
        { UserId = TestUserId };
    }

    private static ShippingProvider CreateTestProvider(
        Guid? id = null,
        ShippingProviderCode providerCode = ShippingProviderCode.GHTK,
        string? tenantId = TestTenantId)
    {
        var provider = ShippingProvider.Create(
            providerCode,
            "Test Provider",
            "Test Provider Name",
            GatewayEnvironment.Sandbox,
            tenantId);

        if (id.HasValue)
        {
            typeof(Entity<Guid>).GetProperty("Id")!.SetValue(provider, id.Value);
        }

        return provider;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_ValidCommand_CreatesNewProvider()
    {
        // Arrange
        var command = CreateTestCommand();

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider?)null);

        _encryptionServiceMock
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns(TestEncryptedCredentials);

        _providerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.ProviderCode.Should().Be(ShippingProviderCode.GHTK);
        result.Value.DisplayName.Should().Be("Giao Hang Tiet Kiem");
        result.Value.IsActive.Should().BeTrue();
        result.Value.Environment.Should().Be(GatewayEnvironment.Sandbox);

        _providerRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithCredentials_EncryptsCredentials()
    {
        // Arrange
        var command = CreateTestCommand();

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider?)null);

        _encryptionServiceMock
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns(TestEncryptedCredentials);

        _providerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasCredentials.Should().BeTrue();

        _encryptionServiceMock.Verify(
            x => x.Encrypt(It.Is<string>(s => s.Contains("ApiKey"))),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSupportedServices_SerializesServices()
    {
        // Arrange
        var command = CreateTestCommand();

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider?)null);

        _encryptionServiceMock
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns(TestEncryptedCredentials);

        ShippingProvider? addedProvider = null;
        _providerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()))
            .Callback<ShippingProvider, CancellationToken>((p, _) => addedProvider = p)
            .ReturnsAsync((ShippingProvider p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        addedProvider.Should().NotBeNull();
        // SupportedServices is serialized as JSON array of enum values
        addedProvider!.SupportedServices.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Handle_InactiveProvider_CreatesInactiveProvider()
    {
        // Arrange
        var command = CreateTestCommand(isActive: false);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider?)null);

        _encryptionServiceMock
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns(TestEncryptedCredentials);

        _providerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_GHNProvider_CreatesGHNProvider()
    {
        // Arrange
        var command = CreateTestCommand(
            providerCode: ShippingProviderCode.GHN,
            displayName: "Giao Hang Nhanh");

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider?)null);

        _encryptionServiceMock
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns(TestEncryptedCredentials);

        _providerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ProviderCode.Should().Be(ShippingProviderCode.GHN);
        result.Value.DisplayName.Should().Be("Giao Hang Nhanh");
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_DuplicateProviderCode_ReturnsConflictError()
    {
        // Arrange
        var existingProvider = CreateTestProvider();
        var command = CreateTestCommand();

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProvider);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Message.Should().Contain("already exists");

        _providerRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_PassesTokenToServices()
    {
        // Arrange
        var command = CreateTestCommand();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider?)null);

        _encryptionServiceMock
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns(TestEncryptedCredentials);

        _providerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _providerRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), token),
            Times.Once);
        _providerRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ShippingProvider>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SetsCorrectTenantId()
    {
        // Arrange
        var command = CreateTestCommand();

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider?)null);

        _encryptionServiceMock
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns(TestEncryptedCredentials);

        ShippingProvider? addedProvider = null;
        _providerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()))
            .Callback<ShippingProvider, CancellationToken>((p, _) => addedProvider = p)
            .ReturnsAsync((ShippingProvider p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        addedProvider.Should().NotBeNull();
        addedProvider!.TenantId.Should().Be(TestTenantId);
    }

    [Fact]
    public async Task Handle_WithApiBaseUrl_SetsApiBaseUrl()
    {
        // Arrange
        var command = new ConfigureShippingProviderCommand(
            ShippingProviderCode.GHTK,
            "GHTK",
            GatewayEnvironment.Sandbox,
            new Dictionary<string, string> { { "ApiKey", "test" } },
            new List<ShippingServiceType> { ShippingServiceType.Standard },
            SortOrder: 1,
            IsActive: true,
            ApiBaseUrl: "https://custom-api.example.com");

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider?)null);

        _encryptionServiceMock
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns(TestEncryptedCredentials);

        ShippingProvider? addedProvider = null;
        _providerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()))
            .Callback<ShippingProvider, CancellationToken>((p, _) => addedProvider = p)
            .ReturnsAsync((ShippingProvider p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        addedProvider.Should().NotBeNull();
        addedProvider!.ApiBaseUrl.Should().Be("https://custom-api.example.com");
    }

    [Fact]
    public async Task Handle_ProductionEnvironment_CreatesProductionProvider()
    {
        // Arrange
        var command = CreateTestCommand(environment: GatewayEnvironment.Production);

        _providerRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ShippingProviderByCodeSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider?)null);

        _encryptionServiceMock
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Returns(TestEncryptedCredentials);

        _providerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ShippingProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingProvider p, CancellationToken _) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Environment.Should().Be(GatewayEnvironment.Production);
    }

    #endregion
}
