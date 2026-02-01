using NOIR.Application.Features.Orders.Commands.CreateOrder;
using NOIR.Application.Features.Orders.DTOs;
using NOIR.Application.Features.Orders.Specifications;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.CreateOrder;

/// <summary>
/// Unit tests for CreateOrderCommandHandler.
/// Tests order creation scenarios with mocked dependencies.
/// </summary>
public class CreateOrderCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Order, Guid>> _orderRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly CreateOrderCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "550e8400-e29b-41d4-a716-446655440000";

    public CreateOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IRepository<Order, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        // Setup default current user
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(x => x.UserId).Returns(TestUserId);

        _handler = new CreateOrderCommandHandler(
            _orderRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static CreateOrderCommand CreateTestCommand(
        string customerEmail = "customer@example.com",
        string? customerName = "John Doe",
        string? customerPhone = "0901234567",
        CreateAddressDto? shippingAddress = null,
        CreateAddressDto? billingAddress = null,
        string? shippingMethod = "Standard",
        decimal shippingAmount = 10.00m,
        string? couponCode = null,
        decimal discountAmount = 0m,
        string? customerNotes = null,
        List<CreateOrderItemDto>? items = null,
        string currency = "VND",
        Guid? checkoutSessionId = null)
    {
        shippingAddress ??= CreateTestAddress();
        items ??= new List<CreateOrderItemDto> { CreateTestOrderItemDto() };

        return new CreateOrderCommand(
            customerEmail,
            customerName,
            customerPhone,
            shippingAddress,
            billingAddress,
            shippingMethod,
            shippingAmount,
            couponCode,
            discountAmount,
            customerNotes,
            items,
            currency,
            checkoutSessionId);
    }

    private static CreateAddressDto CreateTestAddress(
        string fullName = "John Doe",
        string phone = "0901234567",
        string addressLine1 = "123 Main Street",
        string? addressLine2 = null,
        string ward = "Ward 1",
        string district = "District 1",
        string province = "Ho Chi Minh City",
        string country = "Vietnam",
        string? postalCode = "700000")
    {
        return new CreateAddressDto(
            fullName,
            phone,
            addressLine1,
            addressLine2,
            ward,
            district,
            province,
            country,
            postalCode);
    }

    private static CreateOrderItemDto CreateTestOrderItemDto(
        Guid? productId = null,
        Guid? productVariantId = null,
        string productName = "Test Product",
        string variantName = "Size: M",
        decimal unitPrice = 100.00m,
        int quantity = 2,
        string? sku = "SKU-001",
        string? imageUrl = "https://example.com/image.jpg",
        string? optionsSnapshot = null)
    {
        return new CreateOrderItemDto(
            productId ?? Guid.NewGuid(),
            productVariantId ?? Guid.NewGuid(),
            productName,
            variantName,
            unitPrice,
            quantity,
            sku,
            imageUrl,
            optionsSnapshot);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateOrderSuccessfully()
    {
        // Arrange
        var command = CreateTestCommand();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken _) => o);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.CustomerEmail.Should().Be(command.CustomerEmail);
        result.Value.CustomerName.Should().Be(command.CustomerName);
        result.Value.Status.Should().Be(OrderStatus.Pending);

        _orderRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleItems_ShouldCreateOrderWithAllItems()
    {
        // Arrange
        var items = new List<CreateOrderItemDto>
        {
            CreateTestOrderItemDto(productName: "Product 1", unitPrice: 100m, quantity: 2),
            CreateTestOrderItemDto(productName: "Product 2", unitPrice: 50m, quantity: 3),
            CreateTestOrderItemDto(productName: "Product 3", unitPrice: 75m, quantity: 1)
        };
        var command = CreateTestCommand(items: items);

        Order? capturedOrder = null;

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithShippingAddress_ShouldSetShippingAddressCorrectly()
    {
        // Arrange
        var shippingAddress = CreateTestAddress(
            fullName: "Jane Doe",
            phone: "0902345678",
            addressLine1: "456 Oak Street",
            ward: "Ward 5",
            district: "District 3",
            province: "Hanoi");

        var command = CreateTestCommand(shippingAddress: shippingAddress);

        Order? capturedOrder = null;

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.ShippingAddress.Should().NotBeNull();
        capturedOrder.ShippingAddress!.FullName.Should().Be("Jane Doe");
        capturedOrder.ShippingAddress.Province.Should().Be("Hanoi");
    }

    [Fact]
    public async Task Handle_WithBillingAddress_ShouldSetBillingAddressCorrectly()
    {
        // Arrange
        var billingAddress = CreateTestAddress(
            fullName: "Billing Person",
            phone: "0903456789",
            addressLine1: "789 Billing Street");

        var command = CreateTestCommand(billingAddress: billingAddress);

        Order? capturedOrder = null;

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.BillingAddress.Should().NotBeNull();
        capturedOrder.BillingAddress!.FullName.Should().Be("Billing Person");
    }

    [Fact]
    public async Task Handle_WithoutBillingAddress_ShouldUsShippingAddressAsBilling()
    {
        // Arrange
        var shippingAddress = CreateTestAddress(fullName: "Shipping Person");
        var command = CreateTestCommand(shippingAddress: shippingAddress, billingAddress: null);

        Order? capturedOrder = null;

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.BillingAddress.Should().NotBeNull();
        capturedOrder.BillingAddress!.FullName.Should().Be("Shipping Person");
    }

    [Fact]
    public async Task Handle_WithDiscount_ShouldApplyDiscountCorrectly()
    {
        // Arrange
        var command = CreateTestCommand(
            discountAmount: 20.00m,
            couponCode: "SAVE20");

        Order? capturedOrder = null;

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.DiscountAmount.Should().Be(20.00m);
        capturedOrder.CouponCode.Should().Be("SAVE20");
    }

    [Fact]
    public async Task Handle_WithCheckoutSessionId_ShouldLinkToCheckoutSession()
    {
        // Arrange
        var checkoutSessionId = Guid.NewGuid();
        var command = CreateTestCommand(checkoutSessionId: checkoutSessionId);

        Order? capturedOrder = null;

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.CheckoutSessionId.Should().Be(checkoutSessionId);
    }

    [Fact]
    public async Task Handle_WithCustomerNotes_ShouldSetCustomerNotes()
    {
        // Arrange
        var command = CreateTestCommand(customerNotes: "Please deliver after 5 PM");

        Order? capturedOrder = null;

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.CustomerNotes.Should().Be("Please deliver after 5 PM");
    }

    [Fact]
    public async Task Handle_ShouldGenerateSequentialOrderNumber()
    {
        // Arrange
        var existingOrder = Order.Create(
            $"ORD-{DateTime.UtcNow:yyyyMMdd}-0005",
            "existing@example.com",
            100m,
            110m,
            "VND",
            TestTenantId);

        var command = CreateTestCommand();

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken _) => o);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OrderNumber.Should().EndWith("0006");
    }

    #endregion

    #region Validation Scenarios

    [Fact]
    public async Task Handle_WithNullItems_ShouldReturnValidationError()
    {
        // Arrange
        var command = new CreateOrderCommand(
            "customer@example.com",
            "John Doe",
            "0901234567",
            CreateTestAddress(),
            null,
            "Standard",
            10.00m,
            null,
            0m,
            null,
            null!,
            "VND",
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-ORDER-001");
        result.Error.Message.Should().Contain("at least one item");

        _orderRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyItems_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand(items: new List<CreateOrderItemDto>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        result.Error.Code.Should().Be("NOIR-ORDER-001");
        result.Error.Message.Should().Contain("at least one item");

        _orderRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToServices()
    {
        // Arrange
        var command = CreateTestCommand();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken _) => o);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _orderRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<LatestOrderNumberTodaySpec>(), token),
            Times.Once);
        _orderRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Order>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCalculateSubTotalFromItems()
    {
        // Arrange
        var items = new List<CreateOrderItemDto>
        {
            CreateTestOrderItemDto(unitPrice: 100m, quantity: 2), // 200
            CreateTestOrderItemDto(unitPrice: 50m, quantity: 3)   // 150
        };
        var command = CreateTestCommand(items: items, shippingAmount: 10m, discountAmount: 0m);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken _) => o);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SubTotal.Should().Be(350m); // 200 + 150
        result.Value.GrandTotal.Should().Be(360m); // 350 + 10 (shipping)
    }

    [Fact]
    public async Task Handle_ShouldCalculateGrandTotalWithDiscountAndShipping()
    {
        // Arrange
        var items = new List<CreateOrderItemDto>
        {
            CreateTestOrderItemDto(unitPrice: 100m, quantity: 2) // 200
        };
        var command = CreateTestCommand(
            items: items,
            shippingAmount: 20m,
            discountAmount: 30m);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken _) => o);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SubTotal.Should().Be(200m);
        result.Value.DiscountAmount.Should().Be(30m);
        result.Value.ShippingAmount.Should().Be(20m);
        result.Value.GrandTotal.Should().Be(190m); // 200 - 30 + 20
    }

    [Fact]
    public async Task Handle_ForAuthenticatedUser_ShouldSetCustomerId()
    {
        // Arrange
        var command = CreateTestCommand();

        Order? capturedOrder = null;

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.CustomerId.Should().Be(Guid.Parse(TestUserId));
    }

    [Fact]
    public async Task Handle_ForGuestUser_ShouldNotSetCustomerId()
    {
        // Arrange
        _currentUserMock.Setup(x => x.IsAuthenticated).Returns(false);

        var command = CreateTestCommand();

        Order? capturedOrder = null;

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<LatestOrderNumberTodaySpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.CustomerId.Should().BeNull();
    }

    #endregion
}
