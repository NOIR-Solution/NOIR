namespace NOIR.Application.Specifications.PasswordResetOtps;

/// <summary>
/// Specification to find an active (not used, not expired) password reset OTP by email.
/// Used to prevent OTP bypass attacks by checking if user already has an active OTP.
/// </summary>
public sealed class ActivePasswordResetOtpByEmailSpec : Specification<PasswordResetOtp>
{
    public ActivePasswordResetOtpByEmailSpec(string email)
    {
        var now = DateTimeOffset.UtcNow;
        Query.Where(o => o.Email == email.ToLowerInvariant() &&
                        !o.IsUsed &&
                        o.ExpiresAt > now)
             .AsTracking()  // Required for entity modification (reuse existing OTP session)
             .TagWith("ActivePasswordResetOtpByEmail");
    }
}
