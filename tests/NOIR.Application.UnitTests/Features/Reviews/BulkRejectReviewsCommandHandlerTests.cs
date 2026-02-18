namespace NOIR.Application.UnitTests.Features.Reviews;

/// <summary>
/// Unit tests for BulkRejectReviewsCommandHandler.
/// Tests bulk review rejection scenarios with mocked dependencies.
/// </summary>
public class BulkRejectReviewsCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductReview, Guid>> _reviewRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IReviewAggregationService> _aggregationServiceMock;
    private readonly BulkRejectReviewsCommandHandler _handler;

    public BulkRejectReviewsCommandHandlerTests()
    {
        _reviewRepositoryMock = new Mock<IRepository<ProductReview, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _aggregationServiceMock = new Mock<IReviewAggregationService>();

        _handler = new BulkRejectReviewsCommandHandler(
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
    public async Task Handle_WhenReviewsExist_ShouldRejectAllAndReturnCount()
    {
        // Arrange
        var reviews = new List<ProductReview>
        {
            CreateTestReview(userId: "user-1"),
            CreateTestReview(userId: "user-2"),
            CreateTestReview(userId: "user-3")
        };

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var command = new BulkRejectReviewsCommand(
            new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() },
            "Spam content");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(3);

        reviews.Should().AllSatisfy(r => r.Status.Should().Be(ReviewStatus.Rejected));

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullReason_ShouldStillSucceed()
    {
        // Arrange
        var review = CreateTestReview();

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReview> { review });

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new BulkRejectReviewsCommand(new List<Guid> { Guid.NewGuid() }, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(1);
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
            CreateTestReview(productId: product2Id, userId: "user-2")
        };

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsByIdsForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        var command = new BulkRejectReviewsCommand(
            new List<Guid> { Guid.NewGuid(), Guid.NewGuid() },
            "Violation");

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

        var command = new BulkRejectReviewsCommand(new List<Guid> { Guid.NewGuid() }, "Spam");

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

        var command = new BulkRejectReviewsCommand(new List<Guid> { Guid.NewGuid() }, "Reason");

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
