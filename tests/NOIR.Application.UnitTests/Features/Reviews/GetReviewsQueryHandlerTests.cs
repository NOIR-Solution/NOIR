namespace NOIR.Application.UnitTests.Features.Reviews;

/// <summary>
/// Unit tests for GetReviewsQueryHandler.
/// Tests paged review list retrieval (admin moderation queue) with mocked dependencies.
/// </summary>
public class GetReviewsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductReview, Guid>> _reviewRepositoryMock;
    private readonly GetReviewsQueryHandler _handler;

    public GetReviewsQueryHandlerTests()
    {
        _reviewRepositoryMock = new Mock<IRepository<ProductReview, Guid>>();

        _handler = new GetReviewsQueryHandler(_reviewRepositoryMock.Object);
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

    private static List<ProductReview> CreateTestReviews(int count)
    {
        return Enumerable.Range(1, count)
            .Select(i => CreateTestReview(userId: $"user-{i}", rating: (i % 5) + 1))
            .ToList();
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDefaultPaging_ShouldReturnPagedResult()
    {
        // Arrange
        var reviews = CreateTestReviews(5);

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsModerationListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        _reviewRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ReviewsModerationCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var query = new GetReviewsQuery(Page: 1, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(5);
        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_WithPaging_ShouldReturnCorrectPage()
    {
        // Arrange
        var page2Reviews = CreateTestReviews(10);

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsModerationListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page2Reviews);

        _reviewRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ReviewsModerationCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        var query = new GetReviewsQuery(Page: 2, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
        result.Value.TotalCount.Should().Be(25);
        result.Value.PageNumber.Should().Be(2);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsModerationListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReview>());

        _reviewRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ReviewsModerationCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetReviewsQuery(Page: 1, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldMapReviewsToDto()
    {
        // Arrange
        var review = CreateTestReview(rating: 5);

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsModerationListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReview> { review });

        _reviewRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ReviewsModerationCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var query = new GetReviewsQuery(Page: 1, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var item = result.Value.Items.First();
        item.Rating.Should().Be(5);
        item.Title.Should().Be("Test Review");
        item.Status.Should().Be(ReviewStatus.Pending);
    }

    #endregion

    #region Filter Scenarios

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldPassToSpecification()
    {
        // Arrange
        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsModerationListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReview>());

        _reviewRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ReviewsModerationCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetReviewsQuery(
            Page: 1,
            PageSize: 20,
            Status: ReviewStatus.Pending);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _reviewRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ReviewsModerationListSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _reviewRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<ReviewsModerationCountSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithProductIdFilter_ShouldPassToSpecification()
    {
        // Arrange
        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsModerationListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReview>());

        _reviewRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ReviewsModerationCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetReviewsQuery(
            Page: 1,
            PageSize: 20,
            ProductId: Guid.NewGuid());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _reviewRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ReviewsModerationListSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithRatingFilter_ShouldPassToSpecification()
    {
        // Arrange
        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsModerationListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReview>());

        _reviewRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ReviewsModerationCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetReviewsQuery(
            Page: 1,
            PageSize: 20,
            Rating: 5);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _reviewRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ReviewsModerationListSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldPassToSpecification()
    {
        // Arrange
        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsModerationListSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReview>());

        _reviewRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ReviewsModerationCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetReviewsQuery(
            Page: 1,
            PageSize: 20,
            Search: "great product");

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _reviewRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ReviewsModerationListSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsModerationListSpec>(),
                token))
            .ReturnsAsync(new List<ProductReview>());

        _reviewRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ReviewsModerationCountSpec>(),
                token))
            .ReturnsAsync(0);

        var query = new GetReviewsQuery(Page: 1, PageSize: 20);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _reviewRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ReviewsModerationListSpec>(), token),
            Times.Once);

        _reviewRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<ReviewsModerationCountSpec>(), token),
            Times.Once);
    }

    #endregion
}
