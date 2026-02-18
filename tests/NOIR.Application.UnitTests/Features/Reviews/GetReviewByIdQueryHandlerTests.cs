namespace NOIR.Application.UnitTests.Features.Reviews;

/// <summary>
/// Unit tests for GetReviewByIdQueryHandler.
/// Tests review retrieval by ID with mocked dependencies.
/// </summary>
public class GetReviewByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductReview, Guid>> _reviewRepositoryMock;
    private readonly GetReviewByIdQueryHandler _handler;

    public GetReviewByIdQueryHandlerTests()
    {
        _reviewRepositoryMock = new Mock<IRepository<ProductReview, Guid>>();

        _handler = new GetReviewByIdQueryHandler(_reviewRepositoryMock.Object);
    }

    private static ProductReview CreateTestReview(
        Guid? productId = null,
        string userId = "user-123",
        int rating = 4,
        string? title = "Great Product",
        string content = "This product is really excellent and works well.")
    {
        var review = ProductReview.Create(
            productId ?? Guid.NewGuid(),
            userId,
            rating,
            title,
            content,
            tenantId: "tenant-123");
        return review;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WhenReviewExists_ShouldReturnReviewDetailDto()
    {
        // Arrange
        var reviewId = Guid.NewGuid();
        var existingReview = CreateTestReview();

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReview);

        var query = new GetReviewByIdQuery(reviewId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Rating.Should().Be(4);
        result.Value.Title.Should().Be("Great Product");
        result.Value.Content.Should().Be("This product is really excellent and works well.");
        result.Value.Status.Should().Be(ReviewStatus.Pending);
        result.Value.HelpfulVotes.Should().Be(0);
        result.Value.NotHelpfulVotes.Should().Be(0);
        result.Value.AdminResponse.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithMinimalReview_ShouldReturnDto()
    {
        // Arrange
        var existingReview = CreateTestReview(title: null);

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingReview);

        var query = new GetReviewByIdQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().BeNull();
        result.Value.Content.Should().Be("This product is really excellent and works well.");
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
                It.IsAny<ReviewByIdSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductReview?)null);

        var query = new GetReviewByIdQuery(reviewId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-REVIEW-002");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var existingReview = CreateTestReview();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _reviewRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ReviewByIdSpec>(),
                token))
            .ReturnsAsync(existingReview);

        var query = new GetReviewByIdQuery(Guid.NewGuid());

        // Act
        await _handler.Handle(query, token);

        // Assert
        _reviewRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<ReviewByIdSpec>(), token),
            Times.Once);
    }

    #endregion
}
