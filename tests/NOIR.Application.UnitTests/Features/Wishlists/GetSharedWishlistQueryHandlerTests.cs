namespace NOIR.Application.UnitTests.Features.Wishlists;

/// <summary>
/// Unit tests for GetSharedWishlistQueryHandler.
/// Tests retrieving shared wishlists by share token.
/// </summary>
public class GetSharedWishlistQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Wishlist, Guid>> _wishlistRepositoryMock;
    private readonly GetSharedWishlistQueryHandler _handler;

    public GetSharedWishlistQueryHandlerTests()
    {
        _wishlistRepositoryMock = new Mock<IRepository<Wishlist, Guid>>();

        _handler = new GetSharedWishlistQueryHandler(_wishlistRepositoryMock.Object);
    }

    private static Wishlist CreateSharedWishlist(
        string userId = "user-123",
        string name = "Shared Wishlist")
    {
        var wishlist = Wishlist.Create(userId, name);
        wishlist.GenerateShareToken();
        return wishlist;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenSharedWishlistExists_ShouldReturnDetailDto()
    {
        // Arrange
        var wishlist = CreateSharedWishlist(name: "Holiday Gifts");
        var query = new GetSharedWishlistQuery(wishlist.ShareToken!);

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByShareTokenSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(wishlist);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Holiday Gifts");
    }

    [Fact]
    public async Task Handle_WhenSharedWishlistHasItems_ShouldReturnWithItems()
    {
        // Arrange
        var wishlist = CreateSharedWishlist();
        wishlist.AddItem(Guid.NewGuid());
        wishlist.AddItem(Guid.NewGuid());
        var query = new GetSharedWishlistQuery(wishlist.ShareToken!);

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByShareTokenSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(wishlist);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ItemCount.Should().Be(2);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenShareTokenNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetSharedWishlistQuery("non-existent-token");

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByShareTokenSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wishlist?)null);

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
        var wishlist = CreateSharedWishlist();
        var query = new GetSharedWishlistQuery(wishlist.ShareToken!);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByShareTokenSpec>(),
                token))
            .ReturnsAsync(wishlist);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _wishlistRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<WishlistByShareTokenSpec>(), token),
            Times.Once);
    }

    #endregion
}
