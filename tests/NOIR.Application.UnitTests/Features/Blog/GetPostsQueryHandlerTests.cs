using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Queries.GetPosts;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for GetPostsQueryHandler.
/// Tests list retrieval scenarios with pagination and filtering.
/// </summary>
public class GetPostsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<Post, Guid>> _repositoryMock;
    private readonly GetPostsQueryHandler _handler;

    public GetPostsQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<Post, Guid>>();
        _handler = new GetPostsQueryHandler(_repositoryMock.Object);
    }

    private static Post CreateTestPost(
        string title = "Test Post",
        string slug = "test-post",
        Guid? authorId = null,
        PostStatus status = PostStatus.Draft)
    {
        var post = Post.Create(
            title,
            slug,
            authorId ?? Guid.NewGuid(),
            tenantId: "test-tenant");

        if (status == PostStatus.Published)
        {
            post.Publish();
        }

        return post;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithNoFilters_ShouldReturnAllPosts()
    {
        // Arrange
        var posts = new List<Post>
        {
            CreateTestPost(title: "First Post", slug: "first-post"),
            CreateTestPost(title: "Second Post", slug: "second-post"),
            CreateTestPost(title: "Third Post", slug: "third-post")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts.Count);

        var query = new GetPostsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldPassFilterToSpec()
    {
        // Arrange
        var posts = new List<Post>
        {
            CreateTestPost(title: "Welcome Post", slug: "welcome-post")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts.Count);

        var query = new GetPostsQuery(Search: "welcome");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldPassFilterToSpec()
    {
        // Arrange
        var publishedPost = CreateTestPost(title: "Published Post", slug: "published-post", status: PostStatus.Published);
        var posts = new List<Post> { publishedPost };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts.Count);

        var query = new GetPostsQuery(Status: PostStatus.Published);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Status.Should().Be(PostStatus.Published);
    }

    [Fact]
    public async Task Handle_WithCategoryIdFilter_ShouldPassFilterToSpec()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var posts = new List<Post>
        {
            CreateTestPost(title: "Categorized Post", slug: "categorized-post")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts.Count);

        var query = new GetPostsQuery(CategoryId: categoryId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithAuthorIdFilter_ShouldPassFilterToSpec()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var posts = new List<Post>
        {
            CreateTestPost(title: "Author Post", slug: "author-post", authorId: authorId)
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts.Count);

        var query = new GetPostsQuery(AuthorId: authorId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithPublishedOnlyFilter_ShouldUsePublishedPostsSpec()
    {
        // Arrange
        var publishedPost = CreateTestPost(title: "Public Post", slug: "public-post", status: PostStatus.Published);
        var posts = new List<Post> { publishedPost };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts.Count);

        var query = new GetPostsQuery(PublishedOnly: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var post = CreateTestPost(
            title: "Mapped Post",
            slug: "mapped-post",
            authorId: authorId,
            status: PostStatus.Published);

        var posts = new List<Post> { post };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts.Count);

        var query = new GetPostsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value.Items[0];
        dto.Title.Should().Be("Mapped Post");
        dto.Slug.Should().Be("mapped-post");
        dto.Status.Should().Be(PostStatus.Published);
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ShouldReturnEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Post>());

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new GetPostsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    #endregion

    #region Pagination Scenarios

    [Fact]
    public async Task Handle_WithPagination_ShouldReturnCorrectPage()
    {
        // Arrange
        var posts = new List<Post>
        {
            CreateTestPost(title: "Page 2 Post 1", slug: "page-2-post-1"),
            CreateTestPost(title: "Page 2 Post 2", slug: "page-2-post-2")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25); // Total 25 items

        var query = new GetPostsQuery(Page: 2, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(10);
        result.Value.TotalCount.Should().Be(25);
        result.Value.TotalPages.Should().Be(3);
        result.Value.HasPreviousPage.Should().BeTrue();
        result.Value.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithFirstPage_ShouldNotHavePreviousPage()
    {
        // Arrange
        var posts = new List<Post>
        {
            CreateTestPost(title: "First Page Post", slug: "first-page-post")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        var query = new GetPostsQuery(Page: 1, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasPreviousPage.Should().BeFalse();
        result.Value.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithLastPage_ShouldNotHaveNextPage()
    {
        // Arrange
        var posts = new List<Post>
        {
            CreateTestPost(title: "Last Page Post", slug: "last-page-post")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(25);

        var query = new GetPostsQuery(Page: 3, PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.HasPreviousPage.Should().BeTrue();
        result.Value.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WithCustomPageSize_ShouldRespectPageSize()
    {
        // Arrange
        var posts = Enumerable.Range(1, 5)
            .Select(i => CreateTestPost(title: $"Post {i}", slug: $"post-{i}"))
            .ToList();

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        var query = new GetPostsQuery(Page: 1, PageSize: 5);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(5);
        result.Value.PageSize.Should().Be(5);
        result.Value.TotalPages.Should().Be(10);
    }

    #endregion

    #region Combined Filters

    [Fact]
    public async Task Handle_WithMultipleFilters_ShouldApplyAllFilters()
    {
        // Arrange
        var categoryId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var posts = new List<Post>
        {
            CreateTestPost(title: "Filtered Post", slug: "filtered-post", authorId: authorId, status: PostStatus.Published)
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts.Count);

        var query = new GetPostsQuery(
            Search: "filtered",
            Status: PostStatus.Published,
            CategoryId: categoryId,
            AuthorId: authorId,
            Page: 1,
            PageSize: 10);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithPublishedOnlyAndTagId_ShouldUsePublishedPostsSpec()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var posts = new List<Post>
        {
            CreateTestPost(title: "Tagged Post", slug: "tagged-post", status: PostStatus.Published)
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        _repositoryMock
            .Setup(x => x.CountAsync(
                It.IsAny<ISpecification<Post>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts.Count);

        var query = new GetPostsQuery(
            TagId: tagId,
            PublishedOnly: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
    }

    #endregion
}
