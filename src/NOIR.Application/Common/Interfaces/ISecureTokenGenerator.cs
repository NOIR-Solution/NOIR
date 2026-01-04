namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for generating cryptographically secure random tokens.
/// Centralizes token generation logic for security auditing.
/// </summary>
public interface ISecureTokenGenerator
{
    /// <summary>
    /// Generates a URL-safe base64 token of specified byte length.
    /// </summary>
    /// <param name="byteLength">Number of random bytes (default: 32).</param>
    /// <returns>URL-safe base64 encoded string.</returns>
    string GenerateToken(int byteLength = 32);
}
