using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace NOIR.Infrastructure.Services.Payment.Providers.VnPay;

/// <summary>
/// Helper class for VNPay HMAC-SHA512 signature generation and verification.
/// </summary>
public static class VnPaySignatureHelper
{
    /// <summary>
    /// Creates an HMAC-SHA512 signature from the given data and hash secret.
    /// </summary>
    /// <param name="rawData">The raw data to sign (query string format without vnp_SecureHash).</param>
    /// <param name="hashSecret">The VNPay hash secret key.</param>
    /// <returns>The HMAC-SHA512 signature in lowercase hexadecimal format.</returns>
    public static string CreateSignature(string rawData, string hashSecret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(hashSecret);
        var dataBytes = Encoding.UTF8.GetBytes(rawData);

        using var hmac = new HMACSHA512(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Verifies the HMAC-SHA512 signature of VNPay response data.
    /// </summary>
    /// <param name="rawData">The raw data that was signed.</param>
    /// <param name="hashSecret">The VNPay hash secret key.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public static bool VerifySignature(string rawData, string hashSecret, string signature)
    {
        var expectedSignature = CreateSignature(rawData, hashSecret);
        return string.Equals(expectedSignature, signature, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Builds the data string from parameters for signature generation.
    /// Parameters are sorted alphabetically and URL-encoded.
    /// </summary>
    /// <param name="parameters">Dictionary of parameters.</param>
    /// <returns>The raw data string for signing.</returns>
    public static string BuildDataString(SortedDictionary<string, string> parameters)
    {
        var dataBuilder = new StringBuilder();

        foreach (var (key, value) in parameters)
        {
            if (!string.IsNullOrEmpty(value))
            {
                if (dataBuilder.Length > 0)
                {
                    dataBuilder.Append('&');
                }
                dataBuilder.Append(key);
                dataBuilder.Append('=');
                dataBuilder.Append(HttpUtility.UrlEncode(value, Encoding.UTF8));
            }
        }

        return dataBuilder.ToString();
    }

    /// <summary>
    /// Parses VNPay response query string into a dictionary.
    /// </summary>
    /// <param name="queryString">The query string to parse.</param>
    /// <returns>Dictionary of parsed parameters.</returns>
    public static SortedDictionary<string, string> ParseQueryString(string queryString)
    {
        var result = new SortedDictionary<string, string>();
        var query = queryString.TrimStart('?');
        var pairs = query.Split('&');

        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = HttpUtility.UrlDecode(parts[0]);
                var value = HttpUtility.UrlDecode(parts[1]);
                result[key] = value;
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts and validates signature from VNPay response parameters.
    /// </summary>
    /// <param name="parameters">The response parameters.</param>
    /// <param name="hashSecret">The hash secret key.</param>
    /// <returns>True if signature is valid, false otherwise.</returns>
    public static bool ValidateResponseSignature(
        SortedDictionary<string, string> parameters,
        string hashSecret)
    {
        if (!parameters.TryGetValue("vnp_SecureHash", out var receivedSignature))
        {
            return false;
        }

        // Remove hash and hash type from parameters before verification
        var paramsForSignature = new SortedDictionary<string, string>(parameters);
        paramsForSignature.Remove("vnp_SecureHash");
        paramsForSignature.Remove("vnp_SecureHashType");

        var dataString = BuildDataString(paramsForSignature);
        return VerifySignature(dataString, hashSecret, receivedSignature);
    }
}
