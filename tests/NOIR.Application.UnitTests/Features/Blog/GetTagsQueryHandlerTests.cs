using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Queries.GetTags;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for GetTagsQueryHandler.
/// Tests tag list retrieval with search filtering.
/// </summary>
public class GetTagsQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PostTag, Guid>> _repositoryMock;
    private readonly GetTagsQueryHandler _handler;

    public GetTagsQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<PostTag, Guid>>();
        _handler = new GetTagsQueryHandler(_repositoryMock.Object);
    }

    private static PostTag CreateTestTag(
        Guid? id = null,
        string name = "Test Tag",
        string slug = "test-tag",
        string? description = null,
        string? color = null)
    {
        var tag = PostTag.Create(
            name,
            slug,
            description,
            color,
            tenantId: "test-tenant");

        // Use reflection to set the ID if provided
        if (id.HasValue)
        {
            var idProperty = typeof(PostTag).GetProperty("Id");
            idProperty?.SetValue(tag, id.Value);
        }

        return tag;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithNoFilters_ShouldReturnAllTags()
    {
        // Arrange
        var tags = new List<PostTag>
        {
            CreateTestTag(name: "JavaScript", slug: "javascript"),
            CreateTestTag(name: "TypeScript", slug: "typescript"),
            CreateTestTag(name: "CSharp", slug: "csharp")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        var query = new GetTagsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_WithSearchFilter_ShouldPassFilterToSpec()
    {
        // Arrange
        var tags = new List<PostTag>
        {
            CreateTestTag(name: "JavaScript", slug: "javascript"),
            CreateTestTag(name: "TypeScript", slug: "typescript")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        var query = new GetTagsQuery(Search: "script");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var tag = CreateTestTag(
            id: tagId,
            name: "JavaScript",
            slug: "javascript",
            description: "JavaScript programming language",
            color: "#F7DF1E");

        var tags = new List<PostTag> { tag };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        var query = new GetTagsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value[0];
        dto.Id.Should().Be(tagId);
        dto.Name.Should().Be("JavaScript");
        dto.Slug.Should().Be("javascript");
        dto.Description.Should().Be("JavaScript programming language");
        dto.Color.Should().Be("#F7DF1E");
    }

    [Fact]
    public async Task Handle_WithEmptyResult_ShouldReturnEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostTag>());

        var query = new GetTagsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    #endregion

    #region Search Scenarios

    [Fact]
    public async Task Handle_WithCaseInsensitiveSearch_ShouldFindTags()
    {
        // Arrange
        var tags = new List<PostTag>
        {
            CreateTestTag(name: "JavaScript", slug: "javascript")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        var query = new GetTagsQuery(Search: "JAVASCRIPT");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_WithPartialSearch_ShouldFindMatchingTags()
    {
        // Arrange
        var tags = new List<PostTag>
        {
            CreateTestTag(name: "React", slug: "react"),
            CreateTestTag(name: "ReactNative", slug: "react-native")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        var query = new GetTagsQuery(Search: "react");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithNoMatchingSearch_ShouldReturnEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PostTag>());

        var query = new GetTagsQuery(Search: "nonexistent");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithEmptySearch_ShouldReturnAllTags()
    {
        // Arrange
        var tags = new List<PostTag>
        {
            CreateTestTag(name: "Tag1", slug: "tag1"),
            CreateTestTag(name: "Tag2", slug: "tag2")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        var query = new GetTagsQuery(Search: "");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithWhitespaceSearch_ShouldTreatAsNoFilter()
    {
        // Arrange
        var tags = new List<PostTag>
        {
            CreateTestTag(name: "Tag1", slug: "tag1"),
            CreateTestTag(name: "Tag2", slug: "tag2")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        var query = new GetTagsQuery(Search: "   ");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    #endregion

    #region PostCount Scenarios

    [Fact]
    public async Task Handle_ShouldIncludePostCount()
    {
        // Arrange
        var tag = CreateTestTag(name: "Popular Tag", slug: "popular-tag");

        // Increment post count to simulate posts with this tag
        tag.IncrementPostCount();
        tag.IncrementPostCount();
        tag.IncrementPostCount();
        tag.IncrementPostCount();
        tag.IncrementPostCount();

        var tags = new List<PostTag> { tag };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        var query = new GetTagsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[0].PostCount.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WithZeroPostCount_ShouldReturnZero()
    {
        // Arrange
        var tag = CreateTestTag(name: "Unused Tag", slug: "unused-tag");

        var tags = new List<PostTag> { tag };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        var query = new GetTagsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[0].PostCount.Should().Be(0);
    }

    #endregion

    #region Color Scenarios

    [Fact]
    public async Task Handle_WithColor_ShouldIncludeColor()
    {
        // Arrange
        var tag = CreateTestTag(
            name: "Colored Tag",
            slug: "colored-tag",
            color: "#3B82F6");

        var tags = new List<PostTag> { tag };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        var query = new GetTagsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[0].Color.Should().Be("#3B82F6");
    }

    [Fact]
    public async Task Handle_WithNullColor_ShouldReturnNullColor()
    {
        // Arrange
        var tag = CreateTestTag(
            name: "No Color Tag",
            slug: "no-color-tag",
            color: null);

        var tags = new List<PostTag> { tag };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        var query = new GetTagsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[0].Color.Should().BeNull();
    }

    #endregion

    #region Description Scenarios

    [Fact]
    public async Task Handle_WithDescription_ShouldIncludeDescription()
    {
        // Arrange
        var tag = CreateTestTag(
            name: "Described Tag",
            slug: "described-tag",
            description: "This is a detailed description of the tag");

        var tags = new List<PostTag> { tag };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        var query = new GetTagsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[0].Description.Should().Be("This is a detailed description of the tag");
    }

    [Fact]
    public async Task Handle_WithNullDescription_ShouldReturnNullDescription()
    {
        // Arrange
        var tag = CreateTestTag(
            name: "No Description Tag",
            slug: "no-description-tag",
            description: null);

        var tags = new List<PostTag> { tag };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        var query = new GetTagsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value[0].Description.Should().BeNull();
    }

    #endregion

    #region Multiple Tags Scenarios

    [Fact]
    public async Task Handle_WithMultipleTags_ShouldReturnAllInOrder()
    {
        // Arrange
        var tags = new List<PostTag>
        {
            CreateTestTag(name: "Alpha", slug: "alpha"),
            CreateTestTag(name: "Beta", slug: "beta"),
            CreateTestTag(name: "Gamma", slug: "gamma"),
            CreateTestTag(name: "Delta", slug: "delta")
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        var query = new GetTagsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(4);
        result.Value.Select(t => t.Name).Should().Contain(new[] { "Alpha", "Beta", "Gamma", "Delta" });
    }

    [Fact]
    public async Task Handle_WithVariousTagProperties_ShouldMapAllCorrectly()
    {
        // Arrange
        var tags = new List<PostTag>
        {
            CreateTestTag(name: "Tag1", slug: "tag1", description: "Desc1", color: "#FF0000"),
            CreateTestTag(name: "Tag2", slug: "tag2", description: null, color: "#00FF00"),
            CreateTestTag(name: "Tag3", slug: "tag3", description: "Desc3", color: null)
        };

        _repositoryMock
            .Setup(x => x.ListAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tags);

        var query = new GetTagsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);

        var tag1 = result.Value.First(t => t.Name == "Tag1");
        tag1.Description.Should().Be("Desc1");
        tag1.Color.Should().Be("#FF0000");

        var tag2 = result.Value.First(t => t.Name == "Tag2");
        tag2.Description.Should().BeNull();
        tag2.Color.Should().Be("#00FF00");

        var tag3 = result.Value.First(t => t.Name == "Tag3");
        tag3.Description.Should().Be("Desc3");
        tag3.Color.Should().BeNull();
    }

    #endregion
}
