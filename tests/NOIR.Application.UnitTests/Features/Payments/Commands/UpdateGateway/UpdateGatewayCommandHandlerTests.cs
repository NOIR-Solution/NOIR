namespace NOIR.Application.UnitTests.Features.Payments.Commands.UpdateGateway;

using NOIR.Application.Features.Payments.Commands.UpdateGateway;
using NOIR.Application.Features.Payments.DTOs;
using NOIR.Application.Features.Payments.Specifications;

/// <summary>
/// Unit tests for UpdateGatewayCommandHandler.
/// Tests payment gateway update scenarios with mocked dependencies.
/// </summary>
public class UpdateGatewayCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PaymentGateway, Guid>> _gatewayRepositoryMock;
    private readonly Mock<ICredentialEncryptionService> _encryptionServiceMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateGatewayCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string EncryptedCredentials = "new-encrypted-credentials";
    private static readonly Guid TestGatewayId = Guid.NewGuid();

    public UpdateGatewayCommandHandlerTests()
    {
        _gatewayRepositoryMock = new Mock<IRepository<PaymentGateway, Guid>>();
        _encryptionServiceMock = new Mock<ICredentialEncryptionService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        // Default setup
        _encryptionServiceMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns(EncryptedCredentials);

        _handler = new UpdateGatewayCommandHandler(
            _gatewayRepositoryMock.Object,
            _encryptionServiceMock.Object,
            _unitOfWorkMock.Object);
    }

    private static UpdateGatewayCommand CreateTestCommand(
        Guid? gatewayId = null,
        string? displayName = "Updated VNPay",
        GatewayEnvironment? environment = null,
        Dictionary<string, string>? credentials = null,
        int? sortOrder = null,
        bool? isActive = null)
    {
        return new UpdateGatewayCommand(
            gatewayId ?? TestGatewayId,
            displayName,
            environment,
            credentials,
            null,
            sortOrder,
            isActive);
    }

    private static PaymentGateway CreateTestGateway(
        bool isActive = true,
        string provider = "vnpay",
        string displayName = "VNPay Original")
    {
        var gateway = PaymentGateway.Create(
            provider,
            displayName,
            GatewayEnvironment.Sandbox,
            TestTenantId);

        typeof(PaymentGateway).GetProperty("Id")?.SetValue(gateway, TestGatewayId);

        gateway.Configure("old-encrypted-credentials", "webhook-secret");
        gateway.SetSortOrder(1);
        if (isActive)
        {
            gateway.Activate();
        }

        return gateway;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateGateway()
    {
        // Arrange
        var command = CreateTestCommand(displayName: "Updated VNPay Display");
        var existingGateway = CreateTestGateway();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGateway);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.DisplayName.Should().Be("Updated VNPay Display");
        result.Value.Provider.Should().Be("vnpay");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithEnvironmentUpdate_ShouldUpdateEnvironment()
    {
        // Arrange
        var command = CreateTestCommand(environment: GatewayEnvironment.Production);
        var existingGateway = CreateTestGateway();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGateway);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Environment.Should().Be(GatewayEnvironment.Production);
    }

    [Fact]
    public async Task Handle_WithCredentialsUpdate_ShouldEncryptAndUpdateCredentials()
    {
        // Arrange
        var newCredentials = new Dictionary<string, string>
        {
            { "merchantId", "NEW_MERCHANT" },
            { "secretKey", "new-secret" }
        };
        var command = CreateTestCommand(credentials: newCredentials, displayName: null);
        var existingGateway = CreateTestGateway();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGateway);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasCredentials.Should().BeTrue();

        _encryptionServiceMock.Verify(x => x.Encrypt(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithActivationUpdate_ShouldActivateGateway()
    {
        // Arrange
        var command = CreateTestCommand(isActive: true, displayName: null);
        var inactiveGateway = CreateTestGateway(isActive: false);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(inactiveGateway);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithDeactivationUpdate_ShouldDeactivateGateway()
    {
        // Arrange
        var command = CreateTestCommand(isActive: false, displayName: null);
        var activeGateway = CreateTestGateway(isActive: true);

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeGateway);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeFalse();
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenGatewayNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateTestCommand();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PaymentGateway?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be(ErrorCodes.Payment.GatewayNotFound);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithSortOrderUpdate_ShouldUpdateSortOrder()
    {
        // Arrange
        var command = CreateTestCommand(sortOrder: 5, displayName: null);
        var existingGateway = CreateTestGateway();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGateway);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SortOrder.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WithNoChanges_ShouldStillSucceed()
    {
        // Arrange
        var command = new UpdateGatewayCommand(
            TestGatewayId,
            null,
            null,
            null,
            null,
            null,
            null);
        var existingGateway = CreateTestGateway();

        _gatewayRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<PaymentGatewayByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingGateway);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DisplayName.Should().Be("VNPay Original");

        _encryptionServiceMock.Verify(x => x.Encrypt(It.IsAny<string>()), Times.Never);
    }

    #endregion
}
