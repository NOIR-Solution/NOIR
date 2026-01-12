namespace NOIR.Application.Specifications.EmailChangeOtps;

/// <summary>
/// Specification to find an active (not used, not expired) email change OTP by user ID.
/// Used to prevent OTP bypass attacks by checking if user already has an active OTP.
/// </summary>
public sealed class ActiveEmailChangeOtpByUserIdSpec : Specification<EmailChangeOtp>
{
    public ActiveEmailChangeOtpByUserIdSpec(string userId)
    {
        var now = DateTimeOffset.UtcNow;
        Query.Where(o => o.UserId == userId &&
                        !o.IsUsed &&
                        o.ExpiresAt > now)
             .AsTracking()  // Required for entity modification (reuse existing OTP session)
             .TagWith("ActiveEmailChangeOtpByUserId");
    }
}
