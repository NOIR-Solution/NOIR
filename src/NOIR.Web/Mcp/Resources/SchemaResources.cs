using System.ComponentModel;
using ModelContextProtocol.Server;

namespace NOIR.Web.Mcp.Resources;

/// <summary>
/// MCP resources providing schema and configuration information.
/// Resources are read-only data that AI agents can reference for context.
/// </summary>
[McpServerResourceType]
public sealed class SchemaResources(
    IEnumerable<IModuleDefinition> moduleDefinitions,
    IFeatureChecker featureChecker)
{
    [McpServerResource(UriTemplate = "noir://schema/modules", Name = "modules", MimeType = "application/json")]
    [Description("List of all NOIR modules with their effective enabled/disabled state for the current tenant.")]
    public async Task<string> GetModules(CancellationToken ct = default)
    {
        var moduleStates = new List<object>();

        foreach (var module in moduleDefinitions.OrderBy(m => m.SortOrder))
        {
            var isEnabled = await featureChecker.IsEnabledAsync(module.Name, ct);
            moduleStates.Add(new
            {
                name = module.Name,
                displayNameKey = module.DisplayNameKey,
                icon = module.Icon,
                isCore = module.IsCore,
                isEnabled
            });
        }

        return JsonSerializer.Serialize(new { modules = moduleStates }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    [McpServerResource(UriTemplate = "noir://schema/api-overview", Name = "api-overview", MimeType = "application/json")]
    [Description("Overview of the NOIR API including available endpoint groups, authentication methods, and base URL.")]
    public string GetApiOverview()
    {
        var overview = new
        {
            name = "NOIR API",
            version = "v1",
            baseUrl = "/api",
            authentication = new[]
            {
                new { method = "API Key + Secret", headers = new[] { "X-API-Key", "X-API-Secret" }, description = "Recommended for MCP. Auto-resolves user, tenant, and scoped permissions." },
                new { method = "JWT Bearer", headers = new[] { "Authorization: Bearer <token>" }, description = "Standard JWT authentication." }
            },
            endpointGroups = new[]
            {
                new { group = "Dashboard", tags = new[] { "Dashboard" } },
                new { group = "Orders & Fulfillment", tags = new[] { "Orders", "Payments", "Shipping", "Inventory", "Cart", "Checkout" } },
                new { group = "Customers", tags = new[] { "Customers", "Customer Groups", "Reviews", "Wishlists" } },
                new { group = "Catalog", tags = new[] { "Products", "Product Categories", "Product Attributes", "Brands", "Media" } },
                new { group = "Human Resources", tags = new[] { "HR - Employees", "HR - Departments", "HR - Tags" } },
                new { group = "Project Management", tags = new[] { "PM - Projects", "PM - Tasks" } },
                new { group = "CRM", tags = new[] { "CRM - Contacts", "CRM - Companies", "CRM - Leads", "CRM - Pipelines", "CRM - Activities" } },
                new { group = "Content", tags = new[] { "Blog Posts", "Blog Categories", "Legal Pages" } },
                new { group = "Users & Access", tags = new[] { "Authentication", "Users", "Roles", "Permissions", "API Keys" } },
                new { group = "Settings", tags = new[] { "Platform Settings", "Tenant Settings", "Feature Management", "Email Templates", "Webhooks" } },
                new { group = "System", tags = new[] { "Search", "Audit", "Notifications", "SSE", "Developer Logs" } }
            }
        };

        return JsonSerializer.Serialize(overview, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }
}
