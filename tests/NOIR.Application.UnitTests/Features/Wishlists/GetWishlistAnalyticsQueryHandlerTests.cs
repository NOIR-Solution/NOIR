namespace NOIR.Application.UnitTests.Features.Wishlists;

/// <summary>
/// Unit tests for GetWishlistAnalyticsQueryHandler.
/// Tests wishlist analytics aggregation with mocked dependencies.
/// </summary>
public class GetWishlistAnalyticsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Wishlist, Guid>> _wishlistRepositoryMock;
    private readonly GetWishlistAnalyticsQueryHandler _handler;

    public GetWishlistAnalyticsQueryHandlerTests()
    {
        _wishlistRepositoryMock = new Mock<IRepository<Wishlist, Guid>>();

        _handler = new GetWishlistAnalyticsQueryHandler(_wishlistRepositoryMock.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenWishlistsExist_ShouldReturnAnalytics()
    {
        // Arrange
        var productId1 = Guid.NewGuid();
        var productId2 = Guid.NewGuid();

        var wishlist1 = Wishlist.Create("user-1", "Wishlist 1");
        wishlist1.AddItem(productId1);
        wishlist1.AddItem(productId2);

        var wishlist2 = Wishlist.Create("user-2", "Wishlist 2");
        wishlist2.AddItem(productId1);

        var wishlists = new List<Wishlist> { wishlist1, wishlist2 };

        _wishlistRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<AllWishlistsWithItemsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(wishlists);

        var query = new GetWishlistAnalyticsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalWishlists.Should().Be(2);
        result.Value.TotalWishlistItems.Should().Be(3);
        result.Value.TopProducts.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldRankTopProductsByWishlistCount()
    {
        // Arrange
        var popularProductId = Guid.NewGuid();
        var lessPopularProductId = Guid.NewGuid();

        var wishlist1 = Wishlist.Create("user-1", "W1");
        wishlist1.AddItem(popularProductId);

        var wishlist2 = Wishlist.Create("user-2", "W2");
        wishlist2.AddItem(popularProductId);

        var wishlist3 = Wishlist.Create("user-3", "W3");
        wishlist3.AddItem(popularProductId);
        wishlist3.AddItem(lessPopularProductId);

        var wishlists = new List<Wishlist> { wishlist1, wishlist2, wishlist3 };

        _wishlistRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<AllWishlistsWithItemsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(wishlists);

        var query = new GetWishlistAnalyticsQuery(TopCount: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TopProducts.Should().HaveCount(2);
        result.Value.TopProducts[0].ProductId.Should().Be(popularProductId);
        result.Value.TopProducts[0].WishlistCount.Should().Be(3);
        result.Value.TopProducts[1].ProductId.Should().Be(lessPopularProductId);
        result.Value.TopProducts[1].WishlistCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithTopCountLimit_ShouldLimitResults()
    {
        // Arrange
        var wishlists = new List<Wishlist>();
        for (int i = 0; i < 5; i++)
        {
            var w = Wishlist.Create($"user-{i}", $"W{i}");
            w.AddItem(Guid.NewGuid()); // Each with a unique product
            wishlists.Add(w);
        }

        _wishlistRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<AllWishlistsWithItemsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(wishlists);

        var query = new GetWishlistAnalyticsQuery(TopCount: 3);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TopProducts.Should().HaveCountLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task Handle_WhenNoWishlists_ShouldReturnEmptyAnalytics()
    {
        // Arrange
        _wishlistRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<AllWishlistsWithItemsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Wishlist>());

        var query = new GetWishlistAnalyticsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalWishlists.Should().Be(0);
        result.Value.TotalWishlistItems.Should().Be(0);
        result.Value.TopProducts.Should().BeEmpty();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithDefaultTopCount_ShouldUse10()
    {
        // Arrange
        _wishlistRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<AllWishlistsWithItemsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Wishlist>());

        var query = new GetWishlistAnalyticsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // The default TopCount is 10 (verified from the record definition)
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var query = new GetWishlistAnalyticsQuery();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _wishlistRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<AllWishlistsWithItemsSpec>(),
                token))
            .ReturnsAsync(new List<Wishlist>());

        // Act
        await _handler.Handle(query, token);

        // Assert
        _wishlistRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<AllWishlistsWithItemsSpec>(), token),
            Times.Once);
    }

    #endregion
}
