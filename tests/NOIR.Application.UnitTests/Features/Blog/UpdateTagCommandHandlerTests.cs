using NOIR.Application.Features.Blog.Commands.UpdateTag;
using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Specifications;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for UpdateTagCommandHandler.
/// Tests tag update scenarios with mocked dependencies.
/// </summary>
public class UpdateTagCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PostTag, Guid>> _tagRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly UpdateTagCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public UpdateTagCommandHandlerTests()
    {
        _tagRepositoryMock = new Mock<IRepository<PostTag, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        // Setup default tenant
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new UpdateTagCommandHandler(
            _tagRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static PostTag CreateTestTag(
        Guid? id = null,
        string name = "Test Tag",
        string slug = "test-tag",
        string? description = null,
        string? color = "#3B82F6",
        string? tenantId = TestTenantId)
    {
        var tag = PostTag.Create(name, slug, description, color, tenantId);
        // Use reflection to set the Id since it's generated in Create
        if (id.HasValue)
        {
            typeof(PostTag).GetProperty("Id")!.SetValue(tag, id.Value);
        }
        return tag;
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidData_ShouldSucceed()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var existingTag = CreateTestTag(
            id: tagId,
            name: "Old Name",
            slug: "old-slug",
            description: "Old description",
            color: "#000000");

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateTagCommand(
            tagId,
            "New Name",
            "new-slug",
            "New description",
            "#FFFFFF");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("New Name");
        result.Value.Slug.Should().Be("new-slug");
        result.Value.Description.Should().Be("New description");
        result.Value.Color.Should().Be("#FFFFFF");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithSameSlug_ShouldNotCheckForConflict()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        const string existingSlug = "existing-slug";
        var existingTag = CreateTestTag(
            id: tagId,
            name: "Old Name",
            slug: existingSlug);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Command uses the same slug (case-insensitive)
        var command = new UpdateTagCommand(
            tagId,
            "Updated Name",
            existingSlug,  // Same slug
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Should not check for slug conflicts when slug hasn't changed
        _tagRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(
                It.IsAny<TagSlugExistsSpec>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithNewUniqueSlug_ShouldSucceed()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var existingTag = CreateTestTag(
            id: tagId,
            name: "Old Name",
            slug: "old-slug");

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateTagCommand(
            tagId,
            "New Name",
            "completely-new-slug",
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("completely-new-slug");

        // Should check for slug conflicts when slug changes
        _tagRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(
                It.IsAny<TagSlugExistsSpec>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithUpperCaseSlug_ShouldConvertToLowerCase()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var existingTag = CreateTestTag(
            id: tagId,
            slug: "old-slug");

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag?)null);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateTagCommand(
            tagId,
            "Updated Tag",
            "UPPER-CASE-SLUG",
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be("upper-case-slug");
    }

    #endregion

    #region NotFound Scenarios

    [Fact]
    public async Task Handle_WhenTagNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var tagId = Guid.NewGuid();

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag?)null);

        var command = new UpdateTagCommand(
            tagId,
            "New Name",
            "new-slug",
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        result.Error.Code.Should().Be("NOIR-BLOG-012");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenNewSlugAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var otherTagId = Guid.NewGuid();

        var existingTag = CreateTestTag(
            id: tagId,
            name: "My Tag",
            slug: "my-tag");

        var conflictingTag = CreateTestTag(
            id: otherTagId,
            name: "Another Tag",
            slug: "taken-slug");

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(conflictingTag);

        var command = new UpdateTagCommand(
            tagId,
            "My Tag",
            "taken-slug",  // This slug belongs to another tag
            null,
            null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("NOIR-BLOG-011");

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var existingTag = CreateTestTag(id: tagId);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateTagCommand(
            tagId,
            "Updated",
            "test-tag",  // Same as existing slug
            null,
            null);

        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _tagRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<TagByIdForUpdateSpec>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldPreservePostCount()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var existingTag = CreateTestTag(id: tagId, slug: "test-tag");

        // Simulate a tag with posts
        existingTag.IncrementPostCount();
        existingTag.IncrementPostCount();
        existingTag.IncrementPostCount();

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateTagCommand(
            tagId,
            "Updated Name",
            "test-tag",
            "Updated description",
            "#123456");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.PostCount.Should().Be(3);
    }

    [Fact]
    public async Task Handle_ShouldClearDescriptionAndColor()
    {
        // Arrange
        var tagId = Guid.NewGuid();
        var existingTag = CreateTestTag(
            id: tagId,
            slug: "test-tag",
            description: "Old description",
            color: "#FF0000");

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagByIdForUpdateSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateTagCommand(
            tagId,
            "Updated Name",
            "test-tag",
            Description: null,  // Clear description
            Color: null);       // Clear color

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().BeNull();
        result.Value.Color.Should().BeNull();
    }

    #endregion
}
