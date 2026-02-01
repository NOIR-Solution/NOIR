using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Queries.GetTagById;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for GetTagByIdQueryHandler.
/// Tests retrieving a single blog tag by ID.
/// </summary>
public class GetTagByIdQueryHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PostTag, Guid>> _repositoryMock;
    private readonly GetTagByIdQueryHandler _handler;

    public GetTagByIdQueryHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<PostTag, Guid>>();
        _handler = new GetTagByIdQueryHandler(_repositoryMock.Object);
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
    public async Task Handle_WithValidId_ShouldReturnTag()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var tag = CreateTestTag(
            id: tagId,
            name: "JavaScript",
            slug: "javascript",
            description: "JavaScript programming language");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tag);

        var query = new GetTagByIdQuery(tagId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Id.Should().Be(tagId);
        result.Value.Name.Should().Be("JavaScript");
        result.Value.Slug.Should().Be("javascript");
        result.Value.Description.Should().Be("JavaScript programming language");
    }

    [Fact]
    public async Task Handle_ShouldMapAllFieldsCorrectly()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var tag = CreateTestTag(
            id: tagId,
            name: "TypeScript",
            slug: "typescript",
            description: "TypeScript programming language",
            color: "#3178C6");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tag);

        var query = new GetTagByIdQuery(tagId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var dto = result.Value;
        dto.Id.Should().Be(tagId);
        dto.Name.Should().Be("TypeScript");
        dto.Slug.Should().Be("typescript");
        dto.Description.Should().Be("TypeScript programming language");
        dto.Color.Should().Be("#3178C6");
    }

    [Fact]
    public async Task Handle_WithPostCount_ShouldIncludePostCount()
    {
        // Arrange
        var tag = CreateTestTag(name: "Popular Tag", slug: "popular-tag");

        // Increment post count
        tag.IncrementPostCount();
        tag.IncrementPostCount();
        tag.IncrementPostCount();
        tag.IncrementPostCount();
        tag.IncrementPostCount();

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tag);

        var query = new GetTagByIdQuery(tag.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PostCount.Should().Be(5);
    }

    [Fact]
    public async Task Handle_WithColor_ShouldIncludeColor()
    {
        // Arrange
        var tag = CreateTestTag(
            name: "React",
            slug: "react",
            color: "#61DAFB");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tag);

        var query = new GetTagByIdQuery(tag.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Color.Should().Be("#61DAFB");
    }

    #endregion

    #region Failure Scenarios

    [Fact]
    public async Task Handle_WhenTagNotFound_ShouldReturnNotFoundError()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag?)null);

        var query = new GetTagByIdQuery(nonExistentId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("NOIR-BLOG-021");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithNullDescription_ShouldReturnNullDescription()
    {
        // Arrange
        var tag = CreateTestTag(
            name: "NoDescription",
            slug: "no-description",
            description: null);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tag);

        var query = new GetTagByIdQuery(tag.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithNullColor_ShouldReturnNullColor()
    {
        // Arrange
        var tag = CreateTestTag(
            name: "NoColor",
            slug: "no-color",
            color: null);

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tag);

        var query = new GetTagByIdQuery(tag.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Color.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithZeroPostCount_ShouldReturnZero()
    {
        // Arrange
        var tag = CreateTestTag(name: "Unused Tag", slug: "unused-tag");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tag);

        var query = new GetTagByIdQuery(tag.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PostCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassToRepository()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var tag = CreateTestTag(id: tagId);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostTag>>(),
                token))
            .ReturnsAsync(tag);

        var query = new GetTagByIdQuery(tagId);

        // Act
        await _handler.Handle(query, token);

        // Assert
        _repositoryMock.Verify(
            x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostTag>>(),
                token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldIncludeTimestamps()
    {
        // Arrange
        var tag = CreateTestTag(name: "WithTimestamps", slug: "with-timestamps");

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tag);

        var query = new GetTagByIdQuery(tag.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CreatedAt.Should().NotBe(default);
    }

    [Fact]
    public async Task Handle_WithAllFieldsSet_ShouldReturnCompleteDto()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var tag = CreateTestTag(
            id: tagId,
            name: "Complete Tag",
            slug: "complete-tag",
            description: "A tag with all fields set",
            color: "#FF5733");

        tag.IncrementPostCount();
        tag.IncrementPostCount();

        _repositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<ISpecification<PostTag>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tag);

        var query = new GetTagByIdQuery(tagId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(tagId);
        result.Value.Name.Should().Be("Complete Tag");
        result.Value.Slug.Should().Be("complete-tag");
        result.Value.Description.Should().Be("A tag with all fields set");
        result.Value.Color.Should().Be("#FF5733");
        result.Value.PostCount.Should().Be(2);
        result.Value.CreatedAt.Should().NotBe(default);
    }

    #endregion
}
