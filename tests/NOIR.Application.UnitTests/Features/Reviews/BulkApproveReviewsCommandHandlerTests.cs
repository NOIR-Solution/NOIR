namespace NOIR.Application.UnitTests.Features.Reviews;

/// <summary>
/// Unit tests for BulkApproveReviewsCommandHandler.
/// Tests bulk review approval scenarios with mocked dependencies.
/// </summary>
public class BulkApproveReviewsCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductReview, Guid>> _reviewRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IReviewAggregationService> _aggregationServiceMock;
    private readonly BulkApproveReviewsCommandHandler _handler;

    public BulkApproveReviewsCommandHandlerTests()
    {
        _reviewRepositoryMock = new Mock<IRepository<ProductReview, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _aggregationServiceMock = new Mock<IReviewAggregationService>();

        _handler = new BulkApproveReviewsCommandHandler(
            _reviewRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _aggregationServiceMock.Object);
    }

    private static ProductReview CreateTestReview(
        Guid? productId = null,
        string userId = "user-123",
        int rating = 4)
    {
        return ProductReview.Create(
            productId ?? Guid.NewGuid(),
            userId,
            rating,
            "Test Review",
            "This is a great product worth buying.",
            tenantId: "tenant-123");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenReviewsExist_ShouldApproveAllAndReturnCount()
    {
        // Arrange
        var reviews = new List<ProductReview>
        {
            CreateTestReview(userId: "user-1"),
            CreateTestReview(userId: "user-2"),
            CreateTestReview(userId: "user-3")
        };

        var reviewIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var command = new BulkApproveReviewsCommand(reviewIds);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);

        reviews.Should().AllSatisfy(r => r.Status.Should().Be(ReviewStatus.Approved));

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithReviewsFromDifferentProducts_ShouldRecalculateAllProductRatings()
    {
        // Arrange
        var product1Id = Guid.NewGuid();
        var product2Id = Guid.NewGuid();

        var reviews = new List<ProductReview>
        {
            CreateTestReview(productId: product1Id, userId: "user-1"),
            CreateTestReview(productId: product1Id, userId: "user-2"),
            CreateTestReview(productId: product2Id, userId: "user-3")
        };

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var command = new BulkApproveReviewsCommand(new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _aggregationServiceMock.Verify(
            x => x.RecalculateProductRatingAsync(product1Id, It.IsAny<CancellationToken>()),
            Times.Once);

        _aggregationServiceMock.Verify(
            x => x.RecalculateProductRatingAsync(product2Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSingleReview_ShouldSucceed()
    {
        // Arrange
        var review = CreateTestReview();
        var reviews = new List<ProductReview> { review };

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new BulkApproveReviewsCommand(new List<Guid> { Guid.NewGuid() });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
        review.Status.Should().Be(ReviewStatus.Approved);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenNoReviewsFound_ShouldReturnNotFound()
    {
        // Arrange
        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReview>());

        var command = new BulkApproveReviewsCommand(new List<Guid> { Guid.NewGuid() });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-REVIEW-003");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);

        _aggregationServiceMock.Verify(
            x => x.RecalculateProductRatingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToAllDependencies()
    {
        // Arrange
        var review = CreateTestReview();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsByIdsForUpdateSpec>(),
                token))
            .ReturnsAsync(new List<ProductReview> { review });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = new BulkApproveReviewsCommand(new List<Guid> { Guid.NewGuid() });

        // Act
        await _handler.Handle(command, token);

        // Assert
        _reviewRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ReviewsByIdsForUpdateSpec>(), token),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);

        _aggregationServiceMock.Verify(
            x => x.RecalculateProductRatingAsync(It.IsAny<Guid>(), token),
            Times.Once);
    }

    #endregion
}
