namespace NOIR.Application.Common.Interfaces;

/// <summary>
/// Service for generating and verifying OTP (One-Time Password) codes.
/// Uses cryptographically secure random number generation and bcrypt hashing.
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generates a new OTP code with the configured length.
    /// </summary>
    /// <returns>The generated OTP code as a string (e.g., "123456").</returns>
    string GenerateOtp();

    /// <summary>
    /// Generates a new OTP code with a specific length.
    /// </summary>
    /// <param name="length">The number of digits (4-10).</param>
    /// <returns>The generated OTP code as a string.</returns>
    string GenerateOtp(int length);

    /// <summary>
    /// Hashes an OTP code for secure storage using bcrypt.
    /// </summary>
    /// <param name="otp">The plain text OTP code.</param>
    /// <returns>The bcrypt hash of the OTP.</returns>
    string HashOtp(string otp);

    /// <summary>
    /// Verifies an OTP code against a stored hash.
    /// </summary>
    /// <param name="otp">The plain text OTP code to verify.</param>
    /// <param name="hash">The stored bcrypt hash.</param>
    /// <returns>True if the OTP matches the hash, false otherwise.</returns>
    bool VerifyOtp(string otp, string hash);

    /// <summary>
    /// Masks an email address for display (e.g., "j***@example.com").
    /// </summary>
    /// <param name="email">The email address to mask.</param>
    /// <returns>The masked email address.</returns>
    string MaskEmail(string email);
}
