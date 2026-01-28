# Product Management Enhancement - Implementation Workflow

**Generated:** 2026-01-28
**Based On:** product-admin-ui-ux-enhancement-research-2026.md
**Skills Required:** `/ui-ux-pro-max` (Frontend), C#/.NET (Backend)

---

## Executive Summary

This workflow implements the Product Management enhancements identified in the research report. The implementation is divided into 4 phases, with each phase containing backend (C#) and frontend (React/TypeScript) tasks.

**Total Estimated Tasks:** 42 tasks across 4 phases
**Dependencies:** Each phase can be implemented independently, but Phase 3 depends on Phase 1 completion.

---

## Phase Overview

| Phase | Focus | Backend Tasks | Frontend Tasks | Priority |
|-------|-------|---------------|----------------|----------|
| **Phase 1** | Critical Enhancements | 4 | 6 | **P1 - Critical** |
| **Phase 2** | Image Management | 2 | 5 | **P1 - High** |
| **Phase 3** | Variant Management | 6 | 6 | **P2 - Medium** |
| **Phase 4** | Advanced Features | 4 | 5 | **P3 - Low** |

---

## Phase 1: Critical Enhancements

### Overview
- Add Short Description field
- Integrate TinyMCE Rich Text Editor for Long Description
- Enable Variant Edit UI
- Add Alt Text Editor for Images

### 1.1 Backend Tasks (C#)

#### Task 1.1.1: Add ShortDescription Field to Product Entity
**File:** `src/NOIR.Domain/Entities/Product/Product.cs`

```csharp
// Add property
public string? ShortDescription { get; private set; }

// Add domain method
public void UpdateShortDescription(string? shortDescription)
{
    ShortDescription = shortDescription?.Trim();
}
```

**Validation:** Max 300 characters

#### Task 1.1.2: Update Product Entity Configuration
**File:** `src/NOIR.Infrastructure/Persistence/Configurations/ProductConfiguration.cs`

```csharp
builder.Property(e => e.ShortDescription)
    .HasMaxLength(300);
```

#### Task 1.1.3: Create Database Migration
```bash
dotnet ef migrations add AddProductShortDescription \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext \
  --output-dir Migrations/App
```

#### Task 1.1.4: Update Commands and DTOs
**Files to update:**
- `CreateProductCommand.cs` - Add ShortDescription property
- `UpdateProductCommand.cs` - Add ShortDescription property
- `ProductDto.cs` - Add ShortDescription property
- `ProductMapper.cs` - Map ShortDescription
- `CreateProductCommandValidator.cs` - Add validation (max 300 chars)
- `UpdateProductCommandValidator.cs` - Add validation (max 300 chars)

### 1.2 Frontend Tasks (React/TypeScript)

#### Task 1.2.1: Add Short Description Field to Product Form
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/pages/portal/ecommerce/products/ProductFormPage.tsx`

**Requirements:**
- Add `shortDescription` to Zod schema (max 300 chars)
- Add Textarea field with character counter
- Position after Name/Slug, before full description
- Placeholder: "Brief summary for product listings and SEO..."
- Helper text: "Used in product cards, search results, and marketplace feeds"

#### Task 1.2.2: Integrate TinyMCE for Rich Description
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/pages/portal/ecommerce/products/ProductFormPage.tsx`

**Requirements:**
- Replace plain textarea for `descriptionHtml` with TinyMCE Editor
- Configure toolbar: bold, italic, lists, links, images, headings
- Height: 400px minimum
- Enable paste from Word/Google Docs cleanup

**Component:**
```typescript
// New component: RichTextEditor.tsx
import { Editor } from '@tinymce/tinymce-react';

interface RichTextEditorProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}
```

#### Task 1.2.3: Create RichTextEditor Component
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/components/products/RichTextEditor.tsx`

**Requirements:**
- Wrapper around TinyMCE with project styling
- Dark mode support
- Glassmorphism styling for toolbar
- Loading state while editor initializes

#### Task 1.2.4: Enable Variant Edit UI
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/pages/portal/ecommerce/products/ProductFormPage.tsx`

**Requirements:**
- Wire up existing `editingVariantId` state to show edit form
- Convert VariantCard to VariantEditableCard component
- Add "Edit" button to each variant row
- Show inline form when editing (same fields as Add form)
- Save/Cancel buttons for edit mode

#### Task 1.2.5: Add Alt Text Editor to Image Cards
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/pages/portal/ecommerce/products/ProductFormPage.tsx`

**Requirements:**
- Add inline text input below each image for alt text
- Show on hover or always visible (small text)
- Auto-save on blur
- Placeholder: "Describe this image..."

#### Task 1.2.6: Update API Types and Services
**File:** `src/NOIR.Web/frontend/src/types/product.ts`

**Requirements:**
- Add `shortDescription` to Product interface
- Add `shortDescription` to CreateProductRequest
- Add `shortDescription` to UpdateProductRequest

### 1.3 Phase 1 Validation Checklist

- [ ] ShortDescription field appears in product form
- [ ] Character counter shows (0/300)
- [ ] TinyMCE editor loads for full description
- [ ] Rich text (bold, lists, links) saves correctly
- [ ] Variant Edit button shows on each variant
- [ ] Clicking Edit shows inline form with current values
- [ ] Save variant updates correctly
- [ ] Alt text input visible on image cards
- [ ] Alt text saves on blur
- [ ] All localization keys added (EN + VI)
- [ ] All tests pass: `dotnet test src/NOIR.sln`

---

## Phase 2: Image Management

### Overview
- Add Drag-Drop Upload Zone
- Enable Drag-Drop Reorder for Gallery
- Bulk Image Upload Support
- Image Preview Before Adding

### 2.1 Backend Tasks (C#)

#### Task 2.1.1: Add Image Upload Endpoint
**File:** `src/NOIR.Web/Endpoints/ProductEndpoints.cs`

**Requirements:**
- POST `/api/products/{id}/images/upload`
- Accept multipart/form-data
- Upload to configured storage (Azure Blob/S3/local)
- Return image URL after upload

#### Task 2.1.2: Add Bulk Image Reorder Endpoint
**File:** `src/NOIR.Web/Endpoints/ProductEndpoints.cs`

**Requirements:**
- PUT `/api/products/{id}/images/reorder`
- Accept array of `{ imageId, sortOrder }`
- Update all image sort orders in single transaction

**Command:**
```csharp
public record ReorderProductImagesCommand(
    Guid ProductId,
    List<ImageSortOrderDto> ImageOrders
) : IRequest<Result>;

public record ImageSortOrderDto(Guid ImageId, int SortOrder);
```

### 2.2 Frontend Tasks (React/TypeScript)

#### Task 2.2.1: Install Required Packages
```bash
cd src/NOIR.Web/frontend
npm install react-dropzone @dnd-kit/core @dnd-kit/sortable @dnd-kit/utilities
```

#### Task 2.2.2: Create ImageUploadZone Component
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/components/products/ImageUploadZone.tsx`

**Requirements:**
- Drag-drop zone using react-dropzone
- Accept: image/png, image/jpeg, image/webp
- Max size: 10MB per file
- Max files: 10
- Visual feedback on drag over
- Loading state during upload
- Error handling for failed uploads

#### Task 2.2.3: Create SortableImageGallery Component
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/components/products/SortableImageGallery.tsx`

**Requirements:**
- Use @dnd-kit/sortable for drag-drop reordering
- Grid layout matching current design
- Smooth animations during drag
- Primary badge on first image
- Hover actions: Set Primary, Delete, Edit Alt Text

#### Task 2.2.4: Create SortableImageCard Component
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/components/products/SortableImageCard.tsx`

**Requirements:**
- Individual draggable image card
- Drag handle (grip icon)
- Overlay actions on hover
- Alt text inline editor
- Delete confirmation

#### Task 2.2.5: Integrate New Components into ProductFormPage
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/pages/portal/ecommerce/products/ProductFormPage.tsx`

**Requirements:**
- Replace current image section with new components
- Maintain current functionality (primary, delete)
- Add reorder API call on drag end
- Add bulk upload support

### 2.3 Phase 2 Validation Checklist

- [ ] Drag-drop zone visible in Images section
- [ ] Dragging files shows visual feedback
- [ ] Multiple files can be dropped at once
- [ ] Images upload and appear in gallery
- [ ] Images can be reordered by dragging
- [ ] Reorder persists after page refresh
- [ ] Primary image stays marked after reorder
- [ ] Delete still works correctly
- [ ] Alt text editor works on each image
- [ ] Mobile touch drag works
- [ ] All tests pass

---

## Phase 3: Variant Options System

### Overview
- Create Product Options entity (Color, Size, etc.)
- Implement Option Values management
- Add Variant Options selector
- Enable Variant-Image association
- Optional: Variant generator from options matrix

### 3.1 Backend Tasks (C#)

#### Task 3.1.1: Create ProductOption Entity
**File:** `src/NOIR.Domain/Entities/Product/ProductOption.cs`

```csharp
public class ProductOption : TenantEntity<Guid>
{
    public Guid ProductId { get; private set; }
    public string Name { get; private set; } = null!; // "Color", "Size"
    public int Position { get; private set; }

    public virtual Product Product { get; private set; } = null!;
    public virtual ICollection<ProductOptionValue> Values { get; private set; }

    public static ProductOption Create(Guid productId, string name, int position, string? tenantId)
    {
        return new ProductOption
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Name = name,
            Position = position,
            TenantId = tenantId
        };
    }
}
```

#### Task 3.1.2: Create ProductOptionValue Entity
**File:** `src/NOIR.Domain/Entities/Product/ProductOptionValue.cs`

```csharp
public class ProductOptionValue : TenantEntity<Guid>
{
    public Guid ProductOptionId { get; private set; }
    public string Value { get; private set; } = null!; // "Red", "Blue", "S", "M"
    public int Position { get; private set; }

    public virtual ProductOption Option { get; private set; } = null!;
}
```

#### Task 3.1.3: Update ProductVariant Entity
**File:** `src/NOIR.Domain/Entities/Product/ProductVariant.cs`

```csharp
// Add ImageId property
public Guid? ImageId { get; private set; }
public virtual ProductImage? Image { get; private set; }

// Add method to set image
public void SetImage(Guid? imageId)
{
    ImageId = imageId;
}
```

#### Task 3.1.4: Create Entity Configurations
**Files:**
- `src/NOIR.Infrastructure/Persistence/Configurations/ProductOptionConfiguration.cs`
- `src/NOIR.Infrastructure/Persistence/Configurations/ProductOptionValueConfiguration.cs`

#### Task 3.1.5: Create Migration
```bash
dotnet ef migrations add AddProductOptionsAndVariantImage \
  --project src/NOIR.Infrastructure \
  --startup-project src/NOIR.Web \
  --context ApplicationDbContext \
  --output-dir Migrations/App
```

#### Task 3.1.6: Create Commands and Endpoints
**Commands:**
- `CreateProductOptionCommand`
- `UpdateProductOptionCommand`
- `DeleteProductOptionCommand`
- `AddProductOptionValueCommand`
- `DeleteProductOptionValueCommand`
- `UpdateProductVariantImageCommand`

**Endpoints:**
- GET `/api/products/{id}/options`
- POST `/api/products/{id}/options`
- PUT `/api/products/{id}/options/{optionId}`
- DELETE `/api/products/{id}/options/{optionId}`
- POST `/api/products/{id}/options/{optionId}/values`
- DELETE `/api/products/{id}/options/{optionId}/values/{valueId}`

### 3.2 Frontend Tasks (React/TypeScript)

#### Task 3.2.1: Create ProductOptionsManager Component
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/components/products/ProductOptionsManager.tsx`

**Requirements:**
- List of product options (Color, Size, etc.)
- Add new option button
- Inline edit option name
- Delete option (with confirmation)
- Reorder options by drag

#### Task 3.2.2: Create OptionValuesEditor Component
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/components/products/OptionValuesEditor.tsx`

**Requirements:**
- Tag-style input for values
- Add value by typing and pressing Enter
- Remove value by clicking X
- Color swatches for Color option type

#### Task 3.2.3: Create VariantOptionsSelector Component
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/components/products/VariantOptionsSelector.tsx`

**Requirements:**
- Dropdown for each product option
- Populate OptionsJson on variant
- Show selected options as badges on variant card

#### Task 3.2.4: Create VariantImageSelector Component
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/components/products/VariantImageSelector.tsx`

**Requirements:**
- Thumbnail grid of product images
- Click to select for variant
- Show selected image on variant card
- Clear selection option

#### Task 3.2.5: Update VariantForm with New Fields
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/pages/portal/ecommerce/products/ProductFormPage.tsx`

**Requirements:**
- Add VariantOptionsSelector to variant form
- Add VariantImageSelector to variant form
- Update variant display to show options and image

#### Task 3.2.6: Add Product Options Section to Form
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/pages/portal/ecommerce/products/ProductFormPage.tsx`

**Requirements:**
- New collapsible section for Product Options
- Position before Variants section
- Link: "Define options to create variants easily"

### 3.3 Phase 3 Validation Checklist

- [ ] Product Options section visible in form
- [ ] Can add new option (e.g., "Color")
- [ ] Can add values to option (e.g., "Red", "Blue")
- [ ] Can delete options and values
- [ ] Variant form shows option dropdowns
- [ ] Options save to OptionsJson correctly
- [ ] Variant card displays option badges
- [ ] Can select image for variant
- [ ] Variant image shows in card
- [ ] All tests pass

---

## Phase 4: Advanced Features

### Overview
- Variant Generator from Options Matrix
- Bulk Variant Edit
- Stock Adjustment History
- Enhanced Stock Management UI

### 4.1 Backend Tasks (C#)

#### Task 4.1.1: Create GenerateVariantsCommand
**File:** `src/NOIR.Application/Features/Products/Commands/GenerateVariants/`

**Requirements:**
- Generate all combinations of option values
- Create variants with auto-generated names (e.g., "Red - Large")
- Set default price from base product
- Set default stock to 0

#### Task 4.1.2: Create BulkUpdateVariantsCommand
**File:** `src/NOIR.Application/Features/Products/Commands/BulkUpdateVariants/`

**Requirements:**
- Accept array of variant updates
- Update price, stock, SKU in batch
- Single transaction for all updates

#### Task 4.1.3: Create StockAdjustment Entity
**File:** `src/NOIR.Domain/Entities/Product/StockAdjustment.cs`

**Requirements:**
- Track all stock changes
- Fields: VariantId, OldQuantity, NewQuantity, Reason, UserId, CreatedAt
- Query endpoint for history

#### Task 4.1.4: Add Stock Adjustment Logging
**File:** `src/NOIR.Application/Features/Products/Commands/UpdateProductVariant/`

**Requirements:**
- Log stock changes automatically
- Include reason field in command

### 4.2 Frontend Tasks (React/TypeScript)

#### Task 4.2.1: Create VariantGenerator Component
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/components/products/VariantGenerator.tsx`

**Requirements:**
- Show matrix of all option combinations
- Preview before generating
- "Generate X Variants" button
- Warning if existing variants will be affected

#### Task 4.2.2: Create BulkVariantEditor Component
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/components/products/BulkVariantEditor.tsx`

**Requirements:**
- Spreadsheet-like interface
- Editable cells for price, stock, SKU
- Checkbox column for selection
- Bulk actions: Set Price, Set Stock, Delete

#### Task 4.2.3: Create StockHistoryModal Component
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/components/products/StockHistoryModal.tsx`

**Requirements:**
- Table of stock changes
- Columns: Date, From, To, Change, Reason, User
- Filter by date range
- Export to CSV

#### Task 4.2.4: Create EnhancedStockEditor Component
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/components/products/EnhancedStockEditor.tsx`

**Requirements:**
- Current stock display
- Quick adjust buttons (+1, +10, -1, -10)
- Custom adjustment input
- Reason dropdown (Received, Sold, Damaged, Adjustment)
- Low stock threshold setting

#### Task 4.2.5: Integrate Advanced Components
**Skill:** `/ui-ux-pro-max`
**File:** `src/NOIR.Web/frontend/src/pages/portal/ecommerce/products/ProductFormPage.tsx`

**Requirements:**
- Add "Generate Variants" button when options exist
- Add "Bulk Edit" button to variants section
- Add "History" button to each variant for stock history
- Replace basic stock input with EnhancedStockEditor

### 4.3 Phase 4 Validation Checklist

- [ ] Generate Variants creates all combinations
- [ ] Generated variants have correct names
- [ ] Bulk edit mode shows spreadsheet UI
- [ ] Can edit multiple variants at once
- [ ] Stock history shows all changes
- [ ] Reason field appears in stock edit
- [ ] Quick adjust buttons work correctly
- [ ] All tests pass

---

## Implementation Order

### Recommended Execution Sequence

```
Week 1-2: Phase 1 (Critical)
├── Day 1-2: Backend - ShortDescription field
├── Day 3-4: Frontend - Short description + TinyMCE
├── Day 5-6: Frontend - Variant Edit UI
├── Day 7-8: Frontend - Alt Text Editor
└── Day 9-10: Testing + Bug fixes

Week 3: Phase 2 (Images)
├── Day 1-2: Backend - Upload + Reorder endpoints
├── Day 3-4: Frontend - ImageUploadZone
├── Day 5-6: Frontend - SortableImageGallery
└── Day 7: Integration + Testing

Week 4-5: Phase 3 (Variants)
├── Day 1-3: Backend - ProductOption entities
├── Day 4-5: Backend - Commands + Endpoints
├── Day 6-7: Frontend - ProductOptionsManager
├── Day 8-9: Frontend - VariantOptionsSelector
└── Day 10: Integration + Testing

Week 6: Phase 4 (Advanced)
├── Day 1-2: Backend - GenerateVariants + BulkUpdate
├── Day 3-4: Backend - StockAdjustment logging
├── Day 5-6: Frontend - Advanced components
└── Day 7: Final testing + Documentation
```

---

## Skills Mapping

### Backend Tasks → C# Skills
- Entity creation and configuration
- EF Core migrations
- CQRS Commands/Queries with Wolverine
- FluentValidation
- Unit of Work pattern
- Specification pattern

### Frontend Tasks → `/ui-ux-pro-max` Skill
- React component creation with shadcn/ui
- Form handling with react-hook-form + Zod
- Drag-drop with @dnd-kit
- File upload with react-dropzone
- Glassmorphism styling
- Framer Motion animations
- Accessibility (ARIA labels)
- Localization (i18n)

---

## Dependency Graph

```
Phase 1 (Critical)
├── 1.1.1 ShortDescription Entity ──┐
├── 1.1.2 Entity Config ────────────┼──> 1.1.3 Migration
├── 1.1.4 Commands/DTOs ────────────┘         │
│                                              │
├── 1.2.1 Short Description Field <────────────┘
├── 1.2.2 TinyMCE Integration
├── 1.2.3 RichTextEditor Component
├── 1.2.4 Variant Edit UI
├── 1.2.5 Alt Text Editor
└── 1.2.6 API Types

Phase 2 (Images) - Can run parallel with Phase 1
├── 2.1.1 Upload Endpoint
├── 2.1.2 Reorder Endpoint
├── 2.2.1 Install Packages
├── 2.2.2 ImageUploadZone <── 2.2.1
├── 2.2.3 SortableImageGallery <── 2.2.1
├── 2.2.4 SortableImageCard <── 2.2.3
└── 2.2.5 Integration <── 2.2.2, 2.2.4

Phase 3 (Variants) - Depends on Phase 1
├── 3.1.1 ProductOption Entity
├── 3.1.2 ProductOptionValue Entity <── 3.1.1
├── 3.1.3 Update ProductVariant
├── 3.1.4 Entity Configs <── 3.1.1, 3.1.2, 3.1.3
├── 3.1.5 Migration <── 3.1.4
├── 3.1.6 Commands/Endpoints <── 3.1.5
│
├── 3.2.1 ProductOptionsManager <── 3.1.6
├── 3.2.2 OptionValuesEditor <── 3.2.1
├── 3.2.3 VariantOptionsSelector <── 3.1.6
├── 3.2.4 VariantImageSelector
├── 3.2.5 Update VariantForm <── 3.2.3, 3.2.4
└── 3.2.6 Options Section <── 3.2.1, 3.2.2

Phase 4 (Advanced) - Depends on Phase 3
├── 4.1.1 GenerateVariantsCommand <── Phase 3
├── 4.1.2 BulkUpdateVariantsCommand
├── 4.1.3 StockAdjustment Entity
├── 4.1.4 Stock Logging <── 4.1.3
│
├── 4.2.1 VariantGenerator <── 4.1.1
├── 4.2.2 BulkVariantEditor <── 4.1.2
├── 4.2.3 StockHistoryModal <── 4.1.4
├── 4.2.4 EnhancedStockEditor
└── 4.2.5 Integration <── 4.2.1-4.2.4
```

---

## Testing Requirements

### Unit Tests (C#)
- [ ] ShortDescription validation (max 300 chars)
- [ ] ProductOption CRUD operations
- [ ] ProductOptionValue CRUD operations
- [ ] GenerateVariants combinations logic
- [ ] BulkUpdateVariants transaction handling
- [ ] StockAdjustment logging

### Integration Tests (C#)
- [ ] Image upload endpoint
- [ ] Image reorder endpoint
- [ ] Product options endpoints
- [ ] Variant with options creation
- [ ] Stock adjustment history endpoint

### Frontend Tests (Vitest/Playwright)
- [ ] Short description character counter
- [ ] TinyMCE editor initialization
- [ ] Variant edit form submission
- [ ] Image drag-drop upload
- [ ] Image gallery reordering
- [ ] Option creation/deletion
- [ ] Variant option selection

---

## Localization Keys Required

### English (`en/common.json`)
```json
{
  "products": {
    "shortDescription": "Short Description",
    "shortDescriptionPlaceholder": "Brief summary for product listings and SEO...",
    "shortDescriptionHelp": "Used in product cards, search results, and marketplace feeds",
    "fullDescription": "Full Description",
    "richEditorPlaceholder": "Write detailed product description...",
    "dragToReorder": "Drag to reorder",
    "dropImagesHere": "Drag images here or click to upload",
    "maxFileSize": "Max 10MB each",
    "altText": "Alt text",
    "describeImage": "Describe this image...",
    "productOptions": "Product Options",
    "addOption": "Add Option",
    "optionName": "Option Name",
    "optionValues": "Values",
    "addValue": "Add value...",
    "generateVariants": "Generate Variants",
    "bulkEdit": "Bulk Edit",
    "stockHistory": "Stock History",
    "adjustStock": "Adjust Stock",
    "adjustmentReason": "Reason",
    "received": "Received",
    "sold": "Sold",
    "damaged": "Damaged",
    "adjustment": "Adjustment"
  }
}
```

### Vietnamese (`vi/common.json`)
```json
{
  "products": {
    "shortDescription": "Mô tả ngắn",
    "shortDescriptionPlaceholder": "Tóm tắt ngắn cho danh sách sản phẩm và SEO...",
    "shortDescriptionHelp": "Sử dụng trong thẻ sản phẩm, kết quả tìm kiếm và marketplace",
    "fullDescription": "Mô tả chi tiết",
    "richEditorPlaceholder": "Viết mô tả sản phẩm chi tiết...",
    "dragToReorder": "Kéo để sắp xếp",
    "dropImagesHere": "Kéo thả hình ảnh vào đây hoặc nhấp để tải lên",
    "maxFileSize": "Tối đa 10MB mỗi file",
    "altText": "Văn bản thay thế",
    "describeImage": "Mô tả hình ảnh này...",
    "productOptions": "Tùy chọn sản phẩm",
    "addOption": "Thêm tùy chọn",
    "optionName": "Tên tùy chọn",
    "optionValues": "Các giá trị",
    "addValue": "Thêm giá trị...",
    "generateVariants": "Tạo biến thể",
    "bulkEdit": "Chỉnh sửa hàng loạt",
    "stockHistory": "Lịch sử tồn kho",
    "adjustStock": "Điều chỉnh tồn kho",
    "adjustmentReason": "Lý do",
    "received": "Nhập kho",
    "sold": "Đã bán",
    "damaged": "Hư hỏng",
    "adjustment": "Điều chỉnh"
  }
}
```

---

## Next Steps

After this workflow is approved:

1. **Run `/sc:implement` for each phase:**
   ```
   /sc:implement Phase 1 Task 1.1.1 - Add ShortDescription to Product Entity
   ```

2. **Use `/ui-ux-pro-max` for frontend tasks:**
   ```
   /ui-ux-pro-max Build Short Description field component for ProductFormPage
   ```

3. **Run tests after each phase:**
   ```bash
   dotnet test src/NOIR.sln
   npm run test --prefix src/NOIR.Web/frontend
   ```

4. **Commit after each phase:**
   ```
   /sc:git commit "feat(products): add short description field"
   ```

---

**Workflow Generated:** 2026-01-28
**Total Tasks:** 42
**Estimated Duration:** 6 weeks
