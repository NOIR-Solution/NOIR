namespace NOIR.Application.Specifications.EmailChangeOtps;

/// <summary>
/// Specification to count recent email change OTP requests for a user.
/// Used for rate limiting - prevents abuse by limiting requests per user per hour.
/// </summary>
public sealed class RecentEmailChangeOtpsByUserIdSpec : Specification<EmailChangeOtp>
{
    public RecentEmailChangeOtpsByUserIdSpec(string userId, TimeSpan lookbackPeriod)
    {
        var cutoff = DateTimeOffset.UtcNow - lookbackPeriod;
        Query.Where(o => o.UserId == userId &&
                        o.CreatedAt >= cutoff)
             .TagWith("RecentEmailChangeOtpsByUserId");
    }
}
