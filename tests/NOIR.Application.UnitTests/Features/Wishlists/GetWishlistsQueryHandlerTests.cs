namespace NOIR.Application.UnitTests.Features.Wishlists;

/// <summary>
/// Unit tests for GetWishlistsQueryHandler.
/// Tests retrieving all wishlists for a user with mocked dependencies.
/// </summary>
public class GetWishlistsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Wishlist, Guid>> _wishlistRepositoryMock;
    private readonly GetWishlistsQueryHandler _handler;

    public GetWishlistsQueryHandlerTests()
    {
        _wishlistRepositoryMock = new Mock<IRepository<Wishlist, Guid>>();

        _handler = new GetWishlistsQueryHandler(_wishlistRepositoryMock.Object);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenUserHasWishlists_ShouldReturnListOfDtos()
    {
        // Arrange
        var query = new GetWishlistsQuery { UserId = "user-123" };
        var wishlists = new List<Wishlist>
        {
            Wishlist.Create("user-123", "Default Wishlist", isDefault: true),
            Wishlist.Create("user-123", "Holiday Gifts")
        };

        _wishlistRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<WishlistsByUserSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(wishlists);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value[0].Name.Should().Be("Default Wishlist");
        result.Value[1].Name.Should().Be("Holiday Gifts");
    }

    [Fact]
    public async Task Handle_WhenUserHasNoWishlists_ShouldReturnEmptyList()
    {
        // Arrange
        var query = new GetWishlistsQuery { UserId = "user-123" };

        _wishlistRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<WishlistsByUserSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Wishlist>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region Failure Scenarios

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Handle_WhenUserIdIsNullOrEmpty_ShouldReturnFailure(string? userId)
    {
        // Arrange
        var query = new GetWishlistsQuery { UserId = userId };

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
        var query = new GetWishlistsQuery { UserId = "user-123" };
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _wishlistRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<WishlistsByUserSpec>(),
                token))
            .ReturnsAsync(new List<Wishlist>());

        // Act
        await _handler.Handle(query, token);

        // Assert
        _wishlistRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<WishlistsByUserSpec>(), token),
            Times.Once);
    }

    #endregion
}
