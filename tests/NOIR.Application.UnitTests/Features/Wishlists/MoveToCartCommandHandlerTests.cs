using NOIR.Application.Features.Cart.Specifications;
using NOIR.Application.Features.Products.Specifications;

namespace NOIR.Application.UnitTests.Features.Wishlists;

/// <summary>
/// Unit tests for MoveToCartCommandHandler.
/// Tests moving wishlist items to the shopping cart with mocked dependencies.
/// </summary>
public class MoveToCartCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Wishlist, Guid>> _wishlistRepositoryMock;
    private readonly Mock<IRepository<Domain.Entities.Cart.Cart, Guid>> _cartRepositoryMock;
    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<MoveToCartCommandHandler>> _loggerMock;
    private readonly MoveToCartCommandHandler _handler;

    public MoveToCartCommandHandlerTests()
    {
        _wishlistRepositoryMock = new Mock<IRepository<Wishlist, Guid>>();
        _cartRepositoryMock = new Mock<IRepository<Domain.Entities.Cart.Cart, Guid>>();
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<MoveToCartCommandHandler>>();

        _handler = new MoveToCartCommandHandler(
            _wishlistRepositoryMock.Object,
            _cartRepositoryMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    private static Wishlist CreateTestWishlistWithItem(
        string userId = "user-123",
        Guid? productId = null)
    {
        var wishlist = Wishlist.Create(userId, "Test Wishlist");
        wishlist.AddItem(productId ?? Guid.NewGuid());
        return wishlist;
    }

    #endregion

    #region Failure Scenarios

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Handle_WhenUserIdIsNullOrEmpty_ShouldReturnFailure(string? userId)
    {
        // Arrange
        var command = new MoveToCartCommand(Guid.NewGuid()) { UserId = userId };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenWishlistNotFoundForItem_ShouldReturnNotFound()
    {
        // Arrange
        var command = new MoveToCartCommand(Guid.NewGuid()) { UserId = "user-123" };

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistItemByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wishlist?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserDoesNotOwnWishlist_ShouldReturnFailure()
    {
        // Arrange
        var wishlist = CreateTestWishlistWithItem(userId: "other-user");
        var itemId = wishlist.Items.First().Id;
        var command = new MoveToCartCommand(itemId) { UserId = "user-123" };

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistItemByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(wishlist);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenItemNotFoundInWishlist_ShouldReturnNotFound()
    {
        // Arrange
        var wishlist = CreateTestWishlistWithItem();
        var nonExistentItemId = Guid.NewGuid();
        var command = new MoveToCartCommand(nonExistentItemId) { UserId = "user-123" };

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistItemByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(wishlist);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var wishlist = CreateTestWishlistWithItem(productId: productId);
        var itemId = wishlist.Items.First().Id;
        var command = new MoveToCartCommand(itemId) { UserId = "user-123" };

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistItemByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(wishlist);

        _productRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ProductWithVariantByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToWishlistRepository()
    {
        // Arrange
        var command = new MoveToCartCommand(Guid.NewGuid()) { UserId = "user-123" };
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistItemByIdSpec>(),
                token))
            .ReturnsAsync((Wishlist?)null);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _wishlistRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<WishlistItemByIdSpec>(), token),
            Times.Once);
    }

    #endregion
}
