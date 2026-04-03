# Catalog — Test Cases

> Pages: /portal/products, /portal/products/new, /portal/products/:id/edit, /portal/product-categories, /portal/product-attributes, /portal/brands, /portal/inventory | Last updated: 2026-04-03 | Git ref: f6f5cd3
> Total: 98 cases | P0: 8 | P1: 52 | P2: 28 | P3: 10

---

## Page: Products List (`/portal/products`)

### Happy Path

#### TC-CAT-001: View products list with DataTable [P1] [smoke]
- **Pre**: Logged in as tenant admin, products exist
- **Steps**:
  1. Navigate to /portal/products
  2. Observe the products list page loads
- **Expected**: DataTable renders with columns: Actions, Select, Image, Name, Status, Category, Brand, Price, Stock, audit columns. Card shows "Showing X of Y items". ProductStatsCards display above the table.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Stats cards match actual counts | ☐ Pagination works

#### TC-CAT-002: Search products by name [P1] [smoke]
- **Pre**: Multiple products exist
- **Steps**:
  1. Type a product name fragment in the search input
  2. Wait for debounced search
- **Expected**: Table filters to show matching products. Opacity transition during search stale state. Count updates in CardDescription.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CAT-003: Filter products by status [P1] [regression]
- **Pre**: Products in Draft, Active, and Archived statuses exist
- **Steps**:
  1. Select "Active" from status filter dropdown
  2. Select "Draft" from status filter dropdown
  3. Select "Archived" from status filter dropdown
  4. Select "All" to reset
- **Expected**: Each filter shows only products of that status. Status badges use correct colors via getStatusBadgeClasses.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CAT-004: Filter products by category [P1] [regression]
- **Pre**: Products assigned to different categories
- **Steps**:
  1. Select a specific category from the category filter
- **Expected**: Only products in selected category shown. Count updates.

#### TC-CAT-005: Filter products by brand [P1] [regression]
- **Pre**: Products assigned to different brands
- **Steps**:
  1. Select a specific brand from the brand filter
- **Expected**: Only products of that brand shown.

#### TC-CAT-006: Toggle table/grid view mode [P2] [visual]
- **Pre**: Products exist
- **Steps**:
  1. Click the Grid view toggle (LayoutGrid icon)
  2. Observe the grid view with EnhancedProductGridView
  3. Click the Table view toggle (List icon)
- **Expected**: View toggles between DataTable and card grid. Both show same data. Grid shows product cards with images, price, stock info.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CAT-007: Product image preview in table [P2] [visual]
- **Pre**: Products with images exist
- **Steps**:
  1. Hover over a product image thumbnail in the table
  2. Click the thumbnail
- **Expected**: Hover shows popover preview. Click opens FilePreviewTrigger lightbox.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CAT-008: Bulk select and publish draft products [P1] [smoke]
- **Pre**: Multiple draft products exist
- **Steps**:
  1. Select multiple draft products via checkboxes
  2. BulkActionToolbar appears
  3. Click "Publish" bulk action
- **Expected**: BulkActionToolbar shows selected count. Bulk publish succeeds with toast showing success count. Products transition to Active status.
- **Data**: ☐ Only Draft products are published | ☐ Active products ignored in bulk publish

#### TC-CAT-009: Bulk select and archive active products [P1] [regression]
- **Pre**: Multiple active products exist
- **Steps**:
  1. Select multiple active products
  2. Click "Archive" bulk action
- **Expected**: Bulk archive succeeds. Products transition to Archived.
- **Data**: ☐ Only Active products archived | ☐ Draft products ignored

#### TC-CAT-010: Bulk delete products with confirmation [P0] [smoke]
- **Pre**: Products selected
- **Steps**:
  1. Select products via checkboxes
  2. Click "Delete" bulk action
  3. Confirm in the bulk delete confirmation dialog
- **Expected**: Confirmation dialog appears (Credenza). On confirm, products soft-deleted. Toast shows success/partial results.
- **Data**: ☐ Products removed from list | ☐ Inventory not affected (soft delete)

#### TC-CAT-011: Publish single product from actions menu [P1] [regression]
- **Pre**: A Draft product exists
- **Steps**:
  1. Click the actions menu (EllipsisVertical) on a Draft product
  2. Click "Publish"
- **Expected**: Product status changes to Active. Toast success. Row updates in table.

#### TC-CAT-012: Archive single product from actions menu [P1] [regression]
- **Pre**: An Active product exists
- **Steps**:
  1. Click actions menu on Active product
  2. Click "Archive"
- **Expected**: Product status changes to Archived. Toast success.

#### TC-CAT-013: Duplicate product from actions menu [P2] [regression]
- **Pre**: An existing product
- **Steps**:
  1. Click actions menu
  2. Click "Duplicate" (Copy icon)
- **Expected**: New product created as a copy with Draft status. Navigates to edit page of the duplicate.

#### TC-CAT-014: Delete single product from actions menu [P1] [regression]
- **Pre**: A product exists
- **Steps**:
  1. Click actions menu
  2. Click "Delete"
  3. Confirm in DeleteProductDialog
- **Expected**: Confirmation dialog appears with product name. Row fade-out animation plays. Product soft-deleted. Toast success.

#### TC-CAT-015: Low stock alert display [P2] [visual]
- **Pre**: Products with stock below LOW_STOCK_THRESHOLD exist
- **Steps**:
  1. Navigate to products page
- **Expected**: LowStockAlert component renders showing count of low-stock products.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CAT-016: Attribute filter dialog [P2] [regression]
- **Pre**: Filterable product attributes configured
- **Steps**:
  1. Click the attribute filter button in toolbar
  2. AttributeFilterDialog opens
  3. Select attribute values
  4. Apply filter
- **Expected**: Products filtered by selected attribute values. Filter count badge updates on the button.

#### TC-CAT-017: Import/Export products [P1] [regression]
- **Pre**: Products exist
- **Steps**:
  1. Click ImportExportDropdown
  2. Click "Export CSV" — download starts
  3. Click "Export Excel" — download starts
  4. Click "Download Template" — template CSV downloads
  5. Click "Import CSV" — select a valid CSV file
- **Expected**: Exports contain current product data. Import processes file with progress dialog and shows results (success/error counts).
- **Data**: ☐ Exported file has correct data | ☐ Import creates/updates products

#### TC-CAT-018: Column visibility and enterprise table settings [P2] [regression]
- **Pre**: Products page loaded
- **Steps**:
  1. Toggle column visibility via DataTableToolbar columns dropdown
  2. Reorder columns via drag
  3. Change density setting
  4. Reload page
- **Expected**: Settings persist in localStorage under 'products' key. Column visibility, order, and density restored on reload.

#### TC-CAT-019: Sorting columns [P1] [regression]
- **Pre**: Multiple products exist
- **Steps**:
  1. Click "Name" column header to sort ascending
  2. Click again to sort descending
  3. Sort by "Price", "Stock", "Status"
- **Expected**: Server-side sorting applied. Arrow indicator shows direction.

#### TC-CAT-020: Pagination with page size selector [P1] [regression]
- **Pre**: More than 20 products exist
- **Steps**:
  1. Observe default page size (DEFAULT_PRODUCT_PAGE_SIZE)
  2. Navigate to page 2
  3. Change page size to 50
- **Expected**: Pagination controls work. Page size change persists in localStorage. CardDescription updates count.

### Edge Cases

#### TC-CAT-021: No products empty state [P2] [edge-case]
- **Pre**: No products exist (or all filtered out)
- **Steps**:
  1. Navigate to products page
- **Expected**: EmptyState component with Package icon, title, description, and "Add Product" action button (if create permission).
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CAT-022: Bulk publish with mixed statuses [P2] [edge-case]
- **Pre**: Select both Draft and Active products
- **Steps**:
  1. Select all products
  2. Click "Publish"
- **Expected**: Only Draft products published. Warning toast "No draft products selected" if none are Draft.

#### TC-CAT-023: Network error on product list [P2] [edge-case]
- **Pre**: Backend unavailable
- **Steps**:
  1. Navigate to products page
- **Expected**: Error message displayed in destructive banner. OfflineBanner shows if SignalR reconnecting.

### Security

#### TC-CAT-024: Products page without read permission [P1] [security]
- **Pre**: User without Products.Read permission
- **Steps**:
  1. Navigate to /portal/products
- **Expected**: Page not accessible or no data shown. Sidebar item hidden.

#### TC-CAT-025: Bulk actions hidden without manage permission [P1] [security]
- **Pre**: User with read-only permissions
- **Steps**:
  1. Navigate to products page
- **Expected**: Select column not rendered. Publish/Archive/Delete actions hidden in dropdown menu.

---

## Page: Product Create/Edit (`/portal/products/new`, `/portal/products/:id/edit`)

### Happy Path

#### TC-CAT-026: Create new product — basic info [P0] [smoke]
- **Pre**: Logged in with ProductsCreate permission
- **Steps**:
  1. Navigate to /portal/products/new
  2. Fill in Name (slug auto-generates)
  3. Set Base Price
  4. Select a Category
  5. Select a Brand
  6. Enter Short Description
  7. Enter Description via RichTextEditor (Tiptap)
  8. Click Save
- **Expected**: Product created in Draft status. Success toast. Redirects to edit page with product data loaded.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Slug generated from name | ☐ Currency defaults to VND | ☐ trackInventory defaults to true

#### TC-CAT-027: Edit product — update fields [P1] [smoke]
- **Pre**: Existing product in Draft status
- **Steps**:
  1. Navigate to /portal/products/:id/edit
  2. Change Name, Price, Category
  3. Click Save
- **Expected**: Product updated. Slug can be independently edited. Success toast.
- **Data**: ☐ All fields saved correctly | ☐ ModifiedAt updated

#### TC-CAT-028: Product form validation [P1] [regression]
- **Pre**: On create product page
- **Steps**:
  1. Leave Name empty, click Save
  2. Enter name > 200 chars
  3. Enter invalid slug format (uppercase, spaces)
  4. Enter negative base price
- **Expected**: Validation errors shown inline under each field. FormErrorBanner for server errors. Red asterisks on required fields (via getRequiredFields).
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CAT-029: Add product variant [P0] [smoke]
- **Pre**: Existing product on edit page
- **Steps**:
  1. Scroll to Variants section
  2. Click "Add Variant"
  3. Fill in: Name, Price, SKU, Stock Quantity
  4. Save variant
- **Expected**: Variant added to EditableVariantsTable. Price, SKU, stock shown in table. Variant can be edited inline.
- **Data**: ☐ Variant price independent from base price | ☐ SKU unique | ☐ Stock quantity tracked per variant

#### TC-CAT-030: Edit product variant inline [P1] [regression]
- **Pre**: Product with existing variant
- **Steps**:
  1. Click edit on a variant row
  2. EditVariantForm appears inline
  3. Change price, SKU, stock
  4. Click Save
- **Expected**: Variant updated. compareAtPrice must be > price (validation). costPrice >= 0.
- **Data**: ☐ Compare-at-price validation | ☐ Stock quantity updated

#### TC-CAT-031: Delete product variant [P1] [regression]
- **Pre**: Product with multiple variants
- **Steps**:
  1. Click delete on a variant
  2. Confirm deletion
- **Expected**: Variant removed from list. Product still has other variants.

#### TC-CAT-032: Upload product images [P0] [smoke]
- **Pre**: Product on edit page
- **Steps**:
  1. Click ImageUploadZone or drag-and-drop an image
  2. Image uploads via uploadMedia service
  3. Image appears in SortableImageGallery
- **Expected**: Image uploaded and displayed. First image auto-set as primary (star icon). Progress indicator during upload.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px
- **Data**: ☐ Image stored in media library | ☐ Primary flag set

#### TC-CAT-033: Reorder product images via drag-and-drop [P2] [regression]
- **Pre**: Product with multiple images
- **Steps**:
  1. Drag an image to a new position in SortableImageGallery
  2. Drop to reorder
- **Expected**: Sort order updated. API call to reorderProductImages. New order persisted.

#### TC-CAT-034: Set primary product image [P1] [regression]
- **Pre**: Product with multiple images
- **Steps**:
  1. Click the Star icon on a non-primary image
- **Expected**: That image becomes primary. Previous primary loses star. Primary image used as thumbnail in list page.

#### TC-CAT-035: Delete product image [P1] [regression]
- **Pre**: Product with images
- **Steps**:
  1. Click delete on an image
  2. Confirm
- **Expected**: Image removed from product. If it was primary, next image becomes primary.

#### TC-CAT-036: Publish product from edit page [P0] [smoke]
- **Pre**: Draft product with all required fields filled
- **Steps**:
  1. On edit page, click "Publish" button (Send icon)
- **Expected**: Product status changes to Active. Status badge updates. Publish button hidden for Active products.
- **Data**: ☐ Status = Active | ☐ PublishedAt timestamp set

#### TC-CAT-037: Product form — SEO fields [P2] [regression]
- **Pre**: Product edit page
- **Steps**:
  1. Fill in Meta Title
  2. Fill in Meta Description
  3. Save
- **Expected**: SEO fields saved. Character limits enforced.

#### TC-CAT-038: Product form — physical dimensions [P2] [regression]
- **Pre**: Product edit page
- **Steps**:
  1. Enter Weight, select Weight Unit (kg/g/lb/oz)
  2. Enter Length, Width, Height, select Dimension Unit (cm/in/m)
  3. Save
- **Expected**: Dimensions saved. Positive number validation on all dimension fields.

#### TC-CAT-039: Product form — inventory tracking toggle [P1] [regression]
- **Pre**: Product edit page
- **Steps**:
  1. Toggle "Track Inventory" Switch off
  2. Save
  3. Toggle back on
- **Expected**: When off, stock-related features disabled for this product. When on, variant stock quantities tracked.

#### TC-CAT-040: Product attributes section [P1] [regression]
- **Pre**: Product attributes defined, product on edit page
- **Steps**:
  1. Scroll to ProductAttributesSection (or ProductAttributesSectionCreate for new)
  2. Select attribute values for the product
  3. Save via useBulkUpdateProductAttributesMutation
- **Expected**: Attribute values associated with product. Filterable attributes affect product search.

#### TC-CAT-041: Product activity log [P2] [visual]
- **Pre**: Product with audit history
- **Steps**:
  1. Scroll to ProductActivityLog section
- **Expected**: Activity timeline shows creation, edits, status changes with relative timestamps.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CAT-042: Stock history timeline [P2] [visual]
- **Pre**: Product with inventory movements
- **Steps**:
  1. Scroll to stock history section
- **Expected**: StockHistoryTimeline shows stock movements with type (StockIn/StockOut), quantity, and timestamp.

### Edge Cases

#### TC-CAT-043: Concurrent edit — entity conflict dialog [P1] [edge-case]
- **Pre**: Two users editing the same product
- **Steps**:
  1. User A and User B open same product
  2. User A saves changes
  3. User B's page receives SignalR update
- **Expected**: EntityConflictDialog appears for User B with options to reload or continue editing.

#### TC-CAT-044: Product deleted by another user [P1] [edge-case]
- **Pre**: Product open in edit mode
- **Steps**:
  1. Another user deletes the product
  2. SignalR notifies
- **Expected**: EntityDeletedDialog appears. User navigated back to list.

#### TC-CAT-045: Create product — duplicate slug [P2] [edge-case]
- **Pre**: Product with slug "test-product" exists
- **Steps**:
  1. Create new product with name "Test Product" (generates same slug)
  2. Save
- **Expected**: Server validation error shown via FormErrorBanner. Slug field highlighted.

#### TC-CAT-046: Variant with compareAtPrice <= price [P2] [edge-case]
- **Pre**: Editing a variant
- **Steps**:
  1. Set price = 100
  2. Set compareAtPrice = 50
  3. Try to save
- **Expected**: Zod refine validation error: "Compare at price must be higher than price".

---

## Page: Product Categories (`/portal/product-categories`)

### Happy Path

#### TC-CAT-047: View categories in tree mode [P1] [smoke]
- **Pre**: Categories with parent-child hierarchy exist
- **Steps**:
  1. Navigate to /portal/product-categories
  2. Default view is "Tree"
- **Expected**: CategoryTreeView renders hierarchical tree. Each node shows name, product count, child count. Expand/collapse nodes.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CAT-048: View categories in table mode [P2] [visual]
- **Pre**: Categories exist
- **Steps**:
  1. Click "List" view mode toggle
- **Expected**: DataTable renders with columns: Actions, Name (with FolderTree icon), Slug, Parent, Products count, Children count, audit columns.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CAT-049: Create new category [P1] [smoke]
- **Pre**: ProductCategoriesCreate permission
- **Steps**:
  1. Click "New Category" button
  2. ProductCategoryDialog opens (URL: ?dialog=create-product-category)
  3. Fill in Name, Description
  4. Select parent category (optional)
  5. Save
- **Expected**: Category created. Tree refreshes. URL-synced dialog closes.
- **Data**: ☐ Slug auto-generated | ☐ Appears in tree under parent

#### TC-CAT-050: Create child category from tree [P1] [regression]
- **Pre**: Parent category exists
- **Steps**:
  1. In tree view, click "Add Child" on a category
  2. Dialog opens with parent pre-selected (parentIdForCreate state)
  3. Fill in name, save
- **Expected**: New category created as child of selected parent. Tree expands to show new child.

#### TC-CAT-051: Edit category via actions menu [P1] [regression]
- **Pre**: Category exists
- **Steps**:
  1. Click actions menu (table view) or edit in tree view
  2. Modify name, description
  3. Save
- **Expected**: Category updated. URL: ?edit=categoryId. Changes reflected in tree/table.

#### TC-CAT-052: Delete category [P1] [regression]
- **Pre**: Category without products
- **Steps**:
  1. Click Delete in actions menu
  2. DeleteProductCategoryDialog appears
  3. Confirm
- **Expected**: Category deleted (soft delete). Row fade-out animation. Toast success.
- **Data**: ☐ Children orphaned or cascaded | ☐ Products unassigned

#### TC-CAT-053: Manage category attributes [P1] [regression]
- **Pre**: Category and product attributes exist
- **Steps**:
  1. Click "Manage Attributes" (Tags icon) from actions menu
  2. ProductCategoryAttributesDialog opens
  3. Assign/unassign attributes
  4. Save
- **Expected**: Attributes associated with category. Products in this category inherit attribute configuration.

#### TC-CAT-054: Drag-reorder categories in tree [P2] [regression]
- **Pre**: Multiple categories at same level
- **Steps**:
  1. In tree view, drag a category to reorder
  2. Drop at new position
- **Expected**: reorderProductCategories mutation called with new sortOrder/parentId. Order persisted.

#### TC-CAT-055: Search categories [P1] [regression]
- **Pre**: Multiple categories exist
- **Steps**:
  1. Type in search input
- **Expected**: Both tree and table filter by search term. Client-side search on name.

### Edge Cases

#### TC-CAT-056: Delete category with products [P2] [edge-case]
- **Pre**: Category with assigned products
- **Steps**:
  1. Try to delete category
- **Expected**: Error message explaining category has products. Delete blocked or warning shown.

#### TC-CAT-057: Categories empty state [P3] [visual]
- **Pre**: No categories exist
- **Steps**:
  1. Navigate to page
- **Expected**: EmptyState in tree view with "No categories found" and create action. EmptyState in table view.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Page: Product Attributes (`/portal/product-attributes`)

### Happy Path

#### TC-CAT-058: View product attributes list [P1] [smoke]
- **Pre**: Product attributes exist
- **Steps**:
  1. Navigate to /portal/product-attributes
- **Expected**: DataTable with columns: Actions, Name, Code, Type (with icon badge), Values count, Filterable (check/minus), Variant (check/minus), Status, audit columns.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CAT-059: Create new product attribute [P1] [smoke]
- **Pre**: AttributesCreate permission
- **Steps**:
  1. Click "New Attribute"
  2. ProductAttributeDialog opens (URL: ?dialog=create-attribute)
  3. Fill in Name, Code
  4. Select Type (from 13 types)
  5. Set Filterable toggle, Variant toggle
  6. Add values
  7. Save
- **Expected**: Attribute created. Type badge shows correct icon per getTypeBadge. Values count shown.
- **Data**: ☐ Code unique | ☐ Type determines UI rendering

#### TC-CAT-060: Edit product attribute [P1] [regression]
- **Pre**: Attribute exists
- **Steps**:
  1. Click row or edit in actions menu
  2. Modify name, add/remove values
  3. Save
- **Expected**: Attribute updated. Value count reflects changes.

#### TC-CAT-061: Delete product attribute [P1] [regression]
- **Pre**: Attribute not used by products
- **Steps**:
  1. Click Delete in actions menu
  2. Confirm in destructive dialog (border-destructive/30)
  3. Confirm
- **Expected**: Attribute deleted. Row fade-out. Toast success.

#### TC-CAT-062: Attribute type badge rendering [P2] [visual]
- **Pre**: Attributes of various types exist
- **Steps**:
  1. View attributes list
- **Expected**: Each type has distinct badge color and icon from getTypeBadge. Badge uses variant="outline".
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CAT-063: Toggle filterable/variant flags [P2] [regression]
- **Pre**: Editing an attribute
- **Steps**:
  1. Toggle Filterable on/off
  2. Toggle Variant on/off
  3. Save
- **Expected**: Check/Minus icons update in table. Filterable attributes appear in product list filter dialog.

### Edge Cases

#### TC-CAT-064: Delete attribute used by products [P2] [edge-case]
- **Pre**: Attribute assigned to products
- **Steps**:
  1. Try to delete
- **Expected**: Error or warning about attribute being in use.

#### TC-CAT-065: Attributes empty state [P3] [visual]
- **Pre**: No attributes exist
- **Steps**:
  1. Navigate to page
- **Expected**: EmptyState with Tags icon and "Add Attribute" action.

---

## Page: Brands (`/portal/brands`)

### Happy Path

#### TC-CAT-066: View brands list [P1] [smoke]
- **Pre**: Brands exist
- **Steps**:
  1. Navigate to /portal/brands
- **Expected**: DataTable with columns: Actions, Logo (FilePreviewTrigger), Name, Slug, Website, Products count, Status, audit columns.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CAT-067: Create new brand [P1] [smoke]
- **Pre**: BrandsCreate permission
- **Steps**:
  1. Click "New Brand" or equivalent
  2. BrandDialog opens (URL: ?dialog=create-brand)
  3. Fill in Name, Description, Website URL
  4. Upload logo
  5. Save
- **Expected**: Brand created. Logo displayed as FilePreviewTrigger in table.
- **Data**: ☐ Slug auto-generated | ☐ Logo stored in media

#### TC-CAT-068: Edit brand [P1] [regression]
- **Pre**: Brand exists
- **Steps**:
  1. Click row or edit in actions menu (Eye icon)
  2. Modify fields
  3. Save
- **Expected**: Brand updated. Edit URL: ?edit=brandId.

#### TC-CAT-069: Delete brand [P1] [regression]
- **Pre**: Brand without products (or with)
- **Steps**:
  1. Click Delete in actions menu
  2. Confirm in destructive dialog
- **Expected**: Brand soft-deleted. Row fade-out animation. Toast success.

#### TC-CAT-070: Brand logo preview [P2] [visual]
- **Pre**: Brand with logo
- **Steps**:
  1. Hover on logo thumbnail
  2. Click thumbnail
- **Expected**: Hover popover shows larger preview. Click opens lightbox.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CAT-071: Brand website link [P2] [regression]
- **Pre**: Brand with website URL
- **Steps**:
  1. Observe website column shows external link (Globe + ExternalLink icons)
- **Expected**: Link is clickable and opens in new tab.

### Edge Cases

#### TC-CAT-072: Brands empty state [P3] [visual]
- **Pre**: No brands exist
- **Expected**: EmptyState with Award icon.

#### TC-CAT-073: Delete brand with products [P2] [edge-case]
- **Pre**: Brand has assigned products
- **Steps**:
  1. Try to delete
- **Expected**: Error or warning. Products need to be reassigned.

---

## Page: Inventory Receipts (`/portal/inventory`)

### Happy Path

#### TC-CAT-074: View inventory receipts list [P1] [smoke]
- **Pre**: Inventory receipts exist
- **Steps**:
  1. Navigate to /portal/inventory
- **Expected**: DataTable with columns: Actions, Receipt # (mono font), Type (StockIn/StockOut badge), Status (Draft/Confirmed/Cancelled), Items count, Total Qty, Total Cost, audit columns. Filter dropdowns for Type and Status.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

#### TC-CAT-075: Filter by receipt type [P1] [regression]
- **Pre**: Both StockIn and StockOut receipts exist
- **Steps**:
  1. Select "Stock In" from type filter
  2. Select "Stock Out" from type filter
  3. Select "All"
- **Expected**: Table filters by type. Type badge shows correct icon and color from RECEIPT_TYPE_CONFIG.

#### TC-CAT-076: Filter by receipt status [P1] [regression]
- **Pre**: Receipts in Draft, Confirmed, Cancelled
- **Steps**:
  1. Filter by "Draft"
  2. Filter by "Confirmed"
  3. Filter by "Cancelled"
- **Expected**: Status badge shows correct icon and color from RECEIPT_STATUS_CONFIG.

#### TC-CAT-077: View receipt detail [P1] [smoke]
- **Pre**: Receipt exists
- **Steps**:
  1. Click "View Details" in actions menu
  2. InventoryReceiptDetailDialog opens (selectedReceiptId state)
- **Expected**: Dialog shows receipt number, type, status, line items with products, quantities, costs.

#### TC-CAT-078: Confirm a StockIn receipt [P0] [smoke]
- **Pre**: Draft StockIn receipt exists
- **Steps**:
  1. Click "Confirm" (CheckCircle2) in actions menu for a Draft receipt
  2. Confirmation dialog appears
  3. Confirm
- **Expected**: Receipt status changes to Confirmed. Toast with receipt number. Product stock quantities INCREASED by receipt quantities.
- **Data**: ☐ Stock increased | ☐ Receipt status = Confirmed | ☐ Cannot confirm again

#### TC-CAT-079: Confirm a StockOut receipt [P0] [smoke]
- **Pre**: Draft StockOut receipt exists, products have sufficient stock
- **Steps**:
  1. Click "Confirm" for a Draft StockOut receipt
  2. Confirm
- **Expected**: Receipt status changes to Confirmed. Product stock quantities DECREASED by receipt quantities.
- **Data**: ☐ Stock decreased | ☐ Receipt number starts with SHP-

#### TC-CAT-080: Cancel a draft receipt [P1] [regression]
- **Pre**: Draft receipt exists
- **Steps**:
  1. Click "Cancel" (XCircle) in actions menu
  2. Cancel dialog opens with reason textarea
  3. Enter reason, confirm
- **Expected**: Receipt status changes to Cancelled. Cancel reason stored. No inventory change.
- **Data**: ☐ No stock change | ☐ Status = Cancelled | ☐ Reason stored

### Edge Cases

#### TC-CAT-081: Confirm action only on Draft receipts [P1] [edge-case]
- **Pre**: Confirmed or Cancelled receipts
- **Steps**:
  1. Open actions menu on non-Draft receipt
- **Expected**: Confirm and Cancel menu items not shown (isDraft check).

#### TC-CAT-082: StockOut receipt with insufficient stock [P2] [edge-case]
- **Pre**: StockOut receipt for quantity > available stock
- **Steps**:
  1. Confirm the receipt
- **Expected**: Server error. Toast error. Receipt stays Draft.

#### TC-CAT-083: Cancel receipt without reason [P2] [edge-case]
- **Pre**: Draft receipt
- **Steps**:
  1. Open cancel dialog
  2. Leave reason empty
  3. Confirm
- **Expected**: Cancel proceeds (reason is optional per `cancelReason || undefined`).

#### TC-CAT-084: Inventory empty state [P3] [visual]
- **Pre**: No receipts exist
- **Expected**: EmptyState with Warehouse icon.

### Data Consistency

#### TC-CAT-085: StockIn confirm → product stock increase [P0] [data-consistency]
- **Pre**: Product A has stock = 10. StockIn receipt with 5 units of Product A.
- **Steps**:
  1. Confirm StockIn receipt
  2. Navigate to Product A edit page
- **Expected**: Product A stock = 15. Stock history timeline shows the movement.

#### TC-CAT-086: StockOut confirm → product stock decrease [P0] [data-consistency]
- **Pre**: Product A has stock = 10. StockOut receipt with 3 units of Product A.
- **Steps**:
  1. Confirm StockOut receipt
  2. Navigate to Product A edit page
- **Expected**: Product A stock = 7. Stock history timeline shows the movement.

---

## Cross-Feature Tests

#### TC-CAT-087: Product → Category relationship [P1] [cross-feature]
- **Pre**: Product assigned to category
- **Steps**:
  1. View category page — product count column
  2. Change product's category on edit page
  3. Refresh category page
- **Expected**: Product counts update on both old and new categories.

#### TC-CAT-088: Product → Brand relationship [P1] [cross-feature]
- **Pre**: Product assigned to brand
- **Steps**:
  1. View brands page — products count column
  2. Change product's brand
  3. Refresh brands page
- **Expected**: Product counts update on both old and new brands.

#### TC-CAT-089: Attribute → Category → Product flow [P2] [cross-feature]
- **Pre**: Attribute assigned to category, product in that category
- **Steps**:
  1. Assign filterable attribute to category via ProductCategoryAttributesDialog
  2. On product edit page, see the attribute available
  3. Set attribute value
  4. On products list, use attribute filter
- **Expected**: Full flow works end-to-end.

---

## i18n Tests

#### TC-CAT-090: Products page in Vietnamese [P2] [i18n]
- **Pre**: Language set to VI
- **Steps**:
  1. Navigate through all catalog pages
- **Expected**: All labels, buttons, status badges, empty states, tooltips in Vietnamese. No English leaks.
- **Visual**: ☐ Light | ☐ Dark | ☐ VI | ☐ 768px

---

## Responsive Tests

#### TC-CAT-091: Products list at 768px [P2] [responsive]
- **Pre**: Browser at 768px width
- **Steps**:
  1. Navigate to /portal/products
- **Expected**: PageHeader responsive. Table horizontally scrollable. Stats cards stack vertically.
- **Visual**: ☐ 768px

#### TC-CAT-092: Product form at 768px [P2] [responsive]
- **Pre**: Browser at 768px width
- **Steps**:
  1. Navigate to /portal/products/new
- **Expected**: Form fields stack vertically. Image gallery adapts. Variant table scrollable.
- **Visual**: ☐ 768px

---

## Dark Mode Tests

#### TC-CAT-093: Product status badges in dark mode [P3] [dark-mode]
- **Pre**: Dark mode enabled
- **Steps**:
  1. View products with Draft, Active, Archived statuses
- **Expected**: Status badge colors readable. getStatusBadgeClasses provides proper dark mode variants.
- **Visual**: ☐ Dark

#### TC-CAT-094: Category tree in dark mode [P3] [dark-mode]
- **Pre**: Dark mode, categories with hierarchy
- **Expected**: Tree lines, expand/collapse icons, badges all visible against dark background.
- **Visual**: ☐ Dark

#### TC-CAT-095: Inventory receipt type/status badges in dark mode [P3] [dark-mode]
- **Pre**: Dark mode, receipts exist
- **Expected**: StockIn/StockOut badges and Draft/Confirmed/Cancelled badges readable.
- **Visual**: ☐ Dark

---

## Performance Tests

#### TC-CAT-096: Products list with 500+ items [P2] [performance]
- **Pre**: 500+ products in database
- **Steps**:
  1. Navigate to products page
  2. Set page size to 100
- **Expected**: Page loads within 3s. No UI jank. Skeleton loading shows during fetch.

#### TC-CAT-097: Product form with 20+ images [P2] [performance]
- **Pre**: Product with many images
- **Steps**:
  1. Open product edit page
  2. Drag-reorder images
- **Expected**: SortableImageGallery renders smoothly. Drag-and-drop responsive.

#### TC-CAT-098: Category tree with deep nesting [P2] [performance]
- **Pre**: Category tree 5+ levels deep, 100+ categories
- **Steps**:
  1. View category tree
  2. Expand all nodes
- **Expected**: CategoryTreeView renders without lag. Virtual scrolling if applicable.
