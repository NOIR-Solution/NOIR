namespace NOIR.Application.UnitTests.Features.Wishlists;

/// <summary>
/// Unit tests for RemoveFromWishlistCommandHandler.
/// Tests removing items from wishlists with mocked dependencies.
/// </summary>
public class RemoveFromWishlistCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Wishlist, Guid>> _wishlistRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RemoveFromWishlistCommandHandler _handler;

    public RemoveFromWishlistCommandHandlerTests()
    {
        _wishlistRepositoryMock = new Mock<IRepository<Wishlist, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new RemoveFromWishlistCommandHandler(
            _wishlistRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static Wishlist CreateTestWishlistWithItem(
        string userId = "user-123",
        Guid? itemProductId = null)
    {
        var wishlist = Wishlist.Create(userId, "Test Wishlist");
        wishlist.AddItem(itemProductId ?? Guid.NewGuid());
        return wishlist;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldRemoveItemAndSucceed()
    {
        // Arrange
        var wishlist = CreateTestWishlistWithItem();
        var itemId = wishlist.Items.First().Id;
        var command = new RemoveFromWishlistCommand(itemId) { UserId = "user-123" };

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistItemByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(wishlist);

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistDetailByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(wishlist);

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

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenWishlistItemNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = new RemoveFromWishlistCommand(Guid.NewGuid()) { UserId = "user-123" };

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistItemByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wishlist?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Authorization Scenarios

    [Fact]
    public async Task Handle_WhenUserDoesNotOwnWishlist_ShouldReturnFailure()
    {
        // Arrange
        var wishlist = CreateTestWishlistWithItem(userId: "other-user");
        var itemId = wishlist.Items.First().Id;
        var command = new RemoveFromWishlistCommand(itemId) { UserId = "user-123" };

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

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepositories()
    {
        // Arrange
        var wishlist = CreateTestWishlistWithItem();
        var itemId = wishlist.Items.First().Id;
        var command = new RemoveFromWishlistCommand(itemId) { UserId = "user-123" };
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistItemByIdSpec>(),
                token))
            .ReturnsAsync(wishlist);

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistDetailByIdSpec>(),
                token))
            .ReturnsAsync(wishlist);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _wishlistRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<WishlistItemByIdSpec>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}
