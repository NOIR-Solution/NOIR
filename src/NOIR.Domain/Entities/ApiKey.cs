namespace NOIR.Domain.Entities;

/// <summary>
/// Represents an API key for external system authentication.
/// Users create keys with scoped permissions; external systems authenticate
/// via X-API-Key + X-API-Secret headers.
/// </summary>
public class ApiKey : TenantAggregateRoot<Guid>
{
    private ApiKey() : base() { }
    private ApiKey(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Public key identifier with recognizable prefix (noir_key_...).
    /// Safe to log and display in UI.
    /// </summary>
    public string KeyIdentifier { get; private set; } = string.Empty;

    /// <summary>
    /// SHA-256 hash of the API secret. The plaintext secret is never stored.
    /// </summary>
    public string SecretHash { get; private set; } = string.Empty;

    /// <summary>
    /// Last 4 characters of the secret for display purposes (e.g., "...a1b2").
    /// </summary>
    public string SecretSuffix { get; private set; } = string.Empty;

    /// <summary>
    /// User-friendly name for this key (e.g., "Shopify Integration").
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional description of this key's purpose.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// The user who owns this API key.
    /// </summary>
    public string UserId { get; private set; } = string.Empty;

    /// <summary>
    /// JSON array of permission strings assigned to this key.
    /// Subset of the user's own permissions at time of creation/update.
    /// </summary>
    public string Permissions { get; private set; } = "[]";

    /// <summary>
    /// Optional expiration date. Null means never expires.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; private set; }

    /// <summary>
    /// Timestamp of the most recent API call using this key.
    /// </summary>
    public DateTimeOffset? LastUsedAt { get; private set; }

    /// <summary>
    /// IP address of the most recent API call using this key.
    /// </summary>
    public string? LastUsedIp { get; private set; }

    /// <summary>
    /// Whether this key has been revoked.
    /// </summary>
    public bool IsRevoked { get; private set; }

    /// <summary>
    /// When this key was revoked.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <summary>
    /// Reason for revocation (e.g., "Compromised", "No longer needed").
    /// </summary>
    public string? RevokedReason { get; private set; }

    /// <summary>
    /// Whether this key is currently usable (not revoked, not expired, not soft-deleted).
    /// </summary>
    public bool IsActive => !IsRevoked && !IsExpired && !IsDeleted;

    /// <summary>
    /// Whether this key has expired.
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && DateTimeOffset.UtcNow >= ExpiresAt.Value;

    /// <summary>
    /// Creates a new API key. Returns the entity and plaintext secret (shown once).
    /// </summary>
    public static (ApiKey Key, string PlaintextSecret) Create(
        string name,
        string userId,
        List<string> permissions,
        string? description = null,
        DateTimeOffset? expiresAt = null,
        string? tenantId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);

        var keyIdentifier = GenerateKeyIdentifier();
        var plaintextSecret = GenerateSecret();
        var secretHash = HashSecret(plaintextSecret);
        var secretSuffix = plaintextSecret[^4..];

        var key = new ApiKey(Guid.NewGuid(), tenantId)
        {
            KeyIdentifier = keyIdentifier,
            SecretHash = secretHash,
            SecretSuffix = secretSuffix,
            Name = name,
            Description = description,
            UserId = userId,
            Permissions = JsonSerializer.Serialize(permissions),
            ExpiresAt = expiresAt,
            IsRevoked = false
        };

        key.AddDomainEvent(new Events.ApiKey.ApiKeyCreatedEvent(key.Id, userId, name));

        return (key, plaintextSecret);
    }

    /// <summary>
    /// Updates the key's name, description, and permissions.
    /// </summary>
    public void Update(string name, string? description, List<string> permissions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name;
        Description = description;
        Permissions = JsonSerializer.Serialize(permissions);

        AddDomainEvent(new Events.ApiKey.ApiKeyUpdatedEvent(Id, UserId, name));
    }

    /// <summary>
    /// Rotates the secret. Returns the new plaintext secret (shown once).
    /// </summary>
    public string RotateSecret()
    {
        var plaintextSecret = GenerateSecret();
        SecretHash = HashSecret(plaintextSecret);
        SecretSuffix = plaintextSecret[^4..];

        AddDomainEvent(new Events.ApiKey.ApiKeyRotatedEvent(Id, UserId, Name));

        return plaintextSecret;
    }

    /// <summary>
    /// Revokes the key. It immediately stops working.
    /// </summary>
    public void Revoke(string? reason = null)
    {
        IsRevoked = true;
        RevokedAt = DateTimeOffset.UtcNow;
        RevokedReason = reason;

        AddDomainEvent(new Events.ApiKey.ApiKeyRevokedEvent(Id, UserId, Name, reason));
    }

    /// <summary>
    /// Records that this key was used for an API request.
    /// </summary>
    public void RecordUsage(string? ipAddress)
    {
        LastUsedAt = DateTimeOffset.UtcNow;
        LastUsedIp = ipAddress;
    }

    /// <summary>
    /// Verifies a plaintext secret against the stored hash.
    /// </summary>
    public bool VerifySecret(string plaintextSecret)
    {
        return SecretHash == HashSecret(plaintextSecret);
    }

    /// <summary>
    /// Gets the permissions as a list of strings.
    /// </summary>
    public List<string> GetPermissions()
    {
        return JsonSerializer.Deserialize<List<string>>(Permissions) ?? [];
    }

    // Key format: noir_key_ + 32 hex chars
    private static string GenerateKeyIdentifier()
    {
        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);
        return $"noir_key_{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }

    // Secret format: noir_secret_ + 64 hex chars
    private static string GenerateSecret()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return $"noir_secret_{Convert.ToHexString(bytes).ToLowerInvariant()}";
    }

    // SHA-256 hash for secret storage
    private static string HashSecret(string plaintextSecret)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(plaintextSecret));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
