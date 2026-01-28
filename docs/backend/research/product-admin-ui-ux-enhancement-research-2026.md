# Product Admin UI/UX Enhancement Research Report

**Date:** 2026-01-28
**Focus:** Product Form, Images, Descriptions, Variant Management
**Status:** Complete

---

## Executive Summary

This research analyzes best practices for e-commerce product management admin panels, comparing NOIR's current implementation against industry leaders (Shopify, WooCommerce, Medusa, Saleor). The report identifies specific UI/UX enhancements needed for a robust, business-ready product management experience.

**Key Areas Covered:**
1. Product Image Management (Gallery, Featured, Drag-Drop)
2. Product Descriptions (Short + Long with Rich Editor)
3. Variant Management (Stock, Pricing, Options Matrix)
4. Overall Admin Form Best Practices

---

## Table of Contents

1. [Current NOIR Implementation Analysis](#1-current-noir-implementation-analysis)
2. [Image Management Enhancement](#2-image-management-enhancement)
3. [Description Fields Enhancement](#3-description-fields-enhancement)
4. [Variant Management Enhancement](#4-variant-management-enhancement)
5. [Industry Comparison Matrix](#5-industry-comparison-matrix)
6. [Prioritized Enhancement Plan](#6-prioritized-enhancement-plan)
7. [Sources](#7-sources)

---

## 1. Current NOIR Implementation Analysis

### 1.1 Current Product Form Fields

| Field | Status | Notes |
|-------|--------|-------|
| Name | ✅ Complete | Max 200 chars, required |
| Slug | ✅ Complete | Auto-generated, regex validated |
| Description | ⚠️ Partial | Plain textarea only, no rich editor |
| DescriptionHtml | ❌ Missing | Field exists in schema but not used |
| Short Description | ❌ Missing | Not implemented |
| Base Price | ✅ Complete | VND currency (hardcoded) |
| SKU | ✅ Complete | Optional |
| Barcode | ✅ Complete | Optional |
| Brand | ✅ Complete | Optional |
| Category | ✅ Complete | Dropdown selector |
| Weight | ✅ Complete | Optional, for shipping |
| Track Inventory | ✅ Complete | Toggle switch |
| Meta Title | ✅ Complete | 60 char limit with counter |
| Meta Description | ✅ Complete | 160 char limit with counter |

### 1.2 Current Image Management

| Feature | Status | Notes |
|---------|--------|-------|
| Image Gallery | ✅ Basic | Grid display in sidebar |
| Primary/Featured Image | ✅ Complete | Star badge, click to set |
| URL-based Upload | ✅ Complete | Text input for URL |
| File Upload | ❌ Missing | No file picker/drag-drop |
| Alt Text | ⚠️ Partial | Stored but not editable in UI |
| Drag-Drop Reorder | ❌ Missing | `sortOrder` exists, no UI |
| Bulk Upload | ❌ Missing | One at a time only |
| Image Preview | ❌ Missing | No preview before adding |

### 1.3 Current Variant Management

| Feature | Status | Notes |
|---------|--------|-------|
| Add Variant | ✅ Complete | Inline form |
| Variant Name | ✅ Complete | Required |
| Variant Price | ✅ Complete | Can differ from base |
| Variant SKU | ✅ Complete | Optional |
| Compare-at Price | ✅ Complete | For sale display |
| Stock Quantity | ✅ Complete | Min 0 |
| Edit Variant | ❌ Missing | State exists but no UI |
| Delete Variant | ✅ Complete | With confirmation |
| Variant Options | ❌ Missing | `OptionsJson` unused |
| Variant Images | ❌ Missing | No image per variant |
| Option Generator | ❌ Missing | No Color × Size matrix |
| Stock History | ❌ Missing | No audit trail |

---

## 2. Image Management Enhancement

### 2.1 Industry Best Practices

Based on [Retoolers](https://www.retoolers.io/use-cases/drag-and-drop-image-management-for-e-commerce), [Adobe Commerce](https://experienceleague.adobe.com/en/docs/commerce-admin/catalog/products/digital-assets/product-image), and [Omi.so](https://omi.so/resources/blog/product-images-best-practices):

#### Required Features

| Feature | Description | Priority |
|---------|-------------|----------|
| **Drag-Drop Upload** | Drop zone for file uploads | HIGH |
| **Drag-Drop Reorder** | Reorder gallery by dragging | HIGH |
| **Primary Badge** | Visual indicator on featured image | ✅ Done |
| **Alt Text Editor** | Inline text input per image | HIGH |
| **Bulk Upload** | Upload multiple images at once | HIGH |
| **Image Preview** | Show preview before confirming | MEDIUM |
| **Image Roles** | Main, Thumbnail, Gallery, Hover | MEDIUM |
| **Lightbox Viewer** | Full-size view in modal | MEDIUM |
| **Delete Confirmation** | Prevent accidental deletion | ✅ Done |
| **CDN Integration** | Auto-upload to CDN | LOW |

#### Recommended UI Pattern

```
┌─────────────────────────────────────────────────────┐
│  Images                                    [Upload] │
├─────────────────────────────────────────────────────┤
│  ┌──────────────────────────────────────────────┐   │
│  │                                              │   │
│  │     Drag images here or click to upload     │   │
│  │              (Max 10MB each)                 │   │
│  │                                              │   │
│  └──────────────────────────────────────────────┘   │
│                                                     │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐                   │
│  │ ★   │ │     │ │     │ │  +  │  ← Drag to reorder│
│  │ Img │ │ Img │ │ Img │ │ Add │                   │
│  │  1  │ │  2  │ │  3  │ │     │                   │
│  └─────┘ └─────┘ └─────┘ └─────┘                   │
│   Primary                                          │
│                                                     │
│  Alt text: [Product front view____________] [Save] │
└─────────────────────────────────────────────────────┘
```

#### React Implementation

Recommended library: [react-dropzone](https://github.com/react-dropzone/react-dropzone)

```typescript
// Recommended structure
<ImageGalleryManager
  images={images}
  onUpload={handleUpload}          // File upload
  onReorder={handleReorder}        // Drag-drop sort
  onSetPrimary={handleSetPrimary}  // Star click
  onDelete={handleDelete}          // Trash click
  onAltTextChange={handleAltText}  // Inline edit
  maxSize={10 * 1024 * 1024}       // 10MB limit
  accept="image/*"
/>
```

### 2.2 NOIR Enhancement Recommendations

#### ENHANCEMENT-IMG-01: Add Drag-Drop Upload Zone

**Current:** URL text input only
**Proposed:** Full drag-drop zone with react-dropzone

```typescript
// New component: ImageUploadZone.tsx
import { useDropzone } from 'react-dropzone';

const ImageUploadZone = ({ onUpload, maxFiles = 10 }) => {
  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    accept: { 'image/*': ['.png', '.jpg', '.jpeg', '.webp'] },
    maxSize: 10 * 1024 * 1024, // 10MB
    maxFiles,
    onDrop: (acceptedFiles) => {
      // Upload to CDN/storage
      // Return URLs
      onUpload(acceptedFiles);
    },
  });

  return (
    <div {...getRootProps()} className={cn(
      "border-2 border-dashed rounded-lg p-8 text-center",
      isDragActive && "border-primary bg-primary/5"
    )}>
      <input {...getInputProps()} />
      <Upload className="w-8 h-8 mx-auto mb-2 text-muted-foreground" />
      <p>Drag images here or click to upload</p>
    </div>
  );
};
```

#### ENHANCEMENT-IMG-02: Add Drag-Drop Reorder

**Current:** No reordering capability
**Proposed:** @dnd-kit/sortable for smooth reordering

```typescript
// Using dnd-kit for reordering
import { DndContext, closestCenter } from '@dnd-kit/core';
import { SortableContext, rectSortingStrategy } from '@dnd-kit/sortable';

<DndContext onDragEnd={handleDragEnd}>
  <SortableContext items={images} strategy={rectSortingStrategy}>
    {images.map((img) => (
      <SortableImage key={img.id} image={img} />
    ))}
  </SortableContext>
</DndContext>
```

#### ENHANCEMENT-IMG-03: Add Alt Text Editor

**Current:** Alt text stored but not editable
**Proposed:** Inline edit on hover/click

---

## 3. Description Fields Enhancement

### 3.1 Industry Best Practices

Based on [Plytix](https://www.plytix.com/blog/long-or-short-product-description), [AirOps](https://www.airops.com/blog/how-long-should-a-product-description-be), and [Contentsquare](https://contentsquare.com/guides/ecommerce-cro/product-description/):

#### Two-Description Pattern

| Field | Length | Purpose | Format |
|-------|--------|---------|--------|
| **Short Description** | 100-200 chars | Summary for listings, SEO snippets, marketplaces | Plain text |
| **Long Description** | 300-600 words | Full product details, features, benefits | Rich HTML |

#### Why Both Descriptions?

1. **SEO Benefits:** Short description populates meta descriptions and Google snippets
2. **Marketplace Feeds:** Amazon, Google Products, etc. require short summaries
3. **Quick Scanning:** Customers scan short description in listings/cards
4. **Full Details:** Long description for engaged customers on product page

### 3.2 NOIR Enhancement Recommendations

#### ENHANCEMENT-DESC-01: Add Short Description Field

**Backend:** Already has `Description` field (plain text)
**Frontend:** Rename and add character limit

```typescript
// Short Description (new UI)
<FormField
  name="shortDescription"
  render={({ field }) => (
    <FormItem>
      <FormLabel>
        Short Description
        <span className="text-muted-foreground ml-2">
          ({field.value?.length || 0}/200)
        </span>
      </FormLabel>
      <FormControl>
        <Textarea
          {...field}
          maxLength={200}
          rows={2}
          placeholder="Brief summary for listings and SEO..."
        />
      </FormControl>
      <FormDescription>
        Used in product cards, search results, and marketplace feeds
      </FormDescription>
    </FormItem>
  )}
/>
```

#### ENHANCEMENT-DESC-02: Add Rich Text Editor for Long Description

**Current:** `descriptionHtml` field exists but unused
**Proposed:** TinyMCE v6 integration (already available)

```typescript
// Long Description with TinyMCE
import { Editor } from '@tinymce/tinymce-react';

<FormField
  name="descriptionHtml"
  render={({ field }) => (
    <FormItem>
      <FormLabel>Full Description</FormLabel>
      <FormControl>
        <Editor
          apiKey={TINYMCE_API_KEY}
          value={field.value}
          onEditorChange={field.onChange}
          init={{
            height: 400,
            menubar: false,
            plugins: [
              'advlist', 'autolink', 'lists', 'link', 'image',
              'charmap', 'preview', 'anchor', 'searchreplace',
              'visualblocks', 'code', 'fullscreen', 'insertdatetime',
              'media', 'table', 'code', 'help', 'wordcount'
            ],
            toolbar: 'undo redo | blocks | bold italic forecolor | ' +
              'alignleft aligncenter alignright alignjustify | ' +
              'bullist numlist outdent indent | removeformat | help',
            content_style: 'body { font-family: -apple-system, sans-serif; font-size: 14px }'
          }}
        />
      </FormControl>
      <FormDescription>
        Detailed product information with formatting
      </FormDescription>
    </FormItem>
  )}
/>
```

#### Field Mapping Update

| Current Field | New Field Name | Purpose |
|--------------|----------------|---------|
| `description` | `shortDescription` | Plain text summary (200 chars) |
| `descriptionHtml` | `description` | Rich HTML content |

**Backend Change Required:**
- Rename `Description` → `ShortDescription`
- Rename `DescriptionHtml` → `Description`
- OR keep both and add `ShortDescription` as new field

---

## 4. Variant Management Enhancement

### 4.1 Industry Best Practices

Based on [Shopify](https://www.shopify.com/blog/product-variant), [Medusa](https://docs.medusajs.com/user-guide/products/variants), [Saleor](https://docs.saleor.io/developer/products/api), and [NN/g](https://www.nngroup.com/articles/products-with-multiple-variants/):

#### Variant Data Model (Standard)

```
Product
├── Options (e.g., Size, Color)
│   ├── Option 1: Size [S, M, L, XL]
│   └── Option 2: Color [Red, Blue, Black]
│
└── Variants (generated from options)
    ├── S-Red   (SKU: TSHIRT-S-RED, Price: $20, Stock: 50)
    ├── S-Blue  (SKU: TSHIRT-S-BLUE, Price: $20, Stock: 30)
    ├── M-Red   (SKU: TSHIRT-M-RED, Price: $22, Stock: 45)
    └── ... (all combinations)
```

#### Key UI Patterns

| Pattern | Description | When to Use |
|---------|-------------|-------------|
| **Option-Based Generator** | Define options → Auto-generate variants | Products with many combinations |
| **Manual Entry** | Add variants one by one | Simple products, few variants |
| **Matrix View** | Grid showing all option combinations | B2B, bulk ordering |
| **Bulk Edit** | Edit multiple variants at once | Large catalogs |

### 4.2 Recommended Variant UI

#### Pattern 1: Option Definition → Variant Generation

```
┌─────────────────────────────────────────────────────────────┐
│  Product Options                                            │
├─────────────────────────────────────────────────────────────┤
│  Option 1: [Size    ▼]  Values: [S] [M] [L] [XL] [+Add]    │
│  Option 2: [Color   ▼]  Values: [Red] [Blue] [Black] [+Add] │
│  [+ Add Option]                                             │
│                                                             │
│  [Generate Variants] ← Creates all combinations (12 total)  │
└─────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────┐
│  Variants (12)                              [Bulk Edit]     │
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────┐   │
│  │ □ S-Red      SKU: [_______]  Price: [20.00] Stock: [50] │
│  │ □ S-Blue     SKU: [_______]  Price: [20.00] Stock: [30] │
│  │ □ S-Black    SKU: [_______]  Price: [20.00] Stock: [25] │
│  │ □ M-Red      SKU: [_______]  Price: [22.00] Stock: [45] │
│  │ ...                                                      │
│  └─────────────────────────────────────────────────────┘   │
│                                                             │
│  Selected: 0  [Delete Selected] [Set Prices] [Set Stock]   │
└─────────────────────────────────────────────────────────────┘
```

#### Pattern 2: Variant Card View (Current + Enhanced)

```
┌─────────────────────────────────────────────────────────────┐
│  Variants                                    [+ Add Variant]│
├─────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────┐   │
│  │  [🔵] Blue - Large                           [Edit] │   │
│  │  SKU: SHIRT-BLU-L  │  Stock: 45  │  ₫250,000       │   │
│  │  Compare: ₫300,000 (-17%)                    [🗑️]  │   │
│  └─────────────────────────────────────────────────────┘   │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  [🔴] Red - Medium                           [Edit] │   │
│  │  SKU: SHIRT-RED-M  │  Stock: 0 ⚠️  │  ₫250,000     │   │
│  │  Out of Stock                                [🗑️]  │   │
│  └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

### 4.3 NOIR Enhancement Recommendations

#### ENHANCEMENT-VAR-01: Add Variant Edit UI

**Current:** `editingVariantId` state exists but no UI
**Proposed:** Inline edit mode for each variant

```typescript
// Variant edit mode
const [editingVariantId, setEditingVariantId] = useState<string | null>(null);

// In variant list render:
{variants.map((variant) => (
  editingVariantId === variant.id ? (
    <VariantEditForm
      variant={variant}
      onSave={handleUpdateVariant}
      onCancel={() => setEditingVariantId(null)}
    />
  ) : (
    <VariantCard
      variant={variant}
      onEdit={() => setEditingVariantId(variant.id)}
      onDelete={() => handleDeleteVariant(variant.id)}
    />
  )
))}
```

#### ENHANCEMENT-VAR-02: Add Variant Options/Attributes

**Current:** `OptionsJson` field exists but unused
**Proposed:** Option selector in variant form

```typescript
// Variant form with options
interface VariantFormData {
  name: string;
  price: number;
  sku?: string;
  stockQuantity: number;
  compareAtPrice?: number;
  options: Record<string, string>; // { "Color": "Red", "Size": "M" }
}

// UI for options
<div className="grid gap-2">
  {productOptions.map((option) => (
    <FormField
      key={option.name}
      name={`options.${option.name}`}
      render={({ field }) => (
        <FormItem>
          <FormLabel>{option.name}</FormLabel>
          <Select onValueChange={field.onChange} value={field.value}>
            <SelectTrigger>
              <SelectValue placeholder={`Select ${option.name}`} />
            </SelectTrigger>
            <SelectContent>
              {option.values.map((value) => (
                <SelectItem key={value} value={value}>
                  {value}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </FormItem>
      )}
    />
  ))}
</div>
```

#### ENHANCEMENT-VAR-03: Add Stock Management Per Variant

**Current:** Basic stock quantity only
**Proposed:** Enhanced stock management UI

```typescript
// Stock management section in variant
<div className="space-y-2">
  <Label>Inventory</Label>
  <div className="flex items-center gap-4">
    <div className="flex-1">
      <Input
        type="number"
        value={stockQuantity}
        onChange={handleStockChange}
        min={0}
      />
    </div>
    <Button variant="outline" size="sm" onClick={handleAdjustStock}>
      <Plus className="w-4 h-4" /> Adjust
    </Button>
  </div>

  {/* Stock status indicator */}
  <div className="flex items-center gap-2 text-sm">
    {stockQuantity === 0 && (
      <Badge variant="destructive">Out of Stock</Badge>
    )}
    {stockQuantity > 0 && stockQuantity <= 10 && (
      <Badge variant="warning">Low Stock</Badge>
    )}
    {stockQuantity > 10 && (
      <Badge variant="success">In Stock</Badge>
    )}
  </div>

  {/* Low stock threshold setting */}
  <div className="text-xs text-muted-foreground">
    Alert when stock falls below:
    <Input type="number" className="w-16 ml-2 inline" defaultValue={10} />
  </div>
</div>
```

#### ENHANCEMENT-VAR-04: Add Variant Image Association

**Current:** No variant-specific images
**Proposed:** Associate images with variants

```typescript
// Variant image selector
<FormField
  name="imageId"
  render={({ field }) => (
    <FormItem>
      <FormLabel>Variant Image</FormLabel>
      <div className="flex gap-2">
        {productImages.map((img) => (
          <button
            key={img.id}
            type="button"
            onClick={() => field.onChange(img.id)}
            className={cn(
              "w-16 h-16 rounded border-2",
              field.value === img.id && "border-primary"
            )}
          >
            <img src={img.url} alt={img.altText} className="w-full h-full object-cover" />
          </button>
        ))}
      </div>
      <FormDescription>
        Image shown when this variant is selected
      </FormDescription>
    </FormItem>
  )}
/>
```

---

## 5. Industry Comparison Matrix

### Feature Comparison: NOIR vs. Industry Leaders

| Feature | NOIR | Shopify | WooCommerce | Medusa | Saleor |
|---------|------|---------|-------------|--------|--------|
| **Product Fields** |
| Short Description | ❌ | ✅ | ✅ | ✅ | ✅ |
| Rich Text Editor | ❌ | ✅ | ✅ | ✅ | ✅ |
| SEO Fields | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Image Management** |
| Drag-Drop Upload | ❌ | ✅ | ✅ | ✅ | ✅ |
| Drag-Drop Reorder | ❌ | ✅ | ✅ | ✅ | ✅ |
| Primary Image | ✅ | ✅ | ✅ | ✅ | ✅ |
| Alt Text Editor | ❌ | ✅ | ✅ | ✅ | ✅ |
| Bulk Upload | ❌ | ✅ | ✅ | ✅ | ✅ |
| **Variant Management** |
| Add Variant | ✅ | ✅ | ✅ | ✅ | ✅ |
| Edit Variant | ❌ | ✅ | ✅ | ✅ | ✅ |
| Variant Options | ❌ | ✅ | ✅ | ✅ | ✅ |
| Option Generator | ❌ | ✅ | ✅ | ❌ | ✅ |
| Variant Images | ❌ | ✅ | ✅ | ✅ | ✅ |
| Stock per Variant | ✅ | ✅ | ✅ | ✅ | ✅ |
| Bulk Edit Variants | ❌ | ✅ | ✅ | ✅ | ✅ |
| **Pricing** |
| Base Price | ✅ | ✅ | ✅ | ✅ | ✅ |
| Compare-at Price | ✅ | ✅ | ✅ | ✅ | ✅ |
| Multi-Currency | ❌ | ✅ | ✅ | ✅ | ✅ |
| Price per Variant | ✅ | ✅ | ✅ | ✅ | ✅ |
| **UI/UX** |
| Glassmorphism | ✅ | ❌ | ❌ | ❌ | ❌ |
| Animations | ✅ | ⚠️ | ❌ | ⚠️ | ⚠️ |
| Permission-based | ✅ | ✅ | ✅ | ✅ | ✅ |
| Localization | ✅ | ✅ | ✅ | ✅ | ✅ |

**Legend:** ✅ Complete | ⚠️ Partial | ❌ Missing

---

## 6. Prioritized Enhancement Plan

### Phase 1: Critical Enhancements (Sprint 1-2)

| ID | Enhancement | Effort | Impact |
|----|-------------|--------|--------|
| **DESC-02** | Rich Text Editor (TinyMCE) for Long Description | Medium | HIGH |
| **VAR-01** | Add Variant Edit UI | Low | HIGH |
| **IMG-03** | Add Alt Text Editor | Low | MEDIUM |
| **DESC-01** | Add Short Description Field | Low | HIGH |

**Deliverables:**
1. TinyMCE integration for `descriptionHtml`
2. Inline variant editing
3. Alt text input per image
4. Short description field (200 char)

### Phase 2: Image Enhancements (Sprint 3)

| ID | Enhancement | Effort | Impact |
|----|-------------|--------|--------|
| **IMG-01** | Drag-Drop Upload Zone | Medium | HIGH |
| **IMG-02** | Drag-Drop Reorder Gallery | Medium | HIGH |
| **IMG-04** | Bulk Image Upload | Medium | MEDIUM |

**Deliverables:**
1. react-dropzone integration
2. @dnd-kit/sortable for reordering
3. Multi-file upload support

### Phase 3: Variant Options (Sprint 4)

| ID | Enhancement | Effort | Impact |
|----|-------------|--------|--------|
| **VAR-02** | Add Variant Options/Attributes | High | HIGH |
| **VAR-04** | Variant Image Association | Medium | MEDIUM |
| **VAR-05** | Stock Adjustment History | Medium | MEDIUM |

**Deliverables:**
1. Product Options entity and UI
2. Option-to-variant mapping
3. Variant-image linking
4. Stock change audit log

### Phase 4: Advanced Features (Sprint 5+)

| ID | Enhancement | Effort | Impact |
|----|-------------|--------|--------|
| **VAR-03** | Enhanced Stock Management | Medium | MEDIUM |
| **VAR-06** | Option-based Variant Generator | High | HIGH |
| **VAR-07** | Bulk Variant Edit | High | MEDIUM |
| **PRICE-01** | Multi-Currency Support | High | LOW |

---

## 7. Implementation Specifications

### 7.1 Database Changes Required

```sql
-- New table: ProductOptions
CREATE TABLE ProductOptions (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ProductId UNIQUEIDENTIFIER FOREIGN KEY REFERENCES Products(Id),
    Name NVARCHAR(100) NOT NULL,      -- "Color", "Size"
    Position INT DEFAULT 0,
    TenantId NVARCHAR(128)
);

-- New table: ProductOptionValues
CREATE TABLE ProductOptionValues (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    ProductOptionId UNIQUEIDENTIFIER FOREIGN KEY,
    Value NVARCHAR(100) NOT NULL,     -- "Red", "Blue", "S", "M"
    Position INT DEFAULT 0
);

-- Update ProductVariants: add ImageId
ALTER TABLE ProductVariants ADD ImageId UNIQUEIDENTIFIER NULL
    FOREIGN KEY REFERENCES ProductImages(Id);

-- Update Products: rename/add description fields
ALTER TABLE Products ADD ShortDescription NVARCHAR(300) NULL;
-- Keep DescriptionHtml for rich content
```

### 7.2 Frontend Component Structure

```
src/components/products/
├── ImageGalleryManager/
│   ├── ImageGalleryManager.tsx      # Main component
│   ├── ImageUploadZone.tsx          # Drag-drop upload
│   ├── SortableImageGrid.tsx        # Reorderable grid
│   ├── ImageCard.tsx                # Single image with actions
│   └── AltTextEditor.tsx            # Inline alt text edit
│
├── VariantManager/
│   ├── VariantManager.tsx           # Main component
│   ├── VariantList.tsx              # List of variants
│   ├── VariantCard.tsx              # Single variant display
│   ├── VariantForm.tsx              # Add/edit form
│   ├── VariantOptionsSelector.tsx   # Option dropdowns
│   └── StockManager.tsx             # Stock adjustment UI
│
├── ProductDescription/
│   ├── ShortDescriptionField.tsx    # Plain text with counter
│   └── RichDescriptionEditor.tsx    # TinyMCE wrapper
│
└── ProductOptions/
    ├── OptionsManager.tsx           # Option definition UI
    ├── OptionRow.tsx                # Single option with values
    └── VariantGenerator.tsx         # Generate from options
```

### 7.3 New API Endpoints

```
# Product Options
GET    /api/products/{id}/options
POST   /api/products/{id}/options
PUT    /api/products/{id}/options/{optionId}
DELETE /api/products/{id}/options/{optionId}

# Option Values
POST   /api/products/{id}/options/{optionId}/values
DELETE /api/products/{id}/options/{optionId}/values/{valueId}

# Variant Generation
POST   /api/products/{id}/variants/generate  # From options matrix

# Stock Adjustments
POST   /api/products/{productId}/variants/{variantId}/stock/adjust
GET    /api/products/{productId}/variants/{variantId}/stock/history
```

---

## 7. Sources

### Image Management
- [Retoolers - Drag-and-Drop Image Management](https://www.retoolers.io/use-cases/drag-and-drop-image-management-for-e-commerce)
- [Adobe Commerce - Product Images](https://experienceleague.adobe.com/en/docs/commerce-admin/catalog/products/digital-assets/product-image)
- [Omi.so - Product Images Best Practices](https://omi.so/resources/blog/product-images-best-practices)
- [react-dropzone - GitHub](https://github.com/react-dropzone/react-dropzone)
- [LogRocket - React Dropzone Tutorial](https://blog.logrocket.com/create-drag-and-drop-component-react-dropzone/)

### Product Descriptions
- [Plytix - Short vs Long Description](https://www.plytix.com/blog/long-or-short-product-description)
- [AirOps - Product Description Length](https://www.airops.com/blog/how-long-should-a-product-description-be)
- [Contentsquare - Product Description SEO](https://contentsquare.com/guides/ecommerce-cro/product-description/)
- [Shopify - SEO Product Descriptions](https://www.shopify.com/enterprise/blog/seo-product-descriptions)

### Variant Management
- [Shopify - Product Variants](https://www.shopify.com/blog/product-variant)
- [Medusa - Manage Variants](https://docs.medusajs.com/user-guide/products/variants)
- [Saleor - Product Structure](https://saleor-fork.readthedocs.io/en/latest/architecture/products.html)
- [NN/g - Products with Multiple Variants](https://www.nngroup.com/articles/products-with-multiple-variants/)
- [SparkLayer - B2B Product Pages](https://www.sparklayer.io/blog/2024/11/06/b2b-product-pages-ui/)

### Rich Text Editors
- [Liveblocks - Editor Comparison 2025](https://liveblocks.io/blog/which-rich-text-editor-framework-should-you-choose-in-2025)
- [Velt - React Rich Text Editors](https://velt.dev/blog/best-javascript-rich-text-editors-react)

### E-Commerce Admin Panels
- [Medusa UI Documentation](https://docs.medusajs.com/ui)
- [Saleor Dashboard Releases](https://github.com/saleor/saleor-dashboard/releases)
- [WooCommerce Product Data](https://barn2.com/blog/woocommerce-product-data/)

---

## Appendix A: Component Library Requirements

| Package | Purpose | Install |
|---------|---------|---------|
| `react-dropzone` | Drag-drop file upload | `npm i react-dropzone` |
| `@dnd-kit/core` | Drag-drop reordering | `npm i @dnd-kit/core @dnd-kit/sortable` |
| `@tinymce/tinymce-react` | Rich text editor | Already installed |

---

**Report Generated:** 2026-01-28
**Next Steps:** Review with team, prioritize Phase 1 items, begin implementation
