namespace NOIR.Application.UnitTests.Features.Reviews;

/// <summary>
/// Unit tests for GetReviewStatsQueryHandler.
/// Tests review statistics aggregation with mocked dependencies.
/// </summary>
public class GetReviewStatsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductReview, Guid>> _reviewRepositoryMock;
    private readonly GetReviewStatsQueryHandler _handler;

    public GetReviewStatsQueryHandlerTests()
    {
        _reviewRepositoryMock = new Mock<IRepository<ProductReview, Guid>>();

        _handler = new GetReviewStatsQueryHandler(_reviewRepositoryMock.Object);
    }

    private static ProductReview CreateTestReview(
        Guid productId,
        int rating,
        string userId = "user-123")
    {
        return ProductReview.Create(
            productId,
            userId,
            rating,
            "Test Review",
            "This is a great product worth buying.",
            tenantId: "tenant-123");
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithReviews_ShouldCalculateAverageRating()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var reviews = new List<ProductReview>
        {
            CreateTestReview(productId, 5, "user-1"),
            CreateTestReview(productId, 4, "user-2"),
            CreateTestReview(productId, 3, "user-3"),
            CreateTestReview(productId, 4, "user-4"),
            CreateTestReview(productId, 5, "user-5")
        };

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewStatsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        var query = new GetReviewStatsQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReviews.Should().Be(5);
        // Average: (5+4+3+4+5) / 5 = 21/5 = 4.2
        result.Value.AverageRating.Should().Be(4.2m);
    }

    [Fact]
    public async Task Handle_WithReviews_ShouldCalculateRatingDistribution()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var reviews = new List<ProductReview>
        {
            CreateTestReview(productId, 5, "user-1"),
            CreateTestReview(productId, 5, "user-2"),
            CreateTestReview(productId, 4, "user-3"),
            CreateTestReview(productId, 3, "user-4"),
            CreateTestReview(productId, 1, "user-5")
        };

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewStatsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        var query = new GetReviewStatsQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RatingDistribution[1].Should().Be(1);
        result.Value.RatingDistribution[2].Should().Be(0);
        result.Value.RatingDistribution[3].Should().Be(1);
        result.Value.RatingDistribution[4].Should().Be(1);
        result.Value.RatingDistribution[5].Should().Be(2);
    }

    [Fact]
    public async Task Handle_WithNoReviews_ShouldReturnZeroStats()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewStatsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReview>());

        var query = new GetReviewStatsQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReviews.Should().Be(0);
        result.Value.AverageRating.Should().Be(0m);
        result.Value.RatingDistribution[1].Should().Be(0);
        result.Value.RatingDistribution[2].Should().Be(0);
        result.Value.RatingDistribution[3].Should().Be(0);
        result.Value.RatingDistribution[4].Should().Be(0);
        result.Value.RatingDistribution[5].Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithSingleReview_ShouldReturnCorrectStats()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var reviews = new List<ProductReview>
        {
            CreateTestReview(productId, 3, "user-1")
        };

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewStatsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        var query = new GetReviewStatsQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalReviews.Should().Be(1);
        result.Value.AverageRating.Should().Be(3.0m);
        result.Value.RatingDistribution[3].Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldRoundAverageRatingToOneDecimal()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var reviews = new List<ProductReview>
        {
            CreateTestReview(productId, 5, "user-1"),
            CreateTestReview(productId, 4, "user-2"),
            CreateTestReview(productId, 4, "user-3")
        };

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewStatsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        var query = new GetReviewStatsQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        // Average: (5+4+4) / 3 = 13/3 = 4.333... -> rounds to 4.3
        result.Value.AverageRating.Should().Be(4.3m);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewStatsSpec>(),
                token))
            .ReturnsAsync(new List<ProductReview>());

        var query = new GetReviewStatsQuery(productId);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _reviewRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ReviewStatsSpec>(), token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_RatingDistributionShouldAlwaysHaveAllFiveKeys()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewStatsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReview>());

        var query = new GetReviewStatsQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.RatingDistribution.Should().HaveCount(5);
        result.Value.RatingDistribution.Should().ContainKeys(1, 2, 3, 4, 5);
    }

    #endregion
}
