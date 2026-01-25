namespace NOIR.Infrastructure.Services.Payment;

/// <summary>
/// Service for encrypting and decrypting payment gateway credentials using AES-256.
/// </summary>
public class CredentialEncryptionService : ICredentialEncryptionService, IScopedService
{
    private readonly IOptions<PaymentSettings> _paymentSettings;
    private readonly IConfiguration _configuration;

    public CredentialEncryptionService(
        IOptions<PaymentSettings> paymentSettings,
        IConfiguration configuration)
    {
        _paymentSettings = paymentSettings;
        _configuration = configuration;
    }

    public string Encrypt(string plainText)
    {
        var key = GetEncryptionKey();
        return EncryptInternal(plainText, key);
    }

    public string Decrypt(string cipherText)
    {
        var key = GetEncryptionKey();
        return DecryptInternal(cipherText, key);
    }

    private byte[] GetEncryptionKey()
    {
        var keyId = _paymentSettings.Value.EncryptionKeyId;
        var keyString = _configuration[$"Payment:EncryptionKeys:{keyId}"]
            ?? throw new InvalidOperationException($"Encryption key '{keyId}' not found in configuration.");

        // Convert key to bytes (expecting base64 encoded 256-bit key)
        var keyBytes = Convert.FromBase64String(keyString);
        if (keyBytes.Length != 32)
        {
            throw new InvalidOperationException("Encryption key must be 256 bits (32 bytes).");
        }

        return keyBytes;
    }

    private static string EncryptInternal(string plainText, byte[] key)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        // Prepend IV to encrypted data
        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    private static string DecryptInternal(string cipherText, byte[] key)
    {
        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = key;

        // Extract IV from the beginning
        var iv = new byte[aes.BlockSize / 8];
        var cipherBytes = new byte[fullCipher.Length - iv.Length];
        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
