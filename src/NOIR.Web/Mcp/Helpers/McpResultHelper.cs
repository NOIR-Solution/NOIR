namespace NOIR.Web.Mcp.Helpers;

/// <summary>
/// Converts NOIR Result&lt;T&gt; to MCP-friendly return values.
/// The MCP SDK auto-marshals: objects → JSON TextContentBlock, strings → TextContentBlock.
/// For errors, we throw McpException which the SDK converts to isError response.
/// </summary>
public static class McpResultHelper
{
    /// <summary>
    /// Unwraps a Result&lt;T&gt;, returning the value on success or throwing on failure.
    /// The MCP SDK catches exceptions and returns them as isError responses.
    /// </summary>
    public static T Unwrap<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return result.Value;

        throw new InvalidOperationException(
            $"[{result.Error.Type}] {result.Error.Code}: {result.Error.Message}");
    }
}
