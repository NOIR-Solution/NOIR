namespace NOIR.Domain.UnitTests.Entities;

using NOIR.Domain.Enums;

/// <summary>
/// Unit tests for the Post entity.
/// Tests factory methods, content updates, publishing workflow, and SEO.
/// </summary>
public class PostTests
{
    #region Create Factory Tests

    [Fact]
    public void Create_WithRequiredParameters_ShouldCreateValidPost()
    {
        // Arrange
        var title = "My First Blog Post";
        var slug = "my-first-blog-post";
        var authorId = Guid.NewGuid();

        // Act
        var post = Post.Create(title, slug, authorId);

        // Assert
        post.Should().NotBeNull();
        post.Id.Should().NotBe(Guid.Empty);
        post.Title.Should().Be(title);
        post.Slug.Should().Be(slug);
        post.AuthorId.Should().Be(authorId);
        post.Status.Should().Be(PostStatus.Draft);
        post.ViewCount.Should().Be(0);
    }

    [Fact]
    public void Create_ShouldLowercaseSlug()
    {
        // Arrange
        var slug = "My-First-POST";

        // Act
        var post = Post.Create("Title", slug, Guid.NewGuid());

        // Assert
        post.Slug.Should().Be("my-first-post");
    }

    [Fact]
    public void Create_WithTenantId_ShouldBeAssociatedWithTenant()
    {
        // Arrange
        var tenantId = "tenant-123";

        // Act
        var post = Post.Create("Title", "slug", Guid.NewGuid(), tenantId);

        // Assert
        post.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_ShouldInitializeWithDraftStatus()
    {
        // Act
        var post = Post.Create("Title", "slug", Guid.NewGuid());

        // Assert
        post.Status.Should().Be(PostStatus.Draft);
        post.PublishedAt.Should().BeNull();
        post.ScheduledPublishAt.Should().BeNull();
    }

    #endregion

    #region UpdateContent Tests

    [Fact]
    public void UpdateContent_ShouldModifyContentFields()
    {
        // Arrange
        var post = Post.Create("Original", "original", Guid.NewGuid());
        var newTitle = "Updated Title";
        var newSlug = "updated-slug";
        var excerpt = "This is an excerpt";
        var contentJson = "{\"blocks\":[]}";
        var contentHtml = "<p>Hello World</p>";

        // Act
        post.UpdateContent(newTitle, newSlug, excerpt, contentJson, contentHtml);

        // Assert
        post.Title.Should().Be(newTitle);
        post.Slug.Should().Be("updated-slug");
        post.Excerpt.Should().Be(excerpt);
        post.ContentJson.Should().Be(contentJson);
        post.ContentHtml.Should().Be(contentHtml);
    }

    [Fact]
    public void UpdateContent_ShouldLowercaseSlug()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());

        // Act
        post.UpdateContent("New Title", "NEW-SLUG", null, null, null);

        // Assert
        post.Slug.Should().Be("new-slug");
    }

    [Fact]
    public void UpdateContent_ShouldCalculateReadingTime()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());
        // ~200 words = 1 minute, ~400 words = 2 minutes
        var htmlWith400Words = "<p>" + string.Join(" ", Enumerable.Repeat("word", 400)) + "</p>";

        // Act
        post.UpdateContent("Title", "slug", null, null, htmlWith400Words);

        // Assert
        post.ReadingTimeMinutes.Should().Be(2);
    }

    [Fact]
    public void UpdateContent_WithEmptyHtml_ShouldSetMinimumReadingTime()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());

        // Act
        post.UpdateContent("Title", "slug", null, null, "");

        // Assert
        post.ReadingTimeMinutes.Should().Be(1); // Minimum 1 minute
    }

    #endregion

    #region Featured Image Tests

    [Fact]
    public void SetFeaturedImage_WithMediaFileId_ShouldSetImageId()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());
        var mediaFileId = Guid.NewGuid();
        var altText = "Beautiful sunset";

        // Act
        post.SetFeaturedImage(mediaFileId, altText);

        // Assert
        post.FeaturedImageId.Should().Be(mediaFileId);
        post.FeaturedImageAlt.Should().Be(altText);
        post.FeaturedImageUrl.Should().BeNull();
    }

    [Fact]
    public void SetFeaturedImage_WithNull_ShouldClearImage()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());
        post.SetFeaturedImage(Guid.NewGuid(), "Alt");

        // Act
        post.SetFeaturedImage(null);

        // Assert
        post.FeaturedImageId.Should().BeNull();
    }

    [Fact]
    public void UpdateFeaturedImage_WithUrl_ShouldSetUrlAndClearMediaFileId()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());
        post.SetFeaturedImage(Guid.NewGuid());
        var imageUrl = "/images/hero.jpg";
        var altText = "Hero image";

        // Act
        post.UpdateFeaturedImage(imageUrl, altText);

        // Assert
        post.FeaturedImageUrl.Should().Be(imageUrl);
        post.FeaturedImageAlt.Should().Be(altText);
        post.FeaturedImageId.Should().BeNull();
    }

    #endregion

    #region SEO Tests

    [Fact]
    public void UpdateSeo_ShouldSetAllSeoProperties()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());
        var metaTitle = "SEO Optimized Title";
        var metaDescription = "This is the meta description for SEO";
        var canonicalUrl = "https://example.com/posts/slug";
        var allowIndexing = true;

        // Act
        post.UpdateSeo(metaTitle, metaDescription, canonicalUrl, allowIndexing);

        // Assert
        post.MetaTitle.Should().Be(metaTitle);
        post.MetaDescription.Should().Be(metaDescription);
        post.CanonicalUrl.Should().Be(canonicalUrl);
        post.AllowIndexing.Should().BeTrue();
    }

    [Fact]
    public void UpdateSeo_DisallowIndexing_ShouldSetAllowIndexingFalse()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());

        // Act
        post.UpdateSeo(null, null, null, false);

        // Assert
        post.AllowIndexing.Should().BeFalse();
    }

    #endregion

    #region Category Tests

    [Fact]
    public void SetCategory_ShouldSetCategoryId()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());
        var categoryId = Guid.NewGuid();

        // Act
        post.SetCategory(categoryId);

        // Assert
        post.CategoryId.Should().Be(categoryId);
    }

    [Fact]
    public void SetCategory_WithNull_ShouldClearCategory()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());
        post.SetCategory(Guid.NewGuid());

        // Act
        post.SetCategory(null);

        // Assert
        post.CategoryId.Should().BeNull();
    }

    #endregion

    #region Publishing Workflow Tests

    [Fact]
    public void Publish_ShouldSetStatusAndPublishedAt()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());
        var beforePublish = DateTimeOffset.UtcNow;

        // Act
        post.Publish();

        // Assert
        post.Status.Should().Be(PostStatus.Published);
        post.PublishedAt.Should().NotBeNull();
        post.PublishedAt.Should().BeOnOrAfter(beforePublish);
        post.ScheduledPublishAt.Should().BeNull();
    }

    [Fact]
    public void Publish_WhenScheduled_ShouldClearScheduledPublishAt()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());
        post.Schedule(DateTimeOffset.UtcNow.AddDays(1));

        // Act
        post.Publish();

        // Assert
        post.Status.Should().Be(PostStatus.Published);
        post.ScheduledPublishAt.Should().BeNull();
    }

    [Fact]
    public void Schedule_ShouldSetStatusAndScheduledPublishAt()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());
        var scheduledDate = DateTimeOffset.UtcNow.AddDays(7);

        // Act
        post.Schedule(scheduledDate);

        // Assert
        post.Status.Should().Be(PostStatus.Scheduled);
        post.ScheduledPublishAt.Should().Be(scheduledDate);
        post.PublishedAt.Should().BeNull();
    }

    [Fact]
    public void Unpublish_ShouldResetToDraft()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());
        post.Publish();

        // Act
        post.Unpublish();

        // Assert
        post.Status.Should().Be(PostStatus.Draft);
        post.PublishedAt.Should().BeNull();
        post.ScheduledPublishAt.Should().BeNull();
    }

    [Fact]
    public void Unpublish_WhenScheduled_ShouldClearScheduledDate()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());
        post.Schedule(DateTimeOffset.UtcNow.AddDays(1));

        // Act
        post.Unpublish();

        // Assert
        post.Status.Should().Be(PostStatus.Draft);
        post.ScheduledPublishAt.Should().BeNull();
    }

    [Fact]
    public void Archive_ShouldSetArchivedStatus()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());
        post.Publish();

        // Act
        post.Archive();

        // Assert
        post.Status.Should().Be(PostStatus.Archived);
    }

    #endregion

    #region ViewCount Tests

    [Fact]
    public void IncrementViewCount_ShouldIncreaseByOne()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());
        var initialCount = post.ViewCount;

        // Act
        post.IncrementViewCount();

        // Assert
        post.ViewCount.Should().Be(initialCount + 1);
    }

    [Fact]
    public void IncrementViewCount_MultipleTimes_ShouldAccumulate()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());

        // Act
        for (int i = 0; i < 100; i++)
        {
            post.IncrementViewCount();
        }

        // Assert
        post.ViewCount.Should().Be(100);
    }

    #endregion

    #region TagAssignments Tests

    [Fact]
    public void Create_TagAssignments_ShouldBeEmpty()
    {
        // Act
        var post = Post.Create("Title", "slug", Guid.NewGuid());

        // Assert
        post.TagAssignments.Should().NotBeNull();
        post.TagAssignments.Should().BeEmpty();
    }

    #endregion

    #region Status Transition Tests

    [Fact]
    public void StatusTransition_DraftToPublished_ShouldBeValid()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());

        // Act
        post.Publish();

        // Assert
        post.Status.Should().Be(PostStatus.Published);
    }

    [Fact]
    public void StatusTransition_DraftToScheduled_ShouldBeValid()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());

        // Act
        post.Schedule(DateTimeOffset.UtcNow.AddDays(1));

        // Assert
        post.Status.Should().Be(PostStatus.Scheduled);
    }

    [Fact]
    public void StatusTransition_PublishedToArchived_ShouldBeValid()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());
        post.Publish();

        // Act
        post.Archive();

        // Assert
        post.Status.Should().Be(PostStatus.Archived);
    }

    [Fact]
    public void StatusTransition_ArchivedToDraft_ShouldBeValid()
    {
        // Arrange
        var post = Post.Create("Title", "slug", Guid.NewGuid());
        post.Publish();
        post.Archive();

        // Act
        post.Unpublish();

        // Assert
        post.Status.Should().Be(PostStatus.Draft);
    }

    #endregion
}
