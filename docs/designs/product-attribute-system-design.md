# Product Attribute System - Technical Design

**Version:** 1.5
**Date:** 2026-01-29
**Status:** In Progress (Phase 5 Complete) - **CRITICAL GAP: Phase 9 blocks admin UX**
**Author:** Claude Code
**Progress:** 5/9 phases complete (Backend ready, Frontend attribute integration missing)

---

## Table of Contents

1. [Overview](#1-overview)
2. [Entity Model](#2-entity-model)
3. [Database Schema](#3-database-schema)
4. [API Design](#4-api-design)
5. [Filter Index Strategy](#5-filter-index-strategy)
6. [Analytics Event System](#6-analytics-event-system)
7. [Migration Strategy](#7-migration-strategy)
8. [Performance Considerations](#8-performance-considerations)

---

## 1. Overview

### 1.1 Goals

- Implement a flexible attribute system supporting 13 data types
- Enable faceted filtering with real-time counts (like Amazon/Shopee)
- Create Brand as a first-class entity
- Build analytics pipeline for filter usage tracking
- Achieve sub-100ms filter response times for 100K+ products

### 1.2 Scope

| In Scope | Out of Scope |
|----------|--------------|
| Brand entity with full features | ML-based recommendations |
| Attribute/AttributeValue entities | External search engine (Elasticsearch) |
| ProductAttribute junction | Real-time personalization |
| ProductFilterIndex (denormalized) | A/B testing framework |
| Filter analytics events | Data warehouse integration |
| Admin CRUD for attributes | |
| Storefront filter API | |

### 1.3 Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Filter Performance | Denormalized ProductFilterIndex | Sub-100ms queries, no joins, sync via events |
| Brand Structure | Separate Entity | SEO, logos, brand pages, analytics |
| Attribute Types | 13 types with typed storage | Type safety, proper UI controls |
| Analytics | Event table + batch processing | Start simple, migrate to warehouse later |

---

## 2. Entity Model

### 2.1 Entity Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           ENTITY RELATIONSHIPS                               │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌──────────────┐         ┌──────────────┐         ┌──────────────┐        │
│  │    Brand     │◄───────┤   Product    ├────────►│  Category    │        │
│  │ (Aggregate)  │  N:1    │ (Aggregate)  │   N:1   │ (Aggregate)  │        │
│  └──────────────┘         └──────┬───────┘         └──────┬───────┘        │
│                                  │                        │                 │
│                                  │ 1:N                    │ N:M             │
│                                  ▼                        ▼                 │
│                          ┌──────────────┐         ┌──────────────────┐     │
│                          │ Product      │         │ CategoryAttribute│     │
│                          │ Attribute    │         │    (Junction)    │     │
│                          │ (Junction)   │         └────────┬─────────┘     │
│                          └──────┬───────┘                  │               │
│                                 │ N:1                      │ N:1           │
│                                 ▼                          ▼               │
│                          ┌──────────────┐         ┌──────────────┐        │
│                          │  Attribute   │◄────────┤  Attribute   │        │
│                          │ (Aggregate)  │   1:N   │    Value     │        │
│                          └──────────────┘         └──────────────┘        │
│                                                                             │
│  ════════════════════════════════════════════════════════════════════════  │
│                                                                             │
│  ┌──────────────────────┐              ┌────────────────────────┐          │
│  │ ProductFilterIndex   │              │ FilterAnalyticsEvent   │          │
│  │ (Denormalized View)  │              │ (Event Sourcing)       │          │
│  └──────────────────────┘              └────────────────────────┘          │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Brand Entity

```csharp
namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Brand entity for product organization and brand pages.
/// </summary>
public class Brand : TenantAggregateRoot<Guid>
{
    // Identity
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;

    // Branding
    public string? LogoUrl { get; private set; }
    public string? BannerUrl { get; private set; }
    public string? Description { get; private set; }
    public string? Website { get; private set; }

    // SEO
    public string? MetaTitle { get; private set; }
    public string? MetaDescription { get; private set; }

    // Organization
    public bool IsActive { get; private set; } = true;
    public bool IsFeatured { get; private set; }
    public int SortOrder { get; private set; }

    // Cached metrics
    public int ProductCount { get; private set; }

    // Navigation
    public virtual ICollection<Product> Products { get; private set; } = new List<Product>();

    // Factory
    public static Brand Create(string name, string slug, string? tenantId = null);

    // Commands
    public void UpdateDetails(string name, string slug, string? description, string? website);
    public void UpdateBranding(string? logoUrl, string? bannerUrl);
    public void UpdateSeo(string? metaTitle, string? metaDescription);
    public void SetFeatured(bool isFeatured);
    public void Activate();
    public void Deactivate();
    public void UpdateProductCount(int count);
}
```

### 2.3 Attribute Entity

```csharp
namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Attribute types for product characteristics.
/// </summary>
public enum AttributeType
{
    Select = 0,        // Single choice dropdown
    MultiSelect = 1,   // Multiple choice checkboxes
    Text = 2,          // Free text input
    TextArea = 3,      // Multi-line text
    Number = 4,        // Integer input
    Decimal = 5,       // Decimal input
    Boolean = 6,       // Toggle switch
    Date = 7,          // Date picker
    DateTime = 8,      // DateTime picker
    Color = 9,         // Color picker (hex output)
    Range = 10,        // Min/Max range
    Url = 11,          // URL with validation
    File = 12          // File upload
}

/// <summary>
/// Product attribute definition.
/// </summary>
public class Attribute : TenantAggregateRoot<Guid>
{
    // Identity
    public string Code { get; private set; } = string.Empty;      // "screen_size"
    public string Name { get; private set; } = string.Empty;      // "Screen Size"
    public AttributeType Type { get; private set; }

    // Behavior flags
    public bool IsFilterable { get; private set; }                // Show in filter sidebar
    public bool IsSearchable { get; private set; }                // Include in search index
    public bool IsRequired { get; private set; }                  // Mandatory for products
    public bool IsVariantAttribute { get; private set; }          // Creates variants
    public bool ShowInProductCard { get; private set; }           // Show in list view
    public bool ShowInSpecifications { get; private set; }        // Show in spec table

    // Type-specific configuration
    public string? Unit { get; private set; }                     // "inch", "mAh", "kg"
    public string? ValidationRegex { get; private set; }          // For Text type
    public decimal? MinValue { get; private set; }                // For Number/Decimal
    public decimal? MaxValue { get; private set; }                // For Number/Decimal
    public int? MaxLength { get; private set; }                   // For Text/TextArea
    public string? DefaultValue { get; private set; }             // Default when creating
    public string? Placeholder { get; private set; }              // Input placeholder
    public string? HelpText { get; private set; }                 // Tooltip/description

    // Organization
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Navigation (for Select/MultiSelect types)
    public virtual ICollection<AttributeValue> Values { get; private set; } = new List<AttributeValue>();
    public virtual ICollection<CategoryAttribute> CategoryAttributes { get; private set; } = new List<CategoryAttribute>();

    // Factory
    public static Attribute Create(
        string code,
        string name,
        AttributeType type,
        string? tenantId = null);

    // Commands
    public void UpdateDetails(string code, string name);
    public void SetType(AttributeType type);
    public void SetBehaviorFlags(bool isFilterable, bool isSearchable, bool isRequired, bool isVariantAttribute);
    public void SetDisplayFlags(bool showInProductCard, bool showInSpecifications);
    public void SetTypeConfiguration(string? unit, string? validationRegex, decimal? minValue, decimal? maxValue, int? maxLength);
    public void SetDefaults(string? defaultValue, string? placeholder, string? helpText);
    public void Activate();
    public void Deactivate();

    // Value management (for Select/MultiSelect)
    public AttributeValue AddValue(string value, string displayValue);
    public void RemoveValue(Guid valueId);
}
```

### 2.4 AttributeValue Entity

```csharp
namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Predefined value for Select/MultiSelect attributes.
/// </summary>
public class AttributeValue : TenantEntity<Guid>
{
    public Guid AttributeId { get; private set; }

    // Value
    public string Value { get; private set; } = string.Empty;       // "red" (stored)
    public string DisplayValue { get; private set; } = string.Empty; // "Red" (displayed)

    // Visual options
    public string? ColorCode { get; private set; }                  // "#FF0000"
    public string? SwatchUrl { get; private set; }                  // Image swatch
    public string? IconUrl { get; private set; }                    // Brand logo, etc.

    // Organization
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Cached metrics
    public int ProductCount { get; private set; }

    // Navigation
    public virtual Attribute Attribute { get; private set; } = null!;

    // Factory
    internal static AttributeValue Create(
        Guid attributeId,
        string value,
        string displayValue,
        int sortOrder,
        string? tenantId);

    // Commands
    public void Update(string value, string displayValue);
    public void SetVisuals(string? colorCode, string? swatchUrl, string? iconUrl);
    public void UpdateProductCount(int count);
}
```

### 2.5 CategoryAttribute Junction

```csharp
namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Links attributes to categories with category-specific settings.
/// </summary>
public class CategoryAttribute : TenantEntity<Guid>
{
    public Guid CategoryId { get; private set; }
    public Guid AttributeId { get; private set; }

    // Category-specific overrides
    public bool IsRequired { get; private set; }         // Override global setting
    public int SortOrder { get; private set; }           // Category-specific ordering
    public bool InheritToChildren { get; private set; }  // Inherit to child categories

    // Navigation
    public virtual ProductCategory Category { get; private set; } = null!;
    public virtual Attribute Attribute { get; private set; } = null!;

    // Factory
    public static CategoryAttribute Create(
        Guid categoryId,
        Guid attributeId,
        bool isRequired,
        int sortOrder,
        string? tenantId);

    // Commands
    public void Update(bool isRequired, int sortOrder, bool inheritToChildren);
}
```

### 2.6 ProductAttribute Junction

```csharp
namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Stores a product's actual attribute values.
/// </summary>
public class ProductAttribute : TenantEntity<Guid>
{
    public Guid ProductId { get; private set; }
    public Guid AttributeId { get; private set; }
    public Guid? VariantId { get; private set; }         // If variant-specific

    // Value storage (only one used based on Attribute.Type)
    public Guid? AttributeValueId { get; private set; }           // Select
    public string? AttributeValueIds { get; private set; }        // MultiSelect (JSON array)
    public string? TextValue { get; private set; }                // Text, TextArea, Url
    public decimal? NumberValue { get; private set; }             // Number, Decimal
    public bool? BoolValue { get; private set; }                  // Boolean
    public DateTime? DateValue { get; private set; }              // Date
    public DateTime? DateTimeValue { get; private set; }          // DateTime
    public string? ColorValue { get; private set; }               // Color (#RRGGBB)
    public decimal? MinRangeValue { get; private set; }           // Range min
    public decimal? MaxRangeValue { get; private set; }           // Range max
    public string? FileUrl { get; private set; }                  // File

    // Computed display value (for search/filtering)
    public string? DisplayValue { get; private set; }

    // Navigation
    public virtual Product Product { get; private set; } = null!;
    public virtual Attribute Attribute { get; private set; } = null!;
    public virtual AttributeValue? SelectedValue { get; private set; }
    public virtual ProductVariant? Variant { get; private set; }

    // Factory
    public static ProductAttribute Create(
        Guid productId,
        Guid attributeId,
        Guid? variantId,
        string? tenantId);

    // Value setters (only call appropriate one based on Attribute.Type)
    public void SetSelectValue(Guid attributeValueId, string displayValue);
    public void SetMultiSelectValue(List<Guid> attributeValueIds, string displayValue);
    public void SetTextValue(string value);
    public void SetNumberValue(decimal value, string? displayValue);
    public void SetBoolValue(bool value);
    public void SetDateValue(DateTime value);
    public void SetDateTimeValue(DateTime value);
    public void SetColorValue(string hexColor);
    public void SetRangeValue(decimal min, decimal max, string? unit);
    public void SetFileValue(string fileUrl);

    // Computed
    public object? GetTypedValue();
}
```

### 2.7 ProductFilterIndex (Denormalized)

```csharp
namespace NOIR.Domain.Entities.Product;

/// <summary>
/// Denormalized table for high-performance filtering.
/// Updated via domain events when products change.
/// </summary>
public class ProductFilterIndex : TenantEntity<Guid>
{
    // Primary key is ProductId
    public Guid ProductId { get; private set; }

    // Denormalized product info
    public string ProductName { get; private set; } = string.Empty;
    public string ProductSlug { get; private set; } = string.Empty;
    public ProductStatus Status { get; private set; }

    // Category (denormalized for hierarchy filtering)
    public Guid? CategoryId { get; private set; }
    public string? CategoryPath { get; private set; }     // "1/5/23" for hierarchy
    public string? CategoryName { get; private set; }

    // Brand (denormalized)
    public Guid? BrandId { get; private set; }
    public string? BrandName { get; private set; }
    public string? BrandSlug { get; private set; }

    // Pricing (min/max across variants)
    public decimal MinPrice { get; private set; }
    public decimal MaxPrice { get; private set; }
    public string Currency { get; private set; } = "VND";

    // Stock
    public bool InStock { get; private set; }
    public int TotalStock { get; private set; }

    // Rating (future)
    public decimal? AverageRating { get; private set; }
    public int ReviewCount { get; private set; }

    // Attributes as JSONB for flexible filtering
    // Format: {"color": ["red", "blue"], "size": ["m", "l"], "screen_size": 6.7}
    public string AttributesJson { get; private set; } = "{}";

    // Full-text search vector
    public string SearchText { get; private set; } = string.Empty;

    // Images (for list display)
    public string? PrimaryImageUrl { get; private set; }

    // Timestamps
    public DateTime LastSyncedAt { get; private set; }
    public DateTime ProductUpdatedAt { get; private set; }

    // Indexing helper
    public void UpdateFromProduct(Product product, Brand? brand);
}
```

### 2.8 FilterAnalyticsEvent

```csharp
namespace NOIR.Domain.Entities.Analytics;

public enum FilterEventType
{
    FilterApplied = 0,
    FilterRemoved = 1,
    FilterCleared = 2,
    ProductViewed = 3,
    ProductAddedToCart = 4,
    SearchPerformed = 5
}

/// <summary>
/// Tracks filter usage for analytics and optimization.
/// </summary>
public class FilterAnalyticsEvent : TenantEntity<Guid>
{
    // Event identity
    public FilterEventType EventType { get; private set; }
    public DateTime Timestamp { get; private set; }

    // Session context
    public string SessionId { get; private set; } = string.Empty;
    public string? UserId { get; private set; }

    // Filter context
    public Guid? CategoryId { get; private set; }
    public string? SearchQuery { get; private set; }
    public string FiltersJson { get; private set; } = "{}";   // Active filters
    public int ResultCount { get; private set; }

    // For product events
    public Guid? ProductId { get; private set; }
    public Guid? VariantId { get; private set; }
    public int? Position { get; private set; }               // Position in results

    // Attribution
    public string? AttributeCode { get; private set; }        // Which filter changed
    public string? AttributeValue { get; private set; }       // What value selected

    // Device/Location
    public string? DeviceType { get; private set; }           // mobile, desktop, tablet
    public string? IpCountry { get; private set; }
    public string? UserAgent { get; private set; }

    // Processing
    public bool IsProcessed { get; private set; }
    public DateTime? ProcessedAt { get; private set; }

    // Factory
    public static FilterAnalyticsEvent CreateFilterApplied(
        string sessionId,
        string? userId,
        Guid? categoryId,
        string? searchQuery,
        Dictionary<string, List<string>> filters,
        string attributeCode,
        string attributeValue,
        int resultCount,
        string? tenantId);
}
```

---

## 3. Database Schema

### 3.1 Tables

```sql
-- ============================================================================
-- BRAND TABLE
-- ============================================================================
CREATE TABLE Brands (
    Id UUID PRIMARY KEY,
    TenantId VARCHAR(64) NOT NULL,

    -- Identity
    Name VARCHAR(200) NOT NULL,
    Slug VARCHAR(200) NOT NULL,

    -- Branding
    LogoUrl VARCHAR(500),
    BannerUrl VARCHAR(500),
    Description TEXT,
    Website VARCHAR(500),

    -- SEO
    MetaTitle VARCHAR(200),
    MetaDescription VARCHAR(500),

    -- Organization
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,
    IsFeatured BOOLEAN NOT NULL DEFAULT FALSE,
    SortOrder INT NOT NULL DEFAULT 0,

    -- Cached metrics
    ProductCount INT NOT NULL DEFAULT 0,

    -- Audit
    CreatedAt TIMESTAMP NOT NULL,
    CreatedBy VARCHAR(450),
    LastModifiedAt TIMESTAMP,
    LastModifiedBy VARCHAR(450),
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE,
    DeletedAt TIMESTAMP,
    DeletedBy VARCHAR(450),

    -- Constraints
    CONSTRAINT UQ_Brands_Slug_Tenant UNIQUE (Slug, TenantId)
);

CREATE INDEX IX_Brands_TenantId ON Brands(TenantId);
CREATE INDEX IX_Brands_IsActive ON Brands(TenantId, IsActive);
CREATE INDEX IX_Brands_IsFeatured ON Brands(TenantId, IsFeatured);

-- ============================================================================
-- ATTRIBUTE TABLE
-- ============================================================================
CREATE TABLE Attributes (
    Id UUID PRIMARY KEY,
    TenantId VARCHAR(64) NOT NULL,

    -- Identity
    Code VARCHAR(100) NOT NULL,
    Name VARCHAR(200) NOT NULL,
    Type INT NOT NULL,               -- AttributeType enum

    -- Behavior flags
    IsFilterable BOOLEAN NOT NULL DEFAULT FALSE,
    IsSearchable BOOLEAN NOT NULL DEFAULT FALSE,
    IsRequired BOOLEAN NOT NULL DEFAULT FALSE,
    IsVariantAttribute BOOLEAN NOT NULL DEFAULT FALSE,
    ShowInProductCard BOOLEAN NOT NULL DEFAULT FALSE,
    ShowInSpecifications BOOLEAN NOT NULL DEFAULT TRUE,

    -- Type-specific configuration
    Unit VARCHAR(50),
    ValidationRegex VARCHAR(500),
    MinValue DECIMAL(18,4),
    MaxValue DECIMAL(18,4),
    MaxLength INT,
    DefaultValue VARCHAR(500),
    Placeholder VARCHAR(200),
    HelpText VARCHAR(500),

    -- Organization
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,

    -- Audit
    CreatedAt TIMESTAMP NOT NULL,
    CreatedBy VARCHAR(450),
    LastModifiedAt TIMESTAMP,
    LastModifiedBy VARCHAR(450),
    IsDeleted BOOLEAN NOT NULL DEFAULT FALSE,
    DeletedAt TIMESTAMP,
    DeletedBy VARCHAR(450),

    CONSTRAINT UQ_Attributes_Code_Tenant UNIQUE (Code, TenantId)
);

CREATE INDEX IX_Attributes_TenantId ON Attributes(TenantId);
CREATE INDEX IX_Attributes_IsFilterable ON Attributes(TenantId, IsFilterable);
CREATE INDEX IX_Attributes_Type ON Attributes(TenantId, Type);

-- ============================================================================
-- ATTRIBUTE VALUE TABLE
-- ============================================================================
CREATE TABLE AttributeValues (
    Id UUID PRIMARY KEY,
    TenantId VARCHAR(64) NOT NULL,
    AttributeId UUID NOT NULL REFERENCES Attributes(Id) ON DELETE CASCADE,

    -- Value
    Value VARCHAR(200) NOT NULL,
    DisplayValue VARCHAR(200) NOT NULL,

    -- Visual options
    ColorCode VARCHAR(20),           -- "#FF0000"
    SwatchUrl VARCHAR(500),
    IconUrl VARCHAR(500),

    -- Organization
    SortOrder INT NOT NULL DEFAULT 0,
    IsActive BOOLEAN NOT NULL DEFAULT TRUE,

    -- Cached metrics
    ProductCount INT NOT NULL DEFAULT 0,

    -- Audit
    CreatedAt TIMESTAMP NOT NULL,
    CreatedBy VARCHAR(450),
    LastModifiedAt TIMESTAMP,
    LastModifiedBy VARCHAR(450),

    CONSTRAINT UQ_AttributeValues_Value_Attribute UNIQUE (Value, AttributeId)
);

CREATE INDEX IX_AttributeValues_AttributeId ON AttributeValues(AttributeId);
CREATE INDEX IX_AttributeValues_TenantId ON AttributeValues(TenantId);

-- ============================================================================
-- CATEGORY ATTRIBUTE (Junction)
-- ============================================================================
CREATE TABLE CategoryAttributes (
    Id UUID PRIMARY KEY,
    TenantId VARCHAR(64) NOT NULL,
    CategoryId UUID NOT NULL REFERENCES ProductCategories(Id) ON DELETE CASCADE,
    AttributeId UUID NOT NULL REFERENCES Attributes(Id) ON DELETE CASCADE,

    -- Category-specific settings
    IsRequired BOOLEAN NOT NULL DEFAULT FALSE,
    SortOrder INT NOT NULL DEFAULT 0,
    InheritToChildren BOOLEAN NOT NULL DEFAULT TRUE,

    -- Audit
    CreatedAt TIMESTAMP NOT NULL,
    CreatedBy VARCHAR(450),

    CONSTRAINT UQ_CategoryAttributes UNIQUE (CategoryId, AttributeId)
);

CREATE INDEX IX_CategoryAttributes_CategoryId ON CategoryAttributes(CategoryId);
CREATE INDEX IX_CategoryAttributes_AttributeId ON CategoryAttributes(AttributeId);

-- ============================================================================
-- PRODUCT ATTRIBUTE (Junction)
-- ============================================================================
CREATE TABLE ProductAttributes (
    Id UUID PRIMARY KEY,
    TenantId VARCHAR(64) NOT NULL,
    ProductId UUID NOT NULL REFERENCES Products(Id) ON DELETE CASCADE,
    AttributeId UUID NOT NULL REFERENCES Attributes(Id) ON DELETE CASCADE,
    VariantId UUID REFERENCES ProductVariants(Id) ON DELETE CASCADE,

    -- Value storage (polymorphic)
    AttributeValueId UUID REFERENCES AttributeValues(Id),
    AttributeValueIds JSONB,                    -- For MultiSelect
    TextValue TEXT,
    NumberValue DECIMAL(18,4),
    BoolValue BOOLEAN,
    DateValue DATE,
    DateTimeValue TIMESTAMP,
    ColorValue VARCHAR(20),
    MinRangeValue DECIMAL(18,4),
    MaxRangeValue DECIMAL(18,4),
    FileUrl VARCHAR(500),

    -- Computed display value
    DisplayValue VARCHAR(500),

    -- Audit
    CreatedAt TIMESTAMP NOT NULL,
    CreatedBy VARCHAR(450),
    LastModifiedAt TIMESTAMP,
    LastModifiedBy VARCHAR(450),

    CONSTRAINT UQ_ProductAttributes UNIQUE (ProductId, AttributeId, VariantId)
);

CREATE INDEX IX_ProductAttributes_ProductId ON ProductAttributes(ProductId);
CREATE INDEX IX_ProductAttributes_AttributeId ON ProductAttributes(AttributeId);
CREATE INDEX IX_ProductAttributes_AttributeValueId ON ProductAttributes(AttributeValueId);
CREATE INDEX IX_ProductAttributes_VariantId ON ProductAttributes(VariantId);

-- ============================================================================
-- PRODUCT FILTER INDEX (Denormalized)
-- ============================================================================
CREATE TABLE ProductFilterIndex (
    ProductId UUID PRIMARY KEY,
    TenantId VARCHAR(64) NOT NULL,

    -- Product info
    ProductName VARCHAR(200) NOT NULL,
    ProductSlug VARCHAR(200) NOT NULL,
    Status INT NOT NULL,

    -- Category
    CategoryId UUID,
    CategoryPath VARCHAR(500),               -- "1/5/23"
    CategoryName VARCHAR(200),

    -- Brand
    BrandId UUID,
    BrandName VARCHAR(200),
    BrandSlug VARCHAR(200),

    -- Pricing
    MinPrice DECIMAL(18,2) NOT NULL,
    MaxPrice DECIMAL(18,2) NOT NULL,
    Currency VARCHAR(10) NOT NULL DEFAULT 'VND',

    -- Stock
    InStock BOOLEAN NOT NULL,
    TotalStock INT NOT NULL DEFAULT 0,

    -- Rating
    AverageRating DECIMAL(3,2),
    ReviewCount INT NOT NULL DEFAULT 0,

    -- Attributes (JSONB for flexible filtering)
    AttributesJson JSONB NOT NULL DEFAULT '{}',

    -- Search
    SearchText TEXT NOT NULL,

    -- Images
    PrimaryImageUrl VARCHAR(500),

    -- Timestamps
    LastSyncedAt TIMESTAMP NOT NULL,
    ProductUpdatedAt TIMESTAMP NOT NULL
);

-- Critical indexes for filtering performance
CREATE INDEX IX_ProductFilterIndex_TenantId ON ProductFilterIndex(TenantId);
CREATE INDEX IX_ProductFilterIndex_Status ON ProductFilterIndex(TenantId, Status);
CREATE INDEX IX_ProductFilterIndex_CategoryId ON ProductFilterIndex(TenantId, CategoryId);
CREATE INDEX IX_ProductFilterIndex_BrandId ON ProductFilterIndex(TenantId, BrandId);
CREATE INDEX IX_ProductFilterIndex_Price ON ProductFilterIndex(TenantId, MinPrice, MaxPrice);
CREATE INDEX IX_ProductFilterIndex_InStock ON ProductFilterIndex(TenantId, InStock);

-- GIN index for JSONB attribute filtering
CREATE INDEX IX_ProductFilterIndex_Attributes ON ProductFilterIndex
    USING GIN (AttributesJson jsonb_path_ops);

-- Full-text search index
CREATE INDEX IX_ProductFilterIndex_Search ON ProductFilterIndex
    USING GIN (to_tsvector('english', SearchText));

-- ============================================================================
-- FILTER ANALYTICS EVENT
-- ============================================================================
CREATE TABLE FilterAnalyticsEvents (
    Id UUID PRIMARY KEY,
    TenantId VARCHAR(64) NOT NULL,

    -- Event
    EventType INT NOT NULL,
    Timestamp TIMESTAMP NOT NULL,

    -- Session
    SessionId VARCHAR(100) NOT NULL,
    UserId VARCHAR(450),

    -- Filter context
    CategoryId UUID,
    SearchQuery VARCHAR(500),
    FiltersJson JSONB NOT NULL DEFAULT '{}',
    ResultCount INT NOT NULL DEFAULT 0,

    -- Product events
    ProductId UUID,
    VariantId UUID,
    Position INT,

    -- Attribution
    AttributeCode VARCHAR(100),
    AttributeValue VARCHAR(200),

    -- Device/Location
    DeviceType VARCHAR(20),
    IpCountry VARCHAR(10),
    UserAgent VARCHAR(500),

    -- Processing
    IsProcessed BOOLEAN NOT NULL DEFAULT FALSE,
    ProcessedAt TIMESTAMP
);

-- Indexes for analytics queries
CREATE INDEX IX_FilterAnalyticsEvents_TenantId_Timestamp
    ON FilterAnalyticsEvents(TenantId, Timestamp DESC);
CREATE INDEX IX_FilterAnalyticsEvents_EventType
    ON FilterAnalyticsEvents(TenantId, EventType, Timestamp DESC);
CREATE INDEX IX_FilterAnalyticsEvents_AttributeCode
    ON FilterAnalyticsEvents(TenantId, AttributeCode, Timestamp DESC);
CREATE INDEX IX_FilterAnalyticsEvents_IsProcessed
    ON FilterAnalyticsEvents(IsProcessed) WHERE IsProcessed = FALSE;

-- Partitioning by month for large-scale analytics
-- (Enable if event volume exceeds 1M/month)
```

### 3.2 Product Table Modification

```sql
-- Add BrandId foreign key to Products table
ALTER TABLE Products
    ADD COLUMN BrandId UUID REFERENCES Brands(Id);

CREATE INDEX IX_Products_BrandId ON Products(TenantId, BrandId);

-- Migration: Copy existing Brand string to new Brand entities
-- (See Migration Strategy section)
```

---

## 4. API Design

### 4.1 Brand Endpoints

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ BRAND ENDPOINTS                                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│ Admin (requires authentication):                                             │
│                                                                              │
│ GET    /api/admin/brands                   List brands (paged, searchable)  │
│ GET    /api/admin/brands/{id}              Get brand details                │
│ POST   /api/admin/brands                   Create brand                     │
│ PUT    /api/admin/brands/{id}              Update brand                     │
│ DELETE /api/admin/brands/{id}              Soft delete brand                │
│ POST   /api/admin/brands/{id}/activate     Activate brand                   │
│ POST   /api/admin/brands/{id}/deactivate   Deactivate brand                 │
│ POST   /api/admin/brands/{id}/featured     Toggle featured status           │
│                                                                              │
│ Storefront (public):                                                         │
│                                                                              │
│ GET    /api/brands                         List active brands               │
│ GET    /api/brands/{slug}                  Get brand page with products     │
│ GET    /api/brands/featured                Get featured brands              │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.2 Attribute Endpoints

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ATTRIBUTE ENDPOINTS                                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│ Admin:                                                                       │
│                                                                              │
│ GET    /api/admin/attributes                    List all attributes         │
│ GET    /api/admin/attributes/{id}               Get attribute with values   │
│ POST   /api/admin/attributes                    Create attribute            │
│ PUT    /api/admin/attributes/{id}               Update attribute            │
│ DELETE /api/admin/attributes/{id}               Soft delete attribute       │
│                                                                              │
│ Attribute Values:                                                            │
│                                                                              │
│ POST   /api/admin/attributes/{id}/values        Add value                   │
│ PUT    /api/admin/attributes/{id}/values/{vid}  Update value                │
│ DELETE /api/admin/attributes/{id}/values/{vid}  Delete value                │
│ POST   /api/admin/attributes/{id}/values/reorder  Reorder values            │
│                                                                              │
│ Category Attributes:                                                         │
│                                                                              │
│ GET    /api/admin/categories/{id}/attributes    Get category's attributes   │
│ POST   /api/admin/categories/{id}/attributes    Assign attribute            │
│ DELETE /api/admin/categories/{id}/attributes/{aid}  Remove attribute        │
│                                                                              │
│ Product Attributes:                                                          │
│                                                                              │
│ GET    /api/admin/products/{id}/attributes      Get product's attributes    │
│ PUT    /api/admin/products/{id}/attributes      Bulk update attributes      │
│ GET    /api/admin/products/{id}/attributes/form Get form schema for product │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.3 Filter Endpoints

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ FILTER ENDPOINTS (Storefront)                                                │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│ GET /api/products/filter                                                     │
│                                                                              │
│ Query Parameters:                                                            │
│   category={slug}           Filter by category                              │
│   brand={slug1,slug2}       Filter by brands (OR)                           │
│   q={search}                Full-text search                                │
│   price_min={value}         Minimum price                                   │
│   price_max={value}         Maximum price                                   │
│   in_stock={true}           Only in-stock products                          │
│   attr_{code}={value1,v2}   Filter by attribute (OR within, AND across)     │
│   sort={field}              Sort field                                      │
│   order={asc|desc}          Sort order                                      │
│   page={n}                  Page number                                     │
│   page_size={n}             Page size                                       │
│                                                                              │
│ Response:                                                                    │
│ {                                                                            │
│   "products": [...],                                                         │
│   "total": 1234,                                                             │
│   "page": 1,                                                                 │
│   "pageSize": 24,                                                            │
│   "facets": {                                                                │
│     "brand": [                                                               │
│       { "value": "samsung", "label": "Samsung", "count": 234 },              │
│       { "value": "apple", "label": "Apple", "count": 189 }                   │
│     ],                                                                       │
│     "color": [                                                               │
│       { "value": "red", "label": "Red", "colorCode": "#FF0000", "count": 45}│
│     ],                                                                       │
│     "price": { "min": 100, "max": 2000 }                                    │
│   },                                                                         │
│   "appliedFilters": {                                                        │
│     "brand": ["samsung"],                                                    │
│     "color": ["red", "blue"]                                                 │
│   }                                                                          │
│ }                                                                            │
│                                                                              │
│ ─────────────────────────────────────────────────────────────────────────── │
│                                                                              │
│ GET /api/categories/{slug}/filters                                           │
│                                                                              │
│ Returns available filters for a category:                                    │
│ {                                                                            │
│   "filters": [                                                               │
│     {                                                                        │
│       "code": "brand",                                                       │
│       "name": "Brand",                                                       │
│       "type": "select",                                                      │
│       "values": [...]                                                        │
│     },                                                                       │
│     {                                                                        │
│       "code": "screen_size",                                                 │
│       "name": "Screen Size",                                                 │
│       "type": "range",                                                       │
│       "unit": "inch",                                                        │
│       "min": 5.0,                                                            │
│       "max": 7.0                                                             │
│     }                                                                        │
│   ]                                                                          │
│ }                                                                            │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 4.4 Analytics Endpoints

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ ANALYTICS ENDPOINTS                                                          │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│ Event Tracking (from frontend):                                              │
│                                                                              │
│ POST /api/analytics/filter-events                                            │
│ {                                                                            │
│   "eventType": "FilterApplied",                                              │
│   "sessionId": "abc123",                                                     │
│   "categoryId": "...",                                                       │
│   "filters": { "color": ["red"] },                                           │
│   "attributeCode": "color",                                                  │
│   "attributeValue": "red",                                                   │
│   "resultCount": 45                                                          │
│ }                                                                            │
│                                                                              │
│ Analytics Dashboard (admin):                                                 │
│                                                                              │
│ GET /api/admin/analytics/filters/popular                                     │
│   ?from={date}&to={date}&categoryId={id}                                     │
│                                                                              │
│ Response:                                                                    │
│ {                                                                            │
│   "topFilters": [                                                            │
│     { "attribute": "color", "value": "red", "count": 1234, "conversion": 5.2}│
│   ],                                                                         │
│   "filterCombinations": [...],                                               │
│   "zeroResultFilters": [...]                                                 │
│ }                                                                            │
│                                                                              │
│ GET /api/admin/analytics/filters/conversion                                  │
│   ?attributeCode={code}&from={date}&to={date}                                │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 5. Filter Index Strategy

### 5.1 Sync Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    FILTER INDEX SYNC FLOW                                    │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│   Product Command          Domain Event           Event Handler              │
│   ──────────────          ────────────           ─────────────              │
│                                                                              │
│   CreateProduct ────────► ProductCreatedEvent ────► UpdateFilterIndex       │
│   UpdateProduct ────────► ProductUpdatedEvent ────► UpdateFilterIndex       │
│   DeleteProduct ────────► ProductDeletedEvent ────► RemoveFromIndex         │
│   PublishProduct ───────► ProductPublishedEvent ──► UpdateFilterIndex       │
│   ArchiveProduct ───────► ProductArchivedEvent ───► UpdateFilterIndex       │
│                                                                              │
│   UpdateProductAttribute ► ProductAttributeChangedEvent ► UpdateFilterIndex │
│   AddProductVariant ─────► ProductVariantAddedEvent ────► UpdateFilterIndex │
│                                                                              │
│   UpdateBrand ──────────► BrandUpdatedEvent ─────► UpdateProductsWithBrand  │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 5.2 Index Update Handler

```csharp
public class ProductFilterIndexSyncHandler :
    INotificationHandler<ProductCreatedEvent>,
    INotificationHandler<ProductUpdatedEvent>,
    INotificationHandler<ProductAttributeChangedEvent>
{
    public async Task Handle(ProductUpdatedEvent notification, CancellationToken ct)
    {
        // 1. Load product with all needed data
        var product = await _productRepository.FirstOrDefaultAsync(
            new ProductWithAttributesSpec(notification.ProductId), ct);

        if (product is null) return;

        // 2. Load brand if exists
        Brand? brand = null;
        if (product.BrandId.HasValue)
        {
            brand = await _brandRepository.GetByIdAsync(product.BrandId.Value, ct);
        }

        // 3. Build attributes JSON
        var attributesJson = BuildAttributesJson(product);

        // 4. Update or insert index
        var index = await _filterIndexRepository.GetByIdAsync(product.Id, ct)
            ?? new ProductFilterIndex { ProductId = product.Id };

        index.UpdateFromProduct(product, brand, attributesJson);

        await _filterIndexRepository.UpsertAsync(index, ct);
    }

    private string BuildAttributesJson(Product product)
    {
        var attrs = new Dictionary<string, object>();

        foreach (var pa in product.ProductAttributes)
        {
            var attr = pa.Attribute;
            var key = attr.Code;

            object? value = attr.Type switch
            {
                AttributeType.Select => pa.SelectedValue?.Value,
                AttributeType.MultiSelect => JsonSerializer.Deserialize<List<string>>(pa.AttributeValueIds ?? "[]"),
                AttributeType.Number or AttributeType.Decimal => pa.NumberValue,
                AttributeType.Boolean => pa.BoolValue,
                AttributeType.Color => pa.ColorValue,
                AttributeType.Range => new { min = pa.MinRangeValue, max = pa.MaxRangeValue },
                _ => pa.TextValue
            };

            if (value != null) attrs[key] = value;
        }

        return JsonSerializer.Serialize(attrs);
    }
}
```

### 5.3 Filter Query Builder

```csharp
public class ProductFilterQueryBuilder
{
    public async Task<FilteredProductsResult> ExecuteAsync(
        ProductFilterRequest request,
        CancellationToken ct)
    {
        // Start with tenant filter
        var query = _dbContext.ProductFilterIndex
            .Where(p => p.TenantId == _currentTenant.TenantId)
            .Where(p => p.Status == ProductStatus.Active);

        // Category filter (with hierarchy support)
        if (!string.IsNullOrEmpty(request.CategorySlug))
        {
            var category = await GetCategoryBySlug(request.CategorySlug, ct);
            if (category != null)
            {
                // Include subcategories
                query = query.Where(p =>
                    p.CategoryId == category.Id ||
                    p.CategoryPath.StartsWith(category.Id.ToString()));
            }
        }

        // Brand filter (OR within)
        if (request.Brands?.Any() == true)
        {
            query = query.Where(p => request.Brands.Contains(p.BrandSlug));
        }

        // Price range
        if (request.PriceMin.HasValue)
        {
            query = query.Where(p => p.MaxPrice >= request.PriceMin.Value);
        }
        if (request.PriceMax.HasValue)
        {
            query = query.Where(p => p.MinPrice <= request.PriceMax.Value);
        }

        // In stock
        if (request.InStockOnly)
        {
            query = query.Where(p => p.InStock);
        }

        // Attribute filters (JSONB queries)
        foreach (var (attrCode, values) in request.AttributeFilters)
        {
            // OR within attribute values
            var jsonPath = $"$.{attrCode}";
            query = query.Where(p =>
                EF.Functions.JsonContains(p.AttributesJson,
                    JsonSerializer.Serialize(values)));
        }

        // Full-text search
        if (!string.IsNullOrEmpty(request.SearchQuery))
        {
            query = query.Where(p =>
                EF.Functions.ToTsVector("english", p.SearchText)
                    .Matches(EF.Functions.PlainToTsQuery("english", request.SearchQuery)));
        }

        // Get total count
        var totalCount = await query.CountAsync(ct);

        // Get facets (counts for each filter value)
        var facets = await CalculateFacetsAsync(query, request.CategorySlug, ct);

        // Apply sorting
        query = request.Sort switch
        {
            "price_asc" => query.OrderBy(p => p.MinPrice),
            "price_desc" => query.OrderByDescending(p => p.MinPrice),
            "name" => query.OrderBy(p => p.ProductName),
            "newest" => query.OrderByDescending(p => p.ProductUpdatedAt),
            _ => query.OrderByDescending(p => p.ProductUpdatedAt)
        };

        // Pagination
        var products = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        return new FilteredProductsResult
        {
            Products = products.Select(MapToDto).ToList(),
            Total = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
            Facets = facets,
            AppliedFilters = request.AttributeFilters
        };
    }
}
```

---

## 6. Analytics Event System

### 6.1 Frontend Integration

```typescript
// services/filterAnalytics.ts

interface FilterEvent {
  eventType: 'FilterApplied' | 'FilterRemoved' | 'FilterCleared' | 'ProductViewed' | 'ProductAddedToCart';
  sessionId: string;
  categoryId?: string;
  searchQuery?: string;
  filters: Record<string, string[]>;
  attributeCode?: string;
  attributeValue?: string;
  productId?: string;
  variantId?: string;
  position?: number;
  resultCount: number;
}

class FilterAnalyticsService {
  private queue: FilterEvent[] = [];
  private flushInterval = 5000; // 5 seconds

  constructor() {
    setInterval(() => this.flush(), this.flushInterval);
    window.addEventListener('beforeunload', () => this.flush());
  }

  trackFilterApplied(
    attributeCode: string,
    value: string,
    filters: Record<string, string[]>,
    resultCount: number
  ) {
    this.queue.push({
      eventType: 'FilterApplied',
      sessionId: this.getSessionId(),
      filters,
      attributeCode,
      attributeValue: value,
      resultCount
    });
  }

  trackProductViewed(productId: string, position: number, filters: Record<string, string[]>) {
    this.queue.push({
      eventType: 'ProductViewed',
      sessionId: this.getSessionId(),
      productId,
      position,
      filters,
      resultCount: 0
    });
  }

  private async flush() {
    if (this.queue.length === 0) return;

    const events = [...this.queue];
    this.queue = [];

    await fetch('/api/analytics/filter-events/batch', {
      method: 'POST',
      body: JSON.stringify({ events }),
      headers: { 'Content-Type': 'application/json' }
    });
  }
}

export const filterAnalytics = new FilterAnalyticsService();
```

### 6.2 Backend Event Handler

```csharp
public class FilterAnalyticsEventHandler
{
    public async Task Handle(
        CreateFilterAnalyticsEventsCommand command,
        CancellationToken ct)
    {
        var events = command.Events.Select(e => FilterAnalyticsEvent.Create(
            sessionId: e.SessionId,
            userId: _currentUser.UserId,
            eventType: e.EventType,
            categoryId: e.CategoryId,
            searchQuery: e.SearchQuery,
            filters: e.Filters,
            attributeCode: e.AttributeCode,
            attributeValue: e.AttributeValue,
            productId: e.ProductId,
            variantId: e.VariantId,
            position: e.Position,
            resultCount: e.ResultCount,
            deviceType: _requestInfo.DeviceType,
            ipCountry: _requestInfo.Country,
            tenantId: _currentTenant.TenantId
        )).ToList();

        await _repository.AddRangeAsync(events, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
```

### 6.3 Analytics Aggregation (Background Job)

```csharp
public class FilterAnalyticsAggregationJob : IRecurringJob
{
    public string CronExpression => "0 */15 * * * *"; // Every 15 minutes

    public async Task ExecuteAsync(CancellationToken ct)
    {
        // 1. Get unprocessed events
        var events = await _repository.ListAsync(
            new UnprocessedFilterEventsSpec(limit: 10000), ct);

        if (!events.Any()) return;

        // 2. Aggregate by attribute/value combinations
        var aggregations = events
            .Where(e => e.EventType == FilterEventType.FilterApplied)
            .GroupBy(e => new { e.TenantId, e.AttributeCode, e.AttributeValue })
            .Select(g => new FilterUsageAggregate
            {
                TenantId = g.Key.TenantId,
                AttributeCode = g.Key.AttributeCode,
                AttributeValue = g.Key.AttributeValue,
                UsageCount = g.Count(),
                Date = DateOnly.FromDateTime(DateTime.UtcNow)
            });

        // 3. Upsert aggregations
        await _aggregateRepository.UpsertRangeAsync(aggregations, ct);

        // 4. Mark events as processed
        foreach (var e in events)
        {
            e.MarkAsProcessed();
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
```

---

## 7. Migration Strategy

### 7.1 Phase Overview

| Phase | Description | Effort | Dependencies | Status | Priority |
|-------|-------------|--------|--------------|--------|----------|
| **Phase 1** | Create Brand entity, migrate Product.Brand | 3 days | None | ✅ DONE | - |
| **Phase 2** | Create Attribute entities & management UI | 3 days | Phase 1 | ✅ DONE | - |
| **Phase 3** | Create CategoryAttribute management UI | 2 days | Phase 2 | ✅ DONE | - |
| **Phase 4** | Create ProductAttributeAssignment entity | 3 days | Phase 3 | ✅ DONE | - |
| **Phase 5** | Create ProductFilterIndex + sync | 5 days | Phase 4 | ✅ DONE | - |
| **Phase 9** | Product form attribute integration (FE) | 3 days | Phase 4 | ❌ TODO | **🔴 CRITICAL** |
| **Phase 6** | Filter API endpoints | 3 days | Phase 5 | ❌ TODO | High |
| **Phase 7** | Analytics events | 2 days | Phase 6 | ❌ TODO | Medium |
| **Phase 8** | Frontend filter UI (Storefront) | 5 days | Phase 6 | ❌ TODO | Medium |

> **⚠️ CRITICAL GAP**: Phase 9 must be implemented FIRST. Without it, admins cannot set product attributes despite having all backend APIs ready. The entire attribute system is unusable from an admin perspective.

### 7.1.1 Detailed Phase Status

#### Phase 1: Brand Entity ✅ DONE
- [x] Brand domain entity (BE)
- [x] Brand EF configuration (BE)
- [x] Brand Commands: Create, Update, Delete (BE)
- [x] Brand Queries: GetBrands, GetBrandById (BE)
- [x] Brand API Endpoints (BE)
- [x] Brand Permissions (BE)
- [x] Product.BrandId integration (BE)
- [x] Brands management page (FE)
- [x] Brand dialog (create/edit) (FE)
- [x] Localization (EN/VI)

#### Phase 2: Attribute Entities ✅ DONE
- [x] ProductAttribute domain entity (aggregate root) (BE)
- [x] ProductAttributeValue domain entity (child) (BE)
- [x] CategoryAttribute domain entity (junction) (BE)
- [x] AttributeType enum (13 types) (BE)
- [x] EF configurations for all entities (BE)
- [x] DTOs, Specifications, Mapper (BE)
- [x] ProductAttribute Commands: Create, Update, Delete, AddValue, UpdateValue, RemoveValue (BE)
- [x] ProductAttribute Queries: GetProductAttributes, GetProductAttributeById (BE)
- [x] ProductAttribute API Endpoints (BE)
- [x] Attribute Permissions (BE)
- [x] Product Attributes management page (FE)
- [x] ProductAttribute dialog (create/edit with tabs) (FE)
- [x] Types, Services, Hooks (FE)
- [x] Route and navigation (FE)
- [x] Localization (EN/VI)

#### Phase 3: CategoryAttribute Management UI ✅ DONE
- [x] CategoryAttribute Commands: Assign, Remove, Update (BE)
- [x] CategoryAttribute Queries: GetCategoryAttributes (BE)
- [x] CategoryAttribute API Endpoints (BE)
- [x] CategoryAttributesDialog component (FE)
- [x] "Manage Attributes" menu in ProductCategoriesPage (FE)
- [x] Localization for EN and VI

#### Phase 4: ProductAttributeAssignment Entity ✅ DONE
- [x] ProductAttributeAssignment entity (stores product's attribute values) (BE)
- [x] EF configuration with polymorphic value storage (BE)
- [x] ProductAttributeAssignment Commands: SetProductAttributeValue, BulkUpdateProductAttributes (BE)
- [x] ProductAttributeAssignment Queries: GetProductAttributeAssignments, GetProductAttributeFormSchema (BE)
- [x] ProductAttributeAssignment API Endpoints (BE)
- [x] ProductAttributesByIdsSpec specification for category-based filtering (BE)
- [x] Frontend types and API service functions (FE)

#### Phase 5: ProductFilterIndex ✅ DONE
- [x] ProductFilterIndex entity (denormalized) (BE)
- [x] EF configuration with optimized indexes (BE)
- [x] Index sync event handlers (ProductFilterIndexSyncHandler) (BE)
- [x] Full reindex background job (ProductFilterIndexReindexJob) (BE)
- [x] Index maintenance job (ProductFilterIndexMaintenanceJob) (BE)
- [x] AttributeJsonBuilder shared service (BE)
- [x] Domain events: ProductUpdatedEvent, ProductAttributeAssignmentChangedEvent (BE)
- [x] Unit tests for sync handler (12 tests) (BE)

#### Phase 6: Filter API Endpoints ❌ TODO
- [ ] ProductFilterQueryBuilder service (BE)
- [ ] Facet calculation service (BE)
- [ ] Filter Products query (BE)
- [ ] Get Category Filters query (BE)
- [ ] Filter caching strategy (BE)

#### Phase 7: Analytics Events ❌ TODO
- [ ] FilterAnalyticsEvent entity (BE)
- [ ] FilterEventType enum (BE)
- [ ] Analytics Commands: CreateFilterEvents (BE)
- [ ] Analytics Queries: GetPopularFilters, GetFilterConversion (BE)
- [ ] Analytics aggregation job (BE)
- [ ] Analytics API Endpoints (BE)

#### Phase 8: Frontend Filter UI (Storefront) ❌ TODO

> **Build with**: `/ui-ux-pro-max` skill for all components below

**Components Required**:
```
src/NOIR.Web/frontend/src/components/storefront/
├── FilterSidebar.tsx              # Main filter container
├── facets/
│   ├── FacetCheckbox.tsx          # Multi-select checkboxes
│   ├── FacetColorSwatch.tsx       # Color picker swatches
│   ├── FacetPriceRange.tsx        # Price slider
│   ├── FacetRating.tsx            # Star rating filter
│   └── FacetBrand.tsx             # Brand logos
├── AppliedFilters.tsx             # Chip display of active filters
└── FilterMobileDrawer.tsx         # Mobile responsive drawer
```

**Subtasks**:
- [ ] 8.1 Create `FilterSidebar.tsx` - collapsible sections (FE)
- [ ] 8.2 Create facet components for each filter type (FE)
- [ ] 8.3 Create `AppliedFilters.tsx` - removable chips (FE)
- [ ] 8.4 Create `FilterMobileDrawer.tsx` - slide-in on mobile (FE)
- [ ] 8.5 Create filter analytics tracking service (FE)
- [ ] 8.6 Integrate with storefront product list page (FE)

#### Phase 9: Product Form Attribute Integration ❌ TODO (🔴 CRITICAL - DO FIRST)

> **Why Critical**: Backend APIs are complete but ProductFormPage.tsx has NO attributes section. Admins can create attributes, assign to categories, but CANNOT set values on products!

---

##### 9.A Backend Tasks (Already Complete ✅)

| Task | Status | Location |
|------|--------|----------|
| ProductAttributeAssignment entity | ✅ Done | `Domain/Entities/Product/ProductAttributeAssignment.cs` |
| EF configuration | ✅ Done | `Infrastructure/Persistence/Configurations/` |
| SetProductAttributeValue command | ✅ Done | `Application/Features/ProductAttributes/Commands/` |
| BulkUpdateProductAttributes command | ✅ Done | `Application/Features/ProductAttributes/Commands/` |
| GetProductAttributeFormSchema query | ✅ Done | `Application/Features/ProductAttributes/Queries/` |
| GetProductAttributeAssignments query | ✅ Done | `Application/Features/ProductAttributes/Queries/` |
| API endpoints | ✅ Done | `Web/Endpoints/ProductAttributeEndpoints.cs` |

**API Endpoints Available**:
```
GET  /api/products/{id}/attributes/form-schema  → ProductAttributeFormSchemaDto
GET  /api/products/{id}/attributes              → List<ProductAttributeAssignmentDto>
PUT  /api/products/{id}/attributes/{attrId}     → ProductAttributeAssignmentDto
PUT  /api/products/{id}/attributes              → List<ProductAttributeAssignmentDto> (bulk)
```

---

##### 9.B Frontend Tasks (TODO)

> **⚠️ IMPORTANT**: All UI components MUST be built using `/ui-ux-pro-max` skill per CLAUDE.md rules.

**Missing Hooks** (useProductAttributes.ts):
- [ ] Add `useProductAttributeForm(productId, categoryId)` hook (FE)
- [ ] Add `useBulkUpdateProductAttributes()` mutation hook (FE)

**New Components Required**:
```
src/NOIR.Web/frontend/src/components/products/
├── ProductAttributesSection.tsx       # Card for ProductFormPage
└── AttributeInputs/                    # 13 input components
    ├── SelectAttributeInput.tsx        # Dropdown (shadcn Select)
    ├── MultiSelectAttributeInput.tsx   # Checkboxes (shadcn Checkbox group)
    ├── TextAttributeInput.tsx          # Text input (shadcn Input)
    ├── TextAreaAttributeInput.tsx      # Multi-line (shadcn Textarea)
    ├── NumberAttributeInput.tsx        # Integer (shadcn Input type=number)
    ├── DecimalAttributeInput.tsx       # Decimal (shadcn Input with step)
    ├── BooleanAttributeInput.tsx       # Toggle (shadcn Switch)
    ├── DateAttributeInput.tsx          # Date picker (shadcn Calendar + Popover)
    ├── DateTimeAttributeInput.tsx      # DateTime picker
    ├── ColorAttributeInput.tsx         # Color picker with swatches
    ├── RangeAttributeInput.tsx         # Min/Max range (dual Input)
    ├── UrlAttributeInput.tsx           # URL input with validation
    ├── FileAttributeInput.tsx          # File upload (ImageUploadZone pattern)
    └── AttributeInputFactory.tsx       # Factory component
```

---

##### 9.C UI/UX Design Specifications

> **Build with**: `/ui-ux-pro-max` skill for all components below

**ProductAttributesSection Card Design**:
```
┌─────────────────────────────────────────────────────────────┐
│ 📦 Product Attributes                            [Collapse] │
│ ─────────────────────────────────────────────────────────── │
│                                                             │
│ Category: Electronics > Smartphones                         │
│ ─────────────────────────────────────────────────────────── │
│                                                             │
│ Screen Size *                    Storage Capacity           │
│ ┌─────────────────────────┐     ┌─────────────────────────┐│
│ │ 6.7 ▾                   │     │ ☑ 128GB  ☑ 256GB       ││
│ │ inch                    │     │ ☐ 512GB  ☐ 1TB        ││
│ └─────────────────────────┘     └─────────────────────────┘│
│                                                             │
│ Color                            Water Resistant            │
│ ┌─────────────────────────┐     ┌─────────────────────────┐│
│ │ 🔴 🔵 ⚫ ⚪ 🟡          │     │ ◉ Yes  ○ No            ││
│ └─────────────────────────┘     └─────────────────────────┘│
│                                                             │
│ ⚠️ Fill in required (*) attributes before publishing       │
└─────────────────────────────────────────────────────────────┘
```

**UX Requirements**:
| Requirement | Implementation |
|-------------|----------------|
| Responsive grid | 1 col mobile, 2 cols tablet, 3 cols desktop |
| Required indicators | Red asterisk (*) + validation message |
| Help text | Tooltip icon with attribute.helpText |
| Unit display | Suffix label (e.g., "inch", "mAh", "kg") |
| Color swatches | Circular buttons with colorCode background |
| Loading state | Skeleton placeholders for form fields |
| Empty state | "No attributes for this category" message |
| Category change | Smooth transition, confirm if values exist |

**Accessibility (ARIA)**:
- All inputs must have `aria-label` with attribute name
- Required fields: `aria-required="true"`
- Error messages: `aria-describedby` linked to FormMessage
- Color swatches: `aria-label="Select {colorName}"` + keyboard nav

**Styling (shadcn/ui + Tailwind)**:
- Card: `shadow-sm hover:shadow-lg transition-all duration-300`
- Section header: Gradient text like other cards
- Inputs: Match existing ProductFormPage styling
- Color picker: `rounded-full w-8 h-8 cursor-pointer ring-2 ring-offset-2`

---

##### 9.D Subtasks (Ordered)

**Hooks (9.1-9.2)**:
- [ ] 9.1 Create `useProductAttributeForm(productId)` hook - fetches form schema (FE)
- [ ] 9.2 Create `useBulkUpdateProductAttributes()` mutation hook (FE)

**Components (9.3-9.5)** - Use `/ui-ux-pro-max`:
- [ ] 9.3 Create `AttributeInputFactory.tsx` - routes to correct input by AttributeType (FE)
- [ ] 9.4 Create 13 attribute input components matching UX specs above (FE)
- [ ] 9.5 Create `ProductAttributesSection.tsx` - renders dynamic form from schema (FE)

**Integration (9.6-9.9)**:
- [ ] 9.6 Integrate into ProductFormPage.tsx after "Organization" card (FE)
- [ ] 9.7 Handle category change → re-fetch attribute schema, warn if values exist (FE)
- [ ] 9.8 Save attributes on form submit (call bulkUpdateProductAttributes after product save) (FE)
- [ ] 9.9 Add loading/error/empty states for attribute section (FE)

**Polish (9.10-9.11)**:
- [ ] 9.10 Add localization keys for all attribute labels (EN/VI)
- [ ] 9.11 Unit tests for attribute hooks and components (Test)

### 7.1.2 Additional Gaps Identified (2026-01-29 Audit)

#### Gap A: ProductDto Missing Attribute Assignments
**Location**: `src/NOIR.Application/Features/Products/DTOs/ProductDtos.cs`

**Current State**: `ProductDto` and `ProductListDto` do not include product attribute values.

**Impact**: Even after Phase 9, product detail API won't return attribute data unless we update DTOs.

**Required Changes**:
```csharp
// Add to ProductDto
List<ProductAttributeAssignmentDto> Attributes

// Add to ProductListDto (only showInProductCard=true attributes)
List<ProductAttributeDisplayDto> DisplayAttributes
```

**Subtasks**:
- [ ] A.1 Add `Attributes` property to `ProductDto` (BE)
- [ ] A.2 Add `DisplayAttributes` property to `ProductListDto` (BE)
- [ ] A.3 Update `ProductMapper` to include attributes (BE)
- [ ] A.4 Update `ProductByIdSpec` to include attribute assignments (BE)
- [ ] A.5 Update frontend `Product` type to include attributes (FE)

#### Gap B: Product Listing Missing Attribute Badges
**Location**: `src/NOIR.Web/frontend/src/pages/portal/ecommerce/products/components/EnhancedProductCard.tsx`

**Current State**: Product cards don't display any attribute badges.

**Impact**: Admin can't see key attributes (color, size, etc.) in product grid.

> **Build with**: `/ui-ux-pro-max` skill

**Design Mockup**:
```
┌──────────────────────────────┐
│  [Product Image]             │
│                              │
│  Product Name                │
│  $199.00                     │
│  ─────────────────────────── │
│  🔴 🔵 ⚫  128GB | 6.7"      │  ← Attribute badges
│  [Draft] [Low Stock]         │
└──────────────────────────────┘
```

**Subtasks**:
- [ ] B.1 Add `DisplayAttributes` to frontend Product type (FE)
- [ ] B.2 Create `AttributeBadges.tsx` component (FE) - use `/ui-ux-pro-max`
- [ ] B.3 Integrate into `EnhancedProductCard.tsx` (FE)
- [ ] B.4 Color swatches: circular, max 5 shown, "+3 more" overflow (FE)
- [ ] B.5 Text badges: compact pills with unit suffix (FE)

#### Gap C: Product Search Missing Attribute Filtering (Admin)
**Location**: `src/NOIR.Web/frontend/src/pages/portal/ecommerce/products/ProductsPage.tsx`

**Current State**: Can only filter by status, category, brand. No attribute filtering.

**Impact**: Admin can't find products by attribute values.

**Subtasks** (Lower priority - Phase 6 will handle storefront):
- [ ] C.1 Add attribute filter dropdown to ProductsPage (FE)
- [ ] C.2 Update `GetProductsQuery` to accept attribute filters (BE)

---

### 7.2 Phase 1: Brand Migration

```csharp
// Data migration script
public class MigrateBrandDataCommand
{
    public async Task Execute(CancellationToken ct)
    {
        // 1. Get distinct brand strings from products
        var brandStrings = await _dbContext.Products
            .Where(p => p.Brand != null)
            .Select(p => new { p.TenantId, p.Brand })
            .Distinct()
            .ToListAsync(ct);

        // 2. Create Brand entities
        foreach (var b in brandStrings)
        {
            var slug = GenerateSlug(b.Brand);
            var brand = Brand.Create(b.Brand, slug, b.TenantId);
            await _brandRepository.AddAsync(brand, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // 3. Update Product.BrandId references
        var brands = await _brandRepository.ListAsync(ct);
        var brandLookup = brands.ToDictionary(
            b => (b.TenantId, b.Name.ToLower()),
            b => b.Id);

        var products = await _dbContext.Products
            .Where(p => p.Brand != null)
            .ToListAsync(ct);

        foreach (var p in products)
        {
            var key = (p.TenantId, p.Brand.ToLower());
            if (brandLookup.TryGetValue(key, out var brandId))
            {
                p.SetBrandId(brandId);
            }
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
```

### 7.3 Phase 3: ProductOption Migration

```csharp
// Migrate ProductOption to Attribute system
public class MigrateProductOptionsCommand
{
    public async Task Execute(CancellationToken ct)
    {
        // 1. Create Attributes from unique ProductOption names
        var options = await _dbContext.ProductOptions
            .Include(o => o.Values)
            .GroupBy(o => new { o.TenantId, o.Name })
            .ToListAsync(ct);

        foreach (var group in options)
        {
            // Create attribute
            var attr = Attribute.Create(
                code: group.Key.Name,
                name: group.First().DisplayName ?? group.Key.Name,
                type: AttributeType.Select,
                tenantId: group.Key.TenantId);

            attr.SetBehaviorFlags(
                isFilterable: true,
                isSearchable: true,
                isRequired: false,
                isVariantAttribute: true);

            // Create values
            var uniqueValues = group
                .SelectMany(o => o.Values)
                .GroupBy(v => v.Value)
                .Select(g => g.First());

            foreach (var v in uniqueValues)
            {
                var attrValue = attr.AddValue(v.Value, v.DisplayValue);
                attrValue.SetVisuals(v.ColorCode, v.SwatchUrl, null);
            }

            await _attributeRepository.AddAsync(attr, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // 2. Create ProductAttributes from existing data
        // (Similar pattern for ProductOption -> ProductAttribute)
    }
}
```

---

## 8. Performance Considerations

### 8.1 Benchmarks

| Operation | Target | Strategy |
|-----------|--------|----------|
| Filter query (100K products) | < 100ms | Denormalized index + covering indexes |
| Facet calculation | < 200ms | Pre-aggregated counts + cache |
| Index sync | < 1s | Async event handler |
| Full reindex | < 5 min | Batch processing, parallel writes |

### 8.2 Caching Strategy

```csharp
// Cache popular filter combinations
public class FilterResultCache
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(5);

    public async Task<FilteredProductsResult?> GetAsync(
        string tenantId,
        ProductFilterRequest request,
        CancellationToken ct)
    {
        var key = BuildCacheKey(tenantId, request);
        var cached = await _cache.GetStringAsync(key, ct);

        if (cached == null) return null;

        return JsonSerializer.Deserialize<FilteredProductsResult>(cached);
    }

    public async Task SetAsync(
        string tenantId,
        ProductFilterRequest request,
        FilteredProductsResult result,
        CancellationToken ct)
    {
        var key = BuildCacheKey(tenantId, request);
        var json = JsonSerializer.Serialize(result);

        await _cache.SetStringAsync(key, json, new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = _ttl
        }, ct);
    }

    // Invalidate on product changes
    public async Task InvalidateAsync(string tenantId, Guid? categoryId, CancellationToken ct)
    {
        // Invalidate pattern-matched keys
        await _cache.RemoveByPrefixAsync($"filter:{tenantId}:", ct);
    }
}
```

### 8.3 Index Maintenance

```csharp
// Periodic full reindex for data consistency
public class ProductFilterIndexMaintenanceJob : IRecurringJob
{
    public string CronExpression => "0 0 3 * * *"; // 3 AM daily

    public async Task ExecuteAsync(CancellationToken ct)
    {
        // 1. Find products with stale index
        var staleProducts = await _dbContext.Products
            .Where(p => p.LastModifiedAt >
                        _dbContext.ProductFilterIndex
                            .Where(i => i.ProductId == p.Id)
                            .Select(i => i.LastSyncedAt)
                            .FirstOrDefault())
            .Select(p => p.Id)
            .ToListAsync(ct);

        // 2. Reindex in batches
        foreach (var batch in staleProducts.Chunk(100))
        {
            await _indexer.ReindexProductsAsync(batch, ct);
        }

        // 3. Update AttributeValue.ProductCount
        await _dbContext.Database.ExecuteSqlRawAsync(@"
            UPDATE AttributeValues av
            SET ProductCount = (
                SELECT COUNT(DISTINCT pa.ProductId)
                FROM ProductAttributes pa
                WHERE pa.AttributeValueId = av.Id
            )", ct);
    }
}
```

---

## Appendix A: DTOs

```csharp
// Request/Response DTOs for API endpoints

public record CreateBrandRequest(
    string Name,
    string Slug,
    string? LogoUrl,
    string? Description,
    string? Website);

public record BrandDto(
    Guid Id,
    string Name,
    string Slug,
    string? LogoUrl,
    string? BannerUrl,
    string? Description,
    string? Website,
    bool IsActive,
    bool IsFeatured,
    int ProductCount);

public record CreateAttributeRequest(
    string Code,
    string Name,
    AttributeType Type,
    bool IsFilterable,
    bool IsSearchable,
    bool IsRequired,
    bool IsVariantAttribute,
    string? Unit,
    decimal? MinValue,
    decimal? MaxValue);

public record AttributeDto(
    Guid Id,
    string Code,
    string Name,
    AttributeType Type,
    bool IsFilterable,
    bool IsSearchable,
    bool IsRequired,
    bool IsVariantAttribute,
    bool ShowInProductCard,
    bool ShowInSpecifications,
    string? Unit,
    string? Placeholder,
    string? HelpText,
    List<AttributeValueDto> Values);

public record AttributeValueDto(
    Guid Id,
    string Value,
    string DisplayValue,
    string? ColorCode,
    string? SwatchUrl,
    int ProductCount);

public record ProductFilterRequest(
    string? CategorySlug,
    List<string>? Brands,
    string? SearchQuery,
    decimal? PriceMin,
    decimal? PriceMax,
    bool InStockOnly,
    Dictionary<string, List<string>> AttributeFilters,
    string Sort = "newest",
    int Page = 1,
    int PageSize = 24);

public record FilteredProductsResult(
    List<ProductListDto> Products,
    int Total,
    int Page,
    int PageSize,
    FacetsDto Facets,
    Dictionary<string, List<string>> AppliedFilters);

public record FacetsDto(
    List<FacetDto> Brand,
    List<FacetDto> Attributes,
    PriceRangeFacet Price);

public record FacetDto(
    string Code,
    string Name,
    string DisplayType, // "checkbox" | "color" | "range"
    List<FacetValueDto> Values);

public record FacetValueDto(
    string Value,
    string Label,
    string? ColorCode,
    string? SwatchUrl,
    int Count,
    bool IsSelected);
```

---

## Appendix B: File Structure

```
src/NOIR.Domain/Entities/Product/
├── Brand.cs                      (new)
├── Attribute.cs                  (new)
├── AttributeType.cs              (new)
├── AttributeValue.cs             (new)
├── CategoryAttribute.cs          (new)
├── ProductAttribute.cs           (new)
├── ProductFilterIndex.cs         (new)

src/NOIR.Domain/Entities/Analytics/
├── FilterAnalyticsEvent.cs       (new)
├── FilterEventType.cs            (new)

src/NOIR.Application/Features/Brands/
├── Commands/
│   ├── CreateBrand/
│   ├── UpdateBrand/
│   ├── DeleteBrand/
├── Queries/
│   ├── GetBrands/
│   ├── GetBrandBySlug/
├── DTOs/BrandDtos.cs
├── Specifications/BrandSpecifications.cs

src/NOIR.Application/Features/Attributes/
├── Commands/
│   ├── CreateAttribute/
│   ├── UpdateAttribute/
│   ├── AddAttributeValue/
│   ├── AssignCategoryAttribute/
├── Queries/
│   ├── GetAttributes/
│   ├── GetCategoryAttributes/
├── DTOs/AttributeDtos.cs
├── Specifications/AttributeSpecifications.cs

src/NOIR.Application/Features/ProductFilter/
├── Queries/
│   ├── FilterProducts/
│   ├── GetCategoryFilters/
├── DTOs/FilterDtos.cs
├── Services/
│   ├── ProductFilterIndexService.cs
│   ├── FacetCalculator.cs

src/NOIR.Application/Features/FilterAnalytics/
├── Commands/
│   ├── CreateFilterEvents/
├── Queries/
│   ├── GetPopularFilters/
│   ├── GetFilterConversion/
├── Jobs/
│   ├── FilterAnalyticsAggregationJob.cs

src/NOIR.Web/Endpoints/
├── BrandEndpoints.cs             (new)
├── AttributeEndpoints.cs         (new)
├── ProductFilterEndpoints.cs     (new)
├── FilterAnalyticsEndpoints.cs   (new)
```

---

## Appendix C: Recommended Implementation Order (Updated 2026-01-29)

Based on the gap analysis, here is the recommended implementation order:

> **⚠️ IMPORTANT**: All Frontend UI tasks (marked with 🎨) MUST use `/ui-ux-pro-max` skill per CLAUDE.md rules.

### Sprint 1: Admin UX (Unblock Attribute Management)
| Order | Task | Effort | Tool | Impact |
|-------|------|--------|------|--------|
| 1 | **Phase 9.1-9.2**: Add missing hooks to useProductAttributes.ts | S | Direct | Enables FE work |
| 2 | 🎨 **Phase 9.3-9.4**: Create AttributeInputFactory and input components | L | `/ui-ux-pro-max` | Core UI components |
| 3 | 🎨 **Phase 9.5-9.6**: Create ProductAttributesSection, integrate into ProductFormPage | M | `/ui-ux-pro-max` | Admin can set attributes |
| 4 | **Phase 9.7-9.9**: Category change handling, save flow, loading states | M | Direct | Complete UX |
| 5 | **Phase 9.10**: Localization (EN/VI) | S | Direct | i18n compliance |
| 6 | **Gap A.1-A.5**: Update ProductDto to include attributes | M | Direct (BE) | API completeness |

### Sprint 2: Admin Enhancements
| Order | Task | Effort | Tool | Impact |
|-------|------|--------|------|--------|
| 7 | 🎨 **Gap B.1-B.3**: Product card attribute badges | S | `/ui-ux-pro-max` | Better list view |
| 8 | **Phase 9.11**: Unit tests for attribute components | M | Direct | Quality |

### Sprint 3: Storefront Filtering
| Order | Task | Effort | Tool | Impact |
|-------|------|--------|------|--------|
| 9 | **Phase 6**: Filter API endpoints | L | Direct (BE) | Backend for filtering |
| 10 | 🎨 **Phase 8**: Storefront filter UI | L | `/ui-ux-pro-max` | Customer-facing filters |

### Sprint 4: Analytics (Optional)
| Order | Task | Effort | Tool | Impact |
|-------|------|--------|------|--------|
| 11 | **Phase 7**: Analytics events | M | Direct (BE) | Filter usage tracking |
| 12 | 🎨 **Gap C**: Admin attribute filtering | S | `/ui-ux-pro-max` | Admin convenience |

---

## Appendix D: Skill Usage Guide

| Task Type | Skill to Use | When |
|-----------|--------------|------|
| UI Components | `/ui-ux-pro-max` | Creating/modifying React components with visual elements |
| React Hooks | Direct implementation | Custom hooks, state management, API calls |
| Backend (C#) | Direct implementation | Commands, Queries, Handlers, DTOs |
| Localization | Direct implementation | Adding keys to EN/VI JSON files |
| Tests | Direct implementation | Unit/integration tests |

**Example Usage for Phase 9**:
```
User: "Build the ProductAttributesSection component"
Claude: Uses /ui-ux-pro-max skill → Gets production-ready React component with shadcn/ui
```

---

**Next Step:** Start with Phase 9 to unblock admin attribute management.

1. First, add hooks (9.1-9.2) directly
2. Then use `/ui-ux-pro-max` for components (9.3-9.5)
3. Finally, integrate and polish (9.6-9.11)
