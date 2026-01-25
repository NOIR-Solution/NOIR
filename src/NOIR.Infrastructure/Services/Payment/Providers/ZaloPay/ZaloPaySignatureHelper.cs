using System.Security.Cryptography;
using System.Text;

namespace NOIR.Infrastructure.Services.Payment.Providers.ZaloPay;

/// <summary>
/// Helper class for ZaloPay HMAC-SHA256 signature (MAC) generation and verification.
/// </summary>
public static class ZaloPaySignatureHelper
{
    /// <summary>
    /// Creates an HMAC-SHA256 signature (MAC) from the given data and key.
    /// </summary>
    /// <param name="rawData">The raw data to sign.</param>
    /// <param name="key">The ZaloPay key.</param>
    /// <returns>The HMAC-SHA256 signature in lowercase hexadecimal format.</returns>
    public static string CreateMac(string rawData, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(rawData);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Verifies the HMAC-SHA256 signature (MAC).
    /// </summary>
    /// <param name="rawData">The raw data that was signed.</param>
    /// <param name="key">The ZaloPay key.</param>
    /// <param name="mac">The MAC to verify.</param>
    /// <returns>True if the MAC is valid, false otherwise.</returns>
    public static bool VerifyMac(string rawData, string key, string mac)
    {
        var expectedMac = CreateMac(rawData, key);
        return string.Equals(expectedMac, mac, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Builds the raw data string for order creation MAC.
    /// Format: app_id|app_trans_id|app_user|amount|app_time|embed_data|item
    /// </summary>
    public static string BuildOrderMacData(
        string appId,
        string appTransId,
        string appUser,
        long amount,
        long appTime,
        string embedData,
        string item)
    {
        return $"{appId}|{appTransId}|{appUser}|{amount}|{appTime}|{embedData}|{item}";
    }

    /// <summary>
    /// Builds the raw data string for callback MAC verification.
    /// Format: data + mac (uses Key2)
    /// </summary>
    public static string BuildCallbackMacData(string data)
    {
        return data;
    }

    /// <summary>
    /// Builds the raw data string for query MAC.
    /// Format: app_id|app_trans_id|key1
    /// </summary>
    public static string BuildQueryMacData(string appId, string appTransId, string key1)
    {
        return $"{appId}|{appTransId}|{key1}";
    }

    /// <summary>
    /// Builds the raw data string for refund MAC.
    /// Format: app_id|zp_trans_id|amount|description|timestamp
    /// </summary>
    public static string BuildRefundMacData(
        string appId,
        string zpTransId,
        long amount,
        string description,
        long timestamp)
    {
        return $"{appId}|{zpTransId}|{amount}|{description}|{timestamp}";
    }

    /// <summary>
    /// Generates the app_trans_id in ZaloPay format.
    /// Format: yyMMdd_uniqueId
    /// </summary>
    public static string GenerateAppTransId(string transactionNumber)
    {
        var datePrefix = DateTime.UtcNow.AddHours(7).ToString("yyMMdd");
        return $"{datePrefix}_{transactionNumber}";
    }

    /// <summary>
    /// Gets the current timestamp in milliseconds.
    /// </summary>
    public static long GetTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
