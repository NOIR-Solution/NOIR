# NOIR Product E2E Test Suite

**Comprehensive End-to-End Browser Testing for Product Ecosystem**

> This document provides robust, detailed test cases for the complete Product module including Categories, Attributes, Brands, Products, Variants, Images, Bulk Operations, and Import/Export functionality.

**Total Test Cases: 247**
**Estimated Execution Time: 4-5 hours (full suite)**

---

## Table of Contents

1. [Product Categories](#1-product-categories-30-tests)
2. [Product Attributes](#2-product-attributes-45-tests)
3. [Brands](#3-brands-12-tests)
4. [Product CRUD](#4-product-crud-48-tests)
5. [Product Variants](#5-product-variants-28-tests)
6. [Product Images](#6-product-images-22-tests)
7. [Product Options](#7-product-options-14-tests)
8. [Product Filters & Search](#8-product-filters--search-20-tests)
9. [Bulk Operations](#9-bulk-operations-16-tests)
10. [Import/Export](#10-importexport-18-tests)
11. [Integration Scenarios](#11-integration-scenarios-14-tests)

---

## Test Execution Legend

| Priority | Symbol | Description | SLA |
|----------|--------|-------------|-----|
| **P0** | 🔴 | Critical Path | 100% pass |
| **P1** | 🟠 | High Priority | 95%+ pass |
| **P2** | 🟡 | Medium Priority | 90%+ pass |
| **P3** | 🟢 | Low Priority | Best effort |

| Status | Icon | Meaning |
|--------|------|---------|
| ⬜ | Not Started | Test not yet executed |
| 🔄 | In Progress | Currently executing |
| ✅ | Passed | Test passed |
| ❌ | Failed | Test failed |
| 🚫 | Blocked | Cannot execute |

---

## 1. Product Categories (30 Tests)

### CAT-001: Category List View (6 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| CAT-001-01 | View categories page | 🔴 P0 | Logged as Admin | Navigate to `/portal/ecommerce/categories` | Tree view displayed with root categories | ⬜ |
| CAT-001-02 | Toggle tree view | 🟠 P1 | Categories exist | 1. Click expand arrow on parent<br>2. Click collapse | Children show/hide correctly | ⬜ |
| CAT-001-03 | Switch to table view | 🟠 P1 | Categories exist | Click "Table View" toggle | Flat list with parent name column | ⬜ |
| CAT-001-04 | Search categories | 🟠 P1 | Multiple categories | Type category name in search | Matching categories highlighted | ⬜ |
| CAT-001-05 | View product count | 🟠 P1 | Category has products | Check product count badge | Accurate count displayed | ⬜ |
| CAT-001-06 | Empty state | 🟡 P2 | No categories | Navigate to categories | "No categories found" message | ⬜ |

### CAT-002: Create Category (10 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| CAT-002-01 | Create root category | 🔴 P0 | Admin logged in | 1. Click "Create Category"<br>2. Enter name: "Electronics"<br>3. Enter slug: "electronics"<br>4. Save | Category created at root level | ⬜ |
| CAT-002-02 | Create subcategory | 🔴 P0 | Parent exists | 1. Click Add Subcategory on parent<br>2. Enter name: "Laptops"<br>3. Slug auto-generates | Subcategory under parent | ⬜ |
| CAT-002-03 | Auto-generate slug | 🟠 P1 | Form open | 1. Enter name: "Smart Phones"<br>2. Tab out of name field | Slug auto-fills: "smart-phones" | ⬜ |
| CAT-002-04 | Custom slug | 🟠 P1 | Form open | 1. Enter name<br>2. Manually change slug to "custom-slug" | Custom slug preserved | ⬜ |
| CAT-002-05 | Add description | 🟠 P1 | Form open | 1. Fill required fields<br>2. Add description text<br>3. Save | Description saved | ⬜ |
| CAT-002-06 | Add SEO meta title | 🟠 P1 | Form open | 1. Fill required<br>2. Add meta title<br>3. Save | Meta title saved | ⬜ |
| CAT-002-07 | Add SEO meta description | 🟠 P1 | Form open | 1. Fill required<br>2. Add meta description<br>3. Save | Meta description saved | ⬜ |
| CAT-002-08 | Upload category image | 🟠 P1 | Form open | 1. Click image upload<br>2. Select image<br>3. Save | Image URL saved, thumbnail shown | ⬜ |
| CAT-002-09 | Set sort order | 🟡 P2 | Form open | 1. Set sort order: 5<br>2. Save<br>3. Verify position | Category sorted correctly | ⬜ |
| CAT-002-10 | Multi-level hierarchy | 🟠 P1 | Parent exists | Create: Root > Child > Grandchild | 3-level hierarchy works | ⬜ |

### CAT-003: Category Validation (6 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| CAT-003-01 | Empty name validation | 🟠 P1 | Form open | Leave name empty, click Save | Error: "Name is required" | ⬜ |
| CAT-003-02 | Empty slug validation | 🟠 P1 | Form open | Clear auto-generated slug, Save | Error: "Slug is required" | ⬜ |
| CAT-003-03 | Invalid slug format | 🟠 P1 | Form open | Enter slug: "Invalid Slug!" | Error: "Invalid slug format" | ⬜ |
| CAT-003-04 | Duplicate slug | 🟠 P1 | Category with slug exists | Enter existing slug | Error: "Slug already exists" | ⬜ |
| CAT-003-05 | Name max length | 🟡 P2 | Form open | Enter 250+ characters | Error or truncation | ⬜ |
| CAT-003-06 | Slug max length | 🟡 P2 | Form open | Enter 250+ char slug | Error or truncation | ⬜ |

### CAT-004: Update Category (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| CAT-004-01 | Update category name | 🔴 P0 | Category exists | 1. Click Edit<br>2. Change name<br>3. Save | Name updated | ⬜ |
| CAT-004-02 | Change parent category | 🟠 P1 | Multiple categories | 1. Edit category<br>2. Select new parent<br>3. Save | Category moved in hierarchy | ⬜ |
| CAT-004-03 | Remove parent (make root) | 🟠 P1 | Subcategory exists | 1. Edit subcategory<br>2. Clear parent<br>3. Save | Category becomes root | ⬜ |
| CAT-004-04 | Update all fields | 🟠 P1 | Category exists | Update name, slug, description, meta, image, sort order | All fields updated | ⬜ |

### CAT-005: Delete Category (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| CAT-005-01 | Delete empty category | 🟠 P1 | Category with no products | 1. Click Delete<br>2. Confirm | Category deleted | ⬜ |
| CAT-005-02 | Delete blocked - has products | 🔴 P0 | Category has products | Try to delete | Error: "Cannot delete category with products" | ⬜ |
| CAT-005-03 | Delete blocked - has children | 🟠 P1 | Category has subcategories | Try to delete | Error: "Delete children first" | ⬜ |
| CAT-005-04 | Cancel delete | 🟡 P2 | Category exists | 1. Click Delete<br>2. Click Cancel | Category remains | ⬜ |

---

## 2. Product Attributes (45 Tests)

### ATTR-001: Attribute List View (5 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| ATTR-001-01 | View attributes page | 🔴 P0 | Logged as Admin | Navigate to `/portal/ecommerce/attributes` | Attributes table displayed | ⬜ |
| ATTR-001-02 | Search attributes | 🟠 P1 | Attributes exist | Type attribute name | Filtered results | ⬜ |
| ATTR-001-03 | Filter by type | 🟠 P1 | Various types exist | Select type filter | Only that type shown | ⬜ |
| ATTR-001-04 | View attribute details | 🟠 P1 | Attribute exists | Click attribute row | Details panel or dialog | ⬜ |
| ATTR-001-05 | View usage count | 🟠 P1 | Attribute assigned | Check usage badge | Shows category count | ⬜ |

### ATTR-002: Create Attribute - All 13 Types (26 tests)

#### Text-Based Attributes (6 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| ATTR-002-01 | Create Text attribute | 🔴 P0 | Admin logged in | 1. Click Create<br>2. Code: "material"<br>3. Name: "Material"<br>4. Type: Text<br>5. Save | Text attribute created | ⬜ |
| ATTR-002-02 | Text with max length | 🟠 P1 | Form open | Set maxLength: 100 | Validates length in products | ⬜ |
| ATTR-002-03 | Text with regex validation | 🟠 P1 | Form open | Set regex: `^[A-Z]{3}$` | Validates pattern | ⬜ |
| ATTR-002-04 | Create TextArea attribute | 🟠 P1 | Form open | Type: TextArea, maxLength: 500 | Multi-line input created | ⬜ |
| ATTR-002-05 | Create URL attribute | 🟠 P1 | Form open | Type: URL | URL validation enabled | ⬜ |
| ATTR-002-06 | Create File attribute | 🟠 P1 | Form open | Type: File | File upload field created | ⬜ |

#### Numeric Attributes (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| ATTR-002-07 | Create Number attribute | 🔴 P0 | Form open | Type: Number, min: 0, max: 100, unit: "kg" | Integer input with unit | ⬜ |
| ATTR-002-08 | Create Decimal attribute | 🟠 P1 | Form open | Type: Decimal, min: 0.0, max: 999.99 | Decimal input created | ⬜ |
| ATTR-002-09 | Create Range attribute | 🟠 P1 | Form open | Type: Range, min: 0, max: 100 | Slider component in products | ⬜ |
| ATTR-002-10 | Number with unit display | 🟠 P1 | Number attr exists | Set unit: "cm" | Displays "15 cm" format | ⬜ |

#### Selection Attributes (6 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| ATTR-002-11 | Create Select attribute | 🔴 P0 | Form open | 1. Type: Select<br>2. Add values: Red, Blue, Green<br>3. Save | Single-select dropdown | ⬜ |
| ATTR-002-12 | Select with color codes | 🟠 P1 | Form open | Add value with colorCode: #FF0000 | Color swatch in dropdown | ⬜ |
| ATTR-002-13 | Select with swatch images | 🟡 P2 | Form open | Add value with swatchUrl | Image swatch displayed | ⬜ |
| ATTR-002-14 | Create MultiSelect attribute | 🔴 P0 | Form open | Type: MultiSelect, add 5 values | Checkbox list in products | ⬜ |
| ATTR-002-15 | MultiSelect with colors | 🟠 P1 | Form open | Add values with color codes | Color badges displayed | ⬜ |
| ATTR-002-16 | Reorder selection values | 🟡 P2 | Values exist | Drag values to reorder | Order persisted | ⬜ |

#### Date/Time Attributes (3 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| ATTR-002-17 | Create Date attribute | 🟠 P1 | Form open | Type: Date | Date picker in products | ⬜ |
| ATTR-002-18 | Create DateTime attribute | 🟠 P1 | Form open | Type: DateTime | DateTime picker | ⬜ |
| ATTR-002-19 | Date with default value | 🟡 P2 | Form open | Set defaultValue: today | Pre-fills current date | ⬜ |

#### Other Types (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| ATTR-002-20 | Create Boolean attribute | 🟠 P1 | Form open | Type: Boolean, name: "Is Featured" | Toggle switch in products | ⬜ |
| ATTR-002-21 | Create Color attribute | 🟠 P1 | Form open | Type: Color | Color picker in products | ⬜ |
| ATTR-002-22 | Color with hex validation | 🟠 P1 | Color attr exists | Enter invalid hex | Validation error | ⬜ |
| ATTR-002-23 | All attribute options | 🟠 P1 | Form open | Set: isFilterable, isSearchable, showInCard, showInSpecs, isRequired | All options saved | ⬜ |

#### Attribute Configuration (3 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| ATTR-002-24 | Set as variant attribute | 🟠 P1 | Form open | Enable isVariantAttribute | Attribute per variant | ⬜ |
| ATTR-002-25 | Set as filterable | 🔴 P0 | Form open | Enable isFilterable | Appears in product filters | ⬜ |
| ATTR-002-26 | Set as required | 🟠 P1 | Form open | Enable isRequired | Mandatory in product form | ⬜ |

### ATTR-003: Attribute Validation (6 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| ATTR-003-01 | Empty code validation | 🟠 P1 | Form open | Leave code empty | Error: "Code is required" | ⬜ |
| ATTR-003-02 | Duplicate code | 🟠 P1 | Attribute exists | Enter existing code | Error: "Code already exists" | ⬜ |
| ATTR-003-03 | Invalid code format | 🟠 P1 | Form open | Code: "Invalid Code!" | Error: "Invalid code format" | ⬜ |
| ATTR-003-04 | Empty name validation | 🟠 P1 | Form open | Leave name empty | Error: "Name is required" | ⬜ |
| ATTR-003-05 | Select without values | 🟠 P1 | Type: Select | Save without adding values | Error: "Add at least one value" | ⬜ |
| ATTR-003-06 | Range invalid min/max | 🟡 P2 | Type: Range | Set min > max | Error: "Min must be less than max" | ⬜ |

### ATTR-004: Update & Delete Attribute (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| ATTR-004-01 | Update attribute name | 🟠 P1 | Attribute exists | Change name, save | Name updated | ⬜ |
| ATTR-004-02 | Add values to existing | 🟠 P1 | Select attr exists | Add new value | Value added | ⬜ |
| ATTR-004-03 | Delete unused attribute | 🟠 P1 | Unassigned attribute | Delete | Attribute deleted | ⬜ |
| ATTR-004-04 | Delete blocked - in use | 🟠 P1 | Assigned to category | Try to delete | Error: "Attribute in use" | ⬜ |

### ATTR-005: Category Attribute Assignment (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| ATTR-005-01 | Assign attribute to category | 🔴 P0 | Both exist | 1. Open category<br>2. Click Manage Attributes<br>3. Select attributes<br>4. Save | Attributes linked | ⬜ |
| ATTR-005-02 | Mark as required | 🟠 P1 | Assignment exists | Toggle "Required" checkbox | Required in product form | ⬜ |
| ATTR-005-03 | Set attribute order | 🟠 P1 | Multiple assigned | Drag to reorder | Order persisted | ⬜ |
| ATTR-005-04 | Unassign attribute | 🟠 P1 | Assignment exists | Uncheck attribute, save | Attribute removed | ⬜ |

---

## 3. Brands (12 Tests)

### BRAND-001: Brand CRUD (12 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| BRAND-001-01 | View brands page | 🔴 P0 | Logged as Admin | Navigate to `/portal/ecommerce/brands` | Brands table displayed | ⬜ |
| BRAND-001-02 | Create brand | 🔴 P0 | Admin logged in | 1. Click Create<br>2. Name: "Apple"<br>3. Save | Brand created | ⬜ |
| BRAND-001-03 | Create with description | 🟠 P1 | Form open | Add description, save | Description saved | ⬜ |
| BRAND-001-04 | Create with logo | 🟠 P1 | Form open | Upload logo image | Logo displayed | ⬜ |
| BRAND-001-05 | Empty name validation | 🟠 P1 | Form open | Leave name empty, save | Error: "Name is required" | ⬜ |
| BRAND-001-06 | Duplicate name | 🟠 P1 | Brand exists | Enter existing name | Error: "Brand already exists" | ⬜ |
| BRAND-001-07 | Search brands | 🟠 P1 | Brands exist | Type brand name | Filtered results | ⬜ |
| BRAND-001-08 | Update brand | 🟠 P1 | Brand exists | Change name, save | Name updated | ⬜ |
| BRAND-001-09 | Toggle brand status | 🟠 P1 | Brand exists | Toggle Active/Inactive | Status changes | ⬜ |
| BRAND-001-10 | Delete unused brand | 🟠 P1 | Brand not used | Delete | Brand deleted | ⬜ |
| BRAND-001-11 | Delete blocked - in use | 🟠 P1 | Brand has products | Try to delete | Error: "Brand in use" | ⬜ |
| BRAND-001-12 | Filter active brands only | 🟡 P2 | Mixed status | Filter by active | Only active shown | ⬜ |

---

## 4. Product CRUD (48 Tests)

### PROD-001: Product List View (12 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| PROD-001-01 | View products page | 🔴 P0 | Logged as Admin | Navigate to `/portal/ecommerce/products` | Products displayed with stats | ⬜ |
| PROD-001-02 | View stats dashboard | 🔴 P0 | Products exist | Check stats cards | Total, Active, Draft, Archived, Out of Stock, Low Stock counts | ⬜ |
| PROD-001-03 | Toggle grid view | 🟠 P1 | Table view active | Click Grid View | Product cards displayed | ⬜ |
| PROD-001-04 | Toggle table view | 🟠 P1 | Grid view active | Click Table View | Table with columns | ⬜ |
| PROD-001-05 | Table columns visible | 🟠 P1 | Table view | Check all columns | Image, Name, SKU, Status, Price, Stock, Actions | ⬜ |
| PROD-001-06 | Grid card content | 🟠 P1 | Grid view | Check card | Image, Name, Price, Status badge, Stock | ⬜ |
| PROD-001-07 | Low stock badge | 🟠 P1 | Low stock product | Check product row | Orange "Low Stock" badge | ⬜ |
| PROD-001-08 | Out of stock badge | 🟠 P1 | Zero stock product | Check product row | Red "Out of Stock" badge | ⬜ |
| PROD-001-09 | Attribute badges | 🟠 P1 | Product with showInCard attr | Check product card | Attribute badge visible | ⬜ |
| PROD-001-10 | Click to view details | 🔴 P0 | Products exist | Click product row | Navigate to product detail | ⬜ |
| PROD-001-11 | Actions menu | 🟠 P1 | Product exists | Click actions menu | Edit, Publish/Archive, Duplicate, Delete options | ⬜ |
| PROD-001-12 | Empty state | 🟡 P2 | No products | Navigate to products | "No products found" with Create button | ⬜ |

### PROD-002: Create Product - Basic (12 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| PROD-002-01 | Navigate to create form | 🔴 P0 | Admin logged in | Click "Create Product" | Form page loads | ⬜ |
| PROD-002-02 | Create minimal product | 🔴 P0 | Form open | 1. Name: "Test Product"<br>2. Slug: "test-product"<br>3. Base Price: 100000<br>4. Save as Draft | Product created in Draft status | ⬜ |
| PROD-002-03 | Auto-generate slug | 🟠 P1 | Form open | Enter name, tab out | Slug auto-fills from name | ⬜ |
| PROD-002-04 | Custom slug | 🟠 P1 | Form open | Manually enter slug | Custom slug preserved | ⬜ |
| PROD-002-05 | Add short description | 🟠 P1 | Form open | Enter short desc (max 300 chars) | Saved and displayed | ⬜ |
| PROD-002-06 | Add full description (rich text) | 🟠 P1 | Form open | Use TinyMCE editor with formatting | HTML saved correctly | ⬜ |
| PROD-002-07 | Select category | 🔴 P0 | Categories exist | Select category from dropdown | Category assigned | ⬜ |
| PROD-002-08 | Select brand | 🟠 P1 | Brands exist | Select brand from dropdown | Brand assigned | ⬜ |
| PROD-002-09 | Enter SKU | 🟠 P1 | Form open | Enter SKU: "SKU-12345" | SKU saved | ⬜ |
| PROD-002-10 | Enter barcode | 🟡 P2 | Form open | Enter barcode | Barcode saved | ⬜ |
| PROD-002-11 | Toggle track inventory | 🟠 P1 | Form open | Toggle off track inventory | Inventory tracking disabled | ⬜ |
| PROD-002-12 | Add SEO meta fields | 🟠 P1 | Form open | Enter meta title & description | SEO fields saved | ⬜ |

### PROD-003: Create Product - With Attributes (8 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| PROD-003-01 | Attributes appear by category | 🔴 P0 | Category with attrs | Select category | Category attributes show | ⬜ |
| PROD-003-02 | Fill Text attribute | 🟠 P1 | Text attr assigned | Enter text value | Value saved | ⬜ |
| PROD-003-03 | Fill Number attribute | 🟠 P1 | Number attr assigned | Enter number with unit | Value with unit saved | ⬜ |
| PROD-003-04 | Fill Select attribute | 🟠 P1 | Select attr assigned | Choose from dropdown | Selection saved | ⬜ |
| PROD-003-05 | Fill MultiSelect attribute | 🟠 P1 | MultiSelect assigned | Check multiple values | All selections saved | ⬜ |
| PROD-003-06 | Fill Color attribute | 🟠 P1 | Color attr assigned | Use color picker | Hex code saved | ⬜ |
| PROD-003-07 | Fill Boolean attribute | 🟠 P1 | Boolean attr assigned | Toggle switch | Boolean saved | ⬜ |
| PROD-003-08 | Required attribute validation | 🔴 P0 | Required attr | Leave required empty | Error on save | ⬜ |

### PROD-004: Product Validation (10 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| PROD-004-01 | Empty name validation | 🔴 P0 | Form open | Leave name empty, save | Error: "Name is required" | ⬜ |
| PROD-004-02 | Empty slug validation | 🟠 P1 | Form open | Clear slug, save | Error: "Slug is required" | ⬜ |
| PROD-004-03 | Invalid slug format | 🟠 P1 | Form open | Slug: "Invalid Slug!" | Error: regex validation | ⬜ |
| PROD-004-04 | Duplicate slug | 🟠 P1 | Product with slug exists | Enter existing slug | Error: "Slug already exists" | ⬜ |
| PROD-004-05 | Negative price | 🟠 P1 | Form open | Enter -100 | Error: "Price must be positive" | ⬜ |
| PROD-004-06 | Name max length | 🟡 P2 | Form open | Enter 201+ characters | Error or truncation | ⬜ |
| PROD-004-07 | Short desc max length | 🟡 P2 | Form open | Enter 301+ characters | Error: "Max 300 characters" | ⬜ |
| PROD-004-08 | Duplicate SKU | 🟠 P1 | Product with SKU exists | Enter existing SKU | Error: "SKU already exists" | ⬜ |
| PROD-004-09 | Negative stock | 🟠 P1 | Form open | Enter -10 for stock | Error: "Stock cannot be negative" | ⬜ |
| PROD-004-10 | Form validation mode onBlur | 🟠 P1 | Form open | Fill invalid, tab out | Error shows on blur | ⬜ |

### PROD-005: Update Product (6 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| PROD-005-01 | Navigate to edit | 🔴 P0 | Product exists | Click Edit in actions | Form loads with data | ⬜ |
| PROD-005-02 | Update basic info | 🔴 P0 | Edit form open | Change name, price, save | Changes persisted | ⬜ |
| PROD-005-03 | Update description | 🟠 P1 | Edit form open | Modify rich text, save | HTML updated | ⬜ |
| PROD-005-04 | Change category | 🟠 P1 | Edit form open | Select different category | Category changed, attrs refresh | ⬜ |
| PROD-005-05 | Update attribute values | 🟠 P1 | Product with attrs | Change attribute values, save | New values saved | ⬜ |
| PROD-005-06 | Concurrent edit warning | 🟡 P2 | Same product open in 2 tabs | Edit in both, save | Warning or conflict handling | ⬜ |

---

## 5. Product Variants (28 Tests)

### VAR-001: Add Variants (10 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| VAR-001-01 | Add single option | 🔴 P0 | Product form open | 1. Click Add Option<br>2. Name: "Size"<br>3. Values: S, M, L | Option added with 3 values | ⬜ |
| VAR-001-02 | Add multiple options | 🟠 P1 | Form open | Add Size (S, M) and Color (Red, Blue) | Two options added | ⬜ |
| VAR-001-03 | Generate variants | 🔴 P0 | Options added | Click "Generate Variants" | Creates S-Red, S-Blue, M-Red, M-Blue | ⬜ |
| VAR-001-04 | Variant matrix display | 🟠 P1 | Variants generated | Check variant list | All combinations shown | ⬜ |
| VAR-001-05 | Set variant prices | 🔴 P0 | Variants exist | Enter different prices | Each variant has own price | ⬜ |
| VAR-001-06 | Set variant SKUs | 🟠 P1 | Variants exist | Enter unique SKUs | Each variant has own SKU | ⬜ |
| VAR-001-07 | Set compare at price | 🟠 P1 | Variant exists | Enter compare price > price | Shows strikethrough in store | ⬜ |
| VAR-001-08 | Set variant stock | 🔴 P0 | Variants exist | Enter stock quantities | Total stock calculated | ⬜ |
| VAR-001-09 | Add variant manually | 🟠 P1 | Product exists | Add variant without generating | Single variant added | ⬜ |
| VAR-001-10 | Variant sort order | 🟡 P2 | Variants exist | Set sort order | Display order changes | ⬜ |

### VAR-002: Edit Variants (8 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| VAR-002-01 | Update variant name | 🟠 P1 | Variant exists | Change name, save | Name updated | ⬜ |
| VAR-002-02 | Update variant price | 🔴 P0 | Variant exists | Change price, save | Price updated | ⬜ |
| VAR-002-03 | Update variant stock | 🔴 P0 | Variant exists | Change stock, save | Stock updated, total recalculated | ⬜ |
| VAR-002-04 | Bulk update prices | 🟠 P1 | Multiple variants | Apply same price to all | All prices updated | ⬜ |
| VAR-002-05 | Bulk update stock | 🟠 P1 | Multiple variants | Increment all by 10 | All stock updated | ⬜ |
| VAR-002-06 | Inline edit variant | 🟠 P1 | Variant row | Click cell, edit inline | Changes saved | ⬜ |
| VAR-002-07 | Variant validation | 🟠 P1 | Edit form | Enter negative price | Error shown | ⬜ |
| VAR-002-08 | Duplicate SKU validation | 🟠 P1 | Edit form | Enter existing SKU | Error: "SKU exists" | ⬜ |

### VAR-003: Delete Variants (5 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| VAR-003-01 | Delete single variant | 🟠 P1 | Multiple variants | Delete one variant | Variant removed, total updated | ⬜ |
| VAR-003-02 | Delete last variant blocked | 🟠 P1 | Single variant | Try to delete | Error: "At least one variant required" | ⬜ |
| VAR-003-03 | Delete variant with orders | 🟡 P2 | Variant in orders | Try to delete | Soft delete or warning | ⬜ |
| VAR-003-04 | Delete option value | 🟠 P1 | Option with values | Delete one value | Related variants removed | ⬜ |
| VAR-003-05 | Delete entire option | 🟠 P1 | Option exists | Delete option | All related variants removed | ⬜ |

### VAR-004: Variant Attributes (5 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| VAR-004-01 | Variant-level attribute display | 🟠 P1 | isVariantAttribute = true | Open variant edit | Attribute field per variant | ⬜ |
| VAR-004-02 | Set variant attribute value | 🟠 P1 | Variant attr exists | Enter value for variant | Saved per variant | ⬜ |
| VAR-004-03 | Different values per variant | 🟠 P1 | Multiple variants | Set different values | Each variant has own value | ⬜ |
| VAR-004-04 | Variant attribute in filters | 🟠 P1 | Filterable variant attr | Use filter | Filters by variant values | ⬜ |
| VAR-004-05 | Variant attribute in specs | 🟡 P2 | showInSpecs variant attr | View product | Shows variant-specific value | ⬜ |

---

## 6. Product Images (22 Tests)

### IMG-001: Upload Images (8 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| IMG-001-01 | Upload single image (click) | 🔴 P0 | Form open | 1. Click upload zone<br>2. Select image<br>3. Wait for upload | Thumbnail displayed | ⬜ |
| IMG-001-02 | Upload via drag-drop | 🔴 P0 | Form open | Drag image file to zone | Upload starts, thumbnail shown | ⬜ |
| IMG-001-03 | Upload multiple images | 🟠 P1 | Form open | Select/drag 5 images | All 5 uploaded | ⬜ |
| IMG-001-04 | Upload progress indicator | 🟠 P1 | Upload in progress | Watch during upload | Progress bar/spinner | ⬜ |
| IMG-001-05 | Image optimization | 🟠 P1 | Image uploaded | Check response | ThumbUrl, MediumUrl, LargeUrl generated | ⬜ |
| IMG-001-06 | ThumbHash generation | 🟡 P2 | Image uploaded | Check response | ThumbHash and dominant color | ⬜ |
| IMG-001-07 | Invalid file type | 🟠 P1 | Form open | Upload .pdf file | Error: "Invalid file type" | ⬜ |
| IMG-001-08 | File size limit | 🟠 P1 | Form open | Upload 50MB image | Error: "File too large" | ⬜ |

### IMG-002: Manage Images (8 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| IMG-002-01 | Set primary image | 🔴 P0 | Multiple images | Click "Set as Primary" on image 2 | Image 2 marked primary | ⬜ |
| IMG-002-02 | Only one primary | 🟠 P1 | Primary exists | Set different image as primary | Old primary unmarked | ⬜ |
| IMG-002-03 | Edit alt text | 🟠 P1 | Image exists | Click edit, enter alt text | Alt text saved (SEO) | ⬜ |
| IMG-002-04 | Reorder by drag-drop | 🔴 P0 | Multiple images | Drag image 3 to position 1 | Order changes immediately | ⬜ |
| IMG-002-05 | Reorder persists | 🟠 P1 | Reordered images | Save and reload | Order preserved | ⬜ |
| IMG-002-06 | Delete image | 🟠 P1 | Image exists | Click delete on image | Image removed | ⬜ |
| IMG-002-07 | Delete primary reassigns | 🟠 P1 | Delete primary image | Delete | Next image becomes primary | ⬜ |
| IMG-002-08 | View image fullsize | 🟡 P2 | Image exists | Click thumbnail | Lightbox or full view | ⬜ |

### IMG-003: Image Display (6 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| IMG-003-01 | Primary in product list | 🔴 P0 | Product with images | View product list | Primary image thumbnail | ⬜ |
| IMG-003-02 | Placeholder for no image | 🟠 P1 | Product without images | View product list | Placeholder image | ⬜ |
| IMG-003-03 | Gallery in product detail | 🟠 P1 | Multiple images | View product detail | Image gallery with thumbnails | ⬜ |
| IMG-003-04 | Gallery navigation | 🟠 P1 | Gallery visible | Click thumbnails/arrows | Main image changes | ⬜ |
| IMG-003-05 | Responsive images | 🟠 P1 | Product detail | Resize viewport | Appropriate size loaded | ⬜ |
| IMG-003-06 | Lazy loading | 🟡 P2 | Many products | Scroll list | Images load on viewport | ⬜ |

---

## 7. Product Options (14 Tests)

### OPT-001: Create Options (6 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| OPT-001-01 | Add first option | 🔴 P0 | Product form | Click "Add Option", name: "Size" | Option section appears | ⬜ |
| OPT-001-02 | Add option values | 🔴 P0 | Option exists | Add: Small, Medium, Large | 3 values listed | ⬜ |
| OPT-001-03 | Add option with colors | 🟠 P1 | Option: "Color" | Add values with color codes | Color swatches displayed | ⬜ |
| OPT-001-04 | Multiple options | 🟠 P1 | One option exists | Add second option: "Color" | Two option sections | ⬜ |
| OPT-001-05 | Option value display name | 🟠 P1 | Adding value | Set value: "sm", display: "Small" | Display name shown | ⬜ |
| OPT-001-06 | Option value sort order | 🟡 P2 | Multiple values | Set sort orders | Values ordered correctly | ⬜ |

### OPT-002: Edit Options (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| OPT-002-01 | Rename option | 🟠 P1 | Option exists | Change name to "Dimensions" | Name updated | ⬜ |
| OPT-002-02 | Add value to existing | 🟠 P1 | Option with values | Add new value: "XL" | Value added | ⬜ |
| OPT-002-03 | Edit value | 🟠 P1 | Value exists | Change "Large" to "L" | Value updated | ⬜ |
| OPT-002-04 | Reorder values | 🟠 P1 | Multiple values | Drag to reorder | Order saved | ⬜ |

### OPT-003: Delete Options (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| OPT-003-01 | Delete option value | 🟠 P1 | Value not in variants | Delete value | Value removed | ⬜ |
| OPT-003-02 | Delete value with variants | 🟠 P1 | Value used in variants | Delete value | Warning: affects N variants | ⬜ |
| OPT-003-03 | Delete entire option | 🟠 P1 | Option exists | Delete option | Option and variants removed | ⬜ |
| OPT-003-04 | Cannot delete last option | 🟡 P2 | Only one option | Try to delete | Blocked or warning | ⬜ |

---

## 8. Product Filters & Search (20 Tests)

### FILTER-001: Search (6 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| FILTER-001-01 | Search by product name | 🔴 P0 | Products exist | Type "laptop" in search | Products with "laptop" shown | ⬜ |
| FILTER-001-02 | Search by SKU | 🔴 P0 | Products with SKU | Search exact SKU | Matching product | ⬜ |
| FILTER-001-03 | Search partial match | 🟠 P1 | Products exist | Search "lap" | "Laptop", "Overlap" shown | ⬜ |
| FILTER-001-04 | Search case insensitive | 🟠 P1 | Products exist | Search "LAPTOP" | Same as lowercase | ⬜ |
| FILTER-001-05 | Search debounce | 🟠 P1 | Search field | Type quickly | Only searches after pause | ⬜ |
| FILTER-001-06 | Clear search | 🟠 P1 | Search applied | Click clear or delete text | All products shown | ⬜ |

### FILTER-002: Status Filter (5 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| FILTER-002-01 | Filter by Active | 🔴 P0 | Mixed statuses | Select "Active" | Only active products | ⬜ |
| FILTER-002-02 | Filter by Draft | 🟠 P1 | Draft products | Select "Draft" | Only draft products | ⬜ |
| FILTER-002-03 | Filter by Archived | 🟠 P1 | Archived products | Select "Archived" | Only archived products | ⬜ |
| FILTER-002-04 | Filter by Out of Stock | 🟠 P1 | Zero stock products | Select "Out of Stock" | Only zero stock | ⬜ |
| FILTER-002-05 | Show All statuses | 🟠 P1 | Filter applied | Select "All" | All products shown | ⬜ |

### FILTER-003: Category & Brand Filter (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| FILTER-003-01 | Filter by category | 🔴 P0 | Products in categories | Select "Electronics" | Only electronics | ⬜ |
| FILTER-003-02 | Filter by subcategory | 🟠 P1 | Hierarchical categories | Select "Laptops" | Only laptops | ⬜ |
| FILTER-003-03 | Filter by brand | 🟠 P1 | Products with brands | Select "Apple" | Only Apple products | ⬜ |
| FILTER-003-04 | Combined category + brand | 🟠 P1 | Various products | Category: Electronics, Brand: Apple | Apple electronics only | ⬜ |

### FILTER-004: Attribute Filters (5 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| FILTER-004-01 | Single attribute filter | 🔴 P0 | Filterable attribute | Select Color: Red | Only red products | ⬜ |
| FILTER-004-02 | Multi-select within attribute | 🟠 P1 | Select attribute | Check Red AND Blue | Red OR Blue products | ⬜ |
| FILTER-004-03 | Multiple attribute filters | 🟠 P1 | Multiple attrs | Color: Red, Size: Large | Red AND Large | ⬜ |
| FILTER-004-04 | Color swatches in filter | 🟠 P1 | Color attribute | Check filter UI | Color swatches displayed | ⬜ |
| FILTER-004-05 | Clear attribute filters | 🟠 P1 | Attrs applied | Click Clear Filters | All filters reset | ⬜ |

---

## 9. Bulk Operations (16 Tests)

### BULK-001: Selection (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| BULK-001-01 | Select single product | 🔴 P0 | Products exist | Click checkbox | Product selected, count: 1 | ⬜ |
| BULK-001-02 | Select multiple | 🟠 P1 | Products exist | Click 5 checkboxes | Count: 5 selected | ⬜ |
| BULK-001-03 | Select all on page | 🟠 P1 | Products exist | Click "Select All" | All visible selected | ⬜ |
| BULK-001-04 | Deselect all | 🟠 P1 | Selected products | Click "Deselect All" | None selected | ⬜ |

### BULK-002: Bulk Publish (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| BULK-002-01 | Bulk publish drafts | 🔴 P0 | Multiple drafts selected | Bulk Actions > Publish | All become Active | ⬜ |
| BULK-002-02 | Bulk publish mixed | 🟠 P1 | Drafts + Active selected | Bulk Publish | Only drafts change | ⬜ |
| BULK-002-03 | Publish with errors | 🟠 P1 | Draft missing required | Bulk Publish | Error report: which failed | ⬜ |
| BULK-002-04 | Publish count in result | 🟠 P1 | Bulk publish | Complete operation | "5 published, 2 failed" | ⬜ |

### BULK-003: Bulk Archive (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| BULK-003-01 | Bulk archive active | 🔴 P0 | Active products selected | Bulk Actions > Archive | All become Archived | ⬜ |
| BULK-003-02 | Bulk archive mixed | 🟠 P1 | Active + Draft selected | Bulk Archive | Only active archived | ⬜ |
| BULK-003-03 | Confirm dialog | 🟠 P1 | Products selected | Click Bulk Archive | Confirmation required | ⬜ |
| BULK-003-04 | Cancel bulk archive | 🟠 P1 | Confirm dialog | Click Cancel | No changes | ⬜ |

### BULK-004: Bulk Delete (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| BULK-004-01 | Bulk delete | 🔴 P0 | Products selected | Bulk Actions > Delete | All soft-deleted | ⬜ |
| BULK-004-02 | Confirm delete dialog | 🟠 P1 | Products selected | Click Bulk Delete | Confirm required | ⬜ |
| BULK-004-03 | Delete result message | 🟠 P1 | Bulk delete | Complete | "5 products deleted" | ⬜ |
| BULK-004-04 | Products removed from list | 🟠 P1 | Bulk delete | After delete | Products no longer visible | ⬜ |

---

## 10. Import/Export (18 Tests)

### IMPORT-001: CSV Import (10 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| IMPORT-001-01 | Import valid CSV | 🔴 P0 | Valid CSV file | Upload CSV | Products created | ⬜ |
| IMPORT-001-02 | Import with variants | 🟠 P1 | CSV with variants | Upload | Products with variants | ⬜ |
| IMPORT-001-03 | Import with attributes | 🟠 P1 | CSV with attr columns | Upload | Attribute values set | ⬜ |
| IMPORT-001-04 | Import with images | 🟠 P1 | CSV with image URLs | Upload | Images downloaded/linked | ⬜ |
| IMPORT-001-05 | Import progress | 🟠 P1 | Large CSV | Upload | Progress indicator | ⬜ |
| IMPORT-001-06 | Import result summary | 🟠 P1 | CSV imported | Complete | "50 created, 5 failed" | ⬜ |
| IMPORT-001-07 | Import error details | 🟠 P1 | CSV with errors | Complete | Line-by-line errors | ⬜ |
| IMPORT-001-08 | Import limit (1000) | 🟠 P1 | CSV with 1500 rows | Upload | Error: "Max 1000 products" | ⬜ |
| IMPORT-001-09 | Invalid CSV format | 🟠 P1 | Malformed CSV | Upload | Error: "Invalid format" | ⬜ |
| IMPORT-001-10 | Download template | 🟠 P1 | Import dialog | Click Download Template | CSV template downloaded | ⬜ |

### EXPORT-001: CSV Export (8 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| EXPORT-001-01 | Export all products | 🔴 P0 | Products exist | Click Export | CSV downloaded | ⬜ |
| EXPORT-001-02 | Export with filters | 🟠 P1 | Filter applied | Export | Only filtered products | ⬜ |
| EXPORT-001-03 | Export includes variants | 🟠 P1 | Products with variants | Export | Row per variant | ⬜ |
| EXPORT-001-04 | Export includes images | 🟠 P1 | Products with images | Export with images | Image URLs in column | ⬜ |
| EXPORT-001-05 | Export includes attributes | 🟠 P1 | Products with attrs | Export with attrs | attr_code columns | ⬜ |
| EXPORT-001-06 | Large export progress | 🟠 P1 | 1000+ products | Export | Progress indicator | ⬜ |
| EXPORT-001-07 | Export filename | 🟡 P2 | Export | Download | products_YYYY-MM-DD.csv | ⬜ |
| EXPORT-001-08 | Roundtrip: Export → Import | 🔴 P0 | Export completed | Re-import exported CSV | No data loss | ⬜ |

---

## 11. Integration Scenarios (14 Tests)

### INT-001: Category-Attribute-Product Flow (6 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| INT-001-01 | Full setup flow | 🔴 P0 | Empty system | 1. Create Category<br>2. Create Attributes<br>3. Assign attrs to category<br>4. Create Product in category | Product with category attrs | ⬜ |
| INT-001-02 | Change category updates attrs | 🟠 P1 | Product in Cat A | Change to Cat B | Cat B attributes shown | ⬜ |
| INT-001-03 | Required attr blocks save | 🟠 P1 | Required attr not filled | Save product | Validation error | ⬜ |
| INT-001-04 | Filter by category attr | 🟠 P1 | Products with attrs | Filter by attr value | Correct products shown | ⬜ |
| INT-001-05 | Delete category with products | 🟠 P1 | Category has products | Try to delete category | Blocked with message | ⬜ |
| INT-001-06 | Delete attr updates products | 🟠 P1 | Attr used by products | Delete attribute | Attr values removed | ⬜ |

### INT-002: Product Lifecycle Flow (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| INT-002-01 | Draft → Publish → Active | 🔴 P0 | New product | Create as Draft, then Publish | Status: Active | ⬜ |
| INT-002-02 | Active → Archive → Restore | 🟠 P1 | Active product | Archive, then Restore | Back to Active | ⬜ |
| INT-002-03 | Out of stock auto-status | 🟠 P1 | Product with stock | Set all variants to 0 | Status: Out of Stock | ⬜ |
| INT-002-04 | Duplicate preserves relations | 🟠 P1 | Product with everything | Duplicate | Images, variants, attrs copied | ⬜ |

### INT-003: Activity Audit Trail (4 tests)

| ID | Test Case | Priority | Preconditions | Steps | Expected Result | Status |
|----|-----------|----------|---------------|-------|-----------------|--------|
| INT-003-01 | Create logged | 🟠 P1 | Create product | Check activity timeline | "Created product" entry | ⬜ |
| INT-003-02 | Update logged with diff | 🟠 P1 | Update product | Check activity | Before/after values | ⬜ |
| INT-003-03 | Status change logged | 🟠 P1 | Publish product | Check activity | "Published product" entry | ⬜ |
| INT-003-04 | Delete logged | 🟠 P1 | Delete product | Check activity | "Deleted product" entry | ⬜ |

---

## Execution Summary

### Priority Distribution

| Priority | Count | Percentage |
|----------|-------|------------|
| 🔴 P0 (Critical) | 52 | 21% |
| 🟠 P1 (High) | 158 | 64% |
| 🟡 P2 (Medium) | 35 | 14% |
| 🟢 P3 (Low) | 2 | 1% |
| **Total** | **247** | **100%** |

### Module Distribution

| Module | Tests | Est. Time |
|--------|-------|-----------|
| Categories | 30 | 25 min |
| Attributes | 45 | 40 min |
| Brands | 12 | 10 min |
| Product CRUD | 48 | 45 min |
| Variants | 28 | 30 min |
| Images | 22 | 25 min |
| Options | 14 | 15 min |
| Filters & Search | 20 | 20 min |
| Bulk Operations | 16 | 15 min |
| Import/Export | 18 | 20 min |
| Integration | 14 | 15 min |
| **Total** | **247** | **~4.5 hours** |

### Recommended Execution Order

1. **Smoke (10 min):** INT-001-01, PROD-001-01, PROD-002-02, CAT-002-01, ATTR-002-01
2. **Categories (25 min):** All CAT-* tests
3. **Attributes (40 min):** All ATTR-* tests
4. **Brands (10 min):** All BRAND-* tests
5. **Products (45 min):** All PROD-* tests
6. **Variants (30 min):** All VAR-* tests
7. **Images (25 min):** All IMG-* tests
8. **Options (15 min):** All OPT-* tests
9. **Filters (20 min):** All FILTER-* tests
10. **Bulk Ops (15 min):** All BULK-* tests
11. **Import/Export (20 min):** All IMPORT/EXPORT-* tests
12. **Integration (15 min):** All INT-* tests

---

## Playwright Test Tags

Use these tags for selective execution:

```typescript
// Feature tags
@categories, @attributes, @brands, @products, @variants, @images, @options, @filters, @bulk, @import, @export, @integration

// Priority tags
@p0, @p1, @p2, @p3

// Type tags
@smoke, @regression, @visual, @validation

// Combined examples
test('@p0 @categories CAT-001-01', ...);
test('@p1 @products @variants VAR-001-03', ...);
```

---

## References

- [Test Plan](./TEST_PLAN.md)
- [E2E Testing Guide](./E2E-TESTING-GUIDE.md)
- [Main Test Cases](./TEST_CASES.md)
- [Feature Catalog](../FEATURE_CATALOG.md)

---

**Last Updated:** 2026-02-05
**Version:** 1.0
