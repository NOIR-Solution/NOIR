# Phase 8 Sprint 1: Workflow Plan

**Generated:** January 25, 2026
**Updated:** January 26, 2026
**Sprint Duration:** Week 1-2
**Scope:** Foundation (Enums, Address VO, ProductCategory, Product)
**Status:** ✅ COMPLETE

---

## Sprint Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         SPRINT 1 DEPENDENCY GRAPH                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────┐                                                             │
│  │   ENUMS     │ ◄─── No dependencies, start here                           │
│  │  (Task 1)   │                                                             │
│  └──────┬──────┘                                                             │
│         │                                                                    │
│         ▼                                                                    │
│  ┌─────────────┐                                                             │
│  │  ADDRESS VO │ ◄─── Depends on nothing, can parallel with enums           │
│  │  (Task 2)   │                                                             │
│  └──────┬──────┘                                                             │
│         │                                                                    │
│         ▼                                                                    │
│  ┌─────────────────────┐                                                     │
│  │  PRODUCT CATEGORY   │ ◄─── Depends on GlobalUsings updates               │
│  │  (Task 3)           │                                                     │
│  └──────────┬──────────┘                                                     │
│             │                                                                │
│             ▼                                                                │
│  ┌─────────────────────────────────────────┐                                │
│  │  PRODUCT + VARIANT + IMAGE              │ ◄─── Depends on Category       │
│  │  (Task 4)                               │                                │
│  └─────────────────────────────────────────┘                                │
│                                                                              │
│  ══════════════════════════════════════════                                 │
│  CHECKPOINT: Build + Test ───────────────────────────────────► SPRINT 2    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Task Breakdown

### TASK 1: E-commerce Enums
**Estimated Effort:** 1 hour
**Dependencies:** None
**Parallelizable:** Yes (with Task 2)

#### 1.1 Create ProductStatus Enum
**File:** `src/NOIR.Domain/Enums/ProductStatus.cs`

```csharp
namespace NOIR.Domain.Enums;

/// <summary>
/// Status of a product in the catalog.
/// </summary>
public enum ProductStatus
{
    /// <summary>
    /// Product is being prepared and not visible.
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Product is published and available for purchase.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Product is archived and hidden from catalog.
    /// </summary>
    Archived = 2,

    /// <summary>
    /// Product is out of stock across all variants.
    /// </summary>
    OutOfStock = 3
}
```

**Acceptance Criteria:**
- [ ] Enum follows existing naming conventions
- [ ] XML documentation on each value
- [ ] Values explicitly numbered starting from 0

---

#### 1.2 Create CartStatus Enum
**File:** `src/NOIR.Domain/Enums/CartStatus.cs`

```csharp
namespace NOIR.Domain.Enums;

/// <summary>
/// Status of a shopping cart.
/// </summary>
public enum CartStatus
{
    /// <summary>
    /// Cart is active and accepting items.
    /// </summary>
    Active = 0,

    /// <summary>
    /// Cart was abandoned (no activity for 30+ minutes).
    /// </summary>
    Abandoned = 1,

    /// <summary>
    /// Cart was converted to an order.
    /// </summary>
    Converted = 2,

    /// <summary>
    /// Cart expired (session ended).
    /// </summary>
    Expired = 3,

    /// <summary>
    /// Cart was merged into another cart on login.
    /// </summary>
    Merged = 4
}
```

**Acceptance Criteria:**
- [ ] Enum follows existing naming conventions
- [ ] XML documentation on each value

---

#### 1.3 Create OrderStatus Enum
**File:** `src/NOIR.Domain/Enums/OrderStatus.cs`

```csharp
namespace NOIR.Domain.Enums;

/// <summary>
/// Status of an order through its lifecycle.
/// </summary>
public enum OrderStatus
{
    /// <summary>
    /// Order created, awaiting payment confirmation.
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment received, order confirmed.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Order is being prepared for shipment.
    /// </summary>
    Processing = 2,

    /// <summary>
    /// Order has been shipped.
    /// </summary>
    Shipped = 3,

    /// <summary>
    /// Order has been delivered to customer.
    /// </summary>
    Delivered = 4,

    /// <summary>
    /// Order completed successfully.
    /// </summary>
    Completed = 5,

    /// <summary>
    /// Order was cancelled.
    /// </summary>
    Cancelled = 6,

    /// <summary>
    /// Order was refunded.
    /// </summary>
    Refunded = 7
}
```

**Acceptance Criteria:**
- [ ] Enum follows existing naming conventions
- [ ] XML documentation on each value
- [ ] Status flow matches design document

---

#### 1.4 Create CheckoutSessionStatus Enum
**File:** `src/NOIR.Domain/Enums/CheckoutSessionStatus.cs`

```csharp
namespace NOIR.Domain.Enums;

/// <summary>
/// Status of a checkout session.
/// </summary>
public enum CheckoutSessionStatus
{
    /// <summary>
    /// Checkout session started, collecting information.
    /// </summary>
    Started = 0,

    /// <summary>
    /// Shipping address has been entered.
    /// </summary>
    AddressComplete = 1,

    /// <summary>
    /// Shipping method has been selected.
    /// </summary>
    ShippingSelected = 2,

    /// <summary>
    /// Awaiting payment information.
    /// </summary>
    PaymentPending = 3,

    /// <summary>
    /// Payment is being processed.
    /// </summary>
    PaymentProcessing = 4,

    /// <summary>
    /// Checkout completed, order created.
    /// </summary>
    Completed = 5,

    /// <summary>
    /// Checkout session expired (15 minute timeout).
    /// </summary>
    Expired = 6,

    /// <summary>
    /// Checkout was abandoned by user.
    /// </summary>
    Abandoned = 7
}
```

**Acceptance Criteria:**
- [ ] Enum follows existing naming conventions
- [ ] Status flow matches checkout accordion design

---

#### 1.5 Create ReservationStatus Enum
**File:** `src/NOIR.Domain/Enums/ReservationStatus.cs`

```csharp
namespace NOIR.Domain.Enums;

/// <summary>
/// Status of an inventory reservation.
/// </summary>
public enum ReservationStatus
{
    /// <summary>
    /// Temporary hold during checkout (15 min expiry).
    /// </summary>
    Temporary = 0,

    /// <summary>
    /// Confirmed when order is placed.
    /// </summary>
    Confirmed = 1,

    /// <summary>
    /// Released when checkout expired or abandoned.
    /// </summary>
    Released = 2,

    /// <summary>
    /// Cancelled when order is cancelled.
    /// </summary>
    Cancelled = 3
}
```

**Acceptance Criteria:**
- [ ] Enum follows existing naming conventions

---

#### 1.6 Create InventoryMovementType Enum
**File:** `src/NOIR.Domain/Enums/InventoryMovementType.cs`

```csharp
namespace NOIR.Domain.Enums;

/// <summary>
/// Type of inventory movement for audit trail.
/// </summary>
public enum InventoryMovementType
{
    /// <summary>
    /// Stock received from supplier.
    /// </summary>
    StockIn = 0,

    /// <summary>
    /// Stock removed for order fulfillment.
    /// </summary>
    StockOut = 1,

    /// <summary>
    /// Manual inventory adjustment.
    /// </summary>
    Adjustment = 2,

    /// <summary>
    /// Stock returned from customer.
    /// </summary>
    Return = 3,

    /// <summary>
    /// Stock reserved during checkout.
    /// </summary>
    Reservation = 4,

    /// <summary>
    /// Reserved stock released back to inventory.
    /// </summary>
    ReservationRelease = 5,

    /// <summary>
    /// Stock marked as damaged.
    /// </summary>
    Damaged = 6,

    /// <summary>
    /// Stock marked as expired.
    /// </summary>
    Expired = 7
}
```

**Acceptance Criteria:**
- [ ] Enum follows existing naming conventions
- [ ] Covers all movement scenarios from design

---

#### 1.7 Update GlobalUsings.cs
**File:** `src/NOIR.Domain/GlobalUsings.cs`

Add the new enum namespaces (if not auto-discovered).

**Acceptance Criteria:**
- [ ] All new enums accessible without explicit using statements

---

### TASK 2: Address Value Object
**Estimated Effort:** 30 minutes
**Dependencies:** None
**Parallelizable:** Yes (with Task 1)

#### 2.1 Create Address Value Object
**File:** `src/NOIR.Domain/ValueObjects/Address.cs`

```csharp
namespace NOIR.Domain.ValueObjects;

/// <summary>
/// Address value object for shipping and billing.
/// Follows Vietnam address format (Ward/District/Province).
/// </summary>
public record Address
{
    /// <summary>
    /// Full name of recipient.
    /// </summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>
    /// Contact phone number.
    /// </summary>
    public string Phone { get; init; } = string.Empty;

    /// <summary>
    /// Primary street address.
    /// </summary>
    public string AddressLine1 { get; init; } = string.Empty;

    /// <summary>
    /// Secondary address (apartment, suite, etc.).
    /// </summary>
    public string? AddressLine2 { get; init; }

    /// <summary>
    /// Ward (Phuong/Xa in Vietnam).
    /// </summary>
    public string Ward { get; init; } = string.Empty;

    /// <summary>
    /// District (Quan/Huyen in Vietnam).
    /// </summary>
    public string District { get; init; } = string.Empty;

    /// <summary>
    /// Province/City (Tinh/Thanh pho in Vietnam).
    /// </summary>
    public string Province { get; init; } = string.Empty;

    /// <summary>
    /// Country name.
    /// </summary>
    public string Country { get; init; } = "Vietnam";

    /// <summary>
    /// Postal/ZIP code (optional in Vietnam).
    /// </summary>
    public string? PostalCode { get; init; }

    /// <summary>
    /// Whether this is the default address for the user.
    /// </summary>
    public bool IsDefault { get; init; }

    /// <summary>
    /// Returns formatted single-line address.
    /// </summary>
    public string ToSingleLine() =>
        string.Join(", ",
            new[] { AddressLine1, AddressLine2, Ward, District, Province, Country }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>
    /// Creates a copy with IsDefault set.
    /// </summary>
    public Address WithDefault(bool isDefault) => this with { IsDefault = isDefault };
}
```

**Acceptance Criteria:**
- [ ] Record type (immutable)
- [ ] Vietnam-specific fields (Ward, District, Province)
- [ ] Sensible defaults for Country
- [ ] Helper methods for formatting

---

### TASK 3: ProductCategory Entity
**Estimated Effort:** 2 hours
**Dependencies:** Task 1 (Enums), GlobalUsings update
**Parallelizable:** No

#### 3.1 Create ProductCategory Entity
**File:** `src/NOIR.Domain/Entities/Product/ProductCategory.cs`

```csharp
namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Product category with hierarchical support.
/// </summary>
public class ProductCategory : TenantAggregateRoot<Guid>
{
    private ProductCategory() : base() { }
    private ProductCategory(Guid id, string? tenantId) : base(id, tenantId) { }

    /// <summary>
    /// Category display name.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// URL-friendly slug.
    /// </summary>
    public string Slug { get; private set; } = string.Empty;

    /// <summary>
    /// Category description.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Parent category ID for hierarchy.
    /// </summary>
    public Guid? ParentId { get; private set; }

    /// <summary>
    /// Display order within parent.
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Category image URL.
    /// </summary>
    public string? ImageUrl { get; private set; }

    /// <summary>
    /// SEO meta title.
    /// </summary>
    public string? MetaTitle { get; private set; }

    /// <summary>
    /// SEO meta description.
    /// </summary>
    public string? MetaDescription { get; private set; }

    /// <summary>
    /// Cached product count in this category.
    /// </summary>
    public int ProductCount { get; private set; }

    // Navigation properties
    public virtual ProductCategory? Parent { get; private set; }
    public virtual ICollection<ProductCategory> Children { get; private set; } = new List<ProductCategory>();
    public virtual ICollection<Product> Products { get; private set; } = new List<Product>();

    /// <summary>
    /// Factory method to create a new category.
    /// </summary>
    public static ProductCategory Create(
        string name,
        string slug,
        Guid? parentId = null,
        string? tenantId = null)
    {
        var category = new ProductCategory(Guid.NewGuid(), tenantId)
        {
            Name = name,
            Slug = slug,
            ParentId = parentId,
            SortOrder = 0
        };

        category.AddDomainEvent(new ProductCategoryCreatedEvent(category.Id, name, slug));
        return category;
    }

    public void UpdateDetails(
        string name,
        string slug,
        string? description = null,
        string? imageUrl = null)
    {
        Name = name;
        Slug = slug;
        Description = description;
        ImageUrl = imageUrl;
    }

    public void UpdateSeo(string? metaTitle, string? metaDescription)
    {
        MetaTitle = metaTitle;
        MetaDescription = metaDescription;
    }

    public void SetParent(Guid? parentId)
    {
        if (parentId == Id)
            throw new InvalidOperationException("Category cannot be its own parent");

        ParentId = parentId;
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }

    public void UpdateProductCount(int count)
    {
        ProductCount = count;
    }
}
```

**Acceptance Criteria:**
- [ ] Follows existing entity pattern (Post.cs)
- [ ] Self-referencing hierarchy (Parent/Children)
- [ ] Factory method with domain event
- [ ] Private setters with business methods

---

#### 3.2 Create ProductCategoryCreatedEvent
**File:** `src/NOIR.Domain/Events/Product/ProductCategoryEvents.cs`

```csharp
namespace NOIR.Domain.Events.Product;

public record ProductCategoryCreatedEvent(
    Guid CategoryId,
    string Name,
    string Slug) : DomainEvent;

public record ProductCategoryUpdatedEvent(
    Guid CategoryId,
    string Name) : DomainEvent;

public record ProductCategoryDeletedEvent(
    Guid CategoryId) : DomainEvent;
```

**Acceptance Criteria:**
- [ ] Record types
- [ ] Inherits from DomainEvent

---

#### 3.3 Create ProductCategoryConfiguration
**File:** `src/NOIR.Infrastructure/Persistence/Configurations/ProductCategoryConfiguration.cs`

```csharp
namespace NOIR.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for ProductCategory entity.
/// </summary>
public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("ProductCategories");

        // Primary key
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Name
        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        // Slug (unique per tenant - CLAUDE.md Rule 19)
        builder.Property(e => e.Slug)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.Slug })
            .IsUnique()
            .HasDatabaseName("IX_ProductCategories_TenantId_Slug");

        // Description
        builder.Property(e => e.Description)
            .HasMaxLength(500);

        // Image
        builder.Property(e => e.ImageUrl)
            .HasMaxLength(500);

        // SEO
        builder.Property(e => e.MetaTitle)
            .HasMaxLength(100);

        builder.Property(e => e.MetaDescription)
            .HasMaxLength(300);

        // Sort order
        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        // Product count
        builder.Property(e => e.ProductCount)
            .HasDefaultValue(0);

        // Self-referencing hierarchy
        builder.HasOne(e => e.Parent)
            .WithMany(e => e.Children)
            .HasForeignKey(e => e.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(e => new { e.TenantId, e.ParentId, e.SortOrder })
            .HasDatabaseName("IX_ProductCategories_TenantId_Parent_Sort");

        // Tenant ID
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);

        // Soft delete
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);
    }
}
```

**Acceptance Criteria:**
- [ ] Follows PostCategoryConfiguration pattern
- [ ] TenantId in unique constraints (Rule 19)
- [ ] Proper indexes for hierarchy queries
- [ ] Soft delete query filter

---

#### 3.4 Create ProductCategoryRepository
**File:** `src/NOIR.Infrastructure/Persistence/Repositories/ProductCategoryRepository.cs`

```csharp
namespace NOIR.Infrastructure.Persistence.Repositories;

public class ProductCategoryRepository : Repository<ProductCategory, Guid>, IProductCategoryRepository
{
    public ProductCategoryRepository(ApplicationDbContext context) : base(context) { }
}

public interface IProductCategoryRepository : IRepository<ProductCategory, Guid> { }
```

**Acceptance Criteria:**
- [ ] Follows existing repository pattern
- [ ] Has interface for DI

---

#### 3.5 Update ApplicationDbContext
**File:** `src/NOIR.Infrastructure/Persistence/ApplicationDbContext.cs`

Add DbSet:
```csharp
public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
```

**Acceptance Criteria:**
- [ ] DbSet added
- [ ] Configuration auto-discovered

---

#### 3.6 Create DI Verification Test
**File:** `tests/NOIR.IntegrationTests/Products/ProductCategoryRepositoryDiTests.cs`

```csharp
public class ProductCategoryRepositoryDiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly IServiceProvider _serviceProvider;

    public ProductCategoryRepositoryDiTests(CustomWebApplicationFactory factory)
    {
        _serviceProvider = factory.Services;
    }

    [Fact]
    public void Repository_ShouldResolve_FromDI()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetService<IRepository<ProductCategory, Guid>>();
        repo.Should().NotBeNull();
    }
}
```

**Acceptance Criteria:**
- [ ] Test passes (Rule 22)

---

### TASK 4: Product Entity (with Variants and Images)
**Estimated Effort:** 4 hours
**Dependencies:** Task 1, Task 2, Task 3
**Parallelizable:** No

#### 4.1 Create Product Entity
**File:** `src/NOIR.Domain/Entities/Product/Product.cs`

```csharp
namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Product in the catalog.
/// </summary>
public class Product : TenantAggregateRoot<Guid>
{
    private Product() : base() { }
    private Product(Guid id, string? tenantId) : base(id, tenantId) { }

    // Basic Info
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? DescriptionHtml { get; private set; }

    // Pricing
    public decimal BasePrice { get; private set; }
    public string Currency { get; private set; } = "VND";

    // Status
    public ProductStatus Status { get; private set; }

    // Organization
    public Guid? CategoryId { get; private set; }
    public string? Brand { get; private set; }

    // Identification
    public string? Sku { get; private set; }
    public string? Barcode { get; private set; }

    // Physical
    public decimal? Weight { get; private set; }

    // Inventory
    public bool TrackInventory { get; private set; } = true;

    // SEO
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }

    // Display
    public int SortOrder { get; private set; }

    // Navigation
    public virtual ProductCategory? Category { get; private set; }
    public virtual ICollection<ProductVariant> Variants { get; private set; } = new List<ProductVariant>();
    public virtual ICollection<ProductImage> Images { get; private set; } = new List<ProductImage>();

    // Computed
    public bool HasVariants => Variants.Any();
    public int TotalStock => Variants.Sum(v => v.StockQuantity);
    public bool InStock => TotalStock > 0;
    public ProductImage? PrimaryImage => Images.FirstOrDefault(i => i.IsPrimary) ?? Images.FirstOrDefault();

    /// <summary>
    /// Factory method to create a new product.
    /// </summary>
    public static Product Create(
        string name,
        string slug,
        decimal basePrice,
        string currency = "VND",
        string? tenantId = null)
    {
        var product = new Product(Guid.NewGuid(), tenantId)
        {
            Name = name,
            Slug = slug,
            BasePrice = basePrice,
            Currency = currency,
            Status = ProductStatus.Draft,
            TrackInventory = true
        };

        product.AddDomainEvent(new ProductCreatedEvent(product.Id, name, slug));
        return product;
    }

    public void UpdateBasicInfo(
        string name,
        string slug,
        string? description,
        string? descriptionHtml)
    {
        Name = name;
        Slug = slug;
        Description = description;
        DescriptionHtml = descriptionHtml;
    }

    public void UpdatePricing(decimal basePrice, string currency = "VND")
    {
        BasePrice = basePrice;
        Currency = currency;
    }

    public void SetCategory(Guid? categoryId)
    {
        CategoryId = categoryId;
    }

    public void SetBrand(string? brand)
    {
        Brand = brand;
    }

    public void UpdateIdentification(string? sku, string? barcode)
    {
        Sku = sku;
        Barcode = barcode;
    }

    public void UpdateSeo(string? metaTitle, string? metaDescription)
    {
        MetaTitle = metaTitle;
        MetaDescription = metaDescription;
    }

    public void SetWeight(decimal? weight)
    {
        Weight = weight;
    }

    public void SetInventoryTracking(bool trackInventory)
    {
        TrackInventory = trackInventory;
    }

    public void Publish()
    {
        if (Status == ProductStatus.Draft)
        {
            Status = ProductStatus.Active;
            AddDomainEvent(new ProductPublishedEvent(Id, Name));
        }
    }

    public void Archive()
    {
        Status = ProductStatus.Archived;
        AddDomainEvent(new ProductArchivedEvent(Id));
    }

    public void SetOutOfStock()
    {
        if (TotalStock == 0)
        {
            Status = ProductStatus.OutOfStock;
        }
    }

    public void RestoreFromOutOfStock()
    {
        if (Status == ProductStatus.OutOfStock && TotalStock > 0)
        {
            Status = ProductStatus.Active;
        }
    }

    // Variant management
    public ProductVariant AddVariant(
        string name,
        decimal price,
        string? sku = null,
        Dictionary<string, string>? options = null)
    {
        var variant = ProductVariant.Create(Id, name, price, sku, options, TenantId);
        Variants.Add(variant);
        return variant;
    }

    public void RemoveVariant(Guid variantId)
    {
        var variant = Variants.FirstOrDefault(v => v.Id == variantId);
        if (variant != null)
        {
            Variants.Remove(variant);
        }
    }

    // Image management
    public ProductImage AddImage(string url, string? altText = null, bool isPrimary = false)
    {
        // If setting as primary, clear other primaries
        if (isPrimary)
        {
            foreach (var img in Images.Where(i => i.IsPrimary))
            {
                img.ClearPrimary();
            }
        }

        var sortOrder = Images.Any() ? Images.Max(i => i.SortOrder) + 1 : 0;
        var image = ProductImage.Create(Id, url, altText, sortOrder, isPrimary, TenantId);
        Images.Add(image);
        return image;
    }

    public void RemoveImage(Guid imageId)
    {
        var image = Images.FirstOrDefault(i => i.Id == imageId);
        if (image != null)
        {
            Images.Remove(image);
        }
    }

    public void SetPrimaryImage(Guid imageId)
    {
        foreach (var img in Images)
        {
            if (img.Id == imageId)
                img.SetAsPrimary();
            else
                img.ClearPrimary();
        }
    }
}
```

**Acceptance Criteria:**
- [ ] Follows existing entity patterns
- [ ] Factory method with domain event
- [ ] Computed properties for stock/images
- [ ] Variant and image management methods

---

#### 4.2 Create ProductVariant Entity
**File:** `src/NOIR.Domain/Entities/Product/ProductVariant.cs`

```csharp
namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Product variant (size, color, etc.).
/// </summary>
public class ProductVariant : TenantEntity<Guid>
{
    private ProductVariant() { }

    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Sku { get; private set; }
    public decimal Price { get; private set; }
    public decimal? CompareAtPrice { get; private set; }

    [ConcurrencyCheck]
    public int StockQuantity { get; private set; }

    /// <summary>
    /// Flexible attributes as JSON (e.g., {"color": "Red", "size": "M"}).
    /// </summary>
    public string? OptionsJson { get; private set; }

    public int SortOrder { get; private set; }

    // Navigation
    public virtual Product Product { get; private set; } = null!;

    // Computed
    public bool InStock => StockQuantity > 0;
    public bool LowStock => StockQuantity > 0 && StockQuantity <= 5;
    public bool OnSale => CompareAtPrice.HasValue && CompareAtPrice > Price;

    internal static ProductVariant Create(
        Guid productId,
        string name,
        decimal price,
        string? sku,
        Dictionary<string, string>? options,
        string? tenantId)
    {
        return new ProductVariant
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            Name = name,
            Price = price,
            Sku = sku,
            StockQuantity = 0,
            OptionsJson = options != null ? JsonSerializer.Serialize(options) : null,
            SortOrder = 0
        };
    }

    public void UpdateDetails(string name, decimal price, string? sku)
    {
        Name = name;
        Price = price;
        Sku = sku;
    }

    public void SetCompareAtPrice(decimal? compareAtPrice)
    {
        CompareAtPrice = compareAtPrice;
    }

    public void UpdateOptions(Dictionary<string, string> options)
    {
        OptionsJson = JsonSerializer.Serialize(options);
    }

    public Dictionary<string, string> GetOptions()
    {
        if (string.IsNullOrEmpty(OptionsJson))
            return new Dictionary<string, string>();

        return JsonSerializer.Deserialize<Dictionary<string, string>>(OptionsJson)
            ?? new Dictionary<string, string>();
    }

    /// <summary>
    /// Reserve stock for checkout. Throws if insufficient.
    /// </summary>
    public void ReserveStock(int quantity)
    {
        if (StockQuantity < quantity)
            throw new InvalidOperationException($"Insufficient stock. Available: {StockQuantity}, Requested: {quantity}");

        StockQuantity -= quantity;
    }

    /// <summary>
    /// Release reserved stock back to inventory.
    /// </summary>
    public void ReleaseStock(int quantity)
    {
        StockQuantity += quantity;
    }

    /// <summary>
    /// Adjust stock by delta (positive or negative).
    /// </summary>
    public void AdjustStock(int delta)
    {
        var newQuantity = StockQuantity + delta;
        if (newQuantity < 0)
            throw new InvalidOperationException("Stock cannot be negative");

        StockQuantity = newQuantity;
    }

    /// <summary>
    /// Set absolute stock quantity.
    /// </summary>
    public void SetStock(int quantity)
    {
        if (quantity < 0)
            throw new InvalidOperationException("Stock cannot be negative");

        StockQuantity = quantity;
    }
}
```

**Acceptance Criteria:**
- [ ] [ConcurrencyCheck] on StockQuantity
- [ ] Flexible JSON options
- [ ] Stock management methods
- [ ] Computed properties (InStock, LowStock, OnSale)

---

#### 4.3 Create ProductImage Entity
**File:** `src/NOIR.Domain/Entities/Product/ProductImage.cs`

```csharp
namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Product image for gallery.
/// </summary>
public class ProductImage : TenantEntity<Guid>
{
    private ProductImage() { }

    public Guid ProductId { get; private set; }
    public string Url { get; private set; } = string.Empty;
    public string? AltText { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsPrimary { get; private set; }

    // Navigation
    public virtual Product Product { get; private set; } = null!;

    internal static ProductImage Create(
        Guid productId,
        string url,
        string? altText,
        int sortOrder,
        bool isPrimary,
        string? tenantId)
    {
        return new ProductImage
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = productId,
            Url = url,
            AltText = altText,
            SortOrder = sortOrder,
            IsPrimary = isPrimary
        };
    }

    public void Update(string url, string? altText)
    {
        Url = url;
        AltText = altText;
    }

    public void SetSortOrder(int sortOrder)
    {
        SortOrder = sortOrder;
    }

    public void SetAsPrimary()
    {
        IsPrimary = true;
    }

    public void ClearPrimary()
    {
        IsPrimary = false;
    }
}
```

**Acceptance Criteria:**
- [ ] Sort order for gallery
- [ ] Primary image flag
- [ ] Internal factory method

---

#### 4.4 Create Product Domain Events
**File:** `src/NOIR.Domain/Events/Product/ProductEvents.cs`

```csharp
namespace NOIR.Domain.Events.Product;

public record ProductCreatedEvent(
    Guid ProductId,
    string Name,
    string Slug) : DomainEvent;

public record ProductPublishedEvent(
    Guid ProductId,
    string Name) : DomainEvent;

public record ProductArchivedEvent(
    Guid ProductId) : DomainEvent;

public record ProductStockChangedEvent(
    Guid ProductVariantId,
    Guid ProductId,
    int OldQuantity,
    int NewQuantity,
    InventoryMovementType MovementType) : DomainEvent;
```

**Acceptance Criteria:**
- [ ] Record types inheriting DomainEvent

---

#### 4.5 Create ProductConfiguration
**File:** `src/NOIR.Infrastructure/Persistence/Configurations/ProductConfiguration.cs`

```csharp
namespace NOIR.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        // Basic info
        builder.Property(e => e.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(e => new { e.TenantId, e.Slug })
            .IsUnique()
            .HasDatabaseName("IX_Products_TenantId_Slug");

        builder.Property(e => e.Description)
            .HasMaxLength(2000);

        builder.Property(e => e.DescriptionHtml);

        // Pricing
        builder.Property(e => e.BasePrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.Currency)
            .HasMaxLength(10)
            .HasDefaultValue("VND");

        // Status
        builder.Property(e => e.Status)
            .HasConversion<int>();

        // Organization
        builder.HasOne(e => e.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(e => e.CategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.Brand)
            .HasMaxLength(100);

        // Identification
        builder.Property(e => e.Sku)
            .HasMaxLength(50);

        builder.HasIndex(e => new { e.TenantId, e.Sku })
            .IsUnique()
            .HasFilter("[Sku] IS NOT NULL")
            .HasDatabaseName("IX_Products_TenantId_Sku");

        builder.Property(e => e.Barcode)
            .HasMaxLength(50);

        // Physical
        builder.Property(e => e.Weight)
            .HasPrecision(10, 2);

        // SEO
        builder.Property(e => e.MetaTitle)
            .HasMaxLength(100);

        builder.Property(e => e.MetaDescription)
            .HasMaxLength(300);

        // Indexes for filtering
        builder.HasIndex(e => new { e.TenantId, e.Status, e.CategoryId })
            .HasDatabaseName("IX_Products_TenantId_Status_Category");

        builder.HasIndex(e => new { e.TenantId, e.BasePrice })
            .HasDatabaseName("IX_Products_TenantId_Price");

        builder.HasIndex(e => new { e.TenantId, e.Brand })
            .HasDatabaseName("IX_Products_TenantId_Brand");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
        builder.HasIndex(e => e.TenantId);

        // Soft delete
        builder.HasQueryFilter("SoftDelete", e => !e.IsDeleted);
    }
}
```

**Acceptance Criteria:**
- [ ] All indexes from design document
- [ ] Unique constraints include TenantId
- [ ] Proper precision for decimal fields

---

#### 4.6 Create ProductVariantConfiguration
**File:** `src/NOIR.Infrastructure/Persistence/Configurations/ProductVariantConfiguration.cs`

```csharp
namespace NOIR.Infrastructure.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Sku)
            .HasMaxLength(50);

        builder.HasIndex(e => new { e.TenantId, e.Sku })
            .IsUnique()
            .HasFilter("[Sku] IS NOT NULL")
            .HasDatabaseName("IX_ProductVariants_TenantId_Sku");

        builder.Property(e => e.Price)
            .HasPrecision(18, 2);

        builder.Property(e => e.CompareAtPrice)
            .HasPrecision(18, 2);

        builder.Property(e => e.StockQuantity)
            .IsConcurrencyToken(); // ConcurrencyCheck

        builder.Property(e => e.OptionsJson);

        // Relationship
        builder.HasOne(e => e.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Stock filtering index
        builder.HasIndex(e => new { e.TenantId, e.StockQuantity })
            .HasFilter("[StockQuantity] > 0")
            .HasDatabaseName("IX_ProductVariants_TenantId_InStock");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
    }
}
```

**Acceptance Criteria:**
- [ ] Concurrency token on StockQuantity
- [ ] Cascade delete from Product

---

#### 4.7 Create ProductImageConfiguration
**File:** `src/NOIR.Infrastructure/Persistence/Configurations/ProductImageConfiguration.cs`

```csharp
namespace NOIR.Infrastructure.Persistence.Configurations;

public class ProductImageConfiguration : IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedOnAdd();

        builder.Property(e => e.Url)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.AltText)
            .HasMaxLength(200);

        builder.Property(e => e.SortOrder)
            .HasDefaultValue(0);

        builder.Property(e => e.IsPrimary)
            .HasDefaultValue(false);

        // Relationship
        builder.HasOne(e => e.Product)
            .WithMany(p => p.Images)
            .HasForeignKey(e => e.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for ordering
        builder.HasIndex(e => new { e.ProductId, e.SortOrder })
            .HasDatabaseName("IX_ProductImages_Product_Sort");

        // Tenant
        builder.Property(e => e.TenantId)
            .HasMaxLength(DatabaseConstants.TenantIdMaxLength);
    }
}
```

**Acceptance Criteria:**
- [ ] Cascade delete from Product
- [ ] Sort order index

---

#### 4.8 Create ProductRepository
**File:** `src/NOIR.Infrastructure/Persistence/Repositories/ProductRepository.cs`

```csharp
namespace NOIR.Infrastructure.Persistence.Repositories;

public class ProductRepository : Repository<Product, Guid>, IProductRepository
{
    public ProductRepository(ApplicationDbContext context) : base(context) { }
}

public interface IProductRepository : IRepository<Product, Guid> { }
```

**Acceptance Criteria:**
- [ ] Interface for DI

---

#### 4.9 Update ApplicationDbContext
**File:** `src/NOIR.Infrastructure/Persistence/ApplicationDbContext.cs`

Add DbSets:
```csharp
public DbSet<Product> Products => Set<Product>();
public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
public DbSet<ProductImage> ProductImages => Set<ProductImage>();
```

**Acceptance Criteria:**
- [ ] All DbSets added

---

#### 4.10 Create DI Verification Tests
**File:** `tests/NOIR.IntegrationTests/Products/ProductRepositoryDiTests.cs`

```csharp
public class ProductRepositoryDiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly IServiceProvider _serviceProvider;

    public ProductRepositoryDiTests(CustomWebApplicationFactory factory)
    {
        _serviceProvider = factory.Services;
    }

    [Fact]
    public void ProductRepository_ShouldResolve_FromDI()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetService<IRepository<Product, Guid>>();
        repo.Should().NotBeNull();
    }

    [Fact]
    public void ProductCategoryRepository_ShouldResolve_FromDI()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetService<IRepository<ProductCategory, Guid>>();
        repo.Should().NotBeNull();
    }
}
```

**Acceptance Criteria:**
- [ ] Both tests pass

---

## Sprint Checkpoints

### Checkpoint 1: After Task 1+2 (Enums + Address)
```bash
# Verify build
dotnet build src/NOIR.sln

# Expected: Build succeeds
```

### Checkpoint 2: After Task 3 (ProductCategory)
```bash
# Run tests
dotnet test src/NOIR.sln --filter "ProductCategory"

# Expected: DI test passes
```

### Checkpoint 3: After Task 4 (Product)
```bash
# Full build and test
dotnet build src/NOIR.sln
dotnet test src/NOIR.sln

# Expected: All 5,400+ tests pass
```

### Checkpoint 4: Create Migration
```bash
# IMPORTANT: Specify --context (CLAUDE.md Rule 18)
dotnet ef migrations add AddEcommerceEntities \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext \
  --output-dir Migrations/App

# Update database
dotnet ef database update \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext
```

---

## Execution Order Summary

| Order | Task | Files | Est. Time |
|-------|------|-------|-----------|
| 1 | Enums | 6 new files in `Domain/Enums/` | 1h |
| 2 | Address VO | 1 new file in `Domain/ValueObjects/` | 30m |
| 3 | ProductCategory | Entity + Events + Config + Repo + Test | 2h |
| 4 | Product | Entity + Variant + Image + Events + Configs + Repos + Tests | 4h |
| 5 | Migration | EF migration + database update | 30m |

**Total Estimated Time:** ~8 hours

---

## Files to Create (Summary)

### Domain Layer (10 files)
```
src/NOIR.Domain/
├── Enums/
│   ├── ProductStatus.cs           [NEW]
│   ├── CartStatus.cs              [NEW]
│   ├── OrderStatus.cs             [NEW]
│   ├── CheckoutSessionStatus.cs   [NEW]
│   ├── ReservationStatus.cs       [NEW]
│   └── InventoryMovementType.cs   [NEW]
├── ValueObjects/
│   └── Address.cs                 [NEW]
├── Entities/Product/
│   ├── ProductCategory.cs         [NEW]
│   ├── Product.cs                 [NEW]
│   ├── ProductVariant.cs          [NEW]
│   └── ProductImage.cs            [NEW]
└── Events/Product/
    ├── ProductCategoryEvents.cs   [NEW]
    └── ProductEvents.cs           [NEW]
```

### Infrastructure Layer (6 files)
```
src/NOIR.Infrastructure/
└── Persistence/
    ├── Configurations/
    │   ├── ProductCategoryConfiguration.cs   [NEW]
    │   ├── ProductConfiguration.cs           [NEW]
    │   ├── ProductVariantConfiguration.cs    [NEW]
    │   └── ProductImageConfiguration.cs      [NEW]
    ├── Repositories/
    │   ├── ProductCategoryRepository.cs      [NEW]
    │   └── ProductRepository.cs              [NEW]
    └── ApplicationDbContext.cs               [MODIFY - add DbSets]
```

### Tests (2 files)
```
tests/NOIR.IntegrationTests/
└── Products/
    ├── ProductCategoryRepositoryDiTests.cs   [NEW]
    └── ProductRepositoryDiTests.cs           [NEW]
```

---

## Next Sprint Preview

**Sprint 2** will build on this foundation:
- Product admin commands (Create, Update, Publish, Archive)
- Product admin queries (GetById, GetList with filters)
- Product specifications for faceted navigation
- Product admin UI (using 21st.dev)

---

*Workflow plan generated. Ready for implementation with `/sc:implement`.*
