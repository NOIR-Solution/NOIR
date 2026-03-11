namespace NOIR.Application.Features.ApiKeys.DTOs;

/// <summary>
/// Mapper for ApiKey entity to DTOs.
/// </summary>
public static class ApiKeyMapper
{
    public static ApiKeyDto ToDto(Domain.Entities.ApiKey key, string? userDisplayName = null)
    {
        return new ApiKeyDto
        {
            Id = key.Id,
            KeyIdentifier = key.KeyIdentifier,
            SecretSuffix = key.SecretSuffix,
            Name = key.Name,
            Description = key.Description,
            UserId = key.UserId,
            UserDisplayName = userDisplayName,
            Permissions = key.GetPermissions(),
            ExpiresAt = key.ExpiresAt,
            LastUsedAt = key.LastUsedAt,
            LastUsedIp = key.LastUsedIp,
            IsRevoked = key.IsRevoked,
            RevokedAt = key.RevokedAt,
            RevokedReason = key.RevokedReason,
            IsExpired = key.IsExpired,
            IsActive = key.IsActive,
            CreatedAt = key.CreatedAt
        };
    }

    public static ApiKeyCreatedDto ToCreatedDto(Domain.Entities.ApiKey key, string plaintextSecret)
    {
        return new ApiKeyCreatedDto
        {
            Id = key.Id,
            KeyIdentifier = key.KeyIdentifier,
            Secret = plaintextSecret,
            Name = key.Name,
            Permissions = key.GetPermissions(),
            ExpiresAt = key.ExpiresAt,
            CreatedAt = key.CreatedAt
        };
    }

    public static ApiKeyRotatedDto ToRotatedDto(Domain.Entities.ApiKey key, string plaintextSecret)
    {
        return new ApiKeyRotatedDto
        {
            Id = key.Id,
            KeyIdentifier = key.KeyIdentifier,
            Secret = plaintextSecret
        };
    }
}
