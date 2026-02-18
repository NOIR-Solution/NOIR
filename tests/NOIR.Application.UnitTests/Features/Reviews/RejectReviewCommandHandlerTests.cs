namespace NOIR.Application.UnitTests.Features.Reviews;

/// <summary>
/// Unit tests for RejectReviewCommandHandler.
/// Tests review rejection scenarios with mocked dependencies.
/// </summary>
public class RejectReviewCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductReview, Guid>> _reviewRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IReviewAggregationService> _aggregationServiceMock;
    private readonly RejectReviewCommandHandler _handler;

    public RejectReviewCommandHandlerTests()
    {
        _reviewRepositoryMock = new Mock<IRepository<ProductReview, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _aggregationServiceMock = new Mock<IReviewAggregationService>();

        _handler = new RejectReviewCommandHandler(
            _reviewRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _aggregationServiceMock.Object);
    }

    private static ProductReview CreateTestReview(
        Guid? productId = null,
        string userId = "user-123",
        int rating = 4,
        string content = "This is a great product worth buying.")
    {
        return ProductReview.Create(
            productId ?? Guid.NewGuid(),
            userId,
            rating,
            "Test Review",
            content,
            tenantId: "tenant-123");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenReviewExists_ShouldRejectAndReturnDto()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var existingReview = CreateTestReview();

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReview);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RejectReviewCommand(reviewId, "Inappropriate content");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ReviewStatus.Rejected);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullReason_ShouldStillSucceed()
    {
        // Arrange
        var existingReview = CreateTestReview();

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReview);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RejectReviewCommand(Guid.NewGuid(), null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(ReviewStatus.Rejected);
    }

    [Fact]
    public async Task Handle_ShouldRecalculateProductRatingAfterRejection()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var existingReview = CreateTestReview(productId: productId);

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReview);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RejectReviewCommand(Guid.NewGuid(), "Spam");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _aggregationServiceMock.Verify(
            x => x.RecalculateProductRatingAsync(productId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenReviewNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var reviewId = Guid.NewGuid();

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview?)null);

        var command = new RejectReviewCommand(reviewId, "Reason");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-REVIEW-002");

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
        var existingReview = CreateTestReview();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewByIdForUpdateSpec>(),
                token))
            .ReturnsAsync(existingReview);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(token))
            .ReturnsAsync(1);

        var command = new RejectReviewCommand(Guid.NewGuid(), "Spam");

        // Act
        await _handler.Handle(command, token);

        // Assert
        _reviewRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ReviewByIdForUpdateSpec>(), token),
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
