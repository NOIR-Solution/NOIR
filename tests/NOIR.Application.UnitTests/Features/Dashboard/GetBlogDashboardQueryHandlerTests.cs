using NOIR.Application.Features.Dashboard;
using NOIR.Application.Features.Dashboard.DTOs;
using NOIR.Application.Features.Dashboard.Queries.GetBlogDashboard;

namespace NOIR.Application.UnitTests.Features.Dashboard;

/// <summary>
/// Unit tests for GetBlogDashboardQueryHandler.
/// Tests blog dashboard aggregation with mocked IBlogDashboardQueryService.
/// </summary>
public class GetBlogDashboardQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IBlogDashboardQueryService> _blogDashboardServiceMock;
    private readonly GetBlogDashboardQueryHandler _handler;

    public GetBlogDashboardQueryHandlerTests()
    {
        _blogDashboardServiceMock = new Mock<IBlogDashboardQueryService>();
        _handler = new GetBlogDashboardQueryHandler(_blogDashboardServiceMock.Object);
    }

    private static BlogDashboardDto CreateTestBlogDashboard()
    {
        var topPosts = new List<TopPostDto>
        {
            new(Guid.NewGuid(), "Getting Started Guide", "/images/post1.jpg", 1500),
            new(Guid.NewGuid(), "Advanced Tips", null, 800),
        }.AsReadOnly();

        var publishingTrend = Enumerable.Range(0, 7)
            .Select(i => new PublishingTrendDto(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)),
                2 + i % 3))
            .ToList()
            .AsReadOnly();

        return new BlogDashboardDto(
            TotalPosts: 100,
            PublishedPosts: 75,
            DraftPosts: 20,
            ArchivedPosts: 5,
            PendingComments: 8,
            TopPosts: topPosts,
            PublishingTrend: publishingTrend);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithDefaultParameters_ShouldReturnDashboardSuccessfully()
    {
        // Arrange
        var query = new GetBlogDashboardQuery();
        var expected = CreateTestBlogDashboard();

        _blogDashboardServiceMock
            .Setup(x => x.GetBlogDashboardAsync(30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.ShouldBe(expected);
        result.Value.TotalPosts.ShouldBe(100);
        result.Value.PublishedPosts.ShouldBe(75);
        result.Value.DraftPosts.ShouldBe(20);
    }

    [Fact]
    public async Task Handle_ShouldReturnTopPosts()
    {
        // Arrange
        var query = new GetBlogDashboardQuery();
        var expected = CreateTestBlogDashboard();

        _blogDashboardServiceMock
            .Setup(x => x.GetBlogDashboardAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.TopPosts.Count().ShouldBe(2);
        result.Value.TopPosts[0].Title.ShouldBe("Getting Started Guide");
        result.Value.TopPosts[0].ViewCount.ShouldBe(1500);
    }

    [Fact]
    public async Task Handle_ShouldReturnPublishingTrend()
    {
        // Arrange
        var query = new GetBlogDashboardQuery();
        var expected = CreateTestBlogDashboard();

        _blogDashboardServiceMock
            .Setup(x => x.GetBlogDashboardAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBe(true);
        result.Value.PublishingTrend.Count().ShouldBe(7);
        result.Value.PublishingTrend.ShouldAllBe(t => t.PostCount > 0);
    }

    #endregion

    #region Parameter Forwarding

    [Fact]
    public async Task Handle_WithCustomTrendDays_ShouldPassToService()
    {
        // Arrange
        var query = new GetBlogDashboardQuery(TrendDays: 60);
        var expected = CreateTestBlogDashboard();

        _blogDashboardServiceMock
            .Setup(x => x.GetBlogDashboardAsync(60, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _blogDashboardServiceMock.Verify(
            x => x.GetBlogDashboardAsync(60, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUseDefaultTrendDays()
    {
        // Arrange
        var query = new GetBlogDashboardQuery();
        var expected = CreateTestBlogDashboard();

        _blogDashboardServiceMock
            .Setup(x => x.GetBlogDashboardAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _blogDashboardServiceMock.Verify(
            x => x.GetBlogDashboardAsync(30, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
