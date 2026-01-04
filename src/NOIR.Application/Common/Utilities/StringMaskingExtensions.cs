namespace NOIR.Application.Common.Utilities;

/// <summary>
/// Extension methods for masking sensitive strings for display purposes.
/// </summary>
public static class StringMaskingExtensions
{
    /// <summary>
    /// Masks an email address for privacy (e.g., "jo***n@example.com").
    /// </summary>
    /// <param name="email">The email address to mask.</param>
    /// <returns>The masked email address.</returns>
    public static string MaskEmail(this string email)
    {
        if (string.IsNullOrEmpty(email))
            return string.Empty;

        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return email;

        var localPart = email[..atIndex];
        var domainPart = email[atIndex..];

        if (localPart.Length <= 1)
            return $"{localPart}***{domainPart}";

        if (localPart.Length <= 3)
            return $"{localPart[0]}***{domainPart}";

        return $"{localPart[0..2]}***{localPart[^1]}{domainPart}";
    }

    /// <summary>
    /// Masks a phone number for privacy (e.g., "***-***-1234").
    /// Shows only the last 4 digits.
    /// </summary>
    /// <param name="phone">The phone number to mask.</param>
    /// <returns>The masked phone number.</returns>
    public static string MaskPhone(this string phone)
    {
        if (string.IsNullOrEmpty(phone))
            return string.Empty;

        // Remove non-digit characters for processing
        var digitsOnly = new string(phone.Where(char.IsDigit).ToArray());

        if (digitsOnly.Length <= 4)
            return phone;

        // Show only last 4 digits
        var lastFour = digitsOnly[^4..];
        var maskedLength = digitsOnly.Length - 4;

        return new string('*', maskedLength) + lastFour;
    }
}
