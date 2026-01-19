using NOIR.Application.Features.Blog.Commands.CreateTag;
using NOIR.Application.Features.Blog.DTOs;
using NOIR.Application.Features.Blog.Specifications;

namespace NOIR.Application.UnitTests.Features.Blog;

/// <summary>
/// Unit tests for CreateTagCommandHandler.
/// Tests tag creation scenarios with mocked dependencies.
/// </summary>
public class CreateTagCommandHandlerTests
{
    #region Test Setup

    private readonly Mock<IRepository<PostTag, Guid>> _tagRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly CreateTagCommandHandler _handler;

    private const string TestTenantId = "test-tenant";

    public CreateTagCommandHandlerTests()
    {
        _tagRepositoryMock = new Mock<IRepository<PostTag, Guid>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserMock = new Mock<ICurrentUser>();

        // Setup default tenant
        _currentUserMock.Setup(x => x.TenantId).Returns(TestTenantId);

        _handler = new CreateTagCommandHandler(
            _tagRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _currentUserMock.Object);
    }

    private static PostTag CreateTestTag(
        string name = "Test Tag",
        string slug = "test-tag",
        string? description = null,
        string? color = "#3B82F6",
        string? tenantId = TestTenantId)
    {
        return PostTag.Create(name, slug, description, color, tenantId);
    }

    #endregion

    #region Success Scenarios

    [Fact]
    public async Task Handle_WithValidData_ShouldSucceed()
    {
        // Arrange
        const string name = "New Tag";
        const string slug = "new-tag";
        const string description = "A test tag";
        const string color = "#FF5733";

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag?)null);

        _tagRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PostTag>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag tag, CancellationToken _) => tag);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateTagCommand(name, slug, description, color);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(name);
        result.Value.Slug.Should().Be(slug.ToLowerInvariant());
        result.Value.Description.Should().Be(description);
        result.Value.Color.Should().Be(color);
        result.Value.PostCount.Should().Be(0);

        _tagRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<PostTag>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithMinimalData_ShouldSucceed()
    {
        // Arrange
        const string name = "Minimal Tag";
        const string slug = "minimal-tag";

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag?)null);

        _tagRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PostTag>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag tag, CancellationToken _) => tag);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateTagCommand(name, slug, Description: null, Color: null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be(name);
        result.Value.Slug.Should().Be(slug.ToLowerInvariant());
        result.Value.Description.Should().BeNull();
        result.Value.Color.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithUpperCaseSlug_ShouldConvertToLowerCase()
    {
        // Arrange
        const string name = "Upper Case Tag";
        const string slug = "UPPER-CASE-TAG";

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag?)null);

        _tagRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PostTag>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag tag, CancellationToken _) => tag);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateTagCommand(name, slug, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Slug.Should().Be(slug.ToLowerInvariant());
    }

    #endregion

    #region Conflict Scenarios

    [Fact]
    public async Task Handle_WhenSlugAlreadyExists_ShouldReturnConflict()
    {
        // Arrange
        const string name = "Duplicate Tag";
        const string slug = "duplicate-tag";
        var existingTag = CreateTestTag(name: "Existing Tag", slug: slug);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        var command = new CreateTagCommand(name, slug, null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        result.Error.Code.Should().Be("NOIR-BLOG-011");

        _tagRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<PostTag>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSlugExistsWithDifferentCase_ShouldReturnConflict()
    {
        // Arrange - The spec checks lowercase, so even if slug differs in case, it should conflict
        const string existingSlug = "my-tag";
        var existingTag = CreateTestTag(name: "My Tag", slug: existingSlug);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingTag);

        var command = new CreateTagCommand("My Tag", "MY-TAG", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    #endregion

    #region Tenant Isolation

    [Fact]
    public async Task Handle_ShouldUseCurrentUserTenantId()
    {
        // Arrange
        const string customTenantId = "custom-tenant";
        _currentUserMock.Setup(x => x.TenantId).Returns(customTenantId);

        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag?)null);

        PostTag? capturedTag = null;
        _tagRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PostTag>(), It.IsAny<CancellationToken>()))
            .Callback<PostTag, CancellationToken>((tag, _) => capturedTag = tag)
            .ReturnsAsync((PostTag tag, CancellationToken _) => tag);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateTagCommand("Tenant Tag", "tenant-tag", null, null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        capturedTag.Should().NotBeNull();
        capturedTag!.TenantId.Should().Be(customTenantId);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassItToRepository()
    {
        // Arrange
        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag?)null);

        _tagRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PostTag>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag tag, CancellationToken _) => tag);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateTagCommand("CancellationTest", "cancellation-test", null, null);
        var cts = new CancellationTokenSource();
        var token = cts.Token;

        // Act
        await _handler.Handle(command, token);

        // Assert
        _tagRepositoryMock.Verify(
            x => x.FirstOrDefaultAsync(It.IsAny<TagSlugExistsSpec>(), token),
            Times.Once);
        _tagRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<PostTag>(), token),
            Times.Once);
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(token),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnDtoWithCorrectTimestamps()
    {
        // Arrange
        _tagRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(
                It.IsAny<TagSlugExistsSpec>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag?)null);

        _tagRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<PostTag>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PostTag tag, CancellationToken _) => tag);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var beforeCreation = DateTimeOffset.UtcNow;
        var command = new CreateTagCommand("Timestamp Tag", "timestamp-tag", null, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ModifiedAt.Should().BeNull();
    }

    #endregion
}
