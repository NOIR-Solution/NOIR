namespace NOIR.Domain.Events.Media;

/// <summary>
/// Raised when a media file is uploaded and processed.
/// </summary>
public sealed record MediaFileUploadedEvent(
    Guid FileId,
    string FileName,
    string ContentType,
    string Folder,
    long FileSizeBytes) : DomainEvent;

/// <summary>
/// Raised when a media file's alt text is updated.
/// </summary>
public sealed record MediaFileAltTextUpdatedEvent(
    Guid FileId,
    string? NewAltText) : DomainEvent;

/// <summary>
/// Raised when a media file is soft-deleted.
/// </summary>
public sealed record MediaFileDeletedEvent(
    Guid FileId,
    string FileName) : DomainEvent;
