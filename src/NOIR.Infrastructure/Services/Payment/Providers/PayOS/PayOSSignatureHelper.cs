using System.Security.Cryptography;
using System.Text;

namespace NOIR.Infrastructure.Services.Payment.Providers.PayOS;

/// <summary>
/// Helper class for PayOS signature generation and verification.
/// </summary>
public static class PayOSSignatureHelper
{
    /// <summary>
    /// Creates a signature for PayOS request.
    /// PayOS uses HMAC-SHA256 with sorted data string.
    /// </summary>
    public static string CreateSignature(SortedDictionary<string, string> data, string checksumKey)
    {
        var dataString = BuildDataString(data);
        return ComputeHmacSha256(dataString, checksumKey);
    }

    /// <summary>
    /// Verifies a PayOS webhook signature.
    /// </summary>
    public static bool VerifySignature(SortedDictionary<string, string> data, string signature, string checksumKey)
    {
        var expectedSignature = CreateSignature(data, checksumKey);
        return string.Equals(expectedSignature, signature, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Builds the data string for signature from sorted parameters.
    /// </summary>
    public static string BuildDataString(SortedDictionary<string, string> data)
    {
        var sb = new StringBuilder();
        foreach (var kvp in data)
        {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
                if (sb.Length > 0)
                {
                    sb.Append('&');
                }
                sb.Append($"{kvp.Key}={kvp.Value}");
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Computes HMAC-SHA256 signature.
    /// </summary>
    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Parses query string into sorted dictionary.
    /// </summary>
    public static SortedDictionary<string, string> ParseQueryString(string query)
    {
        var result = new SortedDictionary<string, string>(StringComparer.Ordinal);

        if (string.IsNullOrEmpty(query))
        {
            return result;
        }

        // Remove leading '?' if present
        if (query.StartsWith('?'))
        {
            query = query[1..];
        }

        var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var keyValue = pair.Split('=', 2);
            if (keyValue.Length == 2)
            {
                var key = Uri.UnescapeDataString(keyValue[0]);
                var value = Uri.UnescapeDataString(keyValue[1]);
                result[key] = value;
            }
        }

        return result;
    }
}
