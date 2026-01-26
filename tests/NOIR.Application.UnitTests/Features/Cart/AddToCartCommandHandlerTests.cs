using NOIR.Application.Features.Cart.Commands.AddToCart;
using NOIR.Application.Features.Cart.DTOs;
using NOIR.Application.Features.Cart.Specifications;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Cart;

/// <summary>
/// Unit tests for AddToCartCommandHandler.
/// </summary>
public class AddToCartCommandHandlerTests
{
    private readonly Mock<IRepository<Domain.Entities.Cart.Cart, Guid>> _cartRepositoryMock;
    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<AddToCartCommandHandler>> _loggerMock;
    private readonly AddToCartCommandHandler _handler;

    private const string TestTenantId = "test-tenant";
    private const string TestUserId = "test-user-123";
    private const string TestSessionId = "test-session-456";

    public AddToCartCommandHandlerTests()
    {
        _cartRepositoryMock = new Mock<IRepository<Domain.Entities.Cart.Cart, Guid>>();
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<AddToCartCommandHandler>>();

        _handler = new AddToCartCommandHandler(
            _cartRepositoryMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidProduct_AddsItemToNewCart()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var product = CreateTestProduct(productId, variantId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProductWithVariantByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Cart.Cart?)null);

        _cartRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.Cart.Cart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Cart.Cart cart, CancellationToken _) => cart);

        var command = new AddToCartCommand(productId, variantId, 2) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.ItemCount.Should().Be(2);

        _cartRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.Cart.Cart>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidProduct_AddsItemToExistingCart()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var product = CreateTestProduct(productId, variantId);
        var existingCart = Domain.Entities.Cart.Cart.CreateForUser(TestUserId, "VND", TestTenantId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProductWithVariantByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartByUserIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCart);

        var command = new AddToCartCommand(productId, variantId, 1) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.ItemCount.Should().Be(1);

        _cartRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Domain.Entities.Cart.Cart>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProductWithVariantByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = new AddToCartCommand(productId, variantId, 1) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Message.Should().Contain("Product");
    }

    [Fact]
    public async Task Handle_InactiveProduct_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var product = CreateTestProduct(productId, variantId, ProductStatus.Draft);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProductWithVariantByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var command = new AddToCartCommand(productId, variantId, 1) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Message.Should().Contain("not available");
    }

    [Fact]
    public async Task Handle_InsufficientStock_ReturnsFailure()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var product = CreateTestProduct(productId, variantId, stockQuantity: 5);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProductWithVariantByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var command = new AddToCartCommand(productId, variantId, 10) { UserId = TestUserId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Message.Should().Contain("Insufficient stock");
    }

    [Fact]
    public async Task Handle_GuestUser_CreatesGuestCart()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var product = CreateTestProduct(productId, variantId);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ProductWithVariantByIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _cartRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<ActiveCartBySessionIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Cart.Cart?)null);

        _cartRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.Cart.Cart>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Domain.Entities.Cart.Cart cart, CancellationToken _) => cart);

        var command = new AddToCartCommand(productId, variantId, 1) { SessionId = TestSessionId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.IsGuest.Should().BeTrue();

        _cartRepositoryMock.Verify(x => x.AddAsync(
            It.Is<Domain.Entities.Cart.Cart>(c => c.SessionId == TestSessionId && c.UserId == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Product CreateTestProduct(
        Guid productId,
        Guid variantId,
        ProductStatus status = ProductStatus.Active,
        int stockQuantity = 100)
    {
        var product = Product.Create("Test Product", "test-product", 100m, "VND", TestTenantId);
        // Use reflection to set the ID since it's set in the constructor
        typeof(Entity<Guid>).GetProperty("Id")!.SetValue(product, productId);

        var variant = product.AddVariant("Default", 100m, "SKU-001");
        typeof(Entity<Guid>).GetProperty("Id")!.SetValue(variant, variantId);
        variant.SetStock(stockQuantity);

        if (status == ProductStatus.Active)
        {
            product.Publish();
        }

        return product;
    }
}
