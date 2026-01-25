using System.Security.Cryptography;
using System.Text;

namespace NOIR.Infrastructure.Services.Payment.Providers.MoMo;

/// <summary>
/// Helper class for MoMo HMAC-SHA256 signature generation and verification.
/// </summary>
public static class MoMoSignatureHelper
{
    /// <summary>
    /// Creates an HMAC-SHA256 signature from the given raw data and secret key.
    /// </summary>
    /// <param name="rawData">The raw data to sign.</param>
    /// <param name="secretKey">The MoMo secret key.</param>
    /// <returns>The HMAC-SHA256 signature in lowercase hexadecimal format.</returns>
    public static string CreateSignature(string rawData, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var dataBytes = Encoding.UTF8.GetBytes(rawData);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(dataBytes);

        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Verifies the HMAC-SHA256 signature.
    /// </summary>
    /// <param name="rawData">The raw data that was signed.</param>
    /// <param name="secretKey">The MoMo secret key.</param>
    /// <param name="signature">The signature to verify.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    public static bool VerifySignature(string rawData, string secretKey, string signature)
    {
        var expectedSignature = CreateSignature(rawData, secretKey);
        return string.Equals(expectedSignature, signature, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Builds the raw signature string for payment creation.
    /// MoMo requires a specific format: accessKey=xxx&amount=xxx&...
    /// </summary>
    public static string BuildPaymentSignatureData(
        string accessKey,
        long amount,
        string extraData,
        string ipnUrl,
        string orderId,
        string orderInfo,
        string partnerCode,
        string redirectUrl,
        string requestId,
        string requestType)
    {
        return $"accessKey={accessKey}&amount={amount}&extraData={extraData}&ipnUrl={ipnUrl}&orderId={orderId}&orderInfo={orderInfo}&partnerCode={partnerCode}&redirectUrl={redirectUrl}&requestId={requestId}&requestType={requestType}";
    }

    /// <summary>
    /// Builds the raw signature string for callback/IPN verification.
    /// </summary>
    public static string BuildCallbackSignatureData(
        string accessKey,
        long amount,
        string extraData,
        string message,
        string orderId,
        string orderInfo,
        string orderType,
        string partnerCode,
        string payType,
        string requestId,
        int responseTime,
        int resultCode,
        string transId)
    {
        return $"accessKey={accessKey}&amount={amount}&extraData={extraData}&message={message}&orderId={orderId}&orderInfo={orderInfo}&orderType={orderType}&partnerCode={partnerCode}&payType={payType}&requestId={requestId}&responseTime={responseTime}&resultCode={resultCode}&transId={transId}";
    }

    /// <summary>
    /// Builds the raw signature string for query request.
    /// </summary>
    public static string BuildQuerySignatureData(
        string accessKey,
        string orderId,
        string partnerCode,
        string requestId)
    {
        return $"accessKey={accessKey}&orderId={orderId}&partnerCode={partnerCode}&requestId={requestId}";
    }

    /// <summary>
    /// Builds the raw signature string for refund request.
    /// </summary>
    public static string BuildRefundSignatureData(
        string accessKey,
        long amount,
        string description,
        string orderId,
        string partnerCode,
        string requestId,
        long transId)
    {
        return $"accessKey={accessKey}&amount={amount}&description={description}&orderId={orderId}&partnerCode={partnerCode}&requestId={requestId}&transId={transId}";
    }
}
