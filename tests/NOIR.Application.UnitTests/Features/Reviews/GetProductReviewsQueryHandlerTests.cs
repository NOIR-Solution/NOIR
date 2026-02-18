namespace NOIR.Application.UnitTests.Features.Reviews;

/// <summary>
/// Unit tests for GetProductReviewsQueryHandler.
/// Tests public product review retrieval (approved only) with mocked dependencies.
/// </summary>
public class GetProductReviewsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<ProductReview, Guid>> _reviewRepositoryMock;
    private readonly GetProductReviewsQueryHandler _handler;

    public GetProductReviewsQueryHandlerTests()
    {
        _reviewRepositoryMock = new Mock<IRepository<ProductReview, Guid>>();

        _handler = new GetProductReviewsQueryHandler(_reviewRepositoryMock.Object);
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

    private static List<ProductReview> CreateTestReviews(int count, Guid? productId = null)
    {
        var pid = productId ?? Guid.NewGuid();
        return Enumerable.Range(1, count)
            .Select(i => CreateTestReview(productId: pid, userId: $"user-{i}", rating: (i % 5) + 1))
            .ToList();
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDefaultPaging_ShouldReturnPagedResult()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var reviews = CreateTestReviews(5, productId);

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsByProductSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reviews);

        _reviewRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ReviewsByProductCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var query = new GetProductReviewsQuery(productId, Page: 1, PageSize: 20);

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
        var productId = Guid.NewGuid();
        var page2Reviews = CreateTestReviews(10, productId);

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsByProductSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(page2Reviews);

        _reviewRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ReviewsByProductCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        var query = new GetProductReviewsQuery(productId, Page: 2, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(10);
        result.Value.TotalCount.Should().Be(25);
        result.Value.PageNumber.Should().Be(2);
        result.Value.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsByProductSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReview>());

        _reviewRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ReviewsByProductCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetProductReviewsQuery(productId, Page: 1, PageSize: 20);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithSortParameter_ShouldPassToSpecification()
    {
        // Arrange
        var productId = Guid.NewGuid();

        _reviewRepositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ReviewsByProductSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductReview>());

        _reviewRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ReviewsByProductCountSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetProductReviewsQuery(productId, Sort: "highest", Page: 1, PageSize: 20);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _reviewRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ReviewsByProductSpec>(), It.IsAny<CancellationToken>()),
            Times.Once);
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
                It.IsAny<ReviewsByProductSpec>(),
                token))
            .ReturnsAsync(new List<ProductReview>());

        _reviewRepositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ReviewsByProductCountSpec>(),
                token))
            .ReturnsAsync(0);

        var query = new GetProductReviewsQuery(productId, Page: 1, PageSize: 20);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _reviewRepositoryMock.Verify(
            x => x.ListAsync(It.IsAny<ReviewsByProductSpec>(), token),
            Times.Once);

        _reviewRepositoryMock.Verify(
            x => x.CountAsync(It.IsAny<ReviewsByProductCountSpec>(), token),
            Times.Once);
    }

    #endregion
}
