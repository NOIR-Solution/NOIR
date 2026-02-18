namespace NOIR.Application.UnitTests.Features.Wishlists;

/// <summary>
/// Unit tests for GetWishlistByIdQueryHandler.
/// Tests retrieving a wishlist by ID with item details.
/// </summary>
public class GetWishlistByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Wishlist, Guid>> _wishlistRepositoryMock;
    private readonly GetWishlistByIdQueryHandler _handler;

    public GetWishlistByIdQueryHandlerTests()
    {
        _wishlistRepositoryMock = new Mock<IRepository<Wishlist, Guid>>();

        _handler = new GetWishlistByIdQueryHandler(_wishlistRepositoryMock.Object);
    }

    private static Wishlist CreateTestWishlist(string userId = "user-123", string name = "Test Wishlist")
    {
        return Wishlist.Create(userId, name);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenWishlistExists_ShouldReturnDetailDto()
    {
        // Arrange
        var wishlistId = Guid.NewGuid();
        var existingWishlist = CreateTestWishlist(name: "My Favorites");
        var query = new GetWishlistByIdQuery(wishlistId) { UserId = "user-123" };

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistDetailByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWishlist);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("My Favorites");
    }

    [Fact]
    public async Task Handle_WhenWishlistHasItems_ShouldReturnWithItems()
    {
        // Arrange
        var wishlistId = Guid.NewGuid();
        var existingWishlist = CreateTestWishlist();
        existingWishlist.AddItem(Guid.NewGuid());
        existingWishlist.AddItem(Guid.NewGuid());
        var query = new GetWishlistByIdQuery(wishlistId) { UserId = "user-123" };

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistDetailByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWishlist);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ItemCount.Should().Be(2);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenWishlistNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetWishlistByIdQuery(Guid.NewGuid()) { UserId = "user-123" };

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistDetailByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wishlist?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Authorization Scenarios

    [Fact]
    public async Task Handle_WhenUserDoesNotOwnWishlist_ShouldReturnFailure()
    {
        // Arrange
        var existingWishlist = CreateTestWishlist(userId: "other-user");
        var query = new GetWishlistByIdQuery(Guid.NewGuid()) { UserId = "user-123" };

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistDetailByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWishlist);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var existingWishlist = CreateTestWishlist();
        var query = new GetWishlistByIdQuery(Guid.NewGuid()) { UserId = "user-123" };
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistDetailByIdSpec>(),
                token))
            .ReturnsAsync(existingWishlist);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _wishlistRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<WishlistDetailByIdSpec>(), token),
            Times.Once);
    }

    #endregion
}
