namespace NOIR.Domain.Events.ApiKey;

/// <summary>
/// Raised when an API key is created.
/// </summary>
public sealed record ApiKeyCreatedEvent(
    Guid ApiKeyId,
    string UserId,
    string KeyName) : DomainEvent;

/// <summary>
/// Raised when an API key's permissions or metadata are updated.
/// </summary>
public sealed record ApiKeyUpdatedEvent(
    Guid ApiKeyId,
    string UserId,
    string KeyName) : DomainEvent;

/// <summary>
/// Raised when an API key's secret is rotated.
/// </summary>
public sealed record ApiKeyRotatedEvent(
    Guid ApiKeyId,
    string UserId,
    string KeyName) : DomainEvent;

/// <summary>
/// Raised when an API key is revoked.
/// </summary>
public sealed record ApiKeyRevokedEvent(
    Guid ApiKeyId,
    string UserId,
    string KeyName,
    string? Reason) : DomainEvent;
