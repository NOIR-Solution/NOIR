namespace NOIR.Application.UnitTests.Specifications;

/// <summary>
/// Unit tests for MediaFile specifications.
/// Verifies that specifications are correctly configured with expected filters
/// and that IsSatisfiedBy correctly matches entities.
/// </summary>
public class MediaFileSpecificationsTests
{
    private static readonly Guid TestId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

    #region Helper Methods

    private static MediaFile CreateMediaFile(
        Guid? id = null,
        string? shortId = null,
        string? slug = null,
        string? folder = null,
        string? uploadedBy = null)
    {
        return MediaFile.Create(
            shortId: shortId ?? "abc12345",
            slug: slug ?? "test-file_abc12345",
            originalFileName: "test.jpg",
            folder: folder ?? "blog",
            defaultUrl: "/media/blog/test-file_abc12345.webp",
            thumbHash: null,
            dominantColor: "#FFFFFF",
            width: 800,
            height: 600,
            format: "webp",
            mimeType: "image/webp",
            sizeBytes: 1024,
            hasTransparency: false,
            variantsJson: "[]",
            srcsetsJson: "{}",
            uploadedBy: uploadedBy ?? "user-1");
    }

    #endregion

    #region MediaFileByIdSpec Tests

    [Fact]
    public void MediaFileByIdSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFileByIdSpec(TestId1);

        // Assert
        spec.WhereExpressions.Should().HaveCount(1);
    }

    [Fact]
    public void MediaFileByIdSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFileByIdSpec(TestId1);

        // Assert
        spec.QueryTags.Should().Contain("MediaFileById");
    }

    [Fact]
    public void MediaFileByIdSpec_WithMatchingId_ShouldSatisfy()
    {
        // Arrange
        var mediaFile = CreateMediaFile();
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFileByIdSpec(mediaFile.Id);

        // Act & Assert
        spec.IsSatisfiedBy(mediaFile).Should().BeTrue();
    }

    [Fact]
    public void MediaFileByIdSpec_WithNonMatchingId_ShouldNotSatisfy()
    {
        // Arrange
        var mediaFile = CreateMediaFile();
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFileByIdSpec(Guid.NewGuid());

        // Act & Assert
        spec.IsSatisfiedBy(mediaFile).Should().BeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MediaFileByIdSpec_TrackingBehavior_ShouldBeConfigurable(bool asTracking)
    {
        // Arrange & Act
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFileByIdSpec(TestId1, asTracking);

        // Assert
        spec.AsNoTracking.Should().Be(!asTracking);
    }

    #endregion

    #region MediaFileBySlugSpec Tests

    [Fact]
    public void MediaFileBySlugSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFileBySlugSpec("test-slug");

        // Assert
        spec.WhereExpressions.Should().HaveCount(1);
    }

    [Fact]
    public void MediaFileBySlugSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFileBySlugSpec("test-slug");

        // Assert
        spec.QueryTags.Should().Contain("MediaFileBySlug");
    }

    [Fact]
    public void MediaFileBySlugSpec_WithMatchingSlug_ShouldSatisfy()
    {
        // Arrange
        var mediaFile = CreateMediaFile(slug: "hero-banner_abc12345");
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFileBySlugSpec("hero-banner_abc12345");

        // Act & Assert
        spec.IsSatisfiedBy(mediaFile).Should().BeTrue();
    }

    [Fact]
    public void MediaFileBySlugSpec_WithNonMatchingSlug_ShouldNotSatisfy()
    {
        // Arrange
        var mediaFile = CreateMediaFile(slug: "hero-banner_abc12345");
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFileBySlugSpec("different-slug");

        // Act & Assert
        spec.IsSatisfiedBy(mediaFile).Should().BeFalse();
    }

    #endregion

    #region MediaFileByShortIdSpec Tests

    [Fact]
    public void MediaFileByShortIdSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFileByShortIdSpec("abc12345");

        // Assert
        spec.WhereExpressions.Should().HaveCount(1);
    }

    [Fact]
    public void MediaFileByShortIdSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFileByShortIdSpec("abc12345");

        // Assert
        spec.QueryTags.Should().Contain("MediaFileByShortId");
    }

    [Fact]
    public void MediaFileByShortIdSpec_WithMatchingShortId_ShouldSatisfy()
    {
        // Arrange
        var mediaFile = CreateMediaFile(shortId: "abc12345");
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFileByShortIdSpec("abc12345");

        // Act & Assert
        spec.IsSatisfiedBy(mediaFile).Should().BeTrue();
    }

    [Fact]
    public void MediaFileByShortIdSpec_WithNonMatchingShortId_ShouldNotSatisfy()
    {
        // Arrange
        var mediaFile = CreateMediaFile(shortId: "abc12345");
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFileByShortIdSpec("xyz99999");

        // Act & Assert
        spec.IsSatisfiedBy(mediaFile).Should().BeFalse();
    }

    #endregion

    #region MediaFilesByIdsSpec Tests

    [Fact]
    public void MediaFilesByIdsSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var ids = new List<Guid> { TestId1, Guid.NewGuid() };
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesByIdsSpec(ids);

        // Assert
        spec.WhereExpressions.Should().HaveCount(1);
    }

    [Fact]
    public void MediaFilesByIdsSpec_WithEmptyList_ShouldStillHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesByIdsSpec(new List<Guid>());

        // Assert
        spec.WhereExpressions.Should().HaveCount(1);
    }

    [Fact]
    public void MediaFilesByIdsSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var ids = new List<Guid> { TestId1 };
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesByIdsSpec(ids);

        // Assert
        spec.QueryTags.Should().NotBeEmpty();
    }

    #endregion

    #region MediaFilesBySlugsSpec Tests

    [Fact]
    public void MediaFilesBySlugsSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var slugs = new List<string> { "slug-1", "slug-2" };
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesBySlugsSpec(slugs);

        // Assert
        spec.WhereExpressions.Should().HaveCount(1);
    }

    [Fact]
    public void MediaFilesBySlugsSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var slugs = new List<string> { "slug-1" };
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesBySlugsSpec(slugs);

        // Assert
        spec.QueryTags.Should().NotBeEmpty();
    }

    #endregion

    #region MediaFilesByShortIdsSpec Tests

    [Fact]
    public void MediaFilesByShortIdsSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var shortIds = new List<string> { "abc12345", "def67890" };
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesByShortIdsSpec(shortIds);

        // Assert
        spec.WhereExpressions.Should().HaveCount(1);
    }

    [Fact]
    public void MediaFilesByShortIdsSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var shortIds = new List<string> { "abc12345" };
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesByShortIdsSpec(shortIds);

        // Assert
        spec.QueryTags.Should().NotBeEmpty();
    }

    #endregion

    #region MediaFilesByFolderSpec Tests

    [Fact]
    public void MediaFilesByFolderSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesByFolderSpec("blog");

        // Assert
        spec.WhereExpressions.Should().HaveCount(1);
    }

    [Fact]
    public void MediaFilesByFolderSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesByFolderSpec("blog");

        // Assert
        spec.QueryTags.Should().Contain("MediaFilesByFolder");
    }

    [Fact]
    public void MediaFilesByFolderSpec_ShouldHaveOrderByDescending()
    {
        // Arrange & Act
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesByFolderSpec("blog");

        // Assert - Should be ordered by CreatedAt descending
        spec.OrderByDescending.Should().NotBeNull();
    }

    [Fact]
    public void MediaFilesByFolderSpec_WithMatchingFolder_ShouldSatisfy()
    {
        // Arrange
        var mediaFile = CreateMediaFile(folder: "blog");
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesByFolderSpec("blog");

        // Act & Assert
        spec.IsSatisfiedBy(mediaFile).Should().BeTrue();
    }

    [Fact]
    public void MediaFilesByFolderSpec_WithNonMatchingFolder_ShouldNotSatisfy()
    {
        // Arrange
        var mediaFile = CreateMediaFile(folder: "avatars");
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesByFolderSpec("blog");

        // Act & Assert
        spec.IsSatisfiedBy(mediaFile).Should().BeFalse();
    }

    #endregion

    #region MediaFilesByUploaderSpec Tests

    [Fact]
    public void MediaFilesByUploaderSpec_ShouldHaveWhereExpression()
    {
        // Arrange & Act
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesByUploaderSpec("user-1");

        // Assert
        spec.WhereExpressions.Should().HaveCount(1);
    }

    [Fact]
    public void MediaFilesByUploaderSpec_ShouldHaveQueryTag()
    {
        // Arrange & Act
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesByUploaderSpec("user-1");

        // Assert
        spec.QueryTags.Should().Contain("MediaFilesByUploader");
    }

    [Fact]
    public void MediaFilesByUploaderSpec_ShouldHaveOrderByDescending()
    {
        // Arrange & Act
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesByUploaderSpec("user-1");

        // Assert - Should be ordered by CreatedAt descending
        spec.OrderByDescending.Should().NotBeNull();
    }

    [Fact]
    public void MediaFilesByUploaderSpec_WithMatchingUploader_ShouldSatisfy()
    {
        // Arrange
        var mediaFile = CreateMediaFile(uploadedBy: "user-1");
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesByUploaderSpec("user-1");

        // Act & Assert
        spec.IsSatisfiedBy(mediaFile).Should().BeTrue();
    }

    [Fact]
    public void MediaFilesByUploaderSpec_WithNonMatchingUploader_ShouldNotSatisfy()
    {
        // Arrange
        var mediaFile = CreateMediaFile(uploadedBy: "user-1");
        var spec = new NOIR.Application.Specifications.MediaFiles.MediaFilesByUploaderSpec("user-2");

        // Act & Assert
        spec.IsSatisfiedBy(mediaFile).Should().BeFalse();
    }

    #endregion
}
