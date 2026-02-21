# Feature Management System - Technical Architecture Design

> Date: 2026-02-21
> Status: Draft (Pending approval)
> Prerequisites: [Requirements Specification](feature-management-requirements.md) | [Research](module-feature-management-systems.md)

---

## 1. Architecture Overview

### 1.1 Design Philosophy

**ABP-inspired provider chain + Wolverine middleware + NOIR's existing patterns.**

The system adds a feature-gating layer that sits alongside the existing permission system:

```
Request → Auth → Permission Check → Feature Check → Handler → Response
                (existing)          (NEW)
```

- **Permissions** answer: "Can this USER do this?"
- **Features** answer: "Can this TENANT use this?"

### 1.2 Key Architecture Decisions

| # | Decision | Rationale |
|---|----------|-----------|
| AD-1 | **Single table** (`TenantModuleState`) with two columns (`IsAvailable`, `IsEnabled`) | Simpler than two tables. One row per tenant/feature. Platform admin controls `IsAvailable`, tenant admin controls `IsEnabled`. Effective = `IsAvailable AND IsEnabled`. |
| AD-2 | **Wolverine middleware** (not MediatR pipeline behavior) | NOIR uses Wolverine, not MediatR. `FeatureCheckMiddleware` with `BeforeAsync()` checks `[RequiresFeature]` attribute. |
| AD-3 | **Code-defined catalog** via `IModuleDefinition` + Scrutor auto-registration | Type-safe, discoverable, no DB schema for definitions. Only overrides stored in DB. |
| AD-4 | **Throw `FeatureNotAvailableException`** in middleware | Caught by existing `ExceptionHandlingMiddleware` → 403 Forbidden. Consistent with NOIR's error handling. |
| AD-5 | **FusionCache + request-scoped dictionary** | FusionCache (already used for SMTP settings) handles cross-request caching. Dictionary avoids repeated cache lookups within a single request. |
| AD-6 | **SignalR via existing `NotificationHub`** | Broadcast `FeaturesUpdated` event to `tenant_{tenantId}` group. No new hub needed. |

### 1.3 Resolution Chain

```
Effective State = IsAvailable (platform) AND IsEnabled (tenant) AND ParentModuleEffective

Resolution:
1. Load all TenantModuleState rows for current tenant (cached)
2. For each feature request:
   a. Check parent module first (if feature has parent)
   b. Check platform availability (IsAvailable column)
   c. Check tenant toggle (IsEnabled column)
   d. If no row exists → use code-defined default (DefaultEnabled)
   e. Core modules → always enabled (bypass all checks)
```

---

## 2. Data Model

### 2.1 Entity: TenantModuleState

```
Table: TenantModuleStates
Purpose: Stores per-tenant overrides for module/feature state
Base: TenantEntity<Guid> (inherits TenantId, IAuditableEntity, soft delete)
```

```csharp
// src/NOIR.Domain/Entities/TenantModuleState.cs

public class TenantModuleState : TenantEntity<Guid>
{
    /// Module or feature name (e.g., "Ecommerce" or "Ecommerce.Reviews")
    public string FeatureName { get; private set; } = default!;

    /// Platform admin controls this. True = available to tenant.
    /// Default assumption when no row exists: true (available).
    public bool IsAvailable { get; private set; } = true;

    /// Tenant admin controls this. True = enabled by tenant.
    /// Default assumption when no row exists: true (enabled).
    public bool IsEnabled { get; private set; } = true;

    private TenantModuleState() { } // EF Core

    public static TenantModuleState Create(string featureName)
        => new() { Id = Guid.NewGuid(), FeatureName = featureName };

    public void SetAvailability(bool isAvailable) => IsAvailable = isAvailable;
    public void SetEnabled(bool isEnabled) => IsEnabled = isEnabled;
}
```

### 2.2 EF Core Configuration

```csharp
// src/NOIR.Infrastructure/Persistence/Configurations/TenantModuleStateConfiguration.cs

public class TenantModuleStateConfiguration : IEntityTypeConfiguration<TenantModuleState>
{
    public void Configure(EntityTypeBuilder<TenantModuleState> builder)
    {
        builder.ToTable("TenantModuleStates");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.FeatureName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(e => e.IsAvailable)
            .HasDefaultValue(true);

        builder.Property(e => e.IsEnabled)
            .HasDefaultValue(true);

        // Rule 18: Unique constraints MUST include TenantId
        builder.HasIndex(e => new { e.TenantId, e.FeatureName })
            .IsUnique()
            .HasFilter("[IsDeleted] = 0")
            .HasDatabaseName("IX_TenantModuleStates_TenantId_FeatureName");

        // Index for bulk tenant queries
        builder.HasIndex(e => e.TenantId)
            .HasDatabaseName("IX_TenantModuleStates_TenantId");

        // Standard audit fields
        builder.Property(e => e.TenantId).HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.Property(e => e.CreatedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.ModifiedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.DeletedBy).HasMaxLength(DatabaseConstants.UserIdMaxLength);
        builder.Property(e => e.IsDeleted).HasDefaultValue(false);

        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);
    }
}
```

### 2.3 Migration

```bash
dotnet ef migrations add AddTenantModuleStates \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext \
  --output-dir Migrations/App
```

---

## 3. Domain Layer

### 3.1 Module Catalog Interfaces

```csharp
// src/NOIR.Domain/Interfaces/IModuleDefinition.cs

/// A code-defined module with its child features.
public interface IModuleDefinition
{
    /// Unique key (e.g., "Ecommerce", "Content.Blog")
    string Name { get; }

    /// Localization key for display name (e.g., "modules.ecommerce")
    string DisplayNameKey { get; }

    /// Localization key for description
    string DescriptionKey { get; }

    /// Lucide icon name for UI (e.g., "ShoppingCart", "FileText")
    string Icon { get; }

    /// Sort order in admin UI
    int SortOrder { get; }

    /// Core modules cannot be disabled (Auth, Users, Dashboard, etc.)
    bool IsCore { get; }

    /// Default state when no DB override exists
    bool DefaultEnabled { get; }

    /// Child features within this module
    IReadOnlyList<FeatureDefinition> Features { get; }
}
```

```csharp
// src/NOIR.Domain/Common/FeatureDefinition.cs

/// A single toggleable feature within a module.
public sealed record FeatureDefinition(
    /// Unique key, format: "{ModuleName}.{FeatureName}" (e.g., "Ecommerce.Reviews")
    string Name,

    /// Localization key for display name
    string DisplayNameKey,

    /// Localization key for description
    string DescriptionKey,

    /// Default state when no DB override exists
    bool DefaultEnabled = true
);
```

### 3.2 Feature Checker Interface

```csharp
// src/NOIR.Domain/Interfaces/IFeatureChecker.cs

/// Checks whether a module/feature is effectively enabled for the current tenant.
public interface IFeatureChecker
{
    /// Returns true if the feature is effectively enabled (available AND enabled AND parent enabled).
    Task<bool> IsEnabledAsync(string featureName, CancellationToken ct = default);

    /// Returns detailed state for a single feature.
    Task<EffectiveFeatureState> GetStateAsync(string featureName, CancellationToken ct = default);

    /// Returns all feature states for the current tenant (used by frontend API).
    Task<IReadOnlyDictionary<string, EffectiveFeatureState>> GetAllStatesAsync(CancellationToken ct = default);
}

public sealed record EffectiveFeatureState(
    bool IsAvailable,   // Platform admin has made this available
    bool IsEnabled,     // Tenant admin has enabled this
    bool IsEffective,   // Available AND Enabled AND ParentEffective
    bool IsCore         // Core modules are always effective
);
```

### 3.3 Module Catalog Interface

```csharp
// src/NOIR.Domain/Interfaces/IModuleCatalog.cs

/// Aggregates all code-defined module definitions.
public interface IModuleCatalog
{
    IReadOnlyList<IModuleDefinition> GetAllModules();
    IModuleDefinition? GetModule(string moduleName);
    FeatureDefinition? GetFeature(string featureName);
    bool IsCore(string featureName);
    bool Exists(string featureName);

    /// Returns the parent module name for a feature (e.g., "Ecommerce" for "Ecommerce.Reviews").
    string? GetParentModuleName(string featureName);
}
```

### 3.4 RequiresFeature Attribute

```csharp
// src/NOIR.Domain/Common/RequiresFeatureAttribute.cs

/// Marks a command/query as requiring one or more features to be enabled.
/// Checked by FeatureCheckMiddleware in the Wolverine pipeline.
[AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
public sealed class RequiresFeatureAttribute : Attribute
{
    public string[] Features { get; }

    public RequiresFeatureAttribute(params string[] features)
    {
        Features = features;
    }
}
```

### 3.5 Exception Type

```csharp
// src/NOIR.Application/Common/Exceptions/FeatureNotAvailableException.cs

/// Thrown when a command/query requires a feature that is not enabled for the current tenant.
public class FeatureNotAvailableException : Exception
{
    public string FeatureName { get; }

    public FeatureNotAvailableException(string featureName)
        : base($"The feature '{featureName}' is not enabled for your organization.")
    {
        FeatureName = featureName;
    }
}
```

### 3.6 Error Codes Extension

```csharp
// Add to src/NOIR.Domain/Common/ErrorCodes.cs

public static class Feature
{
    public const string NotAvailable = "NOIR-BIZ-3050";
    public const string CoreCannotBeDisabled = "NOIR-BIZ-3051";
    public const string ParentModuleDisabled = "NOIR-BIZ-3052";
    public const string NotFound = "NOIR-BIZ-3053";
}
```

---

## 4. Application Layer

### 4.1 Module Definitions (All 33)

Each module is a class implementing `IModuleDefinition` + `ISingletonService`, auto-registered by Scrutor.

**File structure:**
```
src/NOIR.Application/Modules/
├── Core/
│   ├── AuthModuleDefinition.cs
│   ├── UsersModuleDefinition.cs
│   ├── RolesModuleDefinition.cs
│   ├── PermissionsModuleDefinition.cs
│   ├── DashboardModuleDefinition.cs
│   ├── SettingsModuleDefinition.cs
│   ├── AuditModuleDefinition.cs
│   └── NotificationsModuleDefinition.cs
├── Ecommerce/
│   ├── ProductsModuleDefinition.cs
│   ├── CategoriesModuleDefinition.cs
│   ├── BrandsModuleDefinition.cs
│   ├── AttributesModuleDefinition.cs
│   ├── CartModuleDefinition.cs
│   ├── CheckoutModuleDefinition.cs
│   ├── OrdersModuleDefinition.cs
│   ├── PaymentsModuleDefinition.cs
│   ├── InventoryModuleDefinition.cs
│   ├── PromotionsModuleDefinition.cs
│   ├── ReviewsModuleDefinition.cs
│   ├── CustomersModuleDefinition.cs
│   ├── CustomerGroupsModuleDefinition.cs
│   └── WishlistModuleDefinition.cs
├── Content/
│   ├── BlogModuleDefinition.cs
│   ├── BlogCategoriesModuleDefinition.cs
│   └── BlogTagsModuleDefinition.cs
├── Platform/
│   ├── TenantsModuleDefinition.cs
│   ├── EmailTemplatesModuleDefinition.cs
│   └── LegalPagesModuleDefinition.cs
└── Analytics/
    ├── ReportsModuleDefinition.cs
    └── DeveloperLogsModuleDefinition.cs
```

**Example module definition:**

```csharp
// src/NOIR.Application/Modules/Ecommerce/ProductsModuleDefinition.cs

public sealed class ProductsModuleDefinition : IModuleDefinition, ISingletonService
{
    public string Name => ModuleNames.Ecommerce.Products;
    public string DisplayNameKey => "modules.ecommerce.products";
    public string DescriptionKey => "modules.ecommerce.products.description";
    public string Icon => "Package";
    public int SortOrder => 100;
    public bool IsCore => false;
    public bool DefaultEnabled => true;

    public IReadOnlyList<FeatureDefinition> Features =>
    [
        new("Ecommerce.Products.Variants", "modules.ecommerce.products.variants", "modules.ecommerce.products.variants.description"),
        new("Ecommerce.Products.Options", "modules.ecommerce.products.options", "modules.ecommerce.products.options.description"),
        new("Ecommerce.Products.Import", "modules.ecommerce.products.import", "modules.ecommerce.products.import.description"),
        new("Ecommerce.Products.Export", "modules.ecommerce.products.export", "modules.ecommerce.products.export.description"),
    ];
}
```

**Module name constants:**

```csharp
// src/NOIR.Application/Modules/ModuleNames.cs

/// Centralized module/feature name constants. Prevents magic strings.
public static class ModuleNames
{
    public static class Core
    {
        public const string Auth = "Core.Auth";
        public const string Users = "Core.Users";
        public const string Roles = "Core.Roles";
        public const string Permissions = "Core.Permissions";
        public const string Dashboard = "Core.Dashboard";
        public const string Settings = "Core.Settings";
        public const string Audit = "Core.Audit";
        public const string Notifications = "Core.Notifications";
    }

    public static class Ecommerce
    {
        public const string Products = "Ecommerce.Products";
        public const string Categories = "Ecommerce.Categories";
        public const string Brands = "Ecommerce.Brands";
        public const string Attributes = "Ecommerce.Attributes";
        public const string Cart = "Ecommerce.Cart";
        public const string Checkout = "Ecommerce.Checkout";
        public const string Orders = "Ecommerce.Orders";
        public const string Payments = "Ecommerce.Payments";
        public const string Inventory = "Ecommerce.Inventory";
        public const string Promotions = "Ecommerce.Promotions";
        public const string Reviews = "Ecommerce.Reviews";
        public const string Customers = "Ecommerce.Customers";
        public const string CustomerGroups = "Ecommerce.CustomerGroups";
        public const string Wishlist = "Ecommerce.Wishlist";
    }

    public static class Content
    {
        public const string Blog = "Content.Blog";
        public const string BlogCategories = "Content.BlogCategories";
        public const string BlogTags = "Content.BlogTags";
    }

    public static class Platform
    {
        public const string Tenants = "Platform.Tenants";
        public const string EmailTemplates = "Platform.EmailTemplates";
        public const string LegalPages = "Platform.LegalPages";
    }

    public static class Analytics
    {
        public const string Reports = "Analytics.Reports";
        public const string DeveloperLogs = "Analytics.DeveloperLogs";
    }
}
```

### 4.2 Feature Management Commands & Queries

**Co-located CQRS pattern (NOIR standard):**

```
src/NOIR.Application/Features/FeatureManagement/
├── Commands/
│   ├── SetModuleAvailability/
│   │   ├── SetModuleAvailabilityCommand.cs     (+ Handler + Validator)
│   │   └── SetModuleAvailabilityCommandHandler.cs
│   └── ToggleModule/
│       ├── ToggleModuleCommand.cs              (+ Handler + Validator)
│       └── ToggleModuleCommandHandler.cs
├── Queries/
│   ├── GetModuleCatalog/
│   │   ├── GetModuleCatalogQuery.cs            (+ Handler)
│   │   └── GetModuleCatalogQueryHandler.cs
│   ├── GetTenantFeatureStates/
│   │   ├── GetTenantFeatureStatesQuery.cs      (+ Handler)
│   │   └── GetTenantFeatureStatesQueryHandler.cs
│   └── GetCurrentTenantFeatures/
│       ├── GetCurrentTenantFeaturesQuery.cs    (+ Handler)
│       └── GetCurrentTenantFeaturesQueryHandler.cs
└── DTOs/
    ├── ModuleCatalogDto.cs
    ├── ModuleDto.cs
    ├── FeatureDto.cs
    └── TenantFeatureStateDto.cs
```

**SetModuleAvailabilityCommand** (platform admin only):

```csharp
// src/NOIR.Application/Features/FeatureManagement/Commands/SetModuleAvailability/SetModuleAvailabilityCommand.cs

public sealed record SetModuleAvailabilityCommand(
    string TenantId,
    string FeatureName,
    bool IsAvailable
) : IAuditableCommand<TenantFeatureStateDto>
{
    [JsonIgnore] public string? UserId { get; init; }
    public object? GetTargetId() => $"{TenantId}:{FeatureName}";
    public AuditOperationType OperationType => AuditOperationType.Update;
    public string? GetActionDescription() => IsAvailable
        ? $"Made '{FeatureName}' available"
        : $"Made '{FeatureName}' unavailable";
    public string? GetTargetDisplayName() => FeatureName;
}

public sealed class SetModuleAvailabilityCommandValidator
    : AbstractValidator<SetModuleAvailabilityCommand>
{
    public SetModuleAvailabilityCommandValidator(
        IModuleCatalog catalog,
        ILocalizationService l)
    {
        RuleFor(x => x.TenantId)
            .NotEmpty().WithMessage(l["validation.tenantId.required"]);

        RuleFor(x => x.FeatureName)
            .NotEmpty().WithMessage(l["validation.featureName.required"])
            .Must(name => catalog.Exists(name))
            .WithMessage(l["validation.featureName.notFound"])
            .Must(name => !catalog.IsCore(name))
            .WithMessage(l["validation.featureName.coreCannotBeModified"]);
    }
}
```

**SetModuleAvailabilityCommandHandler:**

```csharp
public class SetModuleAvailabilityCommandHandler
{
    private readonly IRepository<TenantModuleState, Guid> _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFeatureCacheInvalidator _cacheInvalidator;

    public async Task<Result<TenantFeatureStateDto>> Handle(
        SetModuleAvailabilityCommand command,
        CancellationToken ct)
    {
        var spec = new TenantModuleStateByFeatureSpec(command.TenantId, command.FeatureName);
        var state = await _repository.FirstOrDefaultAsync(spec, ct);

        if (state is null)
        {
            state = TenantModuleState.Create(command.FeatureName);
            state.SetAvailability(command.IsAvailable);
            await _repository.AddAsync(state, ct);
        }
        else
        {
            state.SetAvailability(command.IsAvailable);
            _repository.Update(state);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        await _cacheInvalidator.InvalidateAsync(command.TenantId, ct);

        return new TenantFeatureStateDto(
            command.FeatureName, state.IsAvailable, state.IsEnabled);
    }
}
```

**ToggleModuleCommand** (tenant admin):

```csharp
public sealed record ToggleModuleCommand(
    string FeatureName,
    bool IsEnabled
) : IAuditableCommand<TenantFeatureStateDto>
{
    [JsonIgnore] public string? UserId { get; init; }
    public object? GetTargetId() => FeatureName;
    public AuditOperationType OperationType => AuditOperationType.Update;
    public string? GetActionDescription() => IsEnabled
        ? $"Enabled '{FeatureName}'"
        : $"Disabled '{FeatureName}'";
}
```

**GetCurrentTenantFeaturesQuery** (frontend API):

```csharp
public sealed record GetCurrentTenantFeaturesQuery();

// Handler returns Dictionary<string, EffectiveFeatureState>
// Used by GET /api/features/current-tenant
```

### 4.3 DTOs

```csharp
// src/NOIR.Application/Features/FeatureManagement/DTOs/

public sealed record ModuleCatalogDto(
    IReadOnlyList<ModuleDto> Modules
);

public sealed record ModuleDto(
    string Name,
    string DisplayNameKey,
    string DescriptionKey,
    string Icon,
    int SortOrder,
    bool IsCore,
    bool DefaultEnabled,
    IReadOnlyList<FeatureDto> Features,
    // Resolved state (when queried for a specific tenant)
    bool? IsAvailable,
    bool? IsEnabled,
    bool? IsEffective
);

public sealed record FeatureDto(
    string Name,
    string DisplayNameKey,
    string DescriptionKey,
    bool DefaultEnabled,
    bool? IsAvailable,
    bool? IsEnabled,
    bool? IsEffective
);

public sealed record TenantFeatureStateDto(
    string FeatureName,
    bool IsAvailable,
    bool IsEnabled
);
```

### 4.4 Specifications

```csharp
// src/NOIR.Application/Features/FeatureManagement/Specifications/

public sealed class TenantModuleStateByTenantSpec : Specification<TenantModuleState>
{
    public TenantModuleStateByTenantSpec(string tenantId)
    {
        Query.Where(x => x.TenantId == tenantId)
             .TagWith("TenantModuleStateByTenant");
    }
}

public sealed class TenantModuleStateByFeatureSpec : Specification<TenantModuleState>
{
    public TenantModuleStateByFeatureSpec(string tenantId, string featureName)
    {
        Query.Where(x => x.TenantId == tenantId && x.FeatureName == featureName)
             .AsTracking()
             .TagWith("TenantModuleStateByFeature");
    }
}
```

---

## 5. Infrastructure Layer

### 5.1 ModuleCatalog Implementation

```csharp
// src/NOIR.Infrastructure/Services/ModuleCatalog.cs

public sealed class ModuleCatalog : IModuleCatalog, ISingletonService
{
    private readonly IReadOnlyList<IModuleDefinition> _modules;
    private readonly Dictionary<string, IModuleDefinition> _modulesByName;
    private readonly Dictionary<string, FeatureDefinition> _featuresByName;
    private readonly HashSet<string> _coreNames;

    public ModuleCatalog(IEnumerable<IModuleDefinition> moduleDefinitions)
    {
        _modules = moduleDefinitions.OrderBy(m => m.SortOrder).ToList();
        _modulesByName = _modules.ToDictionary(m => m.Name, StringComparer.OrdinalIgnoreCase);
        _featuresByName = _modules
            .SelectMany(m => m.Features)
            .ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);
        _coreNames = _modules
            .Where(m => m.IsCore)
            .Select(m => m.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<IModuleDefinition> GetAllModules() => _modules;
    public IModuleDefinition? GetModule(string name) => _modulesByName.GetValueOrDefault(name);
    public FeatureDefinition? GetFeature(string name) => _featuresByName.GetValueOrDefault(name);
    public bool IsCore(string name) => _coreNames.Contains(name);
    public bool Exists(string name) => _modulesByName.ContainsKey(name) || _featuresByName.ContainsKey(name);

    public string? GetParentModuleName(string featureName)
    {
        // "Ecommerce.Products.Variants" → parent = "Ecommerce.Products"
        // "Ecommerce.Products" → parent = "Ecommerce" (if module exists, else null)
        var lastDot = featureName.LastIndexOf('.');
        while (lastDot > 0)
        {
            var parentName = featureName[..lastDot];
            if (_modulesByName.ContainsKey(parentName))
                return parentName;
            lastDot = parentName.LastIndexOf('.');
        }
        return null;
    }
}
```

### 5.2 FeatureChecker Implementation

```csharp
// src/NOIR.Infrastructure/Services/FeatureChecker.cs

public sealed class FeatureChecker : IFeatureChecker, IScopedService
{
    private readonly IFusionCache _cache;
    private readonly ApplicationDbContext _dbContext;
    private readonly IModuleCatalog _catalog;
    private readonly IMultiTenantContextAccessor<Tenant> _tenantAccessor;
    private readonly ILogger<FeatureChecker> _logger;

    // Per-request cache (populated on first call, reused within request)
    private IReadOnlyDictionary<string, EffectiveFeatureState>? _requestCache;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken ct = default)
    {
        // Core modules are always enabled
        if (_catalog.IsCore(featureName))
            return true;

        var states = await LoadStatesAsync(ct);
        return states.TryGetValue(featureName, out var state) && state.IsEffective;
    }

    public async Task<EffectiveFeatureState> GetStateAsync(string featureName, CancellationToken ct = default)
    {
        var states = await LoadStatesAsync(ct);
        return states.TryGetValue(featureName, out var state)
            ? state
            : new EffectiveFeatureState(true, true, true, _catalog.IsCore(featureName));
    }

    public async Task<IReadOnlyDictionary<string, EffectiveFeatureState>> GetAllStatesAsync(
        CancellationToken ct = default) => await LoadStatesAsync(ct);

    private async Task<IReadOnlyDictionary<string, EffectiveFeatureState>> LoadStatesAsync(
        CancellationToken ct)
    {
        // Per-request cache check
        if (_requestCache is not null)
            return _requestCache;

        var tenantId = _tenantAccessor.MultiTenantContext?.TenantInfo?.Id;
        if (string.IsNullOrEmpty(tenantId))
        {
            // No tenant context (platform admin or unauthenticated) → all enabled
            _requestCache = BuildDefaultStates();
            return _requestCache;
        }

        // FusionCache (cross-request, L1+L2)
        var cacheKey = CacheKeys.TenantFeatures(tenantId);
        var dbOverrides = await _cache.GetOrSetAsync(
            cacheKey,
            async (ctx, ct2) => await LoadFromDbAsync(tenantId, ct2),
            new FusionCacheEntryOptions(CacheDuration),
            ct);

        _requestCache = ResolveEffectiveStates(dbOverrides);
        return _requestCache;
    }

    private async Task<Dictionary<string, TenantModuleStateRow>> LoadFromDbAsync(
        string tenantId, CancellationToken ct)
    {
        return await _dbContext.Set<TenantModuleState>()
            .Where(x => x.TenantId == tenantId && !x.IsDeleted)
            .TagWith("FeatureChecker:LoadTenantStates")
            .ToDictionaryAsync(
                x => x.FeatureName,
                x => new TenantModuleStateRow(x.IsAvailable, x.IsEnabled),
                StringComparer.OrdinalIgnoreCase,
                ct);
    }

    private IReadOnlyDictionary<string, EffectiveFeatureState> ResolveEffectiveStates(
        Dictionary<string, TenantModuleStateRow> dbOverrides)
    {
        var result = new Dictionary<string, EffectiveFeatureState>(StringComparer.OrdinalIgnoreCase);

        foreach (var module in _catalog.GetAllModules())
        {
            var moduleState = ResolveModuleState(module.Name, module.IsCore, module.DefaultEnabled, dbOverrides);
            result[module.Name] = moduleState;

            foreach (var feature in module.Features)
            {
                var featureState = ResolveFeatureState(feature.Name, feature.DefaultEnabled, moduleState, dbOverrides);
                result[feature.Name] = featureState;
            }
        }

        return result;
    }

    private EffectiveFeatureState ResolveModuleState(
        string name, bool isCore, bool defaultEnabled,
        Dictionary<string, TenantModuleStateRow> overrides)
    {
        if (isCore)
            return new(true, true, true, true);

        var hasOverride = overrides.TryGetValue(name, out var row);
        var isAvailable = hasOverride ? row!.IsAvailable : true;
        var isEnabled = hasOverride ? row!.IsEnabled : defaultEnabled;
        var isEffective = isAvailable && isEnabled;

        return new(isAvailable, isEnabled, isEffective, false);
    }

    private EffectiveFeatureState ResolveFeatureState(
        string name, bool defaultEnabled,
        EffectiveFeatureState parentState,
        Dictionary<string, TenantModuleStateRow> overrides)
    {
        // Parent module disabled → child is forced off
        if (!parentState.IsEffective)
            return new(parentState.IsAvailable, false, false, false);

        var hasOverride = overrides.TryGetValue(name, out var row);
        var isAvailable = hasOverride ? row!.IsAvailable : true;
        var isEnabled = hasOverride ? row!.IsEnabled : defaultEnabled;
        var isEffective = isAvailable && isEnabled && parentState.IsEffective;

        return new(isAvailable, isEnabled, isEffective, false);
    }

    private IReadOnlyDictionary<string, EffectiveFeatureState> BuildDefaultStates()
    {
        var result = new Dictionary<string, EffectiveFeatureState>(StringComparer.OrdinalIgnoreCase);
        foreach (var module in _catalog.GetAllModules())
        {
            result[module.Name] = new(true, module.DefaultEnabled, module.DefaultEnabled, module.IsCore);
            foreach (var feature in module.Features)
                result[feature.Name] = new(true, feature.DefaultEnabled, feature.DefaultEnabled, false);
        }
        return result;
    }

    private sealed record TenantModuleStateRow(bool IsAvailable, bool IsEnabled);
}
```

### 5.3 Cache Invalidation

```csharp
// src/NOIR.Infrastructure/Services/FeatureCacheInvalidator.cs

public interface IFeatureCacheInvalidator
{
    Task InvalidateAsync(string tenantId, CancellationToken ct = default);
}

public sealed class FeatureCacheInvalidator : IFeatureCacheInvalidator, IScopedService
{
    private readonly IFusionCache _cache;
    private readonly INotificationHubContext _notificationHub;
    private readonly ILogger<FeatureCacheInvalidator> _logger;

    public async Task InvalidateAsync(string tenantId, CancellationToken ct = default)
    {
        var cacheKey = CacheKeys.TenantFeatures(tenantId);
        await _cache.RemoveAsync(cacheKey, token: ct);

        // Notify connected clients of this tenant to refresh features
        await _notificationHub.SendToGroupAsync(
            $"tenant_{tenantId}",
            NotificationDto.System("features_updated", "Module configuration has changed"),
            ct);

        _logger.LogInformation("Invalidated feature cache for tenant {TenantId}", tenantId);
    }
}
```

```csharp
// Add to src/NOIR.Infrastructure/Common/CacheKeys.cs

public static string TenantFeatures(string? tenantId) => $"features:tenant:{tenantId ?? "platform"}";
```

### 5.4 Wolverine Feature Check Middleware

```csharp
// src/NOIR.Infrastructure/Middleware/FeatureCheckMiddleware.cs

/// Wolverine middleware that checks [RequiresFeature] attribute on commands/queries.
/// Throws FeatureNotAvailableException (→ 403) if any required feature is disabled.
public class FeatureCheckMiddleware
{
    public async Task BeforeAsync(
        Envelope envelope,
        IFeatureChecker featureChecker,
        ILogger<FeatureCheckMiddleware> logger)
    {
        var messageType = envelope.Message?.GetType();
        if (messageType is null) return;

        var attr = messageType.GetCustomAttribute<RequiresFeatureAttribute>();
        if (attr is null) return;

        foreach (var feature in attr.Features)
        {
            if (!await featureChecker.IsEnabledAsync(feature))
            {
                logger.LogWarning(
                    "Feature check failed: {Feature} is not enabled. Command: {CommandType}",
                    feature, messageType.Name);
                throw new FeatureNotAvailableException(feature);
            }
        }
    }
}
```

**Registration in Program.cs (add to Wolverine config):**

```csharp
builder.Host.UseWolverine(opts =>
{
    // ... existing middleware ...
    opts.Policies.AddMiddleware<FeatureCheckMiddleware>();  // NEW
});
```

### 5.5 Exception Handling Extension

Add to `ExceptionHandlingMiddleware.cs`:

```csharp
// In the exception switch expression, add:
FeatureNotAvailableException featureException =>
    HandleFeatureNotAvailableException(featureException),

// New method:
private static (int, ProblemDetails, string) HandleFeatureNotAvailableException(
    FeatureNotAvailableException exception)
{
    var errorCode = ErrorCodes.Feature.NotAvailable;
    return (StatusCodes.Status403Forbidden, new ProblemDetails
    {
        Status = StatusCodes.Status403Forbidden,
        Title = "Feature Not Available",
        Detail = exception.Message,
        Type = $"https://api.noir.local/errors/{errorCode}"
    }, errorCode);
}
```

---

## 6. Web Layer

### 6.1 Feature Management Endpoints

```csharp
// src/NOIR.Web/Endpoints/FeatureManagementEndpoints.cs

public static class FeatureManagementEndpoints
{
    public static void MapFeatureManagementEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/features")
            .WithTags("Feature Management")
            .RequireAuthorization();

        // GET /api/features/current-tenant
        // Returns all feature states for current tenant (used by frontend)
        group.MapGet("/current-tenant", async (IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<Dictionary<string, EffectiveFeatureState>>>(
                new GetCurrentTenantFeaturesQuery());
            return result.ToHttpResult();
        })
        .WithName("GetCurrentTenantFeatures")
        .WithSummary("Get all feature states for the current tenant")
        .Produces<Dictionary<string, EffectiveFeatureState>>(StatusCodes.Status200OK);

        // GET /api/features/catalog
        // Returns the module catalog (available to both platform and tenant admin)
        group.MapGet("/catalog", async (IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<ModuleCatalogDto>>(
                new GetModuleCatalogQuery());
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantSettingsRead)
        .WithName("GetModuleCatalog")
        .WithSummary("Get the full module catalog with definitions");

        // GET /api/features/tenant/{tenantId}
        // Returns feature states for a specific tenant (platform admin only)
        group.MapGet("/tenant/{tenantId}", async (
            string tenantId,
            IMessageBus bus) =>
        {
            var result = await bus.InvokeAsync<Result<ModuleCatalogDto>>(
                new GetTenantFeatureStatesQuery(tenantId));
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantsUpdate)
        .WithName("GetTenantFeatureStates")
        .WithSummary("Get feature states for a specific tenant (platform admin)");

        // PUT /api/features/tenant/{tenantId}/availability
        // Set module availability for a tenant (platform admin only)
        group.MapPut("/tenant/{tenantId}/availability", async (
            string tenantId,
            SetModuleAvailabilityCommand command,
            IMessageBus bus,
            ICurrentUser currentUser) =>
        {
            var cmd = command with { TenantId = tenantId, UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<TenantFeatureStateDto>>(cmd);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantsUpdate)
        .WithName("SetModuleAvailability")
        .WithSummary("Set module availability for a tenant (platform admin)");

        // PUT /api/features/toggle
        // Toggle module for current tenant (tenant admin)
        group.MapPut("/toggle", async (
            ToggleModuleCommand command,
            IMessageBus bus,
            ICurrentUser currentUser) =>
        {
            var cmd = command with { UserId = currentUser.UserId };
            var result = await bus.InvokeAsync<Result<TenantFeatureStateDto>>(cmd);
            return result.ToHttpResult();
        })
        .RequireAuthorization(Permissions.TenantSettingsUpdate)
        .WithName("ToggleModule")
        .WithSummary("Toggle a module on/off for current tenant");
    }
}
```

### 6.2 Endpoint Filter (for non-CQRS endpoints)

```csharp
// src/NOIR.Web/Filters/RequireFeatureFilter.cs

/// Endpoint filter that checks feature availability before executing the endpoint.
/// Use: .AddEndpointFilter(new RequireFeatureFilter("Ecommerce.Products"))
public sealed class RequireFeatureFilter : IEndpointFilter
{
    private readonly string[] _features;

    public RequireFeatureFilter(params string[] features)
    {
        _features = features;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var featureChecker = context.HttpContext.RequestServices
            .GetRequiredService<IFeatureChecker>();

        foreach (var feature in _features)
        {
            if (!await featureChecker.IsEnabledAsync(feature))
            {
                return Results.Problem(
                    detail: $"The feature '{feature}' is not enabled for your organization.",
                    statusCode: StatusCodes.Status403Forbidden,
                    title: "Feature Not Available",
                    type: $"https://api.noir.local/errors/{ErrorCodes.Feature.NotAvailable}");
            }
        }

        return await next(context);
    }
}

/// Extension method for clean registration on endpoint groups/routes.
public static class FeatureEndpointExtensions
{
    public static RouteHandlerBuilder RequireFeature(
        this RouteHandlerBuilder builder, params string[] features)
        => builder.AddEndpointFilter(new RequireFeatureFilter(features));

    public static RouteGroupBuilder RequireFeature(
        this RouteGroupBuilder builder, params string[] features)
        => builder.AddEndpointFilter(new RequireFeatureFilter(features));
}
```

**Usage on existing endpoints:**

```csharp
// src/NOIR.Web/Endpoints/BlogEndpoints.cs (existing - add filter)
var group = app.MapGroup("/api/blog")
    .WithTags("Blog")
    .RequireAuthorization()
    .RequireFeature(ModuleNames.Content.Blog);  // NEW

// src/NOIR.Web/Endpoints/ProductEndpoints.cs (existing - add filter)
var group = app.MapGroup("/api/products")
    .WithTags("Products")
    .RequireAuthorization()
    .RequireFeature(ModuleNames.Ecommerce.Products);  // NEW
```

### 6.3 New Permission Constants

```csharp
// Add to existing Permissions class
public const string FeaturesRead = "features:read";
public const string FeaturesUpdate = "features:update";
```

---

## 7. Frontend

### 7.1 API Service

```typescript
// src/NOIR.Web/frontend/src/services/features.ts

import { apiClient } from './apiClient'
import type { EffectiveFeatureState, ModuleCatalogDto, TenantFeatureStateDto } from '@/types'

export const getCurrentTenantFeatures = async (): Promise<Record<string, EffectiveFeatureState>> =>
  apiClient<Record<string, EffectiveFeatureState>>('/features/current-tenant')

export const getModuleCatalog = async (): Promise<ModuleCatalogDto> =>
  apiClient<ModuleCatalogDto>('/features/catalog')

export const getTenantFeatureStates = async (tenantId: string): Promise<ModuleCatalogDto> =>
  apiClient<ModuleCatalogDto>(`/features/tenant/${tenantId}`)

export const setModuleAvailability = async (
  tenantId: string,
  featureName: string,
  isAvailable: boolean
): Promise<TenantFeatureStateDto> =>
  apiClient<TenantFeatureStateDto>(`/features/tenant/${tenantId}/availability`, {
    method: 'PUT',
    body: JSON.stringify({ featureName, isAvailable }),
  })

export const toggleModule = async (
  featureName: string,
  isEnabled: boolean
): Promise<TenantFeatureStateDto> =>
  apiClient<TenantFeatureStateDto>('/features/toggle', {
    method: 'PUT',
    body: JSON.stringify({ featureName, isEnabled }),
  })
```

### 7.2 TypeScript Types

```typescript
// Add to src/NOIR.Web/frontend/src/types/features.ts

export interface EffectiveFeatureState {
  isAvailable: boolean
  isEnabled: boolean
  isEffective: boolean
  isCore: boolean
}

export interface FeatureDto {
  name: string
  displayNameKey: string
  descriptionKey: string
  defaultEnabled: boolean
  isAvailable?: boolean
  isEnabled?: boolean
  isEffective?: boolean
}

export interface ModuleDto {
  name: string
  displayNameKey: string
  descriptionKey: string
  icon: string
  sortOrder: number
  isCore: boolean
  defaultEnabled: boolean
  features: FeatureDto[]
  isAvailable?: boolean
  isEnabled?: boolean
  isEffective?: boolean
}

export interface ModuleCatalogDto {
  modules: ModuleDto[]
}

export interface TenantFeatureStateDto {
  featureName: string
  isAvailable: boolean
  isEnabled: boolean
}
```

### 7.3 React Hooks

```typescript
// src/NOIR.Web/frontend/src/hooks/useFeatures.ts

import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useCallback, useMemo } from 'react'
import { useAuthContext } from '@/contexts/AuthContext'
import { getCurrentTenantFeatures } from '@/services/features'
import type { EffectiveFeatureState } from '@/types'

const featureKeys = {
  all: ['features'] as const,
  currentTenant: () => [...featureKeys.all, 'current-tenant'] as const,
  catalog: () => [...featureKeys.all, 'catalog'] as const,
  tenant: (id: string) => [...featureKeys.all, 'tenant', id] as const,
}

export const useFeatures = () => {
  const { isAuthenticated } = useAuthContext()
  const queryClient = useQueryClient()

  const { data: features, isLoading } = useQuery({
    queryKey: featureKeys.currentTenant(),
    queryFn: getCurrentTenantFeatures,
    enabled: isAuthenticated,
    staleTime: 5 * 60 * 1000, // 5 minutes (matches backend cache)
  })

  const featureMap = useMemo(
    () => features ?? ({} as Record<string, EffectiveFeatureState>),
    [features]
  )

  const isFeatureEnabled = useCallback(
    (featureName: string): boolean =>
      featureMap[featureName]?.isEffective ?? true, // Default: enabled if unknown
    [featureMap]
  )

  const getFeatureState = useCallback(
    (featureName: string): EffectiveFeatureState =>
      featureMap[featureName] ?? { isAvailable: true, isEnabled: true, isEffective: true, isCore: false },
    [featureMap]
  )

  const refreshFeatures = useCallback(
    () => queryClient.invalidateQueries({ queryKey: featureKeys.currentTenant() }),
    [queryClient]
  )

  return {
    features: featureMap,
    isLoading,
    isFeatureEnabled,
    getFeatureState,
    refreshFeatures,
  }
}

/// Simple hook for checking a single feature in a component.
export const useFeature = (featureName: string): boolean => {
  const { isFeatureEnabled } = useFeatures()
  return isFeatureEnabled(featureName)
}
```

### 7.4 Sidebar Integration

In `Sidebar.tsx`, add feature filtering alongside existing permission filtering:

```typescript
// Add to NavItem interface
interface NavItem {
  titleKey: string
  icon: React.ElementType
  path: string
  permission?: PermissionKey
  feature?: string          // NEW: Module/feature name to check
}

// In navSections, add feature keys:
const navSections: NavSection[] = [
  {
    items: [
      { titleKey: 'nav.dashboard', icon: LayoutDashboard, path: '/portal' },
    ],
  },
  {
    labelKey: 'nav.catalog',
    items: [
      { titleKey: 'ecommerce.products', icon: Package, path: '/portal/ecommerce/products',
        permission: Permissions.ProductsRead, feature: 'Ecommerce.Products' },   // NEW
      { titleKey: 'ecommerce.categories', icon: FolderTree, path: '/portal/ecommerce/categories',
        permission: Permissions.ProductCategoriesRead, feature: 'Ecommerce.Categories' },
      // ... etc
    ],
  },
  {
    labelKey: 'nav.content',
    items: [
      { titleKey: 'blog.posts', icon: FileText, path: '/portal/blog/posts',
        permission: Permissions.BlogPostsRead, feature: 'Content.Blog' },        // NEW
      // ...
    ],
  },
]

// Update filtering logic:
const filteredSections = useMemo(() => {
  const { isFeatureEnabled } = useFeatures() // NEW
  return navSections
    .map(section => ({
      ...section,
      items: section.items.filter(item => {
        const hasPermission = !item.permission || hasPermission(item.permission)
        const hasFeature = !item.feature || isFeatureEnabled(item.feature) // NEW
        return hasPermission && hasFeature
      }),
    }))
    .filter(section => section.items.length > 0)
}, [permissions, features]) // Add features dependency
```

### 7.5 Route Guard Component

```typescript
// src/NOIR.Web/frontend/src/components/guards/FeatureGuard.tsx

import { useFeature } from '@/hooks/useFeatures'
import { useTranslation } from 'react-i18next'
import { ShieldOff } from 'lucide-react'

interface FeatureGuardProps {
  feature: string
  children: React.ReactNode
}

export const FeatureGuard = ({ feature, children }: FeatureGuardProps) => {
  const isEnabled = useFeature(feature)
  const { t } = useTranslation('common')

  if (!isEnabled) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[400px] gap-4">
        <ShieldOff className="h-16 w-16 text-muted-foreground" />
        <h2 className="text-xl font-semibold">{t('features.moduleNotAvailable')}</h2>
        <p className="text-muted-foreground text-center max-w-md">
          {t('features.moduleNotAvailableDescription')}
        </p>
      </div>
    )
  }

  return <>{children}</>
}
```

**Usage in routes:**

```typescript
// In router configuration, wrap feature-gated routes:
<Route path="/portal/blog/*" element={
  <FeatureGuard feature="Content.Blog">
    <BlogRoutes />
  </FeatureGuard>
} />
```

### 7.6 SignalR Integration

Listen for `features_updated` notification to auto-refresh:

```typescript
// In existing SignalR connection handler (NotificationContext or similar):
connection.on('ReceiveNotification', (notification) => {
  if (notification.type === 'features_updated') {
    queryClient.invalidateQueries({ queryKey: ['features', 'current-tenant'] })
  }
  // ... existing notification handling
})
```

### 7.7 Tenant Settings - Modules Tab

Add a new tab to the existing TenantSettingsPage:

```typescript
// src/NOIR.Web/frontend/src/portal-app/settings/components/tenant-settings/ModulesSettingsTab.tsx

// Tree view of modules with toggle switches
// - Platform admin: sees "Available" + "Enabled" toggles
// - Tenant admin: sees only "Enabled" toggles for available modules
// - Core modules: shown as always-on (no toggle)
// - Child features: indent under parent, disabled if parent is OFF
// - Uses existing Switch component from uikit
// - Uses optimistic mutations with TanStack Query
```

### 7.8 Localization Keys

```json
// public/locales/en/common.json - add:
{
  "features": {
    "moduleNotAvailable": "Module Not Available",
    "moduleNotAvailableDescription": "This module is not enabled for your organization. Contact your administrator to enable it.",
    "modulesAndFeatures": "Modules & Features",
    "modulesDescription": "Enable or disable modules for your organization",
    "available": "Available",
    "enabled": "Enabled",
    "coreModule": "Core (Always On)",
    "parentDisabled": "Parent module is disabled",
    "toggleSuccess": "Module updated successfully",
    "toggleError": "Failed to update module"
  },
  "tenantSettings": {
    "tabs": {
      "modules": "Modules"
    }
  }
}
```

```json
// public/locales/vi/common.json - add:
{
  "features": {
    "moduleNotAvailable": "Module Không Khả Dụng",
    "moduleNotAvailableDescription": "Module này chưa được bật cho tổ chức của bạn. Liên hệ quản trị viên để bật.",
    "modulesAndFeatures": "Modules & Tính Năng",
    "modulesDescription": "Bật hoặc tắt các module cho tổ chức của bạn",
    "available": "Khả dụng",
    "enabled": "Đã bật",
    "coreModule": "Cốt lõi (Luôn bật)",
    "parentDisabled": "Module cha đã tắt",
    "toggleSuccess": "Cập nhật module thành công",
    "toggleError": "Không thể cập nhật module"
  },
  "tenantSettings": {
    "tabs": {
      "modules": "Modules"
    }
  }
}
```

---

## 8. Background Job Integration

### 8.1 TenantJobRunner Helper

```csharp
// src/NOIR.Infrastructure/Services/TenantJobRunner.cs

public interface ITenantJobRunner
{
    /// Executes an action for each active tenant where the specified feature is enabled.
    Task RunForEnabledTenantsAsync(
        string featureName,
        Func<string, IServiceScope, Task> action,
        CancellationToken ct = default);
}

public sealed class TenantJobRunner : ITenantJobRunner, IScopedService
{
    private readonly TenantStoreDbContext _tenantStore;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantJobRunner> _logger;

    public async Task RunForEnabledTenantsAsync(
        string featureName,
        Func<string, IServiceScope, Task> action,
        CancellationToken ct = default)
    {
        var tenants = await _tenantStore.Set<Tenant>()
            .Where(t => t.IsActive)
            .TagWith("TenantJobRunner:GetActiveTenants")
            .ToListAsync(ct);

        foreach (var tenant in tenants)
        {
            using var scope = _serviceProvider.CreateScope();

            // Set tenant context for this scope
            var tenantSetter = scope.ServiceProvider
                .GetRequiredService<IMultiTenantContextSetter>();
            tenantSetter.MultiTenantContext = new MultiTenantContext<Tenant>(tenant);

            var featureChecker = scope.ServiceProvider
                .GetRequiredService<IFeatureChecker>();

            if (!await featureChecker.IsEnabledAsync(featureName, ct))
            {
                _logger.LogDebug(
                    "Skipping tenant {TenantId} for feature {Feature} (disabled)",
                    tenant.Id, featureName);
                continue;
            }

            try
            {
                await action(tenant.Id, scope);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Job failed for tenant {TenantId}, feature {Feature}",
                    tenant.Id, featureName);
            }
        }
    }
}
```

**Usage in existing jobs:**

```csharp
// ProductFilterIndexMaintenanceJob (modified)
public async Task ExecuteAsync(CancellationToken ct)
{
    await _tenantJobRunner.RunForEnabledTenantsAsync(
        ModuleNames.Ecommerce.Attributes,
        async (tenantId, scope) =>
        {
            var service = scope.ServiceProvider
                .GetRequiredService<IProductFilterIndexService>();
            await service.MaintenanceAsync(ct);
        },
        ct);
}
```

---

## 9. Testing Strategy

### 9.1 Unit Tests

```
tests/NOIR.Application.UnitTests/Modules/
├── ModuleCatalogTests.cs                    (all 33 modules registered, no duplicates)
├── FeatureCheckerTests.cs                   (resolution logic, hierarchy, core bypass)
├── Commands/
│   ├── SetModuleAvailabilityCommandTests.cs (validation, core protection)
│   └── ToggleModuleCommandTests.cs          (validation, parent check)
└── Queries/
    └── GetCurrentTenantFeaturesQueryTests.cs

tests/NOIR.Domain.UnitTests/
├── TenantModuleStateTests.cs                (entity factory methods, state transitions)
└── RequiresFeatureAttributeTests.cs
```

### 9.2 Key Test Cases

| Test | Assertion |
|------|-----------|
| Core module always returns enabled | `IsEnabledAsync("Core.Auth")` == true regardless of DB state |
| Default state with no DB rows | All modules return `DefaultEnabled` value |
| Platform unavailable overrides tenant toggle | If `IsAvailable=false`, `IsEffective=false` even if `IsEnabled=true` |
| Parent module off disables children | Parent OFF → all children `IsEffective=false` |
| Re-enabling parent restores children | Parent back ON → children use their own saved state |
| Validator rejects core module toggle | `SetModuleAvailabilityCommand` with core module → validation error |
| Wolverine middleware blocks disabled feature | `[RequiresFeature("X")]` + X disabled → `FeatureNotAvailableException` |
| Wolverine middleware passes enabled feature | `[RequiresFeature("X")]` + X enabled → handler executes |
| Cache invalidation refreshes state | Toggle → invalidate → next check reads fresh from DB |

### 9.3 Architecture Tests

```csharp
// tests/NOIR.Architecture.Tests/FeatureManagementArchitectureTests.cs

[Fact]
public void All_NonCore_Commands_Should_Have_RequiresFeature_Attribute()
{
    // Ensures developers don't forget to gate commands
    // (can be relaxed with [ExcludeFromFeatureCheck] attribute for shared commands)
}

[Fact]
public void Module_Names_Should_Match_Feature_Directory_Structure()
{
    // Validates that ModuleNames constants align with Features/ folder names
}

[Fact]
public void No_Duplicate_Module_Or_Feature_Names()
{
    // All IModuleDefinition.Name values must be unique
    // All FeatureDefinition.Name values must be unique
}
```

---

## 10. File Inventory

### New Files (Create)

| Layer | File | Purpose |
|-------|------|---------|
| **Domain** | `src/NOIR.Domain/Entities/TenantModuleState.cs` | Entity |
| **Domain** | `src/NOIR.Domain/Interfaces/IModuleDefinition.cs` | Module definition interface |
| **Domain** | `src/NOIR.Domain/Interfaces/IModuleCatalog.cs` | Catalog interface |
| **Domain** | `src/NOIR.Domain/Interfaces/IFeatureChecker.cs` | Feature checker interface |
| **Domain** | `src/NOIR.Domain/Common/FeatureDefinition.cs` | Feature definition record |
| **Domain** | `src/NOIR.Domain/Common/RequiresFeatureAttribute.cs` | Command attribute |
| **Application** | `src/NOIR.Application/Modules/ModuleNames.cs` | Name constants |
| **Application** | `src/NOIR.Application/Modules/Core/*.cs` (8 files) | Core module definitions |
| **Application** | `src/NOIR.Application/Modules/Ecommerce/*.cs` (14 files) | E-commerce module defs |
| **Application** | `src/NOIR.Application/Modules/Content/*.cs` (3 files) | Content module defs |
| **Application** | `src/NOIR.Application/Modules/Platform/*.cs` (3 files) | Platform module defs |
| **Application** | `src/NOIR.Application/Modules/Analytics/*.cs` (2 files) | Analytics module defs |
| **Application** | `src/NOIR.Application/Features/FeatureManagement/**` | Commands, Queries, DTOs, Specs |
| **Application** | `src/NOIR.Application/Common/Exceptions/FeatureNotAvailableException.cs` | Exception |
| **Infrastructure** | `src/NOIR.Infrastructure/Persistence/Configurations/TenantModuleStateConfiguration.cs` | EF config |
| **Infrastructure** | `src/NOIR.Infrastructure/Services/ModuleCatalog.cs` | Catalog implementation |
| **Infrastructure** | `src/NOIR.Infrastructure/Services/FeatureChecker.cs` | Feature checker impl |
| **Infrastructure** | `src/NOIR.Infrastructure/Services/FeatureCacheInvalidator.cs` | Cache invalidation |
| **Infrastructure** | `src/NOIR.Infrastructure/Services/TenantJobRunner.cs` | Job helper |
| **Infrastructure** | `src/NOIR.Infrastructure/Middleware/FeatureCheckMiddleware.cs` | Wolverine middleware |
| **Web** | `src/NOIR.Web/Endpoints/FeatureManagementEndpoints.cs` | API endpoints |
| **Web** | `src/NOIR.Web/Filters/RequireFeatureFilter.cs` | Endpoint filter |
| **Frontend** | `src/services/features.ts` | API service |
| **Frontend** | `src/types/features.ts` | TypeScript types |
| **Frontend** | `src/hooks/useFeatures.ts` | React hooks |
| **Frontend** | `src/components/guards/FeatureGuard.tsx` | Route guard |
| **Frontend** | `src/portal-app/settings/components/tenant-settings/ModulesSettingsTab.tsx` | Admin UI |
| **Tests** | `tests/NOIR.Application.UnitTests/Modules/*.cs` | Unit tests |
| **Tests** | `tests/NOIR.Domain.UnitTests/TenantModuleStateTests.cs` | Domain tests |

### Modified Files (Edit)

| Layer | File | Change |
|-------|------|--------|
| **Domain** | `src/NOIR.Domain/Common/ErrorCodes.cs` | Add `Feature` error codes |
| **Domain** | `src/NOIR.Domain/GlobalUsings.cs` | Add module namespaces |
| **Infrastructure** | `src/NOIR.Infrastructure/Common/CacheKeys.cs` | Add `TenantFeatures` key |
| **Infrastructure** | `src/NOIR.Infrastructure/Persistence/ApplicationDbContext.cs` | Add `DbSet<TenantModuleState>` |
| **Web** | `src/NOIR.Web/Program.cs` | Register Wolverine middleware, map endpoints |
| **Web** | `src/NOIR.Web/Middleware/ExceptionHandlingMiddleware.cs` | Handle `FeatureNotAvailableException` |
| **Web** | `src/NOIR.Web/Endpoints/*.cs` (30+ files) | Add `.RequireFeature()` to endpoint groups |
| **Frontend** | `src/components/portal/Sidebar.tsx` | Add feature filtering to nav items |
| **Frontend** | `src/portal-app/settings/features/tenant-settings/TenantSettingsPage.tsx` | Add Modules tab |
| **Frontend** | `public/locales/en/common.json` | Add feature management keys |
| **Frontend** | `public/locales/vi/common.json` | Add feature management keys (Vietnamese) |
| **Frontend** | `src/types/index.ts` | Export feature types |

### Migration

| Action | Command |
|--------|---------|
| Create migration | `dotnet ef migrations add AddTenantModuleStates --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext --output-dir Migrations/App` |
| Apply migration | `dotnet ef database update --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext` |

---

## 11. Implementation Phases

### Phase 1: Foundation (Backend Core)
1. Domain entities, interfaces, attribute
2. Module definitions (all 33)
3. ModuleCatalog + FeatureChecker + cache
4. EF configuration + migration
5. Unit tests for resolution logic

### Phase 2: CQRS + API
6. Commands + Queries + Validators + Handlers
7. Feature Management endpoints
8. Wolverine middleware
9. Exception handling extension
10. Integration tests

### Phase 3: Endpoint Gating
11. RequireFeatureFilter
12. Add `.RequireFeature()` to all 30+ endpoint groups
13. Endpoint filter tests

### Phase 4: Frontend
14. TypeScript types + API service
15. useFeatures hook
16. FeatureGuard component
17. Sidebar integration
18. Modules Settings tab
19. SignalR integration
20. Localization (EN + VI)

### Phase 5: Background Jobs + Seeders
21. TenantJobRunner helper
22. Update existing jobs to use feature checks
23. Seeder integration

### Phase 6: Quality Gates
24. `dotnet build src/NOIR.sln` → 0 errors
25. `dotnet test src/NOIR.sln` → ALL pass
26. `pnpm run build` → 0 errors
27. `pnpm build-storybook` → 0 errors
28. Architecture test: all non-core commands have `[RequiresFeature]`

---

## 12. Sequence Diagrams

### Feature Check Flow (API Request)

```
Client → API Endpoint
         │
         ├─ RequireFeatureFilter (.RequireFeature("X"))
         │   ├─ IFeatureChecker.IsEnabledAsync("X")
         │   │   ├─ Check per-request cache → HIT? Return
         │   │   ├─ Check FusionCache → HIT? Populate request cache, return
         │   │   ├─ Query DB (TenantModuleStates WHERE TenantId = current)
         │   │   ├─ Merge with code-defined defaults (IModuleCatalog)
         │   │   ├─ Resolve effective states (hierarchy)
         │   │   ├─ Store in FusionCache (5 min TTL)
         │   │   └─ Store in request cache
         │   ├─ Enabled? → Continue to handler
         │   └─ Disabled? → 403 Forbidden
         │
         ├─ Wolverine Pipeline
         │   ├─ FeatureCheckMiddleware.BeforeAsync()
         │   │   ├─ Read [RequiresFeature] from command type
         │   │   ├─ IFeatureChecker.IsEnabledAsync() (request cache → instant)
         │   │   ├─ Enabled? → Continue
         │   │   └─ Disabled? → throw FeatureNotAvailableException
         │   ├─ FluentValidation middleware
         │   ├─ Handler executes
         │   └─ HandlerAuditMiddleware
         │
         └─ Response
```

### Feature Toggle Flow (Admin)

```
Admin UI → PUT /api/features/toggle { featureName: "Content.Blog", isEnabled: false }
           │
           ├─ ToggleModuleCommand → Handler
           │   ├─ Validate (exists, not core, parent available)
           │   ├─ Upsert TenantModuleState row
           │   ├─ SaveChangesAsync
           │   └─ FeatureCacheInvalidator.InvalidateAsync(tenantId)
           │       ├─ FusionCache.RemoveAsync("features:tenant:{id}")
           │       └─ SignalR → tenant_{tenantId} group → "features_updated"
           │
           ├─ Frontend receives SignalR notification
           │   └─ queryClient.invalidateQueries(['features', 'current-tenant'])
           │       └─ Re-fetches GET /api/features/current-tenant
           │           └─ Sidebar re-renders (Blog items disappear)
           │
           └─ Response: { featureName: "Content.Blog", isAvailable: true, isEnabled: false }
```

---

> **Next Step**: After this design is approved, use `/sc:implement` to begin phased implementation.
