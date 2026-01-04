namespace NOIR.Application.Specifications.PasswordResetOtps;

/// <summary>
/// Specification to find a password reset OTP by its reset token.
/// </summary>
public sealed class PasswordResetOtpByResetTokenSpec : Specification<PasswordResetOtp>
{
    public PasswordResetOtpByResetTokenSpec(string resetToken)
    {
        Query.Where(o => o.ResetToken == resetToken)
             .AsTracking()  // Required for entity modification
             .TagWith("PasswordResetOtpByResetToken");
    }
}
