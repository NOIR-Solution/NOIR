namespace NOIR.Application.Specifications.EmailChangeOtps;

/// <summary>
/// Specification to find an email change OTP by its session token.
/// </summary>
public sealed class EmailChangeOtpBySessionTokenSpec : Specification<EmailChangeOtp>
{
    public EmailChangeOtpBySessionTokenSpec(string sessionToken)
    {
        Query.Where(o => o.SessionToken == sessionToken)
             .AsTracking()  // Required for entity modification
             .TagWith("EmailChangeOtpBySessionToken");
    }
}
