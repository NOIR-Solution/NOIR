namespace NOIR.Application.UnitTests.Features.Payments.Commands.ConfigureGateway;

using NOIR.Application.Features.Payments.Commands.ConfigureGateway;
using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Specifications;

/// <summary>
/// Unit tests for ConfigureGatewayCommandHandler.
/// Tests payment gateway configuration scenarios with mocked dependencies.
/// </summary>
public class ConfigureGatewayCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentGateway, Guid>> _gatewayRepositoryMock;
    private readonly Mock<ICredentialEncryptionService> _encryptionServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly ConfigureGatewayCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string EncryptedCredentials = "encrypted-credentials-data";

    public ConfigureGatewayCommandHandlerTests()
    {
        _gatewayRepositoryMock = new Mock<IRepository<PaymentGateway, Guid>>();
        _encryptionServiceMock = new Mock<ICredentialEncryptionService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        // Default setup
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _encryptionServiceMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns(EncryptedCredentials);

        _handler = new ConfigureGatewayCommandHandler(
            _gatewayRepositoryMock.Object,
            _encryptionServiceMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static ConfigureGatewayCommand CreateTestCommand(
        string provider = "vnpay",
        string displayName = "VNPay",
        GatewayEnvironment environment = GatewayEnvironment.Sandbox,
        int sortOrder = 1,
        bool isActive = true)
    {
        return new ConfigureGatewayCommand(
            provider,
            displayName,
            environment,
            new Dictionary<string, string>
            {
                { "merchantId", "MERCHANT001" },
                { "secretKey", "secret-key-value" }
            },
            new List<PaymentMethod> { PaymentMethod.CreditCard, PaymentMethod.BankTransfer },
            sortOrder,
            isActive);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateGateway()
    {
        // Arrange
        var command = CreateTestCommand();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway?)null);

        _gatewayRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PaymentGateway>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Provider.Should().Be("vnpay");
        result.Value.DisplayName.Should().Be("VNPay");
        result.Value.Environment.Should().Be(GatewayEnvironment.Sandbox);
        result.Value.IsActive.Should().BeTrue();
        result.Value.HasCredentials.Should().BeTrue();
        result.Value.SortOrder.Should().Be(1);

        _encryptionServiceMock.Verify(x => x.Encrypt(It.IsAny<string>()), Times.Once);
        _gatewayRepositoryMock.Verify(x => x.AddAsync(It.IsAny<PaymentGateway>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithInactiveGateway_ShouldCreateInactiveGateway()
    {
        // Arrange
        var command = CreateTestCommand(isActive: false);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway?)null);

        _gatewayRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PaymentGateway>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway entity, CancellationToken _) => entity);

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
    public async Task Handle_WithProductionEnvironment_ShouldCreateProductionGateway()
    {
        // Arrange
        var command = CreateTestCommand(environment: GatewayEnvironment.Production);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway?)null);

        _gatewayRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PaymentGateway>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway entity, CancellationToken _) => entity);

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

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenGatewayAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var command = CreateTestCommand();
        var existingGateway = PaymentGateway.Create("vnpay", "Existing VNPay", GatewayEnvironment.Sandbox, TestTenantId);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGateway);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be(ErrorCodes.Payment.GatewayAlreadyExists);
        result.Error.Message.Should().Contain("vnpay");

        _gatewayRepositoryMock.Verify(x => x.AddAsync(It.IsAny<PaymentGateway>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_ShouldEncryptCredentials()
    {
        // Arrange
        var command = CreateTestCommand();
        string? capturedJson = null;

        _encryptionServiceMock
            .Setup(x => x.Encrypt(It.IsAny<string>()))
            .Callback<string>(json => capturedJson = json)
            .Returns(EncryptedCredentials);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway?)null);

        _gatewayRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PaymentGateway>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedJson.Should().NotBeNull();
        capturedJson.Should().Contain("merchantId");
        capturedJson.Should().Contain("secretKey");

        _encryptionServiceMock.Verify(x => x.Encrypt(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDifferentProviders_ShouldCreateEach()
    {
        // Arrange
        var momoCommand = CreateTestCommand(provider: "momo", displayName: "MoMo");

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByProviderSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway?)null);

        _gatewayRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PaymentGateway>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway entity, CancellationToken _) => entity);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(momoCommand, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Provider.Should().Be("momo");
        result.Value.DisplayName.Should().Be("MoMo");
    }

    #endregion
}
