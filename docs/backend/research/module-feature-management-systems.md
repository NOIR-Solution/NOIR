# Module & Feature Management Systems: Comprehensive Research

> Research Date: 2026-02-21
> Author: Claude Research Agent
> Purpose: Inform NOIR's module/feature management system design

---

## Executive Summary

This report compares how the leading open-source multi-tenant SaaS platforms implement module/feature management. The central question: *how do mature systems gate UI, API endpoints, background jobs, and seeders per tenant?*

**Key takeaway:** ABP Framework is the most directly relevant reference. Its provider-chain pattern (DefaultValue → Configuration → Edition → Tenant) cleanly maps to NOIR's existing architecture. Microsoft.FeatureManagement is the right primitive for runtime checks. OrchardCore's shell descriptor model is over-engineered for NOIR's use case.

---

## Table of Contents

1. [ABP Framework — Module System & Feature Management](#1-abp-framework)
2. [OrchardCore — Module/Feature Management](#2-orchardcore)
3. [Microsoft.FeatureManagement — .NET Native Feature Flags](#3-microsoftfeaturemanagement)
4. [Flagsmith & Unleash — Feature Flag Platforms](#4-flagsmith--unleash)
5. [Architectural Comparison Matrix](#5-architectural-comparison-matrix)
6. [Recommendations for NOIR](#6-recommendations-for-noir)
7. [Proposed NOIR Data Model](#7-proposed-noir-data-model)
8. [Integration Patterns (Middleware, Endpoints, Background Jobs, Seeders)](#8-integration-patterns)
9. [Research Gaps & Limitations](#9-research-gaps--limitations)
10. [Sources](#10-sources)

---

## 1. ABP Framework

### 1.1 Module System

ABP modules are the coarsest unit of organization. Every module:

- Derives from `AbpModule`
- Declares dependencies via `[DependsOn(...)]`
- Uses lifecycle hooks to register services and middleware

```csharp
[DependsOn(
    typeof(AbpAspNetCoreMvcModule),
    typeof(AbpFeatureManagementModule)
)]
public class ReportingModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        // Register services, configure options
        Configure<AbpFeatureOptions>(options =>
        {
            options.ValueProviders.Add<CustomFeatureValueProvider>();
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var app = context.GetApplicationBuilder();
        app.UseMiddleware<ReportingMiddleware>(); // Per-module middleware
    }
}
```

ABP resolves the full dependency graph at startup and initializes modules in topological order. This means:
- A module never initializes before its dependencies
- Shutdown runs in reverse order
- Modules declare `PreConfigureServices` / `PostConfigureServices` for ordered setup across dependency chains

**Module Categories:**
- **Framework modules**: Infrastructure (caching, email, validation, EF Core)
- **Application modules**: Business features (Identity, Tenant Management, Feature Management)

### 1.2 Feature Definition

Features are declared by implementing `FeatureDefinitionProvider`, which runs once at startup to register the feature catalog:

```csharp
public class ReportingFeatureDefinitionProvider : FeatureDefinitionProvider
{
    public override void Define(IFeatureDefinitionContext context)
    {
        var group = context.AddGroup(
            "Reporting",
            L("Features:Reporting")
        );

        group.AddFeature(
            "Reporting.PdfExport",
            defaultValue: "false",
            displayName: L("Features:PdfExport"),
            description: L("Features:PdfExportDescription"),
            valueType: new ToggleStringValueType()
        );

        group.AddFeature(
            "Reporting.MaxExportRows",
            defaultValue: "1000",
            displayName: L("Features:MaxExportRows"),
            valueType: new FreeTextStringValueType(
                new NumericValueValidator(min: 0, max: 100000)
            )
        );
    }
}
```

**Value types:**
- `ToggleStringValueType` — boolean on/off
- `FreeTextStringValueType` — arbitrary string (with optional validator)
- `SelectionStringValueType` — enum-like pick from list

**Feature hierarchies:** Child features can be made conditional on a parent feature being enabled. This supports "you must have the base plan to access advanced sub-features."

### 1.3 Provider Chain (Runtime Resolution)

When code calls `IFeatureChecker.IsEnabledAsync("Reporting.PdfExport")`, ABP walks a **provider chain** in priority order, stopping at the first non-null result:

```
Priority 1: TenantFeatureValueProvider    (checks AbpFeatureValues for ProviderName='T')
Priority 2: EditionFeatureValueProvider   (checks AbpFeatureValues for ProviderName='E')
Priority 3: ConfigurationFeatureValueProvider  (reads IConfiguration / appsettings.json)
Priority 4: DefaultValueFeatureValueProvider   (returns feature.DefaultValue)
```

This is a clean **chain of responsibility** pattern. The tenant override wins if set; otherwise falls back to edition, then config, then the feature definition's default.

**Custom providers** extend this chain:

```csharp
public class TrialPlanFeatureValueProvider : FeatureValueProvider
{
    public override string Name => "Trial";

    public override async Task<string> GetOrNullAsync(FeatureDefinition feature)
    {
        // Return "false" for premium features during trial
        if (_currentTenant.IsAvailable && IsTrialTenant())
            return feature.Name.StartsWith("Premium.") ? "false" : null;
        return null;
    }
}

// Registration in module
Configure<AbpFeatureOptions>(options =>
    options.ValueProviders.Add<TrialPlanFeatureValueProvider>()
);
```

### 1.4 Data Model

**Table: `AbpFeatureValues`**

| Column | Type | Description |
|--------|------|-------------|
| `Id` | Guid | Primary key |
| `Name` | string(128) | Feature name (e.g., `Reporting.PdfExport`) |
| `Value` | string(128) | The value (e.g., `"true"`, `"1000"`) |
| `ProviderName` | string(64) | `"T"` = Tenant, `"E"` = Edition |
| `ProviderKey` | string(64) | TenantId or EditionId as string |

**Supporting tables for the dynamic feature catalog (module `FeatureManagement.Application`):**

| Table | Purpose |
|-------|---------|
| `AbpFeatureGroups` | Feature grouping for UI |
| `AbpFeatures` | Dynamic feature definitions (used when not code-defined) |

The code-defined features (via `FeatureDefinitionProvider`) are **not** stored in the DB — they live in memory. Only the *values* (overrides per tenant/edition) are stored in `AbpFeatureValues`.

### 1.5 Runtime Checks

**Declarative (method/class attribute):**
```csharp
[RequiresFeature("Reporting.PdfExport")]
public async Task<PdfResult> ExportAsync(ExportRequest request)
{
    // ABP's AOP interception will throw FeatureNotAvailableException if disabled
}

// Multiple features — require ALL:
[RequiresFeature("ModuleA.Feature1", "ModuleA.Feature2", RequiresAll = true)]
public async Task DoComplexThingAsync() { }
```

**Works via ABP's DI interception.** Constraints:
- Method must be `async` (returns `Task` or `Task<T>`)
- Class must be resolved via DI (not `new`)
- For abstract/interface methods: attribute on interface method
- For non-interface methods: method must be `virtual`

**Programmatic check:**
```csharp
public class ReportingService : IReportingService, IScopedService
{
    private readonly IFeatureChecker _featureChecker;

    public async Task<IActionResult> GetPdfAsync()
    {
        if (!await _featureChecker.IsEnabledAsync("Reporting.PdfExport"))
            throw new AbpAuthorizationException("PDF export not available");

        var limit = await _featureChecker.GetAsync<int>("Reporting.MaxExportRows");
        // ...
    }
}
```

### 1.6 Background Jobs & Workers

ABP's background workers do NOT automatically run per-tenant. The pattern is:

```csharp
public class ScheduledReportWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ICurrentTenant _currentTenant;
    private readonly IFeatureChecker _featureChecker;
    private readonly ITenantRepository _tenantRepository;

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var tenants = await _tenantRepository.GetListAsync(isActive: true);

        foreach (var tenant in tenants)
        {
            // Create isolated scope per tenant
            using (var scope = workerContext.ServiceProvider.CreateScope())
            using (_currentTenant.Change(tenant.Id))
            {
                var featureChecker = scope.ServiceProvider
                    .GetRequiredService<IFeatureChecker>();

                if (!await featureChecker.IsEnabledAsync("Reporting.ScheduledExport"))
                    continue;

                // Execute tenant-scoped work
                await scope.ServiceProvider
                    .GetRequiredService<IReportingService>()
                    .RunScheduledExportAsync();
            }
        }
    }
}
```

**Key:** The `ICurrentTenant.Change(tenantId)` call switches the ambient tenant context for the current async flow. All services resolved after this point (from the scoped container) will see the correct TenantId.

For **edition-based features**, you also need to push the EditionId claim:

```csharp
using (_currentTenant.Change(tenant.Id))
using (_currentPrincipalAccessor.Change(
    new Claim(AbpClaimTypes.EditionId, tenant.EditionId?.ToString())))
{
    var isEnabled = await featureChecker.IsEnabledAsync("Premium.FeatureX");
}
```

### 1.7 Data Seeders

ABP seeders run per-tenant by convention when tenants are created. They implement `IDataSeedContributor`:

```csharp
public class ReportingDataSeeder : IDataSeedContributor, IScopedService
{
    private readonly IFeatureChecker _featureChecker;

    public async Task SeedAsync(DataSeedContext context)
    {
        // context.TenantId is set when seeding for a specific tenant
        if (context.TenantId.HasValue)
        {
            using (_currentTenant.Change(context.TenantId))
            {
                if (await _featureChecker.IsEnabledAsync("Reporting.DefaultTemplates"))
                {
                    await SeedDefaultTemplatesAsync();
                }
            }
        }
    }
}
```

### 1.8 Client-Side Feature Exposure

Feature values flow to the browser via ABP's **Application Configuration API** (`/api/abp/application-configuration`). The JSON response includes:

```json
{
  "features": {
    "values": {
      "Reporting.PdfExport": "true",
      "Reporting.MaxExportRows": "5000"
    }
  }
}
```

Setting `IsVisibleToClients: false` on a feature definition excludes it from this response (useful for backend-only flags). In Angular/React, the feature service checks this cached configuration:

```typescript
// Angular
this.featureService.isEnabled('Reporting.PdfExport') // synchronous, cached

// React (ABP Lepton X theme)
const isPdfEnabled = useFeatures('Reporting.PdfExport');
```

### 1.9 ABP Pros/Cons for NOIR

**Pros:**
- Provider chain is elegant and extensible — NOIR can add `SubscriptionPlanFeatureProvider` without changing existing code
- `[RequiresFeature]` declarative attribute integrates cleanly with CQRS handlers via DI interception
- Background worker tenant-iteration pattern is well-documented
- `AbpFeatureValues` table is minimal and queryable (one row per tenant/feature override)

**Cons:**
- Module-level gating is compile-time (DependsOn), not runtime. ABP modules cannot be "disabled" per tenant at the module level — only individual features within modules
- ABP's full `IAbpModule` lifecycle is heavyweight. NOIR already has its own Application/Infrastructure structure; importing the full ABP module system would be invasive
- The Edition provider adds complexity NOIR may not need initially

---

## 2. OrchardCore

### 2.1 Architecture Overview

OrchardCore is a CMS framework where **each tenant runs as a separate "shell"** with its own DI container and middleware pipeline. This is fundamentally different from ABP's approach:

- **ABP**: Single DI container, feature checks are runtime value lookups
- **OrchardCore**: Per-tenant DI containers, module enable/disable changes which services are registered

This makes OrchardCore's model more powerful for true module isolation but far more complex to implement.

### 2.2 Shell Descriptors

A "shell" = one tenant. Each shell has a **ShellDescriptor** — a list of enabled module/feature IDs stored in `App_Data/Sites/{TenantName}/settings.json`:

```json
{
  "ShellDescriptor": {
    "SerialNumber": 3,
    "Features": [
      { "Id": "OrchardCore.Contents" },
      { "Id": "OrchardCore.Users" },
      { "Id": "MyApp.Reporting" },
      { "Id": "MyApp.PdfExport" }
    ]
  }
}
```

When a feature is enabled/disabled, the ShellDescriptor is updated, the shell is **restarted** (new DI container built), and only the registered services for enabled features are available.

### 2.3 Feature Profiles (Tenant-Level Control)

The **Tenant Feature Profiles** system (added in OrchardCore 1.5) lets the default/host tenant control which features subordinate tenants can enable:

- Profiles are defined on the host with Include/Exclude rules
- Rules support wildcards: `MyApp.*`, `!MyApp.PremiumFeature`
- Tenants are assigned a profile at creation time
- This is a **whitelist/blacklist** for what a tenant admin can toggle — it does NOT directly toggle features on a tenant

```json
// Recipe step to define a profile
{
  "name": "FeatureProfiles",
  "FeatureProfiles": {
    "BasicPlan": {
      "FeatureRules": [
        { "Rule": "Include", "Expression": "MyApp.Basic*" },
        { "Rule": "Exclude", "Expression": "MyApp.Premium*" }
      ]
    }
  }
}
```

### 2.4 Data Model

OrchardCore uses **two storage strategies** selectable at deployment:

1. **File-based (default)**: `App_Data/Sites/{TenantName}/settings.json` per tenant
2. **Database-based**: `OrchardCore_Shells_Database` table — stores all tenant settings including feature lists as serialized JSON

The shell descriptor (enabled features list) is stored alongside tenant settings, not in a normalized relational table.

### 2.5 IShellFeaturesManager

The programmatic API for enabling/disabling features per tenant:

```csharp
// Enable a feature for a specific tenant shell
await _shellFeaturesManager.EnableFeaturesAsync(
    shellContext,
    new[] { featureInfo }
);

// This triggers a shell restart — DI container is rebuilt
```

Enabling a feature in OrchardCore is **not a runtime value check** — it physically changes which services are registered in the DI container, requiring a shell reload.

### 2.6 Module Manifest

Features are declared in a module's `Manifest.cs`:

```csharp
[assembly: Module(
    Name = "PDF Export",
    Author = "MyApp",
    Version = "1.0.0",
    Category = "Reporting",
    Description = "Export reports to PDF",
    Dependencies = new[] { "MyApp.Reporting" }
)]

[assembly: Feature(
    Id = "MyApp.PdfExport",
    Name = "PDF Export",
    Description = "...",
    Dependencies = new[] { "MyApp.Reporting" }
)]
```

### 2.7 OrchardCore Pros/Cons for NOIR

**Pros:**
- True module isolation — disabled modules have zero code path impact (services not registered)
- Shell restart means no stale state issues
- Feature profiles give fine-grained control of what tenants can self-enable

**Cons:**
- **Shell restart on feature toggle** is expensive and disruptive — unsuitable for high-availability or frequent feature changes
- Per-tenant DI containers are architecturally incompatible with NOIR's single-container design
- Finbuckle (NOIR's multi-tenancy library) + shared DbContext model cannot work with OrchardCore's shell paradigm without a full architectural rewrite
- File-based storage creates operational complexity (needs distributed lock in multi-node deployments)
- Not compatible with CQRS/MediatR patterns — OrchardCore's event system is its own pipeline

**Verdict for NOIR:** The OrchardCore model is reference architecture for *what full module isolation looks like*, but adopting it for NOIR would require rewriting the entire tenant isolation layer.

---

## 3. Microsoft.FeatureManagement

### 3.1 Core Interfaces

```csharp
// Check if a feature is enabled
public interface IVariantFeatureManager
{
    Task<bool> IsEnabledAsync(string feature);
    Task<bool> IsEnabledAsync<TContext>(string feature, TContext appContext);
    IAsyncEnumerable<string> GetEnabledFeaturesAsync();
    Task<Variant> GetVariantAsync(string feature, CancellationToken cancellationToken);
}

// Provide feature definitions (source of truth for what features exist)
public interface IFeatureDefinitionProvider
{
    Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName);
    IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync();
}

// Evaluate a feature filter (custom conditions)
public interface IFeatureFilter
{
    Task<bool> EvaluateAsync(FeatureFilterEvaluationContext context);
}

// Context-aware filter (receives caller-provided context)
public interface IContextualFeatureFilter<TContext> : IFeatureFilter
{
    Task<bool> EvaluateAsync(FeatureFilterEvaluationContext featureEvaluationContext, TContext appContext);
}
```

### 3.2 Custom Provider for Multi-Tenancy

The key integration point is `IFeatureDefinitionProvider` + `IContextualFeatureFilter<TTenantContext>`:

```csharp
// Step 1: Custom definition provider reads from database
public class DatabaseFeatureDefinitionProvider : IFeatureDefinitionProvider
{
    private readonly IServiceProvider _serviceProvider;

    public async Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName)
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFeatureRepository>();
        var feature = await repo.FindByNameAsync(featureName);
        return feature?.ToFeatureDefinition();
    }

    public async IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IFeatureRepository>();
        var features = await repo.GetAllAsync();
        foreach (var f in features)
            yield return f.ToFeatureDefinition();
    }
}

// Step 2: Contextual filter checks tenant-specific override
[FilterAlias("TenantFeature")]
public class TenantFeatureFilter : IContextualFeatureFilter<ITenantContext>
{
    private readonly ITenantFeatureRepository _repo;

    public async Task<bool> EvaluateAsync(
        FeatureFilterEvaluationContext context,
        ITenantContext tenantCtx)
    {
        var override = await _repo.GetAsync(tenantCtx.TenantId, context.FeatureName);
        return override?.IsEnabled ?? false;
    }
}

// Step 3: Registration (scoped to enable per-request DI access)
services.AddSingleton<IFeatureDefinitionProvider, DatabaseFeatureDefinitionProvider>()
        .AddScopedFeatureManagement()
        .AddFeatureFilter<TenantFeatureFilter>();

// Step 4: Usage
if (await featureManager.IsEnabledAsync("Reporting.PdfExport", tenantContext))
{
    // ...
}
```

### 3.3 ASP.NET Core Integration Points

```csharp
// Gate entire controller
[FeatureGate("Reporting.PdfExport")]
public class PdfController : ControllerBase { }

// Gate single action
[FeatureGate("Reporting.PdfExport")]
public IActionResult ExportPdf() { }

// Gate middleware conditionally
app.UseMiddlewareForFeature<PdfProcessingMiddleware>("Reporting.PdfExport");

// Gate MVC filter
services.AddMvc(o => o.Filters.AddForFeature<AuditFilter>("Reporting.PdfExport"));

// Razor/View conditional rendering
// <feature name="Reporting.PdfExport"><button>Export PDF</button></feature>
```

### 3.4 Variants (A/B & Plan-Based Values)

Variants allow a feature to return different *values* (not just on/off), enabling plan-tiered behavior:

```json
{
    "id": "Reporting.MaxExportRows",
    "enabled": true,
    "variants": [
        { "name": "Basic",    "configuration_value": "1000" },
        { "name": "Pro",      "configuration_value": "50000" },
        { "name": "Enterprise","configuration_value": "unlimited" }
    ],
    "allocation": {
        "group": [
            { "variant": "Pro",        "groups": ["ProTenants"] },
            { "variant": "Enterprise", "groups": ["EnterpriseTenants"] }
        ],
        "default_when_enabled": "Basic"
    }
}
```

### 3.5 Microsoft.FeatureManagement Pros/Cons for NOIR

**Pros:**
- Official Microsoft library — stable, well-tested, actively maintained
- `IFeatureDefinitionProvider` is easy to back with a database (NOIR's EF Core repos)
- `IContextualFeatureFilter` cleanly handles per-tenant resolution
- `[FeatureGate]` attribute works on both controllers and Minimal API endpoint filters
- `AddScopedFeatureManagement()` enables per-request tenant context resolution
- Variants support plan/tier-based value overrides — not just boolean flags

**Cons:**
- Designed primarily around appsettings.json — custom `IFeatureDefinitionProvider` requires some boilerplate
- No built-in "module" concept — you must design the module grouping layer yourself
- No built-in management UI — NOIR would need to build an admin page
- Background job integration requires manual `IContextualFeatureFilter` calls (no magic)

---

## 4. Flagsmith & Unleash

### 4.1 Flagsmith Data Model

Hierarchy: **Organisation → Project → Environment → Feature → Identity Override**

- **Features** are defined at the **Project** level and shared across all environments
- **Values/states** are set per **Environment** (Development, Staging, Production, or per-tenant environment)
- **Identity Overrides** allow per-user or per-tenant overrides on top of environment defaults

For multi-tenancy in Flagsmith's model, each tenant is typically mapped to either:
1. A separate **Environment** within one project
2. A separate **Identity** within one environment (for per-tenant flag evaluation)

```
Organisation
└── Project: "NOIR"
    ├── Feature: "pdf_export" (defined here, state varies per env)
    └── Environments:
        ├── Production (shared defaults)
        ├── Tenant_ACME (enabled: pdf_export=true, max_rows=50000)
        └── Tenant_STARTUP (enabled: pdf_export=false, max_rows=1000)
```

**Python/Django data model** (Flagsmith is Python; relevant for schema inspiration):
- `Feature` table: name, project, type (STANDARD | MULTIVARIATE)
- `FeatureState` table: feature, environment, enabled, value
- `FeatureStateValue` table: feature_state, type, boolean/string/integer value
- `Identity` table: identifier, environment, created_at

### 4.2 Unleash Data Model

Hierarchy: **Project → Feature Flag → Environments**

Key design decision: **a feature flag is a single entity that spans all environments**. Per-environment, the flag has its own:
- Enabled/disabled state
- Activation strategies (percentage rollout, user targeting, etc.)
- Variants for A/B testing

```
Project: "NOIR"
└── Feature: "pdf_export"
    ├── Development: enabled=true, strategy=default
    ├── Staging:     enabled=true, strategy=userIds(["test@"])
    └── Production:  enabled=false, strategy=tenantIds(["acme"])
```

**Segments** are reusable constraint groups (e.g., "Enterprise Tenants") that can be applied across multiple flags and environments, enabling DRY tenant-group targeting.

### 4.3 Applicability to NOIR

Both Flagsmith and Unleash are **external services** — they require a separate deployment and API calls at runtime. The key patterns to borrow for NOIR:

1. **Single flag entity, per-environment/tenant values** — mirrors ABP's `AbpFeatureValues` approach
2. **Segments/groups for tenant targeting** — group tenants by plan tier, then assign flags to tier groups
3. **Variant values** — not just boolean, but string/integer values for limits and configuration

For NOIR, running an external Flagsmith/Unleash instance is **architectural overkill** when the same patterns can be implemented directly in the existing EF Core stack.

---

## 5. Architectural Comparison Matrix

| Aspect | ABP Framework | OrchardCore | MS.FeatureManagement | Flagsmith/Unleash |
|--------|--------------|-------------|---------------------|-------------------|
| **Feature definition location** | Code (`FeatureDefinitionProvider`) | Code (`Manifest.cs`) | Code + JSON config | External DB/API |
| **Feature value storage** | DB (`AbpFeatureValues`) | JSON file / DB table | IConfiguration | External DB |
| **Resolution mechanism** | Provider chain (waterfall) | Binary enabled/disabled | Filter chain | API lookup |
| **Tenant isolation model** | Ambient TenantId context | Per-tenant DI container | Custom context filter | Environment per tenant |
| **Module on/off per tenant** | Feature values only (module code always loaded) | Shell restart (DI rebuilt) | Custom filter | Environment toggle |
| **Runtime cost** | DB query + cache | Shell restart (expensive) | DB query or config read | HTTP API call |
| **Controller/endpoint gating** | `[RequiresFeature]` + interception | Module not registered = 404 | `[FeatureGate]` attribute | Manual check |
| **Background job support** | `ICurrentTenant.Change()` pattern | Shell-level (complex) | Manual context injection | Manual API call |
| **Seeder support** | `DataSeedContext.TenantId` check | Module-level | Manual check | N/A |
| **Plan/tier support** | Edition provider | Feature profiles | Variants | Segments + environments |
| **Management UI** | Built-in (ABP UI) | Built-in Admin | None (build your own) | Full UI |
| **CQRS compatibility** | Excellent (interception works on handlers) | Poor (different architecture) | Excellent (DI-native) | Good (manual check) |
| **NOIR integration effort** | Medium (ABP conventions conflict with NOIR) | High (architectural mismatch) | Low (pure .NET, no conflicts) | High (external service) |

---

## 6. Recommendations for NOIR

### 6.1 Core Decision: Hybrid Approach

**Use ABP's conceptual model + MS.FeatureManagement's primitives + NOIR's own DB.**

- Adopt ABP's **provider chain concept** as the design pattern
- Use **MS.FeatureManagement's `IVariantFeatureManager`** as the runtime interface for checks
- Build a **custom `IFeatureDefinitionProvider`** backed by NOIR's EF Core repositories
- Build a **custom `IContextualFeatureFilter<ITenantContext>`** for tenant-level resolution
- Store feature state in a new **`ModuleFeatureValue` table** in NOIR's `ApplicationDbContext`

### 6.2 Module vs. Feature Distinction

ABP's lesson: separate **modules** (code groupings) from **features** (runtime flags). NOIR should:

1. **Modules** = code-defined groups of related features (declared in `IModuleDefinition`)
   - Example: `Module("Reporting")` groups features: `PdfExport`, `ScheduledReports`, `MaxExportRows`
   - Modules are catalog-only — they do not gate code by themselves

2. **Features** = individual toggleable capabilities with values
   - Features belong to a module
   - Features have a `ValueType`: `Toggle` (bool), `Limit` (int), `Selection` (enum)
   - Features have a `DefaultValue` that applies unless overridden per-tenant

3. **ModuleFeatureValue** = per-tenant override
   - One row per (TenantId, FeatureName) where the value differs from the default
   - Absence of a row = use the default value (no rows = no DB queries needed for default-only tenants)

### 6.3 Layered Resolution (ABP-Inspired)

```
Priority 1: TenantFeatureValueProvider  (DB lookup in ModuleFeatureValues where TenantId = current)
Priority 2: PlanFeatureValueProvider    (DB lookup based on tenant's subscription plan)
Priority 3: DefaultValueProvider        (from IModuleDefinition.Features[name].DefaultValue)
```

This maps cleanly to NOIR's subscription model and requires minimal DB queries (one per feature check, cached per request).

---

## 7. Proposed NOIR Data Model

### 7.1 Module & Feature Catalog (Code-Defined)

```csharp
// Domain layer — src/NOIR.Domain/Features/

public interface IModuleDefinition
{
    string Name { get; }
    string DisplayName { get; }
    string Description { get; }
    IReadOnlyList<FeatureDefinition> Features { get; }
}

public class FeatureDefinition
{
    public string Name { get; init; }          // "Reporting.PdfExport"
    public string ModuleName { get; init; }    // "Reporting"
    public string DisplayName { get; init; }
    public string Description { get; init; }
    public FeatureValueType ValueType { get; init; }
    public string DefaultValue { get; init; }  // "false", "1000", "Basic"
    public bool IsVisibleToClient { get; init; } = true;
}

public enum FeatureValueType
{
    Toggle,      // "true" / "false"
    Limit,       // integer string "1000"
    Selection    // one of defined options
}
```

```csharp
// Application layer — src/NOIR.Application/Features/Modules/

// Example module definition
public class ReportingModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => "Reporting";
    public string DisplayName => "Reporting Module";
    public string Description => "PDF exports, scheduled reports, and analytics";

    public IReadOnlyList<FeatureDefinition> Features => new[]
    {
        new FeatureDefinition
        {
            Name        = "Reporting.PdfExport",
            ModuleName  = "Reporting",
            DisplayName = "PDF Export",
            ValueType   = FeatureValueType.Toggle,
            DefaultValue = "false"
        },
        new FeatureDefinition
        {
            Name        = "Reporting.MaxExportRows",
            ModuleName  = "Reporting",
            DisplayName = "Max Export Row Limit",
            ValueType   = FeatureValueType.Limit,
            DefaultValue = "1000"
        }
    };
}
```

### 7.2 Database Tables

```csharp
// Entity: ModuleFeatureValue
// Table: ModuleFeatureValues

public class ModuleFeatureValue : Entity<Guid>, IAuditableEntity
{
    public Guid TenantId { get; private set; }
    public string FeatureName { get; private set; }  // e.g. "Reporting.PdfExport"
    public string Value { get; private set; }         // e.g. "true", "5000"
    // IAuditableEntity: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
}
```

**EF Core configuration:**
```csharp
public class ModuleFeatureValueConfiguration : IEntityTypeConfiguration<ModuleFeatureValue>
{
    public void Configure(EntityTypeBuilder<ModuleFeatureValue> builder)
    {
        builder.ToTable("ModuleFeatureValues");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.TenantId, x.FeatureName }).IsUnique();
        // TenantId included per Rule 18 — unique constraints must include TenantId
        // Note: FeatureName is globally namespaced, so tenant+feature is unique

        builder.Property(x => x.FeatureName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(512).IsRequired();
    }
}
```

**Migration trigger:** New `ApplicationDbContext` migration needed once entity is created.

### 7.3 Repository & Spec

```csharp
// Infrastructure

public class ModuleFeatureValueByTenantSpec : Specification<ModuleFeatureValue>
{
    public ModuleFeatureValueByTenantSpec(Guid tenantId)
    {
        Query.Where(x => x.TenantId == tenantId)
             .TagWith("ModuleFeatureValueByTenant");
    }
}

public class ModuleFeatureValueByFeatureSpec : Specification<ModuleFeatureValue>
{
    public ModuleFeatureValueByFeatureSpec(Guid tenantId, string featureName)
    {
        Query.Where(x => x.TenantId == tenantId && x.FeatureName == featureName)
             .TagWith("ModuleFeatureValueByFeature");
    }
}
```

---

## 8. Integration Patterns

### 8.1 Service Interface

```csharp
// Application/Common/Interfaces/IFeatureChecker.cs
public interface IFeatureChecker
{
    Task<bool> IsEnabledAsync(string featureName, CancellationToken ct = default);
    Task<T> GetValueAsync<T>(string featureName, CancellationToken ct = default);
    Task<string> GetRawValueAsync(string featureName, CancellationToken ct = default);
}

// Application/Common/Interfaces/IModuleFeatureManager.cs
public interface IModuleFeatureManager
{
    Task SetForTenantAsync(Guid tenantId, string featureName, string value, CancellationToken ct);
    Task<IReadOnlyList<ModuleFeatureValue>> GetAllForTenantAsync(Guid tenantId, CancellationToken ct);
    Task<IReadOnlyList<IModuleDefinition>> GetAllModulesAsync();
}
```

### 8.2 CQRS Handler Gating

**Option A — Attribute (ABP-style via MediatR pipeline behavior):**

```csharp
// The command declares its feature requirement
[RequiresFeature("Reporting.PdfExport")]
public class ExportPdfCommand : IRequest<PdfResult>, IAuditableCommand<PdfResult>
{
    public Guid ReportId { get; init; }
}

// MediatR pipeline behavior enforces it
public class FeatureCheckBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IFeatureChecker _featureChecker;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var attr = typeof(TRequest).GetCustomAttribute<RequiresFeatureAttribute>();
        if (attr != null)
        {
            foreach (var feature in attr.Features)
            {
                if (!await _featureChecker.IsEnabledAsync(feature, ct))
                    throw new FeatureNotAvailableException(feature);
            }
        }
        return await next();
    }
}
```

**Option B — Explicit check in handler (recommended for NOIR's current style):**

```csharp
public class ExportPdfCommandHandler : IRequestHandler<ExportPdfCommand, PdfResult>
{
    private readonly IFeatureChecker _featureChecker;

    public async Task<PdfResult> Handle(ExportPdfCommand request, CancellationToken ct)
    {
        if (!await _featureChecker.IsEnabledAsync("Reporting.PdfExport", ct))
            return Result.Failure<PdfResult>(FeatureErrors.NotAvailable("Reporting.PdfExport"));

        var maxRows = await _featureChecker.GetValueAsync<int>("Reporting.MaxExportRows", ct);
        // ... proceed
    }
}
```

### 8.3 Minimal API Endpoint Filter

```csharp
// Web layer endpoint filter
public class FeatureEndpointFilter : IEndpointFilter
{
    private readonly IFeatureChecker _featureChecker;
    private readonly string _featureName;

    public FeatureEndpointFilter(IFeatureChecker featureChecker, string featureName)
    {
        _featureChecker = featureChecker;
        _featureName = featureName;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        if (!await _featureChecker.IsEnabledAsync(_featureName))
            return Results.Problem(
                detail: $"Feature '{_featureName}' is not available for your plan.",
                statusCode: 403,
                title: "Feature Not Available"
            );
        return await next(context);
    }
}

// Extension method for clean registration
public static class FeatureFilterExtensions
{
    public static RouteHandlerBuilder RequireFeature(
        this RouteHandlerBuilder builder,
        string featureName)
        => builder.AddEndpointFilter(
            new FeatureEndpointFilter(/* resolved from DI via factory */, featureName));
}

// Usage in endpoint registration
app.MapPost("/api/reports/export-pdf", ExportPdfHandler)
   .RequireFeature("Reporting.PdfExport")
   .WithTags("Reporting");
```

### 8.4 Background Job / Worker Pattern

```csharp
// Infrastructure/BackgroundJobs/ScheduledReportJob.cs

public class ScheduledReportJob : IScheduledJob, IScopedService
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICurrentTenantContext _currentTenant;

    public async Task ExecuteAsync(CancellationToken ct)
    {
        var tenants = await _tenantRepository.GetActiveTenantsAsync(ct);

        foreach (var tenant in tenants)
        {
            using var scope = _serviceProvider.CreateScope();

            // Switch ambient tenant context (NOIR's equivalent of ABP's ICurrentTenant.Change)
            using (_currentTenant.Change(tenant.Id))
            {
                var featureChecker = scope.ServiceProvider
                    .GetRequiredService<IFeatureChecker>();

                if (!await featureChecker.IsEnabledAsync("Reporting.ScheduledReports", ct))
                    continue;

                await scope.ServiceProvider
                    .GetRequiredService<IReportScheduler>()
                    .RunAsync(ct);
            }
        }
    }
}
```

### 8.5 Data Seeder (Conditional on Feature)

```csharp
// Application/Features/Reporting/Seeders/ReportingDataSeeder.cs

public class ReportingDataSeeder : IDataSeeder, IScopedService
{
    private readonly IFeatureChecker _featureChecker;
    private readonly IReportTemplateRepository _templateRepo;
    private readonly IUnitOfWork _unitOfWork;

    public async Task SeedAsync(SeedContext context, CancellationToken ct)
    {
        // Only seed default templates if the feature is enabled for this tenant
        if (!await _featureChecker.IsEnabledAsync("Reporting.DefaultTemplates", ct))
            return;

        var existing = await _templateRepo.CountAsync(ct);
        if (existing > 0) return; // idempotent

        await _templateRepo.AddAsync(DefaultReportTemplates.Create(context.TenantId), ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
```

### 8.6 Frontend (React) Feature Gating

The recommended pattern: backend returns current tenant's feature values as part of the application configuration response. The frontend uses a `useFeature(name)` hook backed by this data.

```typescript
// src/hooks/useFeature.ts
import { useAppConfig } from './useAppConfig';

export const useFeature = (featureName: string): boolean => {
    const { features } = useAppConfig();
    return features?.[featureName] === 'true' || features?.[featureName] === true;
};

export const useFeatureValue = <T>(featureName: string, defaultValue: T): T => {
    const { features } = useAppConfig();
    const raw = features?.[featureName];
    if (raw === undefined || raw === null) return defaultValue;
    // Type coercion based on defaultValue type
    if (typeof defaultValue === 'boolean') return (raw === 'true') as unknown as T;
    if (typeof defaultValue === 'number') return (Number(raw) || defaultValue) as unknown as T;
    return raw as unknown as T;
};

// Usage in a component
const ExportButton = () => {
    const canExportPdf = useFeature('Reporting.PdfExport');
    const maxRows = useFeatureValue('Reporting.MaxExportRows', 1000);

    if (!canExportPdf) return null;

    return <Button onClick={() => exportPdf(maxRows)}>{t('reporting.exportPdf')}</Button>;
};
```

**API endpoint for feature values:**

```csharp
// GET /api/features/current-tenant
// Returns: { "Reporting.PdfExport": "true", "Reporting.MaxExportRows": "5000" }

app.MapGet("/api/features/current-tenant", async (
    IModuleFeatureManager manager,
    ICurrentTenantContext tenant,
    CancellationToken ct) =>
{
    var values = await manager.GetResolvedFeaturesForTenantAsync(tenant.TenantId, ct);
    var clientVisible = values
        .Where(f => f.IsVisibleToClient)
        .ToDictionary(f => f.FeatureName, f => f.Value);
    return Results.Ok(clientVisible);
})
.RequireAuthorization();
```

---

## 9. Research Gaps & Limitations

1. **ABP's `AbpFeatureValues` exact column types** could not be confirmed from documentation alone — the exact schema may differ between ABP versions. Recommend inspecting the ABP source migrations directly if implementing a compatible schema.

2. **OrchardCore's `IShellFeaturesManager` source code** was not directly accessible during research. The implementation details of the shell restart mechanism are inferred from documentation.

3. **NOIR's existing `ICurrentTenantContext`** interface was not inspected during this research. The integration patterns above assume an ABP-style `Change()` pattern exists or can be added. Verify NOIR's actual current tenant switching API.

4. **Caching strategy** for feature values was not deeply researched. Production use requires a `IMemoryCache` or `IDistributedCache` layer on top of DB lookups to avoid per-request DB hits. ABP uses `IAbpDistributedLock`-protected cache invalidation; NOIR should adopt a similar pattern.

5. **LaunchDarkly** was not covered despite being listed in the brief — it is a paid SaaS product and less relevant as an open-source reference. Its patterns (SDK-based evaluation, streaming updates) are complex overkill for NOIR's use case.

---

## 10. Sources

- [ABP Framework Features Documentation](https://abp.io/docs/latest/framework/infrastructure/features)
- [ABP Feature Management Module](https://abp.io/docs/latest/modules/feature-management)
- [ABP Modularity Basics](https://abp.io/docs/latest/framework/architecture/modularity/basics)
- [ABP Database Tables Reference](https://abp.io/docs/latest/modules/database-tables)
- [ABP Multi-Tenancy Architecture](https://abp.io/docs/latest/framework/architecture/multi-tenancy)
- [ABP Background Workers](https://abp.io/docs/latest/framework/infrastructure/background-workers)
- [ABP IFeatureChecker in Tenant Context (Support Thread)](https://abp.io/support/questions/1201/FeatureChecker-is-not-working-when-changing-Tenant-by-code)
- [ABP Background Worker Tenant Safety (Support Thread)](https://abp.io/support/questions/9458/Is-BackgroundWorker-Tenant-Safe)
- [OrchardCore Tenants Documentation](https://docs.orchardcore.net/en/main/reference/modules/Tenants/)
- [OrchardCore Feature Profiles PR #9178](https://github.com/OrchardCMS/OrchardCore/pull/9178)
- [OrchardCore Samples Repository](https://github.com/OrchardCMS/OrchardCore.Samples)
- [Microsoft.FeatureManagement .NET Reference](https://learn.microsoft.com/en-us/azure/azure-app-configuration/feature-management-dotnet-reference)
- [Microsoft FeatureManagement-Dotnet GitHub](https://github.com/microsoft/FeatureManagement-Dotnet)
- [Custom Feature Provider with FeatBit](https://www.featbit.co/practices/original-articles/microsoft-feature-management-custom-feature-provider)
- [Flagsmith Data Model](https://docs.flagsmith.com/flagsmith-concepts/data-model)
- [Unleash Feature Flag Organization](https://docs.getunleash.io/guides/organize-feature-flags)
- [Multitenancy with Orchard Core (Code Maze)](https://code-maze.com/dotnet-multitenancy-with-orchard-core/)
- [ABP Framework GitHub Repository](https://github.com/abpframework/abp)
