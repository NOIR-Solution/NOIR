namespace NOIR.Application.Specifications.PasswordResetOtps;

/// <summary>
/// Specification to count recent password reset OTP requests for an email.
/// Used for rate limiting - prevents abuse by limiting requests per email per hour.
/// </summary>
public sealed class RecentPasswordResetOtpsByEmailSpec : Specification<PasswordResetOtp>
{
    public RecentPasswordResetOtpsByEmailSpec(string email, int hoursBack = 1)
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-hoursBack);
        Query.Where(o => o.Email == email.ToLowerInvariant() &&
                        o.CreatedAt >= cutoff)
             .TagWith("RecentPasswordResetOtpsByEmail");
    }
}
