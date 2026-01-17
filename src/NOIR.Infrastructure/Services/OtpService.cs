namespace NOIR.Infrastructure.Services;

/// <summary>
/// Service for generating and verifying OTP (One-Time Password) codes.
/// Uses cryptographically secure random number generation and bcrypt hashing.
/// </summary>
public class OtpService : IOtpService, IScopedService
{
    private readonly PasswordResetSettings _settings;
    private readonly ILogger<OtpService> _logger;

    public OtpService(
        IOptions<PasswordResetSettings> settings,
        ILogger<OtpService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public string GenerateOtp()
    {
        return GenerateOtp(_settings.OtpLength);
    }

    public string GenerateOtp(int length)
    {
        if (length < 4 || length > 10)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "OTP length must be between 4 and 10");
        }

        // Use cryptographically secure random number generation
        var maxValue = (int)Math.Pow(10, length);
        var randomBytes = new byte[4];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        // Convert to positive integer and constrain to range
        var randomNumber = Math.Abs(BitConverter.ToInt32(randomBytes, 0)) % maxValue;

        // Pad with leading zeros if necessary
        return randomNumber.ToString().PadLeft(length, '0');
    }

    public string HashOtp(string otp)
    {
        // Use bcrypt with work factor 10 (good balance of security and speed for OTPs)
        return BCrypt.Net.BCrypt.HashPassword(otp, BCrypt.Net.BCrypt.GenerateSalt(10));
    }

    public bool VerifyOtp(string otp, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(otp, hash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP hash");
            return false;
        }
    }

    public string MaskEmail(string email)
    {
        // Delegate to shared extension method
        return email.MaskEmail();
    }
}
