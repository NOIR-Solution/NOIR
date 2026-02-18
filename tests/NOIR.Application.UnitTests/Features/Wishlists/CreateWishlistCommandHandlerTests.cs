namespace NOIR.Application.UnitTests.Features.Wishlists;

/// <summary>
/// Unit tests for CreateWishlistCommandHandler.
/// Tests wishlist creation scenarios with mocked dependencies.
/// </summary>
public class CreateWishlistCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Wishlist, Guid>> _wishlistRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly CreateWishlistCommandHandler _handler;

    public CreateWishlistCommandHandlerTests()
    {
        _wishlistRepositoryMock = new Mock<IRepository<Wishlist, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new CreateWishlistCommandHandler(
            _wishlistRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    private static CreateWishlistCommand CreateValidCommand(
        string name = "My Wishlist",
        bool isPublic = false,
        string? userId = "user-123")
    {
        return new CreateWishlistCommand(name, isPublic) { UserId = userId };
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var command = CreateValidCommand();

        _wishlistRepositoryMock
            .Setup(x => x.AnyAsync(
                It.IsAny<DefaultWishlistByUserSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _wishlistRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Wishlist>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Wishlist w, CancellationToken _) => w);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("My Wishlist");

        _wishlistRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Wishlist>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoDefaultExists_ShouldCreateAsDefault()
    {
        // Arrange
        var command = CreateValidCommand();

        _wishlistRepositoryMock
            .Setup(x => x.AnyAsync(
                It.IsAny<DefaultWishlistByUserSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Wishlist? capturedWishlist = null;
        _wishlistRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Wishlist>(), It.IsAny<CancellationToken>()))
            .Callback<Wishlist, CancellationToken>((w, _) => capturedWishlist = w)
            .ReturnsAsync((Wishlist w, CancellationToken _) => w);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedWishlist.Should().NotBeNull();
        capturedWishlist!.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenDefaultAlreadyExists_ShouldCreateAsNonDefault()
    {
        // Arrange
        var command = CreateValidCommand();

        _wishlistRepositoryMock
            .Setup(x => x.AnyAsync(
                It.IsAny<DefaultWishlistByUserSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Wishlist? capturedWishlist = null;
        _wishlistRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Wishlist>(), It.IsAny<CancellationToken>()))
            .Callback<Wishlist, CancellationToken>((w, _) => capturedWishlist = w)
            .ReturnsAsync((Wishlist w, CancellationToken _) => w);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedWishlist.Should().NotBeNull();
        capturedWishlist!.IsDefault.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithIsPublicTrue_ShouldSetPublic()
    {
        // Arrange
        var command = CreateValidCommand(isPublic: true);

        _wishlistRepositoryMock
            .Setup(x => x.AnyAsync(
                It.IsAny<DefaultWishlistByUserSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Wishlist? capturedWishlist = null;
        _wishlistRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Wishlist>(), It.IsAny<CancellationToken>()))
            .Callback<Wishlist, CancellationToken>((w, _) => capturedWishlist = w)
            .ReturnsAsync((Wishlist w, CancellationToken _) => w);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedWishlist.Should().NotBeNull();
        capturedWishlist!.IsPublic.Should().BeTrue();
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

        _wishlistRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Wishlist>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepositories()
    {
        // Arrange
        var command = CreateValidCommand();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _wishlistRepositoryMock
            .Setup(x => x.AnyAsync(
                It.IsAny<DefaultWishlistByUserSpec>(),
                token))
            .ReturnsAsync(false);

        _wishlistRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Wishlist>(), token))
            .ReturnsAsync((Wishlist w, CancellationToken _) => w);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _wishlistRepositoryMock.Verify(
            x => x.AnyAsync(It.IsAny<DefaultWishlistByUserSpec>(), token),
            Times.Once);

        _wishlistRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Wishlist>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}
