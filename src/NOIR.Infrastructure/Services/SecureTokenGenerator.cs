using System.Security.Cryptography;
using NOIR.Application.Common.Interfaces;

namespace NOIR.Infrastructure.Services;

/// <summary>
/// Centralized cryptographically secure token generation.
/// All security tokens (session tokens, reset tokens, refresh tokens)
/// should use this service for consistent, auditable token generation.
/// </summary>
public sealed class SecureTokenGenerator : ISecureTokenGenerator, ISingletonService
{
    /// <inheritdoc />
    public string GenerateToken(int byteLength = 32)
    {
        if (byteLength < 16)
        {
            throw new ArgumentOutOfRangeException(
                nameof(byteLength),
                "Token must be at least 16 bytes for security.");
        }

        var bytes = RandomNumberGenerator.GetBytes(byteLength);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
