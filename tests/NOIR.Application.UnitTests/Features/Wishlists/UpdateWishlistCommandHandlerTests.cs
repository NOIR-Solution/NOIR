namespace NOIR.Application.UnitTests.Features.Wishlists;

/// <summary>
/// Unit tests for UpdateWishlistCommandHandler.
/// Tests wishlist update scenarios with mocked dependencies.
/// </summary>
public class UpdateWishlistCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Wishlist, Guid>> _wishlistRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UpdateWishlistCommandHandler _handler;

    public UpdateWishlistCommandHandlerTests()
    {
        _wishlistRepositoryMock = new Mock<IRepository<Wishlist, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new UpdateWishlistCommandHandler(
            _wishlistRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static UpdateWishlistCommand CreateValidCommand(
        Guid? id = null,
        string name = "Updated Wishlist",
        bool isPublic = false,
        string? userId = "user-123")
    {
        return new UpdateWishlistCommand(id ?? Guid.NewGuid(), name, isPublic) { UserId = userId };
    }

    private static Wishlist CreateTestWishlist(string userId = "user-123", string name = "Test Wishlist")
    {
        return Wishlist.Create(userId, name);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var wishlistId = Guid.NewGuid();
        var existingWishlist = CreateTestWishlist();
        var command = CreateValidCommand(id: wishlistId, name: "Updated Name");

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
        result.Value.Name.Should().Be("Updated Name");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithIsPublicTrue_ShouldUpdatePublicStatus()
    {
        // Arrange
        var existingWishlist = CreateTestWishlist();
        var command = CreateValidCommand(isPublic: true);

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWishlist);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingWishlist.IsPublic.Should().BeTrue();
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenWishlistNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = CreateValidCommand();

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByIdSpec>(),
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
        var existingWishlist = CreateTestWishlist(userId: "other-user");
        var command = CreateValidCommand(userId: "user-123");

        _wishlistRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<WishlistByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingWishlist);

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
        var existingWishlist = CreateTestWishlist();
        var command = CreateValidCommand();
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
