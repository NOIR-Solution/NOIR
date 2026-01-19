namespace NOIR.IntegrationTests.Infrastructure;

using System.Text.Json.Serialization;

/// <summary>
/// Provides shared JSON serializer options for integration tests.
/// Matches the API's JSON configuration which serializes enums as strings.
/// </summary>
public static class JsonSerializerHelper
{
    /// <summary>
    /// JSON serializer options matching the API configuration.
    /// Uses JsonStringEnumConverter for enum serialization as strings.
    /// </summary>
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}

/// <summary>
/// Extension methods for HTTP content deserialization in integration tests.
/// </summary>
public static class HttpContentExtensions
{
    /// <summary>
    /// Reads HTTP content as JSON using the shared serializer options.
    /// This ensures enums are deserialized correctly from string values.
    /// </summary>
    public static async Task<T?> ReadFromJsonWithEnumsAsync<T>(
        this HttpContent content,
        CancellationToken cancellationToken = default)
    {
        return await content.ReadFromJsonAsync<T>(
            JsonSerializerHelper.Options,
            cancellationToken);
    }
}
