namespace NOIR.Application.Features.ApiKeys.DTOs;

/// <summary>
/// Full DTO for an API key (list and detail views). Never includes secret.
/// </summary>
public sealed record ApiKeyDto
{
    public Guid Id { get; init; }
    public string KeyIdentifier { get; init; } = string.Empty;
    public string SecretSuffix { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string? UserDisplayName { get; init; }
    public List<string> Permissions { get; init; } = [];
    public DateTimeOffset? ExpiresAt { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
    public string? LastUsedIp { get; init; }
    public bool IsRevoked { get; init; }
    public DateTimeOffset? RevokedAt { get; init; }
    public string? RevokedReason { get; init; }
    public bool IsExpired { get; init; }
    public bool IsActive { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// DTO returned on creation/rotation — includes the plaintext secret (shown once).
/// </summary>
public sealed record ApiKeyCreatedDto
{
    public Guid Id { get; init; }
    public string KeyIdentifier { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public List<string> Permissions { get; init; } = [];
    public DateTimeOffset? ExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// DTO returned on secret rotation — includes the new plaintext secret (shown once).
/// </summary>
public sealed record ApiKeyRotatedDto
{
    public Guid Id { get; init; }
    public string KeyIdentifier { get; init; } = string.Empty;
    public string Secret { get; init; } = string.Empty;
}
