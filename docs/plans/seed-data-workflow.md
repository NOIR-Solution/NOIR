# Seed Data System - Implementation Workflow

> Generated from approved design plan. Execute phases in order.
> **Team pattern:** `backend-dev` + `test-writer` (per CLAUDE.md team rules)

---

## Phase 1: Foundation Infrastructure

**Goal:** Create the core interfaces, settings, and context classes that all modules depend on.
**Estimated files:** 5 new + 1 modified
**Dependencies:** None (greenfield)

### Task 1.1: SeedDataSettings

**File:** `src/NOIR.Infrastructure/Persistence/SeedData/SeedDataSettings.cs`

Create configuration POCO bound from `"SeedData"` section:

```csharp
SeedDataSettings
  ├── Enabled: bool (default false)
  ├── Modules: SeedDataModuleSettings
  │   ├── Catalog: bool (default true)
  │   ├── Blog: bool (default true)
  │   ├── Commerce: bool (default true)
  │   └── Engagement: bool (default true)
  └── AdditionalTenants: List<SeedTenantSettings>
      ├── Identifier: string
      ├── Name: string
      ├── Domain: string?
      ├── Description: string?
      ├── AdminEmail: string
      ├── AdminPassword: string (default "123qwe")
      ├── AdminFirstName: string
      └── AdminLastName: string
```

**Pattern:** Follow `PlatformSettings.cs` structure with `const string SectionName`.

### Task 1.2: ISeedDataModule Interface

**File:** `src/NOIR.Infrastructure/Persistence/SeedData/ISeedDataModule.cs`

```
interface ISeedDataModule
  ├── Order: int (100=Catalog, 200=Blog, 300=Commerce, 400=Engagement)
  ├── ModuleName: string (for logging)
  └── SeedAsync(SeedDataContext, CancellationToken): Task
```

### Task 1.3: SeedDataContext

**File:** `src/NOIR.Infrastructure/Persistence/SeedData/SeedDataContext.cs`

Per-tenant execution context:
- `ApplicationDbContext DbContext`
- `ILogger Logger`
- `IServiceProvider ServiceProvider`
- `SeedDataSettings Settings`
- `Tenant CurrentTenant`
- `string TenantAdminUserId` (for Post.AuthorId, Receipt.ConfirmedBy)

### Task 1.4: SeedDataConstants

**File:** `src/NOIR.Infrastructure/Persistence/SeedData/SeedDataConstants.cs`

- `DeterministicGuid(string seed)` — SHA256 hash → Guid (first 16 bytes)
- `TenantGuid(string tenantId, string seed)` — scoped to tenant
- `BaseTimestamp` — `2026-01-01T00:00:00+07:00` (UTC+7 Vietnam)
- Helper: `SpreadDate(int dayOffset)` — `BaseTimestamp.AddDays(offset)` for realistic date distribution

### Task 1.5: GlobalUsings Update

**File:** `src/NOIR.Infrastructure/GlobalUsings.cs`

Add: `global using NOIR.Infrastructure.Persistence.SeedData;`

### Checkpoint 1

```bash
dotnet build src/NOIR.sln  # Must compile with 0 errors
```

---

## Phase 2: Vietnamese Data Definitions

**Goal:** Define all static seed data as pure data classes (no logic). These are the "what" — module classes handle the "how".
**Estimated files:** 5 new
**Dependencies:** Phase 1 (uses `SeedDataConstants.TenantGuid`)

### Task 2.1: VietnameseAddresses

**File:** `src/NOIR.Infrastructure/Persistence/SeedData/Data/VietnameseAddresses.cs`

Static collection of ~18 realistic Vietnamese addresses:
- 8 Ho Chi Minh (Quan 1, Quan 3, Binh Thanh, Thu Duc, Phu Nhuan, Tan Binh, Go Vap, Quan 7)
- 6 Hanoi (Hoan Kiem, Dong Da, Ba Dinh, Cau Giay, Thanh Xuan, Ha Dong)
- 4 Da Nang (Hai Chau, Thanh Khe, Son Tra, Ngu Hanh Son)

Fields: `FullName, Phone, AddressLine1, Ward, District, Province`

### Task 2.2: CatalogData

**File:** `src/NOIR.Infrastructure/Persistence/SeedData/Data/CatalogData.cs`

Define record types for structured data:

```csharp
// Category definitions (parent-child via ParentSlug reference)
record CategoryDef(string Name, string Slug, string? ParentSlug, string? Description);

// Brand definitions
record BrandDef(string Name, string Slug, string? Description);

// Attribute definitions with values
record AttributeDef(string Code, string Name, AttributeType Type,
    (string Value, string DisplayValue, string? ColorCode)[]? Values);

// Product definitions with variants
record ProductDef(string Name, string Slug, string CategorySlug, string BrandSlug,
    decimal BasePrice, string ShortDescription, string? Description,
    VariantDef[] Variants, string? ImageAssetName);
record VariantDef(string Name, decimal Price, string? Sku,
    Dictionary<string, string>? Options, int Stock);
```

**Data scale per tenant (3 vertical profiles):**

| Vertical | Categories | Brands | Products | Focus |
|----------|-----------|--------|----------|-------|
| `default` | 6 (general) | 3 | 18 | Mixed: clothing, electronics, accessories |
| `fashion` | 6 (apparel) | 3 | 15 | Ao, Quan, Giay, Phu kien |
| `tech` | 6 (electronics) | 3 | 15 | Dien thoai, Laptop, Phu kien |

Method: `GetCategories(string tenantId)`, `GetProducts(string tenantId)` — returns different data per vertical.

### Task 2.3: BlogData

**File:** `src/NOIR.Infrastructure/Persistence/SeedData/Data/BlogData.cs`

```csharp
record PostCategoryDef(string Name, string Slug, string? Description, string? ParentSlug);
record PostTagDef(string Name, string Slug, string? Color);
record PostDef(string Title, string Slug, string CategorySlug, string[] TagSlugs,
    PostStatus Status, string Excerpt, string ContentHtml, string? ImageAssetName,
    int DayOffset);  // DayOffset from BaseTimestamp for date spread
```

**Content:** Vietnamese blog posts (~500-800 words each):
- Fashion trends, buying guides, product reviews
- Tech comparisons, unboxing reviews
- Lifestyle, seasonal promotions
- ContentHtml: Simple HTML paragraphs (no BlockNote JSON needed for seed)

### Task 2.4: CommerceData

**File:** `src/NOIR.Infrastructure/Persistence/SeedData/Data/CommerceData.cs`

```csharp
record CustomerDef(string FirstName, string LastName, string Email, string? Phone,
    int AddressIndex);  // Index into VietnameseAddresses

record OrderDef(string OrderNumber, int CustomerIndex, int DayOffset,
    OrderStatus TargetStatus, string? CancellationReason,
    OrderItemDef[] Items);
record OrderItemDef(string ProductSlug, string VariantName, int Quantity);

record InventoryReceiptDef(string ReceiptNumber, InventoryReceiptType Type,
    InventoryReceiptStatus TargetStatus, string? Notes,
    ReceiptItemDef[] Items);
record ReceiptItemDef(string ProductSlug, string VariantName, int Quantity, decimal UnitCost);
```

### Task 2.5: EngagementData

**File:** `src/NOIR.Infrastructure/Persistence/SeedData/Data/EngagementData.cs`

```csharp
record PromotionDef(string Name, string Code, PromotionType Type,
    DiscountType DiscountType, decimal DiscountValue,
    int StartDayOffset, int EndDayOffset,
    decimal? MinOrderValue, int? UsageLimitTotal,
    PromotionApplyLevel ApplyLevel);
```

4 promotions: GIAM10, FREESHIP, TET2026, VIP20

### Checkpoint 2

```bash
dotnet build src/NOIR.sln  # Must compile with 0 errors
```

---

## Phase 3: Image Assets

**Goal:** Add embedded WebP images and configure build to include them as resources.
**Estimated:** ~25 image files + 1 csproj modification
**Dependencies:** None (independent of Phase 2)

### Task 3.1: Source Product Images

Create directory: `src/NOIR.Infrastructure/Persistence/SeedData/Assets/products/`

Add 15-20 WebP images (~50-100KB each, 800x600 resolution):
- `ao-thun-trang.webp` — White basic t-shirt
- `ao-polo-den.webp` — Black polo shirt
- `quan-jeans-xanh.webp` — Blue jeans
- `giay-the-thao.webp` — Sports shoes
- `tui-xach-da.webp` — Leather bag
- `dong-ho-nam.webp` — Men's watch
- `ao-khoac-gio.webp` — Windbreaker jacket
- `vay-dam-nu.webp` — Women's dress
- `mu-luoi-trai.webp` — Baseball cap
- `kinh-mat.webp` — Sunglasses
- `dien-thoai.webp` — Smartphone
- `laptop.webp` — Laptop
- `tai-nghe.webp` — Headphones
- `sac-du-phong.webp` — Power bank
- `loa-bluetooth.webp` — Bluetooth speaker

**Source strategy:** Use royalty-free images from Unsplash/Pexels, resize to 800x600 WebP, optimize to <100KB each. Total ~1.5MB.

### Task 3.2: Source Blog Images

Create directory: `src/NOIR.Infrastructure/Persistence/SeedData/Assets/blog/`

Add 5-8 WebP images (~80-120KB each, 1200x630 resolution — OG image ratio):
- `xu-huong-thoi-trang.webp` — Fashion trends
- `review-cong-nghe.webp` — Tech review
- `meo-mua-sam.webp` — Shopping tips
- `phong-cach-nam.webp` — Men's style
- `phu-kien-hot.webp` — Hot accessories
- `so-sanh-dien-thoai.webp` — Phone comparison
- `huong-dan-chon-giay.webp` — Shoe selection guide

**Source strategy:** Same as products. Total ~0.7MB.

### Task 3.3: Update .csproj

**File:** `src/NOIR.Infrastructure/NOIR.Infrastructure.csproj`

Add embedded resource ItemGroup:

```xml
<ItemGroup>
  <EmbeddedResource Include="Persistence\SeedData\Assets\**\*.webp" />
</ItemGroup>
```

### Checkpoint 3

```bash
dotnet build src/NOIR.sln  # Verify embedded resources compile
```

---

## Phase 4: Seed Modules

**Goal:** Implement the 4 ISeedDataModule classes that create entities from data definitions.
**Estimated files:** 4 new
**Dependencies:** Phase 1 + Phase 2 + Phase 3

### Task 4.1: CatalogSeedModule

**File:** `src/NOIR.Infrastructure/Persistence/SeedData/Modules/CatalogSeedModule.cs`

```
Order: 100
ModuleName: "Catalog"

SeedAsync flow:
  1. Idempotency check: if Products exist for tenant → return
  2. Seed ProductCategories (parents first, then children)
     - Use ProductCategory.Create(name, slug, parentId, tenantId)
     - Build slug→ID lookup dictionary for product assignment
  3. Seed Brands
     - Use Brand.Create(name, slug, tenantId)
     - Build slug→ID lookup dictionary
  4. Seed ProductAttributes + Values
     - Use ProductAttribute.Create(code, name, type, tenantId)
     - For Select types: attr.AddValue(value, displayValue, sortOrder)
  5. SaveChangesAsync() — flush categories/brands/attributes
  6. Seed Products with Variants and Images
     For each ProductDef:
       a. Product.Create(name, slug, basePrice, "VND", tenantId)
       b. product.SetCategory(categoryId from lookup)
       c. product.SetBrandId(brandId from lookup)
       d. For each VariantDef: product.AddVariant(name, price, sku, options)
       e. Process image (if ImageAssetName):
          - Load embedded resource stream
          - IImageProcessor.ProcessAsync(stream, fileName, options)
          - Create MediaFile entity
          - product.AddImage(defaultUrl, altText, isPrimary: true)
       f. Set variant stock: variant.SetStock(stock)
       g. If status == Active: product.Publish()
  7. SaveChangesAsync()
  8. Log summary: "{N} products, {M} variants, {K} images seeded"
```

**Critical:** Use `"images/seed-products/{productId}"` as StorageFolder (not `"products/"` — see FileEndpoints allowedPrefixes).

### Task 4.2: BlogSeedModule

**File:** `src/NOIR.Infrastructure/Persistence/SeedData/Modules/BlogSeedModule.cs`

```
Order: 200
ModuleName: "Blog"

SeedAsync flow:
  1. Idempotency check: if Posts exist for tenant → return
  2. Seed PostCategories (parents first)
     - PostCategory.Create(name, slug, description, parentId, tenantId)
  3. Seed PostTags
     - PostTag.Create(name, slug, description, color, tenantId)
  4. SaveChangesAsync() — flush categories/tags
  5. Seed Posts
     For each PostDef:
       a. Post.Create(title, slug, authorId=context.TenantAdminUserId, tenantId)
       b. post.UpdateContent(title, slug, excerpt, null, contentHtml)
       c. post.SetCategory(categoryId from lookup)
       d. Process featured image (if ImageAssetName):
          - IImageProcessor.ProcessAsync → MediaFile → post.SetFeaturedImage(mediaFileId, altText)
       e. Apply status:
          - Published: post.Publish()
          - Scheduled: post.Schedule(futureDate)
          - Archived: post.Publish() then post.Archive() (must be published first to archive)
          - Draft: no-op (default status)
  6. SaveChangesAsync() — flush posts
  7. Seed PostTagAssignments
     - PostTagAssignment.Create(postId, tagId, tenantId)
     - Update tag.PostCount accordingly
  8. SaveChangesAsync()
  9. Log summary
```

### Task 4.3: CommerceSeedModule

**File:** `src/NOIR.Infrastructure/Persistence/SeedData/Modules/CommerceSeedModule.cs`

```
Order: 300
ModuleName: "Commerce"
Depends on: CatalogSeedModule (uses deterministic product/variant GUIDs)

SeedAsync flow:
  1. Idempotency check: if Customers exist for tenant → return
  2. Seed Customers + Addresses
     - Customer.Create(userId=null, email, firstName, lastName, phone, tenantId)
     - For addresses: use DbContext.Set<CustomerAddress>().Add(...)
     - Set segment/tier based on predefined profiles
  3. SaveChangesAsync()
  4. Seed Orders
     For each OrderDef:
       a. Order.Create(orderNumber, customerEmail, subTotal, grandTotal, "VND", tenantId)
       b. Set customer info: order.SetCustomerInfo(customerId, name, phone)
       c. Set addresses: order.SetShippingAddress(address), order.SetBillingAddress(address)
       d. Add items: order.AddItem(productId, variantId, name, variantName, price, qty, sku, imageUrl)
          - Use deterministic GUIDs to reference seeded products/variants
       e. Apply lifecycle based on TargetStatus:
          - Pending: no-op
          - Confirmed: order.Confirm()
          - Processing: order.Confirm() → order.StartProcessing()
          - Shipped: Confirm → StartProcessing → order.Ship(trackingNumber, carrier)
          - Delivered: Confirm → StartProcessing → Ship → order.MarkAsDelivered()
          - Completed: full chain → order.Complete()
          - Cancelled: order.Cancel(reason)
  5. SaveChangesAsync()
  6. Seed InventoryReceipts
     For each ReceiptDef:
       a. InventoryReceipt.Create(receiptNumber, type, notes, tenantId)
       b. receipt.AddItem(variantId, productId, name, variantName, sku, qty, unitCost)
       c. If TargetStatus == Confirmed: receipt.Confirm(context.TenantAdminUserId)
  7. SaveChangesAsync()
  8. Log summary
```

**Date spreading:** Set `CreatedAt` via reflection after entity creation to distribute orders over 3 months.

### Task 4.4: EngagementSeedModule

**File:** `src/NOIR.Infrastructure/Persistence/SeedData/Modules/EngagementSeedModule.cs`

```
Order: 400
ModuleName: "Engagement"

SeedAsync flow:
  1. Idempotency check: if Promotions exist for tenant → return
  2. Seed Promotions
     For each PromotionDef:
       a. Promotion.Create(name, code, type, discountType, discountValue,
            startDate, endDate, applyLevel, ...)
       b. If active: set IsActive = true via appropriate method
  3. SaveChangesAsync()
  4. Log summary
```

### Checkpoint 4

```bash
dotnet build src/NOIR.sln  # Must compile with 0 errors
```

---

## Phase 5: Orchestrator + Integration

**Goal:** Wire everything together — the orchestrator manages tenant iteration and the integration point connects to existing seeder pipeline.
**Estimated files:** 1 new + 2 modified
**Dependencies:** Phase 1 + Phase 4

### Task 5.1: SeedDataOrchestrator

**File:** `src/NOIR.Infrastructure/Persistence/SeedData/SeedDataOrchestrator.cs`

```csharp
public static class SeedDataOrchestrator
{
    public static async Task ExecuteAsync(
        SeederContext seederContext,
        IServiceProvider services,
        CancellationToken ct = default)
    {
        // 1. Environment guard
        var env = services.GetRequiredService<IHostEnvironment>();
        if (!env.IsDevelopment() && !env.IsStaging()) return;

        // 2. Configuration check
        var settings = new SeedDataSettings();
        seederContext.Configuration.GetSection(SeedDataSettings.SectionName).Bind(settings);
        if (!settings.Enabled) return;

        var logger = seederContext.Logger;
        logger.LogInformation("[SeedData] Starting demo data seeding...");
        var totalSw = Stopwatch.StartNew();

        // 3. Build tenant list
        var tenants = await BuildTenantList(seederContext, settings, services, ct);

        // 4. Collect enabled modules
        var modules = GetEnabledModules(settings);

        // 5. Seed per tenant
        foreach (var (tenant, adminUserId) in tenants)
        {
            SetTenantContext(tenant, services, logger);
            var context = new SeedDataContext { ... };

            foreach (var module in modules)
            {
                var sw = Stopwatch.StartNew();
                logger.LogInformation("[SeedData] {Module} for {Tenant}...", module.ModuleName, tenant.Identifier);
                await module.SeedAsync(context, ct);
                logger.LogInformation("[SeedData] {Module} for {Tenant} done ({Ms}ms)",
                    module.ModuleName, tenant.Identifier, sw.ElapsedMilliseconds);
            }
        }

        logger.LogInformation("[SeedData] Completed in {Ms}ms", totalSw.ElapsedMilliseconds);
    }
}
```

**BuildTenantList internals:**
1. Start with `seederContext.DefaultTenant` (already created by TenantSeeder)
2. For each `AdditionalTenant` in settings:
   - Check if tenant exists in `TenantStoreDbContext`
   - If not: create via `Tenant.Create()` + save
   - Set Finbuckle context for this tenant
   - Call `RoleSeeder.SeedTenantRolesAsync()` (public method)
   - Call `UserSeeder.SeedTenantAdminUserAsync()` (internal static, same assembly)
   - Resolve admin userId
3. Return list of `(Tenant, string adminUserId)` tuples

**SetTenantContext:** Replicate the 3-line pattern from `ApplicationDbContextSeeder`:
```csharp
var tenantSetter = services.GetService<IMultiTenantContextSetter>()!;
tenantSetter.MultiTenantContext = new MultiTenantContext<Tenant>(tenant);
```

### Task 5.2: Integration Point

**File:** `src/NOIR.Infrastructure/Persistence/ApplicationDbContextSeeder.cs`

Add after line 166 (after `permissionTemplateSeeder.SeedAsync`):

```csharp
// === PHASE 9: Demo Seed Data (Development/Staging Only) ===
await SeedDataOrchestrator.ExecuteAsync(seederContext, services);
```

This is a **1-line change** to the existing orchestrator.

### Task 5.3: Configuration

**File:** `src/NOIR.Web/appsettings.Development.json`

Add the full `SeedData` section with `Enabled: true`, module toggles, and 2 additional tenants (fashion, tech).

### Checkpoint 5

```bash
dotnet build src/NOIR.sln   # 0 errors
dotnet test src/NOIR.sln    # ALL tests pass (SeedData.Enabled=false in test env)
```

---

## Phase 6: FileEndpoints Fix

**Goal:** Fix pre-existing bug where product images aren't served because `"products/"` isn't in allowedPrefixes.
**Estimated files:** 1 modified
**Dependencies:** None (independent)

### Task 6.1: Add products/ to allowedPrefixes

**File:** `src/NOIR.Web/Endpoints/FileEndpoints.cs`

Change:
```csharp
var allowedPrefixes = new[] { "avatars/", "blog/", "branding/", "content/", "images/" };
```
To:
```csharp
var allowedPrefixes = new[] { "avatars/", "blog/", "branding/", "content/", "images/", "products/" };
```

### Checkpoint 6

```bash
dotnet build src/NOIR.sln  # 0 errors
dotnet test src/NOIR.sln   # ALL tests pass
```

---

## Phase 7: End-to-End Verification

**Goal:** Full quality gate pass and manual verification of seeded data.

### Task 7.1: Quality Gates

```bash
dotnet build src/NOIR.sln                              # 0 errors
dotnet test src/NOIR.sln                               # ALL 10,888+ tests pass
cd src/NOIR.Web/frontend && pnpm run build             # 0 errors, 0 warnings
```

### Task 7.2: Manual Smoke Test

1. Drop database: `dotnet ef database drop --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context ApplicationDbContext --force`
2. Also drop tenant store: `dotnet ef database drop --project src/NOIR.Infrastructure --startup-project src/NOIR.Web --context TenantStoreDbContext --force`
3. Run: `dotnet run --project src/NOIR.Web`
4. Verify seeder logs show:
   - `[SeedData] Starting demo data seeding...`
   - `[SeedData] Catalog for default done (Xms)`
   - `[SeedData] Blog for default done (Xms)`
   - `[SeedData] Commerce for default done (Xms)`
   - `[SeedData] Engagement for default done (Xms)`
   - Same for `fashion` and `tech` tenants
   - `[SeedData] Completed in <30000ms`

5. Login as `admin@noir.local` / `123qwe` (default tenant):
   - [ ] Dashboard: Revenue charts with data, recent orders, top products
   - [ ] Products page: 18 products with images loading correctly
   - [ ] Blog page: 10 posts with featured images
   - [ ] Orders page: 9 orders in various statuses
   - [ ] Inventory page: 3 receipts
   - [ ] Customers page: 12 customers

6. Login as `admin@fashion.noir.local` / `123qwe` (fashion tenant):
   - [ ] Separate product catalog (fashion-focused)
   - [ ] Separate blog content
   - [ ] Separate orders and customers

7. Verify images load at `/media/images/seed-products/{id}/...` and `/media/blog/seed/...`

---

## Summary

| Phase | Files | Type | Estimated Effort |
|-------|-------|------|-----------------|
| **1. Foundation** | 5 new + 1 mod | Interfaces, settings | Small |
| **2. Data Definitions** | 5 new | Static data classes | Medium (VN content writing) |
| **3. Image Assets** | ~25 images + 1 mod | Binary assets | Medium (sourcing + optimization) |
| **4. Seed Modules** | 4 new | Core logic | Large (entity creation + image processing) |
| **5. Orchestrator** | 1 new + 2 mod | Wiring | Medium (tenant iteration + context) |
| **6. FileEndpoints Fix** | 1 mod | Bug fix | Trivial |
| **7. Verification** | 0 | Testing | Medium |
| **Total** | ~18 new + 4 mod | | |

**Critical path:** Phase 1 → Phase 4 → Phase 5 (sequential)
**Parallelizable:** Phase 2 + Phase 3 can run alongside Phase 1
