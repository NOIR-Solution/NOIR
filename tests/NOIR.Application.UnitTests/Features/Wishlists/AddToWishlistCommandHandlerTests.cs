namespace NOIR.Application.UnitTests.Features.Wishlists;

/// <summary>
/// Unit tests for AddToWishlistCommandHandler.
/// Tests adding products to wishlists with mocked dependencies.
/// </summary>
public class AddToWishlistCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Wishlist, Guid>> _wishlistRepositoryMock;
    private readonly Mock<IRepository<Product, Guid>> _productRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly AddToWishlistCommandHandler _handler;

    public AddToWishlistCommandHandlerTests()
    {
        _wishlistRepositoryMock = new Mock<IRepository<Wishlist, Guid>>();
        _productRepositoryMock = new Mock<IRepository<Product, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new AddToWishlistCommandHandler(
            _wishlistRepositoryMock.Object,
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static AddToWishlistCommand CreateValidCommand(
        Guid? wishlistId = null,
        Guid? productId = null,
        Guid? productVariantId = null,
        string? note = null,
        string? userId = "user-123")
    {
        return new AddToWishlistCommand(
            wishlistId,
            productId ?? Guid.NewGuid(),
            productVariantId,
            note) { UserId = userId };
    }

    private static Wishlist CreateTestWishlist(string userId = "user-123", string name = "Test Wishlist")
    {
        return Wishlist.Create(userId, name);
    }

    #endregion

    #region Success Scenarios - Specific Wishlist

    [Fact]
    public async Task Handle_WithSpecificWishlistId_ShouldAddToThatWishlist()
    {
        // Arrange
        var wishlistId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var existingWishlist = CreateTestWishlist();
        var command = CreateValidCommand(wishlistId: wishlistId, productId: productId);

        _productRepositoryMock
            .Setup(x => x.ExistsAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWishlist);

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistDetailByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWishlist);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Success Scenarios - Default Wishlist

    [Fact]
    public async Task Handle_WithNoWishlistId_ShouldUseDefaultWishlist()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var defaultWishlist = CreateTestWishlist();
        var command = CreateValidCommand(wishlistId: null, productId: productId);

        _productRepositoryMock
            .Setup(x => x.ExistsAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<DefaultWishlistByUserSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultWishlist);

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistDetailByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultWishlist);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithNoWishlistIdAndNoDefault_ShouldCreateDefaultWishlist()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = CreateValidCommand(wishlistId: null, productId: productId);

        _productRepositoryMock
            .Setup(x => x.ExistsAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<DefaultWishlistByUserSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wishlist?)null);

        Wishlist? capturedWishlist = null;
        _wishlistRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Wishlist>(), It.IsAny<CancellationToken>()))
            .Callback<Wishlist, CancellationToken>((w, _) => capturedWishlist = w)
            .ReturnsAsync((Wishlist w, CancellationToken _) => w);

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistDetailByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ISpecification<Wishlist> spec, CancellationToken _) => capturedWishlist);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _wishlistRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Wishlist>(), It.IsAny<CancellationToken>()),
            Times.Once);

        capturedWishlist.Should().NotBeNull();
        capturedWishlist!.Name.Should().Be("My Wishlist");
        capturedWishlist.IsDefault.Should().BeTrue();
    }

    #endregion

    #region Failure Scenarios

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Handle_WhenUserIdIsNullOrEmpty_ShouldReturnFailure(string? userId)
    {
        // Arrange
        var command = CreateValidCommand(userId: userId);

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
        var command = CreateValidCommand(productId: productId);

        _productRepositoryMock
            .Setup(x => x.ExistsAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSpecificWishlistNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var wishlistId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var command = CreateValidCommand(wishlistId: wishlistId, productId: productId);

        _productRepositoryMock
            .Setup(x => x.ExistsAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wishlist?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Authorization Scenarios

    [Fact]
    public async Task Handle_WhenUserDoesNotOwnSpecificWishlist_ShouldReturnFailure()
    {
        // Arrange
        var wishlistId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var otherUsersWishlist = CreateTestWishlist(userId: "other-user");
        var command = CreateValidCommand(wishlistId: wishlistId, productId: productId, userId: "user-123");

        _productRepositoryMock
            .Setup(x => x.ExistsAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherUsersWishlist);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithNote_ShouldAddItemWithNote()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingWishlist = CreateTestWishlist();
        var command = CreateValidCommand(
            wishlistId: Guid.NewGuid(),
            productId: productId,
            note: "Want this in blue");

        _productRepositoryMock
            .Setup(x => x.ExistsAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWishlist);

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistDetailByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWishlist);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingWishlist.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepositories()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingWishlist = CreateTestWishlist();
        var command = CreateValidCommand(wishlistId: Guid.NewGuid(), productId: productId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _productRepositoryMock
            .Setup(x => x.ExistsAsync(productId, token))
            .ReturnsAsync(true);

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByIdSpec>(),
                token))
            .ReturnsAsync(existingWishlist);

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistDetailByIdSpec>(),
                token))
            .ReturnsAsync(existingWishlist);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _productRepositoryMock.Verify(
            x => x.ExistsAsync(productId, token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}
