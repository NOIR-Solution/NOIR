namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for encrypting/decrypting payment gateway credentials.
/// Uses AES-256 encryption per-tenant.
/// </summary>
public interface ICredentialEncryptionService : IScopedService
{
    /// <summary>
    /// Encrypts gateway credentials.
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts gateway credentials.
    /// </summary>
    string Decrypt(string cipherText);
}
