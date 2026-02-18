namespace NOIR.Application.UnitTests.Features.Wishlists;

/// <summary>
/// Unit tests for DeleteWishlistCommandHandler.
/// Tests wishlist deletion scenarios with mocked dependencies.
/// </summary>
public class DeleteWishlistCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Wishlist, Guid>> _wishlistRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly DeleteWishlistCommandHandler _handler;

    public DeleteWishlistCommandHandlerTests()
    {
        _wishlistRepositoryMock = new Mock<IRepository<Wishlist, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new DeleteWishlistCommandHandler(
            _wishlistRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static Wishlist CreateTestWishlist(
        string userId = "user-123",
        string name = "Test Wishlist",
        bool isDefault = false)
    {
        return Wishlist.Create(userId, name, isDefault: isDefault);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenWishlistExistsAndIsNotDefault_ShouldSucceed()
    {
        // Arrange
        var wishlistId = Guid.NewGuid();
        var existingWishlist = CreateTestWishlist(isDefault: false);
        var command = new DeleteWishlistCommand(wishlistId) { UserId = "user-123" };

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWishlist);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _wishlistRepositoryMock.Verify(
            x => x.Remove(existingWishlist),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnWishlistDtoBeforeDeletion()
    {
        // Arrange
        var existingWishlist = CreateTestWishlist(name: "To Delete", isDefault: false);
        var command = new DeleteWishlistCommand(Guid.NewGuid()) { UserId = "user-123" };

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWishlist);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("To Delete");
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenWishlistNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = new DeleteWishlistCommand(Guid.NewGuid()) { UserId = "user-123" };

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wishlist?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();

        _wishlistRepositoryMock.Verify(
            x => x.Remove(It.IsAny<Wishlist>()),
            Times.Never);

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
        var existingWishlist = CreateTestWishlist(userId: "other-user", isDefault: false);
        var command = new DeleteWishlistCommand(Guid.NewGuid()) { UserId = "user-123" };

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWishlist);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();

        _wishlistRepositoryMock.Verify(
            x => x.Remove(It.IsAny<Wishlist>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Business Rule Scenarios

    [Fact]
    public async Task Handle_WhenWishlistIsDefault_ShouldReturnFailure()
    {
        // Arrange
        var existingWishlist = CreateTestWishlist(isDefault: true);
        var command = new DeleteWishlistCommand(Guid.NewGuid()) { UserId = "user-123" };

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWishlist);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();

        _wishlistRepositoryMock.Verify(
            x => x.Remove(It.IsAny<Wishlist>()),
            Times.Never);

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
        var existingWishlist = CreateTestWishlist(isDefault: false);
        var command = new DeleteWishlistCommand(Guid.NewGuid()) { UserId = "user-123" };
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByIdSpec>(),
                token))
            .ReturnsAsync(existingWishlist);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _wishlistRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<WishlistByIdSpec>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}
