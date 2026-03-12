using System.Diagnostics;
using System.Reflection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using NOIR.Web.Mcp.Filters;

namespace NOIR.Web.Mcp;

/// <summary>
/// Extension methods for registering the NOIR MCP server.
/// </summary>
public static class McpServiceRegistration
{
    /// <summary>
    /// Registers the NOIR MCP server with Streamable HTTP transport.
    /// Tools, resources, and prompts are auto-discovered from this assembly.
    /// Module gating is enforced via [RequiresModule] attribute on tool classes.
    /// All tool invocations are logged with duration, user, and success/failure.
    /// </summary>
    public static IServiceCollection AddNoirMcpServer(this IServiceCollection services)
    {
        // Build tool name → required module map once at startup via reflection
        var toolModuleMap = BuildToolModuleMap();

        services.AddMcpServer()
            .WithHttpTransport()
            .WithToolsFromAssembly()
            .WithResourcesFromAssembly()
            .WithPromptsFromAssembly()
            .WithRequestFilters(filters =>
            {
                // Primary call filter: module gating + structured telemetry
                filters.AddCallToolFilter((next) => async (context, ct) =>
                {
                    var toolName = context.Params?.Name ?? "unknown";
                    var services = context.Services!;
                    var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("NOIR.MCP");

                    // --- Module gating ---
                    if (toolModuleMap.TryGetValue(toolName, out var moduleName))
                    {
                        var featureChecker = services.GetRequiredService<IFeatureChecker>();
                        if (!await featureChecker.IsEnabledAsync(moduleName, ct))
                        {
                            logger.LogWarning("MCP tool {ToolName} blocked: module {ModuleName} is disabled for this tenant",
                                toolName, moduleName);
                            throw new McpException(
                                $"Tool '{toolName}' is unavailable: module '{moduleName}' is not enabled for this tenant.");
                        }
                    }

                    // --- Invocation telemetry ---
                    var userId = services.GetService<ICurrentUser>()?.UserId;
                    var start = Stopwatch.GetTimestamp();

                    var result = await next(context, ct);

                    var elapsedMs = (int)Stopwatch.GetElapsedTime(start).TotalMilliseconds;

                    if (result.IsError == true)
                        logger.LogWarning("MCP tool {ToolName} returned error for user {UserId} [{ElapsedMs}ms]",
                            toolName, userId, elapsedMs);
                    else
                        logger.LogInformation("MCP tool {ToolName} called by user {UserId} [{ElapsedMs}ms]",
                            toolName, userId, elapsedMs);

                    return result;
                });

                // List filter: hide tools from disabled modules
                filters.AddListToolsFilter((next) => async (context, ct) =>
                {
                    var result = await next(context, ct);
                    if (result.Tools is null or { Count: 0 }) return result;

                    var featureChecker = context.Services!.GetRequiredService<IFeatureChecker>();

                    // Pre-check each unique module once (avoids N redundant cache hits)
                    var moduleEnabled = new Dictionary<string, bool>(StringComparer.Ordinal);
                    foreach (var moduleName in toolModuleMap.Values.Distinct())
                        moduleEnabled[moduleName] = await featureChecker.IsEnabledAsync(moduleName, ct);

                    result.Tools = result.Tools
                        .Where(t => !toolModuleMap.TryGetValue(t.Name, out var m) ||
                                    moduleEnabled.GetValueOrDefault(m, true))
                        .ToList();

                    return result;
                });
            });

        return services;
    }

    /// <summary>
    /// Scans all [McpServerToolType] classes in this assembly for [RequiresModule] attributes
    /// and builds a map of tool name → required module name.
    /// </summary>
    private static Dictionary<string, string> BuildToolModuleMap()
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var type in typeof(McpServiceRegistration).Assembly.GetTypes())
        {
            var moduleAttr = type.GetCustomAttribute<RequiresModuleAttribute>();
            if (moduleAttr is null) continue;

            foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var toolAttr = method.GetCustomAttribute<McpServerToolAttribute>();
                if (toolAttr is null) continue;

                // SDK uses explicit Name if set, otherwise the method name
                var toolName = toolAttr.Name ?? method.Name;
                map[toolName] = moduleAttr.ModuleName;
            }
        }

        return map;
    }
}
