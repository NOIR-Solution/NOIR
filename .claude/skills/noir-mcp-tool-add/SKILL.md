---
name: noir-mcp-tool-add
description: Scaffold a new MCP tool class in `src/NOIR.Web/Mcp/Tools/` following NOIR conventions — `noir_{domain}_{action}` naming, `[RequiresModule]` gate, string-for-Guid parameters. Use when the user asks to add, expose, or create an MCP tool, or when AI agents need access to an existing feature that isn't yet exposed. Covers CLAUDE.md Rules 25-30 (MCP Server).
---

# noir-mcp-tool-add — Expose a NOIR feature to AI agents

MCP tools let AI agents (Claude Desktop, Claude Code, etc.) invoke NOIR features. They differ from HTTP endpoints in three important ways: they're called by LLMs (not typed SDK clients), they must be gated by feature flags, and their naming is part of the discoverability contract.

## Prerequisites

- The underlying Command/Query handler already exists in `src/NOIR.Application/Features/{Feature}/` — MCP tools are thin wrappers, not new business logic
- The feature has a module entry in `ModuleNames.cs` (required for `[RequiresModule]`)

If either is missing, invoke `noir-feature-add` first.

## Inputs to collect

1. **Feature name / domain**: `Products`, `Orders`, `Promotions`, `HrTags`, etc. One tool class per feature.
2. **Module path**: `ModuleNames.Ecommerce.Promotions`, `ModuleNames.Hr.Employees`, etc.
3. **Operations to expose**: list, get, create, update, delete, or domain-specific (`ship`, `cancel`, `win`, `assign`)
4. **Which existing queries/commands to wrap** — read them first to match parameter types

## Naming conventions (Rule 25)

`noir_{domain}_{action}` — lowercase, snake_case. Examples:
- `noir_products_list`, `noir_products_get`, `noir_products_create`
- `noir_orders_ship`, `noir_orders_cancel`
- `noir_crm_leads_win`
- `noir_hr_employees_assign_tag`

**Always** set explicit `Name` in `[McpServerTool(Name = "...")]`. Never rely on method-name default (Rule 25).

## Canonical class structure

Reference: `src/NOIR.Web/Mcp/Tools/PromotionTools.cs` (simple), `OrderTools.cs` (complex with commands).

```csharp
using System.ComponentModel;
using ModelContextProtocol.Server;
using NOIR.Application.Features.Promotions.DTOs;
using NOIR.Application.Features.Promotions.Queries.GetPromotions;
using NOIR.Application.Features.Promotions.Queries.GetPromotionById;
using NOIR.Application.Features.Promotions.Commands.CreatePromotion;
using NOIR.Web.Mcp.Filters;
using NOIR.Web.Mcp.Helpers;

namespace NOIR.Web.Mcp.Tools;

/// <summary>
/// MCP tools for promotion/discount code management.
/// </summary>
[McpServerToolType]
[RequiresModule(ModuleNames.Ecommerce.Promotions)]
public sealed class PromotionTools(IMessageBus bus)
{
    [McpServerTool(Name = "noir_promotions_list", ReadOnly = true, Idempotent = true)]
    [Description("List promotions with pagination and filtering. Supports search, status, type, and date range filters.")]
    public async Task<PagedResult<PromotionDto>> ListPromotions(
        [Description("Search by promotion name or code")] string? search = null,
        [Description("Filter by status: Draft, Active, Scheduled, Expired, Cancelled")] string? status = null,
        [Description("Page number (default: 1)")] int page = 1,
        [Description("Page size, max 100 (default: 20)")] int pageSize = 20,
        CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 100);
        var promoStatus = status is not null && Enum.TryParse<PromotionStatus>(status, true, out var s) ? s : (PromotionStatus?)null;

        var result = await bus.InvokeAsync<Result<PagedResult<PromotionDto>>>(
            new GetPromotionsQuery(page, pageSize, search, promoStatus), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_promotions_get", ReadOnly = true, Idempotent = true)]
    [Description("Get full promotion details by ID.")]
    public async Task<PromotionDto> GetPromotion(
        [Description("The promotion ID (GUID)")] string promotionId,
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<PromotionDto>>(
            new GetPromotionByIdQuery(Guid.Parse(promotionId)), ct);
        return result.Unwrap();
    }

    [McpServerTool(Name = "noir_promotions_create")]
    [Description("Create a new promotion. Returns the created promotion ID.")]
    public async Task<Guid> CreatePromotion(
        [Description("Promotion name")] string name,
        [Description("Discount code (unique)")] string code,
        // ... other fields
        CancellationToken ct = default)
    {
        var result = await bus.InvokeAsync<Result<Guid>>(
            new CreatePromotionCommand(name, code, /* ... */) { AuditUserId = /* see below */ }, ct);
        return result.Unwrap();
    }
}
```

## Critical rules (CLAUDE.md 25-30)

1. **`[RequiresModule]` on the CLASS** (Rule 26) — the filter in `McpServiceRegistration.cs` enforces it globally. Never add per-method checks.

2. **Strings for GUIDs and enums** (Rule 27) — AI clients send JSON strings. Parameters must be:
   - `string entityId` + `Guid.Parse(entityId)` in the body
   - `string? status` + `Enum.TryParse<TStatus>(status, true, out var s)` in the body
   - Never `Guid entityId` or `MyEnum status` directly in the signature

3. **`ListToolsResult` is NOT a record** (Rule 28) — `result with { Tools = ... }` fails. Mutate `result.Tools` directly; it's a settable `IList<Tool>`.

4. **Audit ID field name** (Rule 29) — **check the command** before writing the tool call. Ecommerce (Orders, Blog) uses `UserId`. CRM, HR, PM, Customers use `AuditUserId`. Mixing them silently compiles (both are Guid?) but produces NULL audit entries:
   ```bash
   grep -n "UserId\|AuditUserId" src/NOIR.Application/Features/Promotions/Commands/CreatePromotion/CreatePromotionCommand.cs
   ```

5. **Discoverability** — `[Description]` on the tool AND each parameter. LLMs read these to decide when to call the tool. Be precise about:
   - Status / type enum values (list them)
   - Date format (always "ISO 8601")
   - GUID fields (say "(GUID)")
   - Defaults and limits

6. **`ReadOnly = true, Idempotent = true`** on queries — enables client-side caching and retry safety.

## Tool annotations cheat sheet

| Tool kind | Attributes | Why |
|---|---|---|
| List / Get (pure read) | `ReadOnly = true, Idempotent = true` | Cacheable, retry-safe |
| Create / Update / Delete (mutation) | (no flags) | Mutates, not safe to retry without dedup |
| Idempotent mutation (e.g. SetFlag to value) | `Idempotent = true` | Retry-safe, still mutates |
| Long-running (`import`, `bulk`) | `Destructive = false` (if non-destructive) | Tool UI hints |

## Registering the class

`McpServiceRegistration.cs` auto-discovers types with `[McpServerToolType]` — no explicit registration needed. Just place the file in `src/NOIR.Web/Mcp/Tools/` and build.

Verify:
```bash
dotnet run --project src/NOIR.Web
# In another terminal:
curl http://localhost:4000/api/mcp/tools/list | jq '.tools[] | select(.name | startswith("noir_promotions_"))'
```

## After any API change (Rule 30)

When modifying a Command/Query constructor or adding a new capability:
```bash
grep -r "new YourCommand\|new YourQuery" src/NOIR.Web/Mcp/
```
If results appear, the MCP tool references that type — update the tool invocation to match.

## Common mistakes this skill prevents

- Method name `GetProducts` leaking as MCP tool name `GetProducts` instead of `noir_products_list` (Rule 25)
- `Guid productId` parameter → AI client sends string → validation error (Rule 27)
- `ProductStatus status` parameter → same failure (Rule 27)
- Forgetting `[RequiresModule]` → tool available even when feature is disabled per tenant (Rule 26)
- Per-method feature check instead of class-level → duplicated, forgotten on new methods
- Using `UserId` when the command expects `AuditUserId` (or vice versa) → NULL in audit log (Rule 29)
- Missing `[Description]` → LLMs can't tell when to call the tool
- `Math.Clamp(pageSize, 1, 100)` omitted → LLM asks for 10000, backend OOMs
- Mutating `ListToolsResult` with `with { ... }` → compile error (Rule 28)
- Adding a new command without checking if an existing MCP tool needs updating (Rule 30)
