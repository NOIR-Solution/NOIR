# MCP Server Pattern

> Model Context Protocol (MCP) server embedded in `NOIR.Web`. Exposes NOIR's business operations as AI-callable tools, resources, and prompt templates.

**Implementation:** `src/NOIR.Web/Mcp/` · **Transport:** Streamable HTTP (`POST /api/mcp`) · **SDK:** `ModelContextProtocol.AspNetCore` v1.1.0

---

## Overview

The NOIR MCP server allows AI agents (Claude, GitHub Copilot, etc.) to interact with the platform using natural language. It reuses the existing API Key authentication system and respects per-tenant feature/module gating.

```
AI Agent (Claude Desktop / Claude Code)
       │  MCP Protocol (Streamable HTTP)
       ▼
POST /api/mcp   ← RequireAuthorization + RequireRateLimiting
       │
McpRequestFilters
├── AddCallToolFilter   ← Module gating + telemetry logging
└── AddListToolsFilter  ← Hides disabled-module tools from listing
       │
[McpServerToolType] classes   ← 55 tools across 12 domains
[McpServerResourceType] classes ← 7 resources (schema + entity)
[McpServerPromptType] classes   ← 6 prompt templates
```

---

## File Structure

```
src/NOIR.Web/Mcp/
├── McpServiceRegistration.cs        # DI registration + request filters + telemetry
├── Filters/
│   └── RequiresModuleAttribute.cs   # Declare required module on tool class
├── Helpers/
│   └── McpResultHelper.cs           # result.Unwrap() extension
├── Prompts/
│   └── NoirPrompts.cs               # 5 workflow prompt templates
├── Resources/
│   ├── SchemaResources.cs           # noir://schema/modules, api-overview
│   └── EntityResources.cs           # noir://orders/{id}, products, customers
└── Tools/
    ├── DashboardTools.cs            # 2 tools
    ├── SearchTools.cs               # 1 tool
    ├── OrderTools.cs                # 5 tools
    ├── ProductTools.cs              # 3 tools
    ├── CustomerTools.cs             # 4 tools
    ├── InventoryTools.cs            # 3 tools
    ├── PromotionTools.cs            # 2 tools
    ├── ReportTools.cs               # 3 tools
    ├── HrTools.cs                   # 4 tools
    ├── CrmTools.cs                  # 8 tools
    ├── PmTools.cs                   # 5 tools
    └── BlogTools.cs                 # 3 tools
```

---

## Authentication

AI agents authenticate using the existing **API Key + Secret** system (same as OpenAPI):

```http
POST /api/mcp
X-API-Key: <your-api-key>
X-API-Secret: <your-api-secret>
Content-Type: application/json
```

The API Key resolves the user, tenant, and scoped permissions automatically — no JWT needed. Create keys in **Settings → API Keys** in the Admin portal.

---

## Tool Development Guide

### Minimal tool class

```csharp
[McpServerToolType]
[RequiresModule(ModuleNames.Ecommerce.Orders)]   // Automatically gated — filter handles enforcement
public sealed class MyDomainTools(IMessageBus bus, ICurrentUser currentUser)
{
    [McpServerTool(Name = "noir_domain_action", ReadOnly = true, Idempotent = true)]
    [Description("Human-readable description used by the AI model to choose this tool.")]
    public async Task<MyDto> GetSomething(
        [Description("Parameter description for the AI")] string id,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<MyDto>>(new GetSomethingQuery(Guid.Parse(id)), ct);
        return result.Unwrap();   // Throws McpException on failure; SDK converts to isError response
    }

    [McpServerTool(Name = "noir_domain_mutate", Destructive = true)]
    [Description("Mutation that requires audit trail.")]
    public async Task<MyDto> DoSomething(string id, string reason, CancellationToken ct = default)
    {
        var command = new DoSomethingCommand(Guid.Parse(id), reason)
        {
            AuditUserId = currentUser.UserId   // Required for IAuditableCommand
        };
        var result = await bus.InvokeAsync<Result<MyDto>>(command, ct);
        return result.Unwrap();
    }
}
```

### Naming convention

| Type | Pattern | Example |
|------|---------|---------|
| Tool | `noir_{domain}_{action}` | `noir_orders_ship`, `noir_crm_leads_win` |
| Prompt | `noir_{domain}_{purpose}` | `noir_revenue_analysis` |
| Resource | `noir://{domain}/{identifier}` | `noir://orders/{orderId}` |

### Parameter types

- **GUIDs**: Accept as `string`, call `Guid.Parse(id)` — AI clients send strings, not GUIDs
- **Enums**: Accept as `string?`, parse with `Enum.TryParse<T>(value, true, out var e)` — same reason
- **Dates**: Accept as `string?` ISO 8601, parse with `DateTimeOffset.Parse()`
- **Return types**: Return domain DTOs directly — SDK auto-serializes to JSON

### Audit commands

Two patterns exist in the codebase — use whichever the command defines:

```csharp
// Pattern A — UserId (Orders, Blog)
var command = new MyCommand(...) { UserId = currentUser.UserId };

// Pattern B — AuditUserId (CRM, HR, PM, Customers)
var command = new MyCommand(...) { AuditUserId = currentUser.UserId };
```

---

## Module Gating

Apply `[RequiresModule]` to the **tool class** (not individual methods). The `AddCallToolFilter` and `AddListToolsFilter` in `McpServiceRegistration.cs` handle enforcement automatically:

- **`AddCallToolFilter`** — throws `McpException` if module is disabled for current tenant
- **`AddListToolsFilter`** — hides the tool from `tools/list` so AI agents don't see it

```csharp
[McpServerToolType]
[RequiresModule(ModuleNames.Erp.Hr)]   // ← All methods in this class are gated
public sealed class HrTools(...) { ... }
```

The module map is built at startup via reflection — no per-method code required.

---

## Prompt Templates

Prompts are instruction templates that prime the AI for a specific operational task. They appear in MCP clients as pre-built workflows.

```csharp
[McpServerPromptType]
public sealed class MyPrompts
{
    [McpServerPrompt(Name = "noir_my_workflow")]
    [Description("What this workflow does.")]
    public IEnumerable<PromptMessage> MyWorkflow(
        [Description("Optional parameter")] string? scope = null)
    {
        yield return new PromptMessage
        {
            Role = Role.User,
            Content = new TextContentBlock
            {
                Text = $"You are analyzing NOIR data. Steps: 1. Call noir_... 2. ..."
            }
        };
    }
}
```

**Return types for prompt methods:**
- `string` → single user message (simplest)
- `IEnumerable<PromptMessage>` → multi-message conversation template
- `PromptMessage` → single message

---

## Dynamic Entity Resources

Resources are addressable data endpoints that AI agents can reference directly (e.g., in messages: "read noir://orders/abc123").

```csharp
[McpServerResourceType]
public sealed class MyEntityResources(IMessageBus bus)
{
    [McpServerResource(UriTemplate = "noir://myentity/{entityId}", Name = "myentity", MimeType = "application/json")]
    [Description("Fetch live entity data by ID.")]
    public async Task<string> GetEntity(string entityId, CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<MyDto>>(new GetMyEntityQuery(Guid.Parse(entityId)), ct);
        return JsonSerializer.Serialize(result.Unwrap(), new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }
}
```

---

## SDK Gotchas

| Issue | Detail |
|-------|--------|
| `ListToolsResult` is NOT a record | Cannot use `result with { Tools = ... }`. Mutate `result.Tools` directly — it's a settable `IList<Tool>` |
| `context.Services` is nullable | Use `context.Services!.GetRequiredService<T>()` — it's always populated for authenticated requests |
| `McpRequestFilter<T,R>` delegate | `(next) => async (context, ct) => { return await next(context, ct); }` — "next" wraps the inner handler |
| String return from prompt | Returning `string` from a `[McpServerPrompt]` method creates a single `Role.User` message automatically |
| Tool naming with null `Name` | If `[McpServerTool]` has no `Name`, SDK uses the method name. Always set explicit `Name` to prevent renames breaking clients |

---

## Telemetry

Every tool call logs a structured entry via Serilog:

```
[INF] MCP tool noir_orders_list called by user {UserId} [42ms]
[WRN] MCP tool noir_hr_employees_list blocked: module erp.hr is disabled for this tenant
[WRN] MCP tool noir_orders_cancel returned error for user {UserId} [18ms]
```

Category: `NOIR.MCP` — filterable in log sinks.

---

## Available Tools Reference

| Domain | Tools | Module Required |
|--------|-------|----------------|
| Dashboard | `noir_dashboard_core`, `noir_dashboard_ecommerce` | — |
| Search | `noir_search_global` | — |
| Orders | `noir_orders_list`, `_get`, `_confirm`, `_ship`, `_cancel` | `ecommerce.orders` |
| Products | `noir_products_list`, `_get`, `_search_variants` | `ecommerce.products` |
| Customers | `noir_customers_list`, `_get`, `_orders`, `_stats` | `ecommerce.customers` |
| Inventory | `noir_inventory_receipts_list`, `_receipts_get`, `_dashboard` | `ecommerce.inventory` |
| Promotions | `noir_promotions_list`, `_get` | `ecommerce.promotions` |
| Reports | `noir_reports_revenue`, `_best_sellers`, `_inventory` | `analytics.reports` |
| HR | `noir_hr_employees_list`, `_employees_get`, `_departments_list`, `_reports` | `erp.hr` |
| CRM | `noir_crm_contacts_list`, `_contacts_get`, `_leads_list`, `_leads_get`, `_leads_move`, `_leads_win`, `_leads_lose`, `_dashboard` | `erp.crm` |
| PM | `noir_pm_projects_list`, `_projects_get`, `_tasks_list`, `_tasks_create`, `_tasks_update` | `erp.pm` |
| Blog | `noir_blog_posts_list`, `_posts_get`, `_posts_publish` | `content.blog` |

## Available Resources

| URI | Description |
|-----|-------------|
| `noir://schema/modules` | All modules with enabled/disabled state for current tenant |
| `noir://schema/api-overview` | API endpoint groups and authentication methods |
| `noir://orders/{orderId}` | Live order detail JSON |
| `noir://products/{productId}` | Live product detail JSON |
| `noir://customers/{customerId}` | Live customer profile JSON |

## Available Prompts

| Name | Purpose |
|------|---------|
| `noir_analyze_orders` | Order pipeline review with bottleneck identification |
| `noir_revenue_analysis` | Revenue insights with period comparison |
| `noir_inventory_health_check` | Low-stock and stale inventory audit |
| `noir_crm_pipeline_review` | CRM leads requiring follow-up |
| `noir_hr_team_briefing` | Org structure and headcount summary |
