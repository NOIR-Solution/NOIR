using NOIR.Application.Features.Orders.Specifications;

namespace NOIR.Application.UnitTests.Features.Reviews;

/// <summary>
/// Unit tests for CreateReviewCommandHandler.
/// Tests review creation scenarios with mocked dependencies.
/// </summary>
public class CreateReviewCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductReview, Guid>> _reviewRepositoryMock;
    private readonly Mock<IRepository<Order, Guid>> _orderRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly CreateReviewCommandHandler _handler;

    public CreateReviewCommandHandlerTests()
    {
        _reviewRepositoryMock = new Mock<IRepository<ProductReview, Guid>>();
        _orderRepositoryMock = new Mock<IRepository<Order, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        _currentUserMock.Setup(x => x.UserId).Returns("user-123");
        _currentUserMock.Setup(x => x.TenantId).Returns("tenant-123");

        _handler = new CreateReviewCommandHandler(
            _reviewRepositoryMock.Object,
            _orderRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static CreateReviewCommand CreateValidCommand(
        Guid? productId = null,
        int rating = 4,
        string? title = "Great product",
        string content = "This product is really excellent and works well.",
        Guid? orderId = null,
        List<string>? mediaUrls = null,
        string? userId = null)
    {
        return new CreateReviewCommand(
            productId ?? Guid.NewGuid(),
            rating,
            title,
            content,
            orderId,
            mediaUrls)
        {
            UserId = userId
        };
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceed()
    {
        // Arrange
        var command = CreateValidCommand();

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview?)null);

        _reviewRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductReview>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview review, CancellationToken _) => review);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rating.Should().Be(4);
        result.Value.Title.Should().Be("Great product");
        result.Value.Content.Should().Be("This product is really excellent and works well.");

        _reviewRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ProductReview>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMediaUrls_ShouldAddMediaToReview()
    {
        // Arrange
        var mediaUrls = new List<string>
        {
            "https://cdn.test.com/image1.jpg",
            "https://cdn.test.com/image2.jpg"
        };
        var command = CreateValidCommand(mediaUrls: mediaUrls);

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview?)null);

        ProductReview? capturedReview = null;
        _reviewRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductReview>(), It.IsAny<CancellationToken>()))
            .Callback<ProductReview, CancellationToken>((review, _) => capturedReview = review)
            .ReturnsAsync((ProductReview review, CancellationToken _) => review);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedReview.Should().NotBeNull();
        capturedReview!.Media.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithoutUserId_ShouldUseCurrentUserUserId()
    {
        // Arrange
        var command = CreateValidCommand(userId: null);

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview?)null);

        ProductReview? capturedReview = null;
        _reviewRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductReview>(), It.IsAny<CancellationToken>()))
            .Callback<ProductReview, CancellationToken>((review, _) => capturedReview = review)
            .ReturnsAsync((ProductReview review, CancellationToken _) => review);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedReview.Should().NotBeNull();
        capturedReview!.UserId.Should().Be("user-123");
    }

    [Fact]
    public async Task Handle_WithExplicitUserId_ShouldUseProvidedUserId()
    {
        // Arrange
        var command = CreateValidCommand(userId: "explicit-user");

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview?)null);

        ProductReview? capturedReview = null;
        _reviewRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductReview>(), It.IsAny<CancellationToken>()))
            .Callback<ProductReview, CancellationToken>((review, _) => capturedReview = review)
            .ReturnsAsync((ProductReview review, CancellationToken _) => review);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedReview.Should().NotBeNull();
        capturedReview!.UserId.Should().Be("explicit-user");
    }

    [Fact]
    public async Task Handle_WithNullMediaUrls_ShouldNotAddMedia()
    {
        // Arrange
        var command = CreateValidCommand(mediaUrls: null);

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview?)null);

        ProductReview? capturedReview = null;
        _reviewRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductReview>(), It.IsAny<CancellationToken>()))
            .Callback<ProductReview, CancellationToken>((review, _) => capturedReview = review)
            .ReturnsAsync((ProductReview review, CancellationToken _) => review);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedReview.Should().NotBeNull();
        capturedReview!.Media.Should().BeEmpty();
    }

    #endregion

    #region Unauthorized Scenarios

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        _currentUserMock.Setup(x => x.UserId).Returns((string?)null);
        var command = CreateValidCommand(userId: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();

        _reviewRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ProductReview>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenUserAlreadyReviewedProduct_ShouldReturnConflict()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = CreateValidCommand(productId: productId);

        var existingReview = ProductReview.Create(
            productId, "user-123", 3, "Existing", "This review already exists for this product.", tenantId: "tenant-123");

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReview);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-REVIEW-001");

        _reviewRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ProductReview>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Verified Purchase Scenarios

    [Fact]
    public async Task Handle_WithoutOrderId_ShouldSetIsVerifiedPurchaseFalse()
    {
        // Arrange
        var command = CreateValidCommand(orderId: null);

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview?)null);

        ProductReview? capturedReview = null;
        _reviewRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductReview>(), It.IsAny<CancellationToken>()))
            .Callback<ProductReview, CancellationToken>((review, _) => capturedReview = review)
            .ReturnsAsync((ProductReview review, CancellationToken _) => review);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedReview.Should().NotBeNull();
        capturedReview!.IsVerifiedPurchase.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithOrderIdButOrderNotFound_ShouldSetIsVerifiedPurchaseFalse()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = CreateValidCommand(orderId: orderId);

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview?)null);

        _orderRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<OrderByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        ProductReview? capturedReview = null;
        _reviewRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductReview>(), It.IsAny<CancellationToken>()))
            .Callback<ProductReview, CancellationToken>((review, _) => capturedReview = review)
            .ReturnsAsync((ProductReview review, CancellationToken _) => review);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedReview.Should().NotBeNull();
        capturedReview!.IsVerifiedPurchase.Should().BeFalse();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_ShouldUseTenantIdFromCurrentUser()
    {
        // Arrange
        const string tenantId = "tenant-abc";
        _currentUserMock.Setup(x => x.TenantId).Returns(tenantId);

        var command = CreateValidCommand();

        ProductReview? capturedReview = null;

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview?)null);

        _reviewRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductReview>(), It.IsAny<CancellationToken>()))
            .Callback<ProductReview, CancellationToken>((review, _) => capturedReview = review)
            .ReturnsAsync((ProductReview review, CancellationToken _) => review);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedReview.Should().NotBeNull();
        capturedReview!.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var command = CreateValidCommand();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewExistsSpec>(),
                token))
            .ReturnsAsync((ProductReview?)null);

        _reviewRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<ProductReview>(), token))
            .ReturnsAsync((ProductReview review, CancellationToken _) => review);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        // Act
        await _handler.Handle(command, token);

        // Assert
        _reviewRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ReviewExistsSpec>(), token),
            Times.Once);

        _reviewRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<ProductReview>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    #endregion
}
