# Attribute-Category Inheritance Pattern

**Created:** 2026-02-18
**Status:** Implemented

---

## Overview

The NOIR platform implements a flexible product attribute system where attributes are defined globally and then assigned to categories. Products inherit their applicable attributes from their category. This enables dynamic product specifications without schema changes.

---

## Entity Relationships

```
ProductAttribute (definition)
├── Code, Name, Type (13 types)
├── Behavior flags (IsFilterable, IsSearchable, IsRequired, IsVariantAttribute)
├── Display flags (ShowInProductCard, ShowInSpecifications)
├── Type config (Unit, Validation, MinValue, MaxValue, etc.)
├── IsGlobal (auto-assigned to ALL categories)
└── Values (1:N - for Select/MultiSelect types only)

CategoryAttribute (junction: Category <-> Attribute)
├── CategoryId (FK)
├── AttributeId (FK)
├── IsRequired (category-level override)
└── SortOrder (display order within category)

ProductAttributeAssignment (junction: Product <-> Attribute)
├── ProductId (FK)
├── AttributeId (FK)
├── VariantId (FK, nullable - variant-specific values)
├── Polymorphic value storage (one column per type)
└── DisplayValue (computed for search/filtering)
```

---

## Inheritance Flow

```
1. Admin creates ProductAttribute (e.g., "Screen Size", type: Decimal)
   ├── Defines global behavior (filterable, searchable, unit: "inch")
   └── Adds values if Select/MultiSelect type

2. Admin assigns Attribute to Category
   ├── CategoryAttribute link created
   ├── Can override: IsRequired (category may require what global doesn't)
   └── Can set: SortOrder (display order within this category)

3. Product created in Category
   ├── GetCategoryAttributeFormSchema returns applicable attributes
   ├── Form rendered with correct field types and constraints
   └── Values submitted via SetProductAttributeValue / BulkUpdateProductAttributes

4. Product attribute values stored
   └── ProductAttributeAssignment with typed value columns
```

---

## Three-Level Inheritance Model

### Level 1: Attribute Definition (ProductAttribute)

Global attribute definitions shared across categories.

| Property | Description |
|----------|-------------|
| `Code` | Machine name (lowercase, underscores) |
| `Name` | Human-readable label |
| `Type` | One of 13 AttributeType values |
| `IsRequired` | Default required status |
| `IsGlobal` | If true, auto-assigned to ALL categories |
| `SortOrder` | Default display order |

### Level 2: Category Assignment (CategoryAttribute)

Category-specific overrides and organization.

| Property | Description |
|----------|-------------|
| `CategoryId` | Which category |
| `AttributeId` | Which attribute |
| `IsRequired` | **Override**: Can be more strict than attribute default |
| `SortOrder` | **Override**: Category-specific display order |

**Merge Rule:** Attribute is required if **either** `ProductAttribute.IsRequired` OR `CategoryAttribute.IsRequired` is true. The category can make an optional attribute required, but cannot make a required attribute optional.

### Level 3: Product Values (ProductAttributeAssignment)

Actual values assigned to individual products.

| Property | Description |
|----------|-------------|
| `ProductId` | Which product |
| `AttributeId` | Which attribute |
| `VariantId` | Optional: variant-specific value |
| Value columns | Polymorphic storage (see below) |
| `DisplayValue` | Computed string for search/filtering |

---

## 13 Attribute Types

| Type | Value Column | Example |
|------|-------------|---------|
| `Select` | `AttributeValueId` (Guid) | Brand dropdown |
| `MultiSelect` | `AttributeValueIds` (JSON Guid array) | Features checklist |
| `Text` | `TextValue` | Model number |
| `TextArea` | `TextValue` | Description |
| `Number` | `NumberValue` | Battery (mAh) |
| `Decimal` | `NumberValue` | Screen size (6.7 inch) |
| `Boolean` | `BoolValue` | Waterproof (yes/no) |
| `Date` | `DateValue` | Release date |
| `DateTime` | `DateTimeValue` | Warranty expiry |
| `Color` | `ColorValue` (#hex) | Available colors |
| `Range` | `MinRangeValue` + `MaxRangeValue` | Price range |
| `Url` | `TextValue` | Documentation link |
| `File` | `FileUrl` | Specification PDF |

**Polymorphic Strategy:** Only one value column is used per assignment. `ClearAllValues()` resets all columns before setting a new value, ensuring data integrity.

---

## CQRS Commands

### Attribute Management

| Command | Purpose |
|---------|---------|
| `CreateProductAttributeCommand` | Create new attribute definition |
| `UpdateProductAttributeCommand` | Update attribute details, flags, config |
| `DeleteProductAttributeCommand` | Soft-delete attribute |
| `AddProductAttributeValueCommand` | Add predefined value (Select/MultiSelect) |
| `UpdateProductAttributeValueCommand` | Update value details |
| `RemoveProductAttributeValueCommand` | Remove a value |

### Category Assignment

| Command | Purpose |
|---------|---------|
| `AssignCategoryAttributeCommand` | Link attribute to category (with IsRequired, SortOrder overrides) |
| `UpdateCategoryAttributeCommand` | Update category-level overrides |
| `RemoveCategoryAttributeCommand` | Unlink attribute from category |

### Product Value Assignment

| Command | Purpose |
|---------|---------|
| `SetProductAttributeValueCommand` | Set a single attribute value on a product |
| `BulkUpdateProductAttributesCommand` | Set multiple attribute values at once |

### Form Schema Queries

| Query | Purpose |
|-------|---------|
| `GetCategoryAttributeFormSchemaQuery` | Get form fields for new product creation (no existing values) |
| `GetProductAttributeFormSchemaQuery` | Get form fields with current values for editing |
| `GetCategoryAttributesQuery` | List attributes assigned to a category |
| `GetProductAttributeAssignmentsQuery` | List attribute values for a product |

---

## Form Schema Resolution

When creating/editing a product, the system builds a dynamic form:

```csharp
// For new products (no existing values):
GetCategoryAttributeFormSchemaQuery(categoryId)

// For existing products (with current values):
GetProductAttributeFormSchemaQuery(productId)
```

**Resolution logic (GetCategoryAttributeFormSchemaQueryHandler):**

1. Load `CategoryAttribute` links for the category
2. Load actual `ProductAttribute` entities with values
3. For each attribute, merge settings:
   - `IsRequired = attribute.IsRequired || categoryAttribute.IsRequired`
   - `SortOrder = categoryAttribute.SortOrder ?? attribute.SortOrder`
4. Build `ProductAttributeFormFieldDto` with type-specific metadata
5. Order by category-specific sort order, then by name

**Result:** `CategoryAttributeFormSchemaDto` containing:
- Category ID and name
- List of `ProductAttributeFormFieldDto` with:
  - Field identity (id, code, name, type)
  - Constraints (required, min/max, maxLength, regex)
  - UX hints (unit, placeholder, helpText)
  - Predefined values (for Select/MultiSelect)
  - Current value (for edit forms)

---

## Global Attributes

Attributes with `IsGlobal = true` are automatically applicable to all categories without explicit `CategoryAttribute` links. Used for universal product properties like weight, dimensions, etc.

---

## Domain Events

| Event | Trigger |
|-------|---------|
| `ProductAttributeCreatedEvent` | New attribute created |
| `ProductAttributeUpdatedEvent` | Attribute details changed |
| `ProductAttributeDeletedEvent` | Attribute soft-deleted |
| `ProductAttributeValueAddedEvent` | Value added to Select/MultiSelect |
| `ProductAttributeValueRemovedEvent` | Value removed |

---

## Related Patterns

- **[Repository & Specification](repository-specification.md)** - All queries use specifications with `TagWith()`
- **[Hierarchical Audit Logging](hierarchical-audit-logging.md)** - All commands implement `IAuditableCommand`
- **[Entity Configuration](entity-configuration.md)** - EF Core configuration for polymorphic value storage
- **[Inventory Receipt Pattern](inventory-receipt-pattern.md)** - Similar aggregate root pattern
