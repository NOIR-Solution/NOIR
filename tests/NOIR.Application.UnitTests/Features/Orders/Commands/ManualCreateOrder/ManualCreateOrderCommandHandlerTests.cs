using NOIR.Application.Features.Orders.Commands.ManualCreateOrder;
using NOIR.Application.Features.Orders.DTOs;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Orders.Commands.ManualCreateOrder;

/// <summary>
/// Unit tests for ManualCreateOrderCommandHandler.
/// Tests manual order creation scenarios with mocked dependencies.
/// </summary>
public class ManualCreateOrderCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Order, Guid>> _orderRepositoryMock;
    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IRepository<PaymentTransaction, Guid>> _paymentRepositoryMock;
    private readonly Mock<IRepository<PaymentGateway, Guid>> _gatewayRepositoryMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<IInventoryMovementLogger> _movementLoggerMock;
    private readonly Mock<IOrderNumberGenerator> _orderNumberGeneratorMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly ManualCreateOrderCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestEmail = "admin@noir.local";

    public ManualCreateOrderCommandHandlerTests()
    {
        _orderRepositoryMock = new Mock<IRepository<Order, Guid>>();
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _paymentRepositoryMock = new Mock<IRepository<PaymentTransaction, Guid>>();
        _gatewayRepositoryMock = new Mock<IRepository<PaymentGateway, Guid>>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _movementLoggerMock = new Mock<IInventoryMovementLogger>();
        _orderNumberGeneratorMock = new Mock<IOrderNumberGenerator>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);
        _currentUserMock.Setup(x => x.Email).Returns(TestEmail);

        // Default payment service setup
        _paymentServiceMock.Setup(x => x.GenerateTransactionNumber()).Returns("TXN-20260220-0001");

        // Default order number generator setup
        _orderNumberGeneratorMock
            .Setup(x => x.GenerateNextAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync($"ORD-{DateTime.UtcNow:yyyyMMdd}-0001");

        _handler = new ManualCreateOrderCommandHandler(
            _orderRepositoryMock.Object,
            _productRepositoryMock.Object,
            _paymentRepositoryMock.Object,
            _gatewayRepositoryMock.Object,
            _paymentServiceMock.Object,
            _movementLoggerMock.Object,
            _orderNumberGeneratorMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static Product CreateTestProduct(
        string name = "Test Product",
        ProductStatus status = ProductStatus.Active,
        string? tenantId = TestTenantId)
    {
        var product = Product.Create(name, name.ToLowerInvariant().Replace(' ', '-'), 100m, "VND", tenantId);

        // Publish the product if active
        if (status == ProductStatus.Active)
        {
            product.Publish();
        }

        return product;
    }

    private static ProductVariant AddTestVariant(
        Product product,
        string name = "Default",
        decimal price = 100m,
        string? sku = "SKU-001",
        int stockQuantity = 100)
    {
        var variant = product.AddVariant(name, price, sku);
        variant.SetStock(stockQuantity);
        return variant;
    }

    private static ManualCreateOrderCommand CreateTestCommand(
        string customerEmail = "customer@example.com",
        string? customerName = "Nguyen Van A",
        string? customerPhone = "0901234567",
        Guid? customerId = null,
        List<ManualOrderItemDto>? items = null,
        CreateAddressDto? shippingAddress = null,
        CreateAddressDto? billingAddress = null,
        string? shippingMethod = null,
        string? couponCode = null,
        string? customerNotes = null,
        string? internalNotes = null,
        PaymentMethod? paymentMethod = null,
        PaymentStatus? initialPaymentStatus = null,
        decimal shippingAmount = 0,
        decimal discountAmount = 0,
        decimal taxAmount = 0,
        string currency = "VND")
    {
        return new ManualCreateOrderCommand(
            customerEmail,
            customerName,
            customerPhone,
            customerId,
            items ?? new List<ManualOrderItemDto> { new(Guid.NewGuid(), 1) },
            shippingAddress,
            billingAddress,
            shippingMethod,
            couponCode,
            customerNotes,
            internalNotes,
            paymentMethod,
            initialPaymentStatus,
            shippingAmount,
            discountAmount,
            taxAmount,
            currency);
    }

    private static CreateAddressDto CreateTestAddress(
        string fullName = "Nguyen Van A",
        string phone = "0901234567",
        string addressLine1 = "123 Le Loi Street",
        string? addressLine2 = null,
        string ward = "Ben Nghe",
        string district = "District 1",
        string province = "Ho Chi Minh City",
        string country = "Vietnam",
        string? postalCode = "700000")
    {
        return new CreateAddressDto(
            fullName, phone, addressLine1, addressLine2,
            ward, district, province, country, postalCode);
    }

    private void SetupOrderRepositoryDefaults()
    {
        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken _) => o);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateOrderSuccessfully()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = AddTestVariant(product, "Size M", 150m, "SKU-M");

        var items = new List<ManualOrderItemDto> { new(variant.Id, 2) };
        var command = CreateTestCommand(items: items);

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        SetupOrderRepositoryDefaults();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.CustomerEmail.Should().Be("customer@example.com");
        result.Value.Status.Should().Be(OrderStatus.Pending);

        _orderRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMultipleItems_ShouldCreateOrderWithAllItems()
    {
        // Arrange
        var product1 = CreateTestProduct("Product 1");
        var variant1 = AddTestVariant(product1, "Variant A", 100m, "SKU-A");

        var product2 = CreateTestProduct("Product 2");
        var variant2 = AddTestVariant(product2, "Variant B", 50m, "SKU-B");

        var items = new List<ManualOrderItemDto>
        {
            new(variant1.Id, 2),
            new(variant2.Id, 3)
        };
        var command = CreateTestCommand(items: items);

        Order? capturedOrder = null;

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product1, product2 });

        SetupOrderRepositoryDefaults();
        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithUnitPriceOverride_ShouldUseOverriddenPrice()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = AddTestVariant(product, "Default", 100m);

        var items = new List<ManualOrderItemDto> { new(variant.Id, 1, UnitPrice: 75m) };
        var command = CreateTestCommand(items: items);

        Order? capturedOrder = null;

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        SetupOrderRepositoryDefaults();
        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.Items.First().UnitPrice.Should().Be(75m);
    }

    [Fact]
    public async Task Handle_WithNoUnitPriceOverride_ShouldUseVariantPrice()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = AddTestVariant(product, "Default", 200m);

        var items = new List<ManualOrderItemDto> { new(variant.Id, 1) };
        var command = CreateTestCommand(items: items);

        Order? capturedOrder = null;

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        SetupOrderRepositoryDefaults();
        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.Items.First().UnitPrice.Should().Be(200m);
    }

    [Fact]
    public async Task Handle_WithShippingAddress_ShouldSetShippingAndBillingAddress()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = AddTestVariant(product);

        var shippingAddress = CreateTestAddress(fullName: "Tran Thi B");
        var items = new List<ManualOrderItemDto> { new(variant.Id, 1) };
        var command = CreateTestCommand(items: items, shippingAddress: shippingAddress);

        Order? capturedOrder = null;

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        SetupOrderRepositoryDefaults();
        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.ShippingAddress.Should().NotBeNull();
        capturedOrder.ShippingAddress!.FullName.Should().Be("Tran Thi B");
        // Billing should be same as shipping when not provided
        capturedOrder.BillingAddress.Should().NotBeNull();
        capturedOrder.BillingAddress!.FullName.Should().Be("Tran Thi B");
    }

    [Fact]
    public async Task Handle_WithBothAddresses_ShouldSetSeparateAddresses()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = AddTestVariant(product);

        var shippingAddress = CreateTestAddress(fullName: "Shipping Person");
        var billingAddress = CreateTestAddress(fullName: "Billing Person");
        var items = new List<ManualOrderItemDto> { new(variant.Id, 1) };
        var command = CreateTestCommand(
            items: items,
            shippingAddress: shippingAddress,
            billingAddress: billingAddress);

        Order? capturedOrder = null;

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        SetupOrderRepositoryDefaults();
        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.ShippingAddress!.FullName.Should().Be("Shipping Person");
        capturedOrder.BillingAddress!.FullName.Should().Be("Billing Person");
    }

    [Fact]
    public async Task Handle_WithDiscount_ShouldApplyDiscountCorrectly()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = AddTestVariant(product, price: 100m);

        var items = new List<ManualOrderItemDto> { new(variant.Id, 2) };
        var command = CreateTestCommand(
            items: items,
            discountAmount: 20m,
            couponCode: "SAVE20");

        Order? capturedOrder = null;

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        SetupOrderRepositoryDefaults();
        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.DiscountAmount.Should().Be(20m);
        capturedOrder.CouponCode.Should().Be("SAVE20");
    }

    [Fact]
    public async Task Handle_WithShippingDetails_ShouldSetShippingMethod()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = AddTestVariant(product);

        var items = new List<ManualOrderItemDto> { new(variant.Id, 1) };
        var command = CreateTestCommand(
            items: items,
            shippingMethod: "Express",
            shippingAmount: 30m);

        Order? capturedOrder = null;

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        SetupOrderRepositoryDefaults();
        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.ShippingMethod.Should().Be("Express");
        capturedOrder.ShippingAmount.Should().Be(30m);
    }

    [Fact]
    public async Task Handle_WithPaidStatus_ShouldConfirmOrder()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = AddTestVariant(product);

        var items = new List<ManualOrderItemDto> { new(variant.Id, 1) };
        var command = CreateTestCommand(
            items: items,
            initialPaymentStatus: PaymentStatus.Paid);

        Order? capturedOrder = null;

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        SetupOrderRepositoryDefaults();
        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.Status.Should().Be(OrderStatus.Confirmed);
    }

    [Fact]
    public async Task Handle_WithCustomerNotes_ShouldSetCustomerNotes()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = AddTestVariant(product);

        var items = new List<ManualOrderItemDto> { new(variant.Id, 1) };
        var command = CreateTestCommand(
            items: items,
            customerNotes: "Please deliver after 5 PM");

        Order? capturedOrder = null;

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        SetupOrderRepositoryDefaults();
        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.CustomerNotes.Should().Be("Please deliver after 5 PM");
    }

    [Fact]
    public async Task Handle_WithInternalNotes_ShouldAddInternalNotes()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = AddTestVariant(product);

        var items = new List<ManualOrderItemDto> { new(variant.Id, 1) };
        var command = CreateTestCommand(
            items: items,
            internalNotes: "VIP customer - priority processing");

        Order? capturedOrder = null;

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        SetupOrderRepositoryDefaults();
        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.InternalNotes.Should().Contain("VIP customer - priority processing");
        capturedOrder.InternalNotes.Should().Contain($"Order manually created by {TestEmail}");
    }

    [Fact]
    public async Task Handle_ShouldCalculateSubtotalAndGrandTotal()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = AddTestVariant(product, "Default", 100m);

        var items = new List<ManualOrderItemDto> { new(variant.Id, 3) }; // 3 * 100 = 300
        var command = CreateTestCommand(
            items: items,
            shippingMethod: "Express",
            shippingAmount: 30m,
            discountAmount: 50m,
            taxAmount: 25m);

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        SetupOrderRepositoryDefaults();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SubTotal.Should().Be(300m); // 3 * 100
        // Grand total = 300 - 50 + 30 + 25 = 305
        result.Value.GrandTotal.Should().Be(305m);
    }

    [Fact]
    public async Task Handle_ShouldGenerateSequentialOrderNumber()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = AddTestVariant(product);
        var items = new List<ManualOrderItemDto> { new(variant.Id, 1) };
        var command = CreateTestCommand(items: items);

        // Setup order number generator to return a specific sequence
        _orderNumberGeneratorMock
            .Setup(x => x.GenerateNextAsync(TestTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync($"ORD-{DateTime.UtcNow:yyyyMMdd}-0006");

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

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
        _orderNumberGeneratorMock.Verify(
            x => x.GenerateNextAsync(TestTenantId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldAddAutoInternalNoteWithCreatorEmail()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = AddTestVariant(product);
        var items = new List<ManualOrderItemDto> { new(variant.Id, 1) };
        var command = CreateTestCommand(items: items);

        Order? capturedOrder = null;

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        SetupOrderRepositoryDefaults();
        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.InternalNotes.Should().Contain($"Order manually created by {TestEmail}");
    }

    #endregion

    #region Validation / Error Scenarios

    [Fact]
    public async Task Handle_WithNullItems_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand(items: null!);
        // Override the null guard in helper
        var rawCommand = new ManualCreateOrderCommand(
            "customer@example.com", "Name", "0901234567", null,
            null!, null, null, null, null, null, null, null, null);

        // Act
        var result = await _handler.Handle(rawCommand, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Order.MustHaveItems);

        _orderRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyItems_ShouldReturnValidationError()
    {
        // Arrange
        var command = CreateTestCommand(items: new List<ManualOrderItemDto>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Order.MustHaveItems);

        _orderRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithMissingVariant_ShouldReturnNotFoundError()
    {
        // Arrange
        var missingVariantId = Guid.NewGuid();
        var items = new List<ManualOrderItemDto> { new(missingVariantId, 1) };
        var command = CreateTestCommand(items: items);

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Product.VariantNotFound);

        _orderRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithInactiveProduct_ShouldReturnValidationError()
    {
        // Arrange
        var product = CreateTestProduct(status: ProductStatus.Draft);
        var variant = AddTestVariant(product);

        var items = new List<ManualOrderItemDto> { new(variant.Id, 1) };
        var command = CreateTestCommand(items: items);

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(ErrorCodes.Product.InvalidStatus);

        _orderRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithItemDiscountAmount_ShouldSetItemDiscount()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = AddTestVariant(product, "Default", 100m);

        var items = new List<ManualOrderItemDto>
        {
            new ManualOrderItemDto(
                ProductVariantId: variant.Id,
                Quantity: 2,
                UnitPrice: null,
                DiscountAmount: 10m)
        };
        var command = CreateTestCommand(items: items);

        Order? capturedOrder = null;

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        SetupOrderRepositoryDefaults();
        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();

        // SubTotal = sum of UnitPrice * Quantity (before item discounts)
        capturedOrder!.SubTotal.Should().Be(200m);

        // Item-level discount is set on the OrderItem
        var orderItem = capturedOrder.Items.First();
        orderItem.DiscountAmount.Should().Be(10m);
        orderItem.LineTotal.Should().Be(190m); // (100*2) - 10
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToAllServices()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = AddTestVariant(product);
        var items = new List<ManualOrderItemDto> { new(variant.Id, 1) };
        var command = CreateTestCommand(items: items);

        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        SetupOrderRepositoryDefaults();

        // Act
        await _handler.Handle(command, token);

        // Assert
        _productRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), token), Times.Once);
        _orderRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Order>(), token), Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutPaidStatus_ShouldNotConfirmOrder()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = AddTestVariant(product);
        var items = new List<ManualOrderItemDto> { new(variant.Id, 1) };
        var command = CreateTestCommand(items: items, initialPaymentStatus: PaymentStatus.Pending);

        Order? capturedOrder = null;

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        SetupOrderRepositoryDefaults();
        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.Status.Should().Be(OrderStatus.Pending);
    }

    [Fact]
    public async Task Handle_WithCustomerInfo_ShouldSetCustomerInfo()
    {
        // Arrange
        var product = CreateTestProduct();
        var variant = AddTestVariant(product);
        var customerId = Guid.NewGuid();
        var items = new List<ManualOrderItemDto> { new(variant.Id, 1) };
        var command = CreateTestCommand(
            items: items,
            customerId: customerId,
            customerName: "Test Customer",
            customerPhone: "0909090909");

        Order? capturedOrder = null;

        _productRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<ProductsByVariantIdsForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product> { product });

        SetupOrderRepositoryDefaults();
        _orderRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Callback<Order, CancellationToken>((order, _) => capturedOrder = order)
            .ReturnsAsync((Order o, CancellationToken _) => o);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedOrder.Should().NotBeNull();
        capturedOrder!.CustomerId.Should().Be(customerId);
        capturedOrder.CustomerName.Should().Be("Test Customer");
        capturedOrder.CustomerPhone.Should().Be("0909090909");
    }

    #endregion
}
