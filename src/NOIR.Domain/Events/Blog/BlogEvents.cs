namespace NOIR.Domain.Events.Blog;

/// <summary>
/// Raised when a new blog post is created.
/// </summary>
public record PostCreatedEvent(
    Guid PostId,
    string Title,
    string Slug) : DomainEvent;

/// <summary>
/// Raised when a blog post's content is updated.
/// </summary>
public record PostUpdatedEvent(
    Guid PostId,
    string Title) : DomainEvent;

/// <summary>
/// Raised when a blog post is published.
/// </summary>
public record PostPublishedEvent(
    Guid PostId,
    string Title,
    string Slug) : DomainEvent;

/// <summary>
/// Raised when a blog post is unpublished (reverted to draft).
/// </summary>
public record PostUnpublishedEvent(
    Guid PostId,
    string Title) : DomainEvent;
