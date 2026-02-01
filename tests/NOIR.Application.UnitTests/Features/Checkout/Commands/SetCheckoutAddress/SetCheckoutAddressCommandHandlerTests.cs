using NOIR.Application.Features.Checkout.Commands.SetCheckoutAddress;
using NOIR.Application.Features.Checkout.DTOs;
using NOIR.Application.Features.Checkout.Specifications;

namespace NOIR.Application.UnitTests.Features.Checkout.Commands.SetCheckoutAddress;

/// <summary>
/// Unit tests for SetCheckoutAddressCommandHandler.
/// Tests setting shipping and billing addresses on checkout sessions.
/// </summary>
public class SetCheckoutAddressCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<CheckoutSession, Guid>> _checkoutRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly SetCheckoutAddressCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestCustomerEmail = "customer@test.com";

    public SetCheckoutAddressCommandHandlerTests()
    {
        _checkoutRepositoryMock = new Mock<IRepository<CheckoutSession, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new SetCheckoutAddressCommandHandler(
            _checkoutRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static CheckoutSession CreateTestSession(
        Guid? sessionId = null,
        CheckoutSessionStatus status = CheckoutSessionStatus.Started,
        bool isExpired = false)
    {
        var id = sessionId ?? Guid.NewGuid();
        var session = CheckoutSession.Create(
            cartId: Guid.NewGuid(),
            customerEmail: TestCustomerEmail,
            subTotal: 200000m,
            currency: "VND",
            userId: "user-123",
            tenantId: TestTenantId);

        // Use reflection to set the Id
        var idProperty = typeof(CheckoutSession).BaseType?.BaseType?.GetProperty("Id");
        idProperty?.SetValue(session, id);

        // Set status if not Started
        if (status != CheckoutSessionStatus.Started)
        {
            var statusProperty = typeof(CheckoutSession).GetProperty("Status");
            statusProperty?.SetValue(session, status);
        }

        // Set as expired if needed
        if (isExpired)
        {
            var expiresAtProperty = typeof(CheckoutSession).GetProperty("ExpiresAt");
            expiresAtProperty?.SetValue(session, DateTimeOffset.UtcNow.AddMinutes(-5));
        }

        return session;
    }

    private static SetCheckoutAddressCommand CreateTestCommand(
        Guid sessionId,
        string addressType = "Shipping",
        bool billingSameAsShipping = true)
    {
        return new SetCheckoutAddressCommand(
            SessionId: sessionId,
            AddressType: addressType,
            FullName: "Nguyen Van A",
            Phone: "0901234567",
            AddressLine1: "123 Nguyen Hue",
            AddressLine2: "Floor 5",
            Ward: "Ben Nghe",
            District: "District 1",
            Province: "Ho Chi Minh City",
            PostalCode: "70000",
            Country: "Vietnam",
            BillingSameAsShipping: billingSameAsShipping);
    }

    #endregion

    #region Success Scenarios - Shipping Address

    [Fact]
    public async Task Handle_WithValidShippingAddress_ShouldSetAddressAndReturnSuccess()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateTestCommand(sessionId, "Shipping");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ShippingAddress.Should().NotBeNull();
        result.Value.ShippingAddress!.FullName.Should().Be("Nguyen Van A");
        result.Value.ShippingAddress.Phone.Should().Be("0901234567");
        result.Value.ShippingAddress.AddressLine1.Should().Be("123 Nguyen Hue");
        result.Value.ShippingAddress.District.Should().Be("District 1");
        result.Value.ShippingAddress.Province.Should().Be("Ho Chi Minh City");
        result.Value.Status.Should().Be(CheckoutSessionStatus.AddressComplete);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithShippingAddress_ShouldCopyToBillingWhenSameAsShipping()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateTestCommand(sessionId, "Shipping", billingSameAsShipping: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BillingSameAsShipping.Should().BeTrue();
    }

    #endregion

    #region Success Scenarios - Billing Address

    [Fact]
    public async Task Handle_WithValidBillingAddress_ShouldSetAddressAndReturnSuccess()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateTestCommand(sessionId, "Billing", billingSameAsShipping: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithBillingAddressAndSameAsShippingTrue_ShouldUseSameAddress()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId);

        // First set shipping address
        var shippingAddress = new NOIR.Domain.ValueObjects.Address
        {
            FullName = "Original Shipping Name",
            Phone = "0901111111",
            AddressLine1 = "Original Address",
            AddressLine2 = null,
            Ward = "Ward 1",
            District = "District 1",
            Province = "Ho Chi Minh City",
            Country = "Vietnam",
            PostalCode = "70000",
            IsDefault = false
        };
        session.SetShippingAddress(shippingAddress);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateTestCommand(sessionId, "Billing", billingSameAsShipping: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BillingSameAsShipping.Should().BeTrue();
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenSessionNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var sessionId = Guid.NewGuid();

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CheckoutSession?)null);

        var command = CreateTestCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-004");
        result.Error.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_WhenSessionExpired_ShouldReturnValidationError()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, isExpired: true);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateTestCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-005");
        result.Error.Message.Should().Contain("expired");
    }

    [Fact]
    public async Task Handle_WithInvalidAddressType_ShouldReturnValidationError()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var command = new SetCheckoutAddressCommand(
            SessionId: sessionId,
            AddressType: "InvalidType", // Invalid address type
            FullName: "Test Name",
            Phone: "0901234567",
            AddressLine1: "123 Test Street",
            AddressLine2: null,
            Ward: "Ward 1",
            District: "District 1",
            Province: "Province",
            PostalCode: "12345",
            Country: "Vietnam",
            BillingSameAsShipping: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-006");
        result.Error.Message.Should().Contain("Shipping").And.Contain("Billing");
    }

    [Theory]
    [InlineData(CheckoutSessionStatus.Completed)]
    [InlineData(CheckoutSessionStatus.Expired)]
    [InlineData(CheckoutSessionStatus.Abandoned)]
    public async Task Handle_WhenSessionInFinalStatus_ShouldReturnValidationError(CheckoutSessionStatus status)
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId, status: status);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var command = CreateTestCommand(sessionId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-CHECKOUT-007");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithAddressTypeIgnoringCase_ShouldWork()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Test with lowercase
        var command = new SetCheckoutAddressCommand(
            SessionId: sessionId,
            AddressType: "shipping", // lowercase
            FullName: "Test Name",
            Phone: "0901234567",
            AddressLine1: "123 Test Street",
            AddressLine2: null,
            Ward: "Ward 1",
            District: "District 1",
            Province: "Province",
            PostalCode: "12345",
            Country: "Vietnam",
            BillingSameAsShipping: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNullOptionalFields_ShouldSetDefaultValues()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new SetCheckoutAddressCommand(
            SessionId: sessionId,
            AddressType: "Shipping",
            FullName: "Test Name",
            Phone: "0901234567",
            AddressLine1: "123 Test Street",
            AddressLine2: null, // Null optional field
            Ward: null, // Null optional field
            District: null, // Null optional field
            Province: null, // Null optional field
            PostalCode: null, // Null optional field
            Country: "Vietnam",
            BillingSameAsShipping: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ShippingAddress.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var session = CreateTestSession(sessionId);

        _checkoutRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<CheckoutSessionByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = CreateTestCommand(sessionId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _checkoutRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<CheckoutSessionByIdForUpdateSpec>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}
