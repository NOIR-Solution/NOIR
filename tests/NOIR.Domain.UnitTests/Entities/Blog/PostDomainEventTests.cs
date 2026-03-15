using NOIR.Domain.Events.Blog;

namespace NOIR.Domain.UnitTests.Entities.Blog;

/// <summary>
/// Unit tests verifying that the Post aggregate root raises
/// the correct domain events for creation, content updates, publishing, and unpublishing.
/// </summary>
public class PostDomainEventTests
{
    private const string TestTenantId = "test-tenant";

    private static Post CreateTestPost(
        string title = "My Blog Post",
        string slug = "my-blog-post",
        Guid? authorId = null,
        string? tenantId = TestTenantId)
    {
        return Post.Create(title, slug, authorId ?? Guid.NewGuid(), tenantId);
    }

    #region Create Domain Event

    [Fact]
    public void Create_ShouldRaisePostCreatedEvent()
    {
        // Act
        var post = CreateTestPost();

        // Assert
        post.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<PostCreatedEvent>();
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectPostId()
    {
        // Act
        var post = CreateTestPost();

        // Assert
        var domainEvent = post.DomainEvents.OfType<PostCreatedEvent>().Single();
        domainEvent.PostId.ShouldBe(post.Id);
    }

    [Fact]
    public void Create_ShouldRaiseEventWithCorrectTitle()
    {
        // Act
        var post = CreateTestPost(title: "Getting Started with .NET");

        // Assert
        var domainEvent = post.DomainEvents.OfType<PostCreatedEvent>().Single();
        domainEvent.Title.ShouldBe("Getting Started with .NET");
    }

    [Fact]
    public void Create_ShouldRaiseEventWithLowercasedSlug()
    {
        // Act
        var post = CreateTestPost(slug: "My-First-POST");

        // Assert
        var domainEvent = post.DomainEvents.OfType<PostCreatedEvent>().Single();
        domainEvent.Slug.ShouldBe("my-first-post");
    }

    #endregion

    #region UpdateContent Domain Event

    [Fact]
    public void UpdateContent_ShouldRaisePostUpdatedEvent()
    {
        // Arrange
        var post = CreateTestPost();
        post.ClearDomainEvents();

        // Act
        post.UpdateContent("Updated Title", "updated-slug", "An excerpt", null, "<p>Content</p>");

        // Assert
        post.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<PostUpdatedEvent>();
    }

    [Fact]
    public void UpdateContent_ShouldRaiseEventWithCorrectProperties()
    {
        // Arrange
        var post = CreateTestPost();
        post.ClearDomainEvents();

        // Act
        post.UpdateContent("New Title", "new-slug", null, null, null);

        // Assert
        var domainEvent = post.DomainEvents.OfType<PostUpdatedEvent>().Single();
        domainEvent.PostId.ShouldBe(post.Id);
        domainEvent.Title.ShouldBe("New Title");
    }

    [Fact]
    public void UpdateContent_CalledMultipleTimes_ShouldRaiseEventForEachCall()
    {
        // Arrange
        var post = CreateTestPost();
        post.ClearDomainEvents();

        // Act
        post.UpdateContent("Title v1", "slug-v1", null, null, null);
        post.UpdateContent("Title v2", "slug-v2", null, null, null);

        // Assert
        post.DomainEvents.OfType<PostUpdatedEvent>().Count().ShouldBe(2);
    }

    #endregion

    #region Publish Domain Event

    [Fact]
    public void Publish_ShouldRaisePostPublishedEvent()
    {
        // Arrange
        var post = CreateTestPost();
        post.ClearDomainEvents();

        // Act
        post.Publish();

        // Assert
        post.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<PostPublishedEvent>();
    }

    [Fact]
    public void Publish_ShouldRaiseEventWithCorrectProperties()
    {
        // Arrange
        var post = CreateTestPost(title: "Published Post", slug: "published-post");
        post.ClearDomainEvents();

        // Act
        post.Publish();

        // Assert
        var domainEvent = post.DomainEvents.OfType<PostPublishedEvent>().Single();
        domainEvent.PostId.ShouldBe(post.Id);
        domainEvent.Title.ShouldBe("Published Post");
        domainEvent.Slug.ShouldBe("published-post");
    }

    #endregion

    #region Unpublish Domain Event

    [Fact]
    public void Unpublish_ShouldRaisePostUnpublishedEvent()
    {
        // Arrange - Must publish first, then unpublish
        var post = CreateTestPost();
        post.Publish();
        post.ClearDomainEvents();

        // Act
        post.Unpublish();

        // Assert
        post.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<PostUnpublishedEvent>();
    }

    [Fact]
    public void Unpublish_ShouldRaiseEventWithCorrectProperties()
    {
        // Arrange
        var post = CreateTestPost(title: "Retracted Post");
        post.Publish();
        post.ClearDomainEvents();

        // Act
        post.Unpublish();

        // Assert
        var domainEvent = post.DomainEvents.OfType<PostUnpublishedEvent>().Single();
        domainEvent.PostId.ShouldBe(post.Id);
        domainEvent.Title.ShouldBe("Retracted Post");
    }

    [Fact]
    public void Unpublish_WhenScheduled_ShouldRaiseEvent()
    {
        // Arrange - Schedule then unpublish
        var post = CreateTestPost();
        post.Schedule(DateTimeOffset.UtcNow.AddDays(7));
        post.ClearDomainEvents();

        // Act
        post.Unpublish();

        // Assert
        post.DomainEvents.ShouldHaveSingleItem()
            .ShouldBeOfType<PostUnpublishedEvent>();
    }

    #endregion

    #region Full Lifecycle

    [Fact]
    public void FullLifecycle_CreatePublishUnpublish_ShouldRaiseCorrectEventSequence()
    {
        // Act - Create
        var post = CreateTestPost(title: "Lifecycle Post");
        post.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<PostCreatedEvent>();

        // Act - Update content
        post.ClearDomainEvents();
        post.UpdateContent("Updated Lifecycle Post", "lifecycle-post", "excerpt", null, "<p>Body</p>");
        post.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<PostUpdatedEvent>();

        // Act - Publish
        post.ClearDomainEvents();
        post.Publish();
        post.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<PostPublishedEvent>();

        // Act - Unpublish
        post.ClearDomainEvents();
        post.Unpublish();
        post.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<PostUnpublishedEvent>();
    }

    #endregion
}
