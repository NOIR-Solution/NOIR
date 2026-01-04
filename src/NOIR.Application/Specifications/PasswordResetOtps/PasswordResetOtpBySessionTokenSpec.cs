namespace NOIR.Application.Specifications.PasswordResetOtps;

/// <summary>
/// Specification to find a password reset OTP by its session token.
/// </summary>
public sealed class PasswordResetOtpBySessionTokenSpec : Specification<PasswordResetOtp>
{
    public PasswordResetOtpBySessionTokenSpec(string sessionToken)
    {
        Query.Where(o => o.SessionToken == sessionToken)
             .AsTracking()  // Required for entity modification
             .TagWith("PasswordResetOtpBySessionToken");
    }
}
