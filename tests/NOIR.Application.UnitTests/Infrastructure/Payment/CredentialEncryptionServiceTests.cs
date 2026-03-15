namespace NOIR.Application.UnitTests.Infrastructure.Payment;

using NOIR.Infrastructure.Services.Payment;

/// <summary>
/// Unit tests for CredentialEncryptionService.
/// Tests encryption/decryption and key resolution from environment/config.
/// </summary>
public class CredentialEncryptionServiceTests
{
    private readonly Mock<IOptions<PaymentSettings>> _paymentSettingsMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<CredentialEncryptionService>> _loggerMock;

    // Valid 32-byte base64 encoded key (44 characters = 32 bytes when decoded)
    private const string Valid32ByteKey = "DBTW3bti/yqoq4lqsxLyIcdACdAH7sMNj0Nd8EQjDMg=";

    // Invalid 34-byte base64 encoded key (48 characters = 34 bytes when decoded)
    private const string Invalid34ByteKey = "GRIlxgLt3pJfVXodLWTaQyc+vGToofNoUTPuI+y97AMgRA==";

    public CredentialEncryptionServiceTests()
    {
        _paymentSettingsMock = new Mock<IOptions<PaymentSettings>>();
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<CredentialEncryptionService>>();

        _paymentSettingsMock.Setup(x => x.Value).Returns(new PaymentSettings
        {
            EncryptionKeyId = "payment-credentials-key"
        });
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValid32ByteKeyFromConfig_ShouldNotThrow()
    {
        // Arrange
        SetupConfigurationKey(Valid32ByteKey);

        // Act
        var act = () => new CredentialEncryptionService(
            _paymentSettingsMock.Object,
            _configurationMock.Object);

        // Assert
        act.ShouldNotThrow();
    }

    [Fact]
    public void Constructor_WithValid32ByteKeyFromEnvironment_ShouldNotThrow()
    {
        // Arrange
        Environment.SetEnvironmentVariable("PAYMENT_ENCRYPTION_KEY_PAYMENT_CREDENTIALS_KEY", Valid32ByteKey);
        SetupConfigurationKey(null); // Config has no key

        try
        {
            // Act
            var act = () => new CredentialEncryptionService(
                _paymentSettingsMock.Object,
                _configurationMock.Object);

            // Assert
            act.ShouldNotThrow();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("PAYMENT_ENCRYPTION_KEY_PAYMENT_CREDENTIALS_KEY", null);
        }
    }

    #endregion

    #region Encryption/Decryption Tests

    [Fact]
    public void Encrypt_WithValidKey_ShouldReturnEncryptedString()
    {
        // Arrange
        SetupConfigurationKey(Valid32ByteKey);
        var service = CreateService();
        const string plainText = "test-credentials";

        // Act
        var encrypted = service.Encrypt(plainText);

        // Assert
        encrypted.ShouldNotBeNullOrEmpty();
        encrypted.ShouldNotBe(plainText); // Encrypted should be different from plaintext
        Convert.FromBase64String(encrypted).Length.ShouldBeGreaterThan(0); // Valid base64
    }

    [Fact]
    public void Decrypt_AfterEncrypt_ShouldReturnOriginalValue()
    {
        // Arrange
        SetupConfigurationKey(Valid32ByteKey);
        var service = CreateService();
        const string originalText = "test-credentials-123!@#";

        // Act
        var encrypted = service.Encrypt(originalText);
        var decrypted = service.Decrypt(encrypted);

        // Assert
        decrypted.ShouldBe(originalText);
    }

    [Fact]
    public void Decrypt_WithDifferentKey_ShouldThrowOrReturnGarbage()
    {
        // Arrange - Encrypt with valid key
        SetupConfigurationKey(Valid32ByteKey);
        var encryptService = CreateService();
        const string originalText = "test-credentials";
        var encrypted = encryptService.Encrypt(originalText);

        // Arrange - Create new service with different key (but still 32 bytes)
        var differentKey = "ABCD3bti/yqoq4lqsxLyIcdACdAH7sMNj0Nd8EQjDMg=";
        SetupConfigurationKey(differentKey);
        var decryptService = CreateService();

        // Act & Assert - Should throw cryptographic exception due to bad padding
        var act = () => decryptService.Decrypt(encrypted);
        Should.Throw<CryptographicException>(act);
    }

    #endregion

    #region Key Validation Tests

    [Fact]
    public void Encrypt_With34ByteKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupConfigurationKey(Invalid34ByteKey);
        var service = CreateService();

        // Act
        var act = () => service.Encrypt("test");

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Encryption key must be 256 bits (32 bytes). Current key is 34 bytes.");
    }

    [Fact]
    public void Encrypt_WithMissingKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupConfigurationKey(null);
        Environment.SetEnvironmentVariable("PAYMENT_ENCRYPTION_KEY_PAYMENT_CREDENTIALS_KEY", null);
        var service = CreateService();

        // Act
        var act = () => service.Encrypt("test");

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Encryption key 'payment-credentials-key' not found");
    }

    [Fact]
    public void Encrypt_WithPlaceholderKey_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupConfigurationKey("REPLACE_WITH_REAL_KEY_BEFORE_DEPLOYMENT");
        var service = CreateService();

        // Act
        var act = () => service.Encrypt("test");

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Encryption key 'payment-credentials-key' is still set to placeholder value");
    }

    [Fact]
    public void Encrypt_WithInvalidBase64_ShouldThrowInvalidOperationException()
    {
        // Arrange
        SetupConfigurationKey("not-valid-base64!!!");
        var service = CreateService();

        // Act
        var act = () => service.Encrypt("test");

        // Assert
        Should.Throw<InvalidOperationException>(act)
            .Message.ShouldContain("Encryption key 'payment-credentials-key' is not valid base64");
    }

    #endregion

    #region Key Resolution Priority Tests

    [Fact]
    public void Encrypt_EnvironmentVariableTakesPrecedenceOverConfig()
    {
        // Arrange - Set environment variable with valid key
        Environment.SetEnvironmentVariable("PAYMENT_ENCRYPTION_KEY_PAYMENT_CREDENTIALS_KEY", Valid32ByteKey);

        // Arrange - Set config with invalid key (should be ignored)
        SetupConfigurationKey(Invalid34ByteKey);

        try
        {
            // Act
            var service = CreateService();
            var encrypted = service.Encrypt("test");

            // Assert - Should work because env var takes precedence
            encrypted.ShouldNotBeNullOrEmpty();
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("PAYMENT_ENCRYPTION_KEY_PAYMENT_CREDENTIALS_KEY", null);
        }
    }

    #endregion

    #region Helper Methods

    private CredentialEncryptionService CreateService()
    {
        return new CredentialEncryptionService(
            _paymentSettingsMock.Object,
            _configurationMock.Object);
    }

    private void SetupConfigurationKey(string? keyValue)
    {
        _configurationMock.Setup(x => x["Payment:EncryptionKeys:payment-credentials-key"])
            .Returns(keyValue);
    }

    #endregion
}
