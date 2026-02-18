# Admin Portal UX Research

**Date:** 2026-02-18
**Researcher:** researcher agent
**Scope:** 10 topics covering dashboard, orders, payments, inventory, pagination, tree views, color picker, file preview, attribute indicators, and category-attribute inheritance.

---

## Table of Contents

1. [E-commerce Admin Dashboard](#1-e-commerce-admin-dashboard)
2. [Order Management UI](#2-order-management-ui)
3. [Payment Management UI](#3-payment-management-ui)
4. [Inventory Management](#4-inventory-management)
5. [Product Table Pagination](#5-product-table-pagination)
6. [Tree View for Categories](#6-tree-view-for-categories)
7. [Compact Color Picker](#7-compact-color-picker)
8. [File Preview Component](#8-file-preview-component)
9. [Attribute Type Visual Indicators](#9-attribute-type-visual-indicators)
10. [Category-Attribute Inheritance](#10-category-attribute-inheritance)

---

## 1. E-commerce Admin Dashboard

### Current State

The current `DashboardPage.tsx` shows quick links (API docs, Hangfire, user profile) with no revenue metrics, charts, or KPIs. This needs a complete rebuild.

### Recommended KPI Cards (Top Row)

Based on Shopify, Medusa.js, and WooCommerce admin patterns, the dashboard should display 4-6 KPI cards in a responsive grid:

| KPI | Icon | Color | Format |
|-----|------|-------|--------|
| **Total Revenue** (today/this month) | `DollarSign` | emerald | Currency (VND) |
| **Total Orders** (today/this month) | `ShoppingBag` | blue | Count + % change |
| **Average Order Value** | `TrendingUp` | violet | Currency |
| **Conversion Rate** | `Target` | amber | Percentage |
| **Pending Orders** | `Clock` | orange | Count (actionable) |
| **Low Stock Items** | `AlertTriangle` | red | Count (actionable) |

### Recommended Layout (3-Row Grid)

```
Row 1: [KPI Card] [KPI Card] [KPI Card] [KPI Card]     (4 columns)
Row 2: [Revenue Chart (area)          ] [Orders Chart]   (2:1 ratio)
Row 3: [Recent Orders Table ] [Top Products] [Quick Links] (1:1:1)
```

### Chart Recommendations

**Revenue Over Time (Area Chart)**
- Library: **Recharts** (already widely used with shadcn/ui)
- Time ranges: Today, 7d, 30d, 90d, 1y (tab selector)
- Show revenue line + order count as secondary axis
- Use `useDeferredValue` for smooth range switching

**Orders by Status (Donut/Pie)**
- Show distribution: Pending, Confirmed, Processing, Shipped, Delivered, Completed, Cancelled
- Use the same status colors from the order management page for consistency

### Implementation Pattern

```tsx
// Dashboard query hook
export const useDashboardMetricsQuery = (range: 'today' | '7d' | '30d' | '90d' | '1y') =>
  useQuery({
    queryKey: dashboardKeys.metrics(range),
    queryFn: () => getDashboardMetrics(range),
    staleTime: 60_000, // 1 minute cache - dashboard data doesn't need real-time
  })

// KPI Card component
export const KpiCard = ({ title, value, change, icon: Icon, color, loading }: KpiCardProps) => (
  <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
    <CardContent className="p-6">
      <div className="flex items-center justify-between">
        <div>
          {loading ? <Skeleton className="h-8 w-24" /> : (
            <p className="text-2xl font-bold">{value}</p>
          )}
          <p className="text-sm text-muted-foreground">{title}</p>
        </div>
        <div className={`p-3 rounded-xl bg-${color}-100 dark:bg-${color}-950`}>
          <Icon className={`h-5 w-5 text-${color}-600`} />
        </div>
      </div>
      {change !== undefined && (
        <div className={`mt-2 text-xs ${change >= 0 ? 'text-emerald-600' : 'text-red-600'}`}>
          {change >= 0 ? '+' : ''}{change}% vs previous period
        </div>
      )}
    </CardContent>
  </Card>
)
```

### Reference Implementations

- **Shopify Admin**: Revenue-first dashboard with customizable metric overview
- **Medusa.js Admin**: Clean, modular dashboard with product, order, and customer widgets
- **WooCommerce**: Date-range selector with period comparison

---

## 2. Order Management UI

### Existing Backend

The backend already has:
- **Commands**: CreateOrder, ConfirmOrder, ShipOrder, CancelOrder (DeliverOrder, CompleteOrder, ReturnOrder being added by backend-dev)
- **Queries**: GetOrders (paginated), GetOrderById
- **DTO**: OrderDto with full financial data, customer info, addresses, shipping, items, timestamps
- **OrderStatus enum**: Pending -> Confirmed -> Processing -> Shipped -> Delivered -> Completed | Cancelled | Refunded

### Recommended Page Structure

**Orders List Page** (`/portal/orders`)

```
[PageHeader: "Orders" with stats badges]
[Search bar + Status filter tabs + Date range + Export button]
[Orders Table]
  - Columns: Order #, Customer, Status (badge), Items, Total, Date, Actions
  - Status filter as horizontal tabs (All | Pending | Confirmed | Processing | Shipped | Delivered | Completed | Cancelled)
  - Bulk actions: Confirm, Ship, Cancel (with selection checkboxes)
[Pagination]
```

**Order Detail Page** (`/portal/orders/:id`)

```
[Breadcrumb: Orders > #ORD-12345]
[Header: Order number + Status badge + Action buttons (Confirm/Ship/Deliver/Complete/Cancel)]

[Two-column layout]
  Left (2/3):
    [Order Status Timeline (horizontal stepper)]
    [Order Items Table - product image, name, SKU, qty, price, line total]
    [Order Activity Log / Notes]

  Right (1/3):
    [Customer Info Card]
    [Shipping Address Card]
    [Billing Address Card]
    [Payment Summary Card]
    [Order Summary Card (subtotal, shipping, tax, discount, total)]
```

### Order Status Timeline Component

Build a custom horizontal stepper that maps to NOIR's `OrderStatus` enum:

```tsx
const ORDER_STATUS_STEPS = [
  { status: 'Pending', icon: Clock, color: 'amber' },
  { status: 'Confirmed', icon: CheckCircle, color: 'blue' },
  { status: 'Processing', icon: Package, color: 'indigo' },
  { status: 'Shipped', icon: Truck, color: 'violet' },
  { status: 'Delivered', icon: MapPin, color: 'emerald' },
  { status: 'Completed', icon: CheckCircle2, color: 'green' },
] as const

// Cancelled/Refunded shown as a separate red branch below the timeline
// Each completed step shows the timestamp from OrderDto

export const OrderStatusTimeline = ({ order }: { order: OrderDto }) => {
  const currentIndex = ORDER_STATUS_STEPS.findIndex(s => s.status === order.status)
  const isCancelled = order.status === 'Cancelled' || order.status === 'Refunded'

  return (
    <div className="flex items-center gap-2">
      {ORDER_STATUS_STEPS.map((step, i) => (
        <div key={step.status} className="flex items-center gap-2">
          <div className={cn(
            'flex items-center justify-center w-8 h-8 rounded-full border-2',
            i <= currentIndex && !isCancelled
              ? `bg-${step.color}-100 border-${step.color}-500 text-${step.color}-700`
              : 'bg-muted border-muted-foreground/30 text-muted-foreground'
          )}>
            <step.icon className="h-4 w-4" />
          </div>
          {i < ORDER_STATUS_STEPS.length - 1 && (
            <div className={cn(
              'h-0.5 w-8',
              i < currentIndex && !isCancelled ? 'bg-primary' : 'bg-muted'
            )} />
          )}
        </div>
      ))}
    </div>
  )
}
```

### Status Badge Colors

```tsx
const ORDER_STATUS_CONFIG: Record<OrderStatus, { label: string; variant: string; color: string }> = {
  Pending:    { label: 'Pending',    variant: 'outline', color: 'text-amber-600 border-amber-300 bg-amber-50' },
  Confirmed:  { label: 'Confirmed',  variant: 'outline', color: 'text-blue-600 border-blue-300 bg-blue-50' },
  Processing: { label: 'Processing', variant: 'outline', color: 'text-indigo-600 border-indigo-300 bg-indigo-50' },
  Shipped:    { label: 'Shipped',    variant: 'outline', color: 'text-violet-600 border-violet-300 bg-violet-50' },
  Delivered:  { label: 'Delivered',  variant: 'outline', color: 'text-emerald-600 border-emerald-300 bg-emerald-50' },
  Completed:  { label: 'Completed',  variant: 'default', color: 'text-green-700 border-green-300 bg-green-50' },
  Cancelled:  { label: 'Cancelled',  variant: 'destructive', color: 'text-red-600 border-red-300 bg-red-50' },
  Refunded:   { label: 'Refunded',   variant: 'outline', color: 'text-orange-600 border-orange-300 bg-orange-50' },
}
```

### Key UX Decisions

1. **Action buttons are context-aware**: Only show valid transitions (e.g., "Confirm" only when Pending)
2. **Confirmation dialogs for state changes**: Especially Cancel and Refund
3. **Inline notes on cancel**: Require cancellation reason in dialog
4. **Optimistic updates**: Use `optimisticListPatch` from existing utility for status changes

---

## 3. Payment Management UI

### Existing Backend

Rich payment infrastructure already built:
- **Gateways**: Configure, Update, Test connection, Health status, Gateway schemas
- **Transactions**: Create, Cancel, List, Detail (with status, method, fees)
- **Refunds**: Request, Approve, Reject, List
- **COD**: Pending collections, Confirm collection
- **Webhooks**: Process, View logs
- **Operation Logs**: Audit trail for all payment operations

### Recommended Page Structure

**Payments Overview** (`/portal/payments`)

Tabbed layout with 4 tabs:

```
[Tabs: Transactions | Refunds | Gateways | Webhooks]
```

**Transactions Tab**

```
[KPI Row: Total Revenue | Success Rate | Avg Transaction | Failed Today]
[Filters: Status | Method | Gateway | Date range]
[Transactions Table]
  - Columns: Transaction #, Amount, Status (badge), Method icon, Gateway, Customer, Date
  - Click row -> slide-out Sheet with full transaction details
[Pagination]
```

**Refunds Tab**

```
[Filters: Status (Pending/Approved/Rejected/Processed) | Date range]
[Refunds Table]
  - Columns: Refund #, Original Transaction, Amount, Status, Reason, Requested By, Date
  - Actions: Approve / Reject (for pending refunds)
[Pagination]
```

**Gateways Tab**

```
[Gateway Cards Grid - 2 columns]
  Each card:
  - Provider logo/icon + Display name
  - Status: Active/Inactive toggle
  - Health: Green/Yellow/Red indicator dot
  - Environment badge: Live / Sandbox
  - Min/Max amount range
  - Last health check timestamp
  - Actions: Configure (opens dialog), Test Connection, View Logs
```

**Webhooks Tab**

```
[Webhook Logs Table]
  - Columns: ID, Provider, Event Type, Status (Success/Failed), Received At
  - Expandable row showing raw payload (use existing JsonViewer component)
```

### Payment Status Badge Pattern

```tsx
const PAYMENT_STATUS_CONFIG: Record<PaymentStatus, { icon: LucideIcon; color: string }> = {
  Pending:   { icon: Clock, color: 'amber' },
  Completed: { icon: CheckCircle, color: 'emerald' },
  Failed:    { icon: XCircle, color: 'red' },
  Cancelled: { icon: Ban, color: 'slate' },
  Refunded:  { icon: RotateCcw, color: 'orange' },
}

const PAYMENT_METHOD_ICON: Record<PaymentMethod, LucideIcon> = {
  CreditCard: CreditCard,
  BankTransfer: Building2,
  EWallet: Wallet,
  COD: Banknote,
}
```

---

## 4. Inventory Management

### Existing Backend

- **InventoryMovement** entity with: ProductVariantId, ProductId, MovementType, QuantityBefore, QuantityMoved, QuantityAfter, Reference, Notes, UserId, CorrelationId
- **InventoryMovementType**: StockIn, StockOut, Adjustment, Return, Reservation, ReservationRelease, Damaged, Expired
- Backend-dev is adding StockIn, StockOut, Adjustment commands (Task #13)

### Recommended Page Structure

**Inventory Overview** (`/portal/inventory`)

```
[PageHeader: "Inventory Management"]
[KPI Row: Total SKUs | Low Stock | Out of Stock | Total Value]

[Tabs: Stock Levels | Stock In | Stock Out | Movement History]
```

**Stock Levels Tab (default)**

```
[Search + Category filter + Stock status filter (All/In Stock/Low/Out)]
[Products Table]
  - Columns: Image, Product Name, SKU, Category, Current Stock, Reserved, Available, Status
  - Status: In Stock (green), Low Stock (amber), Out of Stock (red)
  - Click row -> expands to show variant-level breakdown
[Pagination]
```

**Stock In Tab (Phieu Nhap)**

This follows the ERP "receipt ticket" (phieu nhap kho) pattern:

```
[Create Stock-In Receipt button]
[Recent Receipts Table]
  - Columns: Receipt #, Date, Supplier/Source, Items Count, Total Qty, Status, Created By
  - Click -> detail view

[Stock-In Receipt Dialog/Page]
  - Reference number (auto-generated)
  - Source/Supplier (text input)
  - Notes
  - Items table (add rows):
    | Product (search) | Variant | Current Stock | Qty to Add | New Stock |
  - Save creates multiple InventoryMovement records with type=StockIn
```

**Stock Out Tab (Phieu Xuat)**

Similar to Stock In but for outgoing stock:

```
[Create Stock-Out Receipt button]
[Recent Receipts Table]
  - Columns: Receipt #, Date, Reason, Items Count, Total Qty, Created By

[Stock-Out Receipt Dialog/Page]
  - Reference number
  - Reason (select: Manual Removal, Damaged, Expired, Transfer, Other)
  - Notes
  - Items table:
    | Product | Variant | Current Stock | Qty to Remove | New Stock |
  - Validation: Cannot remove more than available stock
```

**Movement History Tab**

```
[Filters: Movement Type | Product | Date Range]
[Timeline/Table view toggle]
[Movements Table]
  - Columns: Date, Product, Variant, Type (badge), Qty (+/-), Before -> After, Reference, User
  - Type badges use distinct colors:
    StockIn: emerald (+)
    StockOut: red (-)
    Adjustment: blue (~)
    Return: violet (+)
    Reservation: amber (lock)
    ReservationRelease: teal (unlock)
    Damaged: orange (!)
    Expired: slate (x)
[Pagination]
```

### Movement Type Badge Colors

```tsx
const MOVEMENT_TYPE_CONFIG: Record<InventoryMovementType, {
  label: string; icon: LucideIcon; color: string; sign: '+' | '-' | '~'
}> = {
  StockIn:            { label: 'Stock In',      icon: ArrowDownToLine, color: 'emerald', sign: '+' },
  StockOut:           { label: 'Stock Out',     icon: ArrowUpFromLine, color: 'red',     sign: '-' },
  Adjustment:         { label: 'Adjustment',    icon: Settings2,      color: 'blue',    sign: '~' },
  Return:             { label: 'Return',        icon: CornerDownLeft, color: 'violet',  sign: '+' },
  Reservation:        { label: 'Reserved',      icon: Lock,           color: 'amber',   sign: '-' },
  ReservationRelease: { label: 'Released',      icon: Unlock,         color: 'teal',    sign: '+' },
  Damaged:            { label: 'Damaged',       icon: AlertTriangle,  color: 'orange',  sign: '-' },
  Expired:            { label: 'Expired',       icon: Timer,          color: 'slate',   sign: '-' },
}
```

---

## 5. Product Table Pagination

### Problem

When paginating, the table flashes empty during data fetch, causing layout shift and poor UX.

### Recommended Multi-Layer Solution

**Layer 1: `placeholderData: keepPreviousData` (TanStack Query v5)**

This is the single most impactful fix. Keep the old page visible while the new page loads:

```tsx
import { keepPreviousData } from '@tanstack/react-query'

export const useProductsQuery = (params: GetProductsParams) =>
  useQuery({
    queryKey: productKeys.list(params),
    queryFn: () => getProducts(params),
    placeholderData: keepPreviousData,  // KEY: old data stays visible
  })

// In the component, use isPlaceholderData for visual feedback:
const { data, isPlaceholderData } = useProductsQuery(params)

<CardContent className={isPlaceholderData ? 'opacity-70 transition-opacity duration-200' : ''}>
  <ProductTable data={data} />
</CardContent>
```

**Layer 2: Fixed Table Height**

Prevent the table container from collapsing during brief loading states:

```tsx
// Use min-height based on expected row count
<div className="min-h-[500px]"> {/* ~10 rows * 50px */}
  <Table>...</Table>
</div>
```

**Layer 3: Skeleton Rows for Initial Load**

Only show skeleton on the very first load (no data yet):

```tsx
const TableSkeleton = ({ rows = 10, cols = 6 }) => (
  <TableBody>
    {Array.from({ length: rows }).map((_, i) => (
      <TableRow key={i}>
        {Array.from({ length: cols }).map((_, j) => (
          <TableCell key={j}>
            <Skeleton className="h-4 w-full" />
          </TableCell>
        ))}
      </TableRow>
    ))}
  </TableBody>
)

// Usage
{loading && !data ? <TableSkeleton /> : <ProductTableRows data={data} />}
```

**Layer 4: CSS Containment (Advanced)**

For very large tables, CSS containment prevents layout recalculations:

```css
.table-container {
  contain: layout style;
  contain-intrinsic-size: auto 500px;
}
```

### Implementation Priority

1. `placeholderData: keepPreviousData` -- **do this first**, solves 90% of the problem
2. Fixed `min-height` on table container -- simple CSS, no risk
3. Skeleton rows for initial load -- already have `Skeleton` component in UIKit
4. CSS containment -- only if performance profiling shows need

---

## 6. Tree View for Categories

### Problem

The category tree can grow to 280+ items. Need virtualization and drag-and-drop support.

### Recommended Approach: React Arborist

**Why React Arborist over alternatives:**

| Feature | React Arborist | @tanstack/react-virtual + custom | shadcn-tree-view |
|---------|---------------|----------------------------------|-----------------|
| Virtualization | Built-in | Manual | None |
| Drag & Drop | Built-in | Need dnd-kit | None |
| Large datasets (280+) | Excellent | Good (manual) | Poor |
| Customizable rendering | Full control | Full control | Limited |
| TypeScript support | Yes | Yes | Yes |
| Bundle size | ~15kb | ~10kb + dnd-kit | ~5kb |

### Implementation

```bash
pnpm add react-arborist
```

```tsx
import { Tree, NodeRendererProps } from 'react-arborist'

interface CategoryNode {
  id: string
  name: string
  slug: string
  children?: CategoryNode[]
  productCount: number
}

const CategoryNodeRenderer = ({ node, style, dragHandle }: NodeRendererProps<CategoryNode>) => (
  <div
    ref={dragHandle}
    style={style}
    className={cn(
      'flex items-center gap-2 px-2 py-1.5 rounded-md cursor-pointer',
      node.isSelected && 'bg-accent',
      node.willReceiveDrop && 'bg-blue-50 dark:bg-blue-950'
    )}
    onClick={() => node.toggle()}
  >
    {node.isInternal && (
      <ChevronRight className={cn('h-4 w-4 transition-transform', node.isOpen && 'rotate-90')} />
    )}
    {node.isLeaf && <div className="w-4" />}
    <Folder className="h-4 w-4 text-muted-foreground" />
    <span className="text-sm flex-1 truncate">{node.data.name}</span>
    <Badge variant="secondary" className="text-xs">{node.data.productCount}</Badge>
  </div>
)

export const CategoryTreeView = ({ categories, onMove, onSelect }: Props) => (
  <Tree
    data={categories}
    width="100%"
    height={600}        // Fixed height for virtualization
    rowHeight={36}
    indent={24}
    onMove={onMove}     // Drag-and-drop handler
    onSelect={onSelect}
    openByDefault={false}
    searchTerm={searchTerm}
    searchMatch={(node, term) =>
      node.data.name.toLowerCase().includes(term.toLowerCase())
    }
  >
    {CategoryNodeRenderer}
  </Tree>
)
```

### Performance Optimizations

1. **Virtualization**: React Arborist only renders visible nodes (default behavior)
2. **Lazy loading children**: For deeply nested trees, load children on expand via API
3. **Debounced search**: Use `useDeferredValue` for tree search filtering
4. **Collapse all by default**: `openByDefault={false}` - user expands what they need

### DnD Considerations

- dnd-kit has known performance issues with 200+ rows during drag
- React Arborist uses its own optimized DnD that works with virtualization
- For cross-level moves (reparenting), backend needs `MoveCategory(id, newParentId, sortOrder)` endpoint

### Alternative: Server-Side Pagination (for 1000+ categories)

If the tree grows beyond 500 items, consider server-side pagination:

```tsx
// Load only root categories initially
const { data: rootCategories } = useCategoriesQuery({ parentId: null })

// Load children on expand
const loadChildren = async (parentId: string) => {
  return await getCategories({ parentId })
}
```

---

## 7. Compact Color Picker

### Existing State

The project already has:
- `uikit/color-picker/ColorPicker.stories.tsx` - Full color picker
- `uikit/color-popover/ColorPopover.stories.tsx` - Color popover (just added)

### Recommended Pattern: Popover Trigger with react-colorful

```bash
pnpm add react-colorful
```

```tsx
import { HexColorPicker, HexColorInput } from 'react-colorful'
import { Popover, PopoverContent, PopoverTrigger } from '@uikit'

export const CompactColorPicker = ({ value, onChange, presets }: Props) => (
  <Popover>
    <PopoverTrigger asChild>
      <Button variant="outline" className="w-[120px] justify-start gap-2 cursor-pointer">
        <div
          className="h-4 w-4 rounded-sm border border-border"
          style={{ backgroundColor: value || '#000000' }}
        />
        <span className="text-sm font-mono">{value || '#000000'}</span>
      </Button>
    </PopoverTrigger>
    <PopoverContent className="w-[240px] p-3" align="start">
      <div className="space-y-3">
        <HexColorPicker color={value} onChange={onChange} style={{ width: '100%' }} />

        {/* Preset colors grid */}
        {presets && (
          <div className="flex flex-wrap gap-1.5">
            {presets.map(color => (
              <button
                key={color}
                className="h-6 w-6 rounded-md border border-border cursor-pointer hover:scale-110 transition-transform"
                style={{ backgroundColor: color }}
                onClick={() => onChange(color)}
                aria-label={`Select color ${color}`}
              />
            ))}
          </div>
        )}

        {/* Hex input */}
        <div className="flex items-center gap-2">
          <span className="text-xs text-muted-foreground">HEX</span>
          <HexColorInput
            color={value}
            onChange={onChange}
            prefixed
            className="flex h-8 w-full rounded-md border border-input bg-background px-2 text-sm font-mono"
          />
        </div>
      </div>
    </PopoverContent>
  </Popover>
)
```

### Key UX Details

- **Trigger size**: Small button (120px) showing color swatch + hex value
- **Popover width**: 240px - compact but usable
- **Preset colors**: Optional grid of frequently used colors
- **Hex input**: Manual entry with validation
- **Keyboard accessible**: Tab through presets, type in hex input

---

## 8. File Preview Component

### Existing State

The project already has `uikit/file-preview/FilePreview.stories.tsx` and `uikit/image-lightbox/ImageLightbox.stories.tsx`.

### Recommended Library: yet-another-react-lightbox

**Why this library:**
- Plugin architecture: Zoom, Video, Thumbnails, Fullscreen
- React 19 compatible
- 5.5kb core, plugins are optional
- Keyboard and touch navigation
- Custom slide renderers for PDF support

```bash
pnpm add yet-another-react-lightbox
```

### Implementation

```tsx
import Lightbox from 'yet-another-react-lightbox'
import Zoom from 'yet-another-react-lightbox/plugins/zoom'
import Video from 'yet-another-react-lightbox/plugins/video'
import Thumbnails from 'yet-another-react-lightbox/plugins/thumbnails'
import 'yet-another-react-lightbox/styles.css'
import 'yet-another-react-lightbox/plugins/thumbnails.css'

export const FilePreviewModal = ({ files, initialIndex, open, onClose }: Props) => {
  const slides = files.map(file => {
    if (file.type.startsWith('video/')) {
      return {
        type: 'video' as const,
        sources: [{ src: file.url, type: file.type }],
      }
    }
    if (file.type === 'application/pdf') {
      return {
        type: 'custom-pdf' as const,
        src: file.url,
      }
    }
    return { src: file.url, alt: file.name }
  })

  return (
    <Lightbox
      open={open}
      close={onClose}
      index={initialIndex}
      slides={slides}
      plugins={[Zoom, Video, Thumbnails]}
      zoom={{ maxZoomPixelRatio: 3 }}
      render={{
        slide: ({ slide }) => {
          if ((slide as any).type === 'custom-pdf') {
            return (
              <iframe
                src={(slide as any).src}
                className="w-full h-full"
                title="PDF Preview"
              />
            )
          }
          return undefined // Use default renderer
        }
      }}
    />
  )
}
```

### Usage in Blog Featured Image

```tsx
const [previewOpen, setPreviewOpen] = useState(false)

<img
  src={post.featuredImageUrl}
  onClick={() => setPreviewOpen(true)}
  className="cursor-pointer hover:opacity-90 transition-opacity"
  alt={post.title}
/>

<FilePreviewModal
  files={[{ url: post.featuredImageUrl, name: post.title, type: 'image/jpeg' }]}
  initialIndex={0}
  open={previewOpen}
  onClose={() => setPreviewOpen(false)}
/>
```

---

## 9. Attribute Type Visual Indicators

### Current State

`attribute.utils.ts` already has color-coded badge styles for all 13 types but no icons.

### Recommended Icon Mapping

Add icons from lucide-react to complement the existing color coding:

```tsx
import {
  List, ListChecks, Type, AlignLeft, Hash, Percent,
  ToggleLeft, Calendar, CalendarClock, Palette,
  SlidersHorizontal, Link, FileUp
} from 'lucide-react'

const ATTRIBUTE_TYPE_ICONS: Record<string, LucideIcon> = {
  Select:      List,             // Single selection from list
  MultiSelect: ListChecks,       // Multiple selections with checkmarks
  Text:        Type,             // Single line text
  TextArea:    AlignLeft,        // Multi-line text
  Number:      Hash,             // Integer number
  Decimal:     Percent,          // Decimal/percentage
  Boolean:     ToggleLeft,       // Toggle on/off
  Date:        Calendar,         // Date only
  DateTime:    CalendarClock,    // Date + time
  Color:       Palette,          // Color selection
  Range:       SlidersHorizontal, // Min-max range
  Url:         Link,             // URL/link
  File:        FileUp,           // File upload
}
```

### Enhanced Badge Component

```tsx
export const AttributeTypeBadge = ({ type, t }: { type: string; t: TFunction }) => {
  const { label, className } = getTypeBadge(type, t)
  const Icon = ATTRIBUTE_TYPE_ICONS[type]

  return (
    <Badge variant="outline" className={cn('gap-1.5', className)}>
      {Icon && <Icon className="h-3 w-3" />}
      {label}
    </Badge>
  )
}
```

### Accessibility

Research from NN/g shows that users are **37% faster** at finding items in a list when visual indicators vary both in color AND icon. The existing colors alone are good; adding icons makes scanning significantly faster, especially for users with color vision deficiencies.

---

## 10. Category-Attribute Inheritance

### Current Architecture

```
ProductAttribute -> ProductAttributeValue (1:N) - predefined options
Category -> CategoryAttribute (M:N) - attributes assigned to categories
Product -> ProductAttributeAssignment (1:N) - actual values on products
```

### How Shopify and Magento Handle This

- **Shopify**: When you set a product's category, Shopify automatically surfaces the standard attributes for that category type. Merchants can add custom metafields on top.
- **Magento**: Uses "Attribute Sets" - a named collection of attributes. You assign an attribute set to a product, and all attributes in that set become available.

### Recommended UX Pattern for NOIR

**Category Attributes Panel** (on Category Edit):

```
[Category: Shoes]
[Inherited from parent: Clothing]
  - Material (Text) [inherited, locked icon]
  - Brand (Select) [inherited, locked icon]

[Own attributes:]
  - Size (Select) [can remove]
  - Color (Color) [can remove]
  - Sole Type (Select) [can remove]

[+ Add Attribute] -> Opens attribute picker dialog
```

**Product Edit - Attributes Tab**:

```
[From Category "Shoes":]
  Material: [________] (required by category)
  Brand: [dropdown___] (required by category)
  Size: [dropdown___] (required by category)
  Color: [color picker] (required by category)
  Sole Type: [dropdown___]

[Additional Attributes:]
  [+ Add Custom Attribute] -> picker excluding already-assigned ones

  Weight: [0.5] kg (manually added)
```

### Implementation

```tsx
// Fetch category attributes when product category changes
export const useProductCategoryAttributesQuery = (categoryId: string | undefined) =>
  useQuery({
    queryKey: productKeys.categoryAttributes(categoryId!),
    queryFn: () => getCategoryAttributes(categoryId!),
    enabled: !!categoryId,
  })

// In ProductFormPage, merge category attrs + product-specific attrs
const { data: categoryAttrs = [] } = useProductCategoryAttributesQuery(selectedCategoryId)
const allAttributes = [
  ...categoryAttrs.map(a => ({ ...a, source: 'category' as const, removable: false })),
  ...productAttrs.map(a => ({ ...a, source: 'product' as const, removable: true })),
]
```

### Key UX Rules

1. **Category attributes are pre-filled** when a product is assigned to a category
2. **Inherited attributes are visually distinct** (locked icon, different background)
3. **Products can add ad-hoc attributes** beyond what the category requires
4. **Changing category** shows a confirmation dialog: "This will change available attributes. Product-specific attributes will be kept."
5. **Required vs Optional**: Category can mark some attributes as required for products in that category

---

## Summary of Library Recommendations

| Need | Library | Size | Notes |
|------|---------|------|-------|
| Charts | **Recharts** | ~55kb | Already common with shadcn/ui ecosystems |
| Lightbox/Preview | **yet-another-react-lightbox** | ~5.5kb + plugins | Zoom, Video, Thumbnails plugins |
| Color Picker | **react-colorful** | ~2.5kb | Minimal, accessible, performant |
| Tree View | **react-arborist** | ~15kb | Built-in virtualization + DnD |
| Pagination | **TanStack Query `keepPreviousData`** | Already installed | Just change query options |
| Table Skeleton | **Existing `@uikit` Skeleton** | Already installed | Use with conditional rendering |

### Total New Dependencies: 3 packages (~23kb total)

---

## Implementation Priority for Frontend Tasks

Based on this research, the recommended implementation order for the downstream tasks:

1. **Task #2** (Pagination fix): Fastest win - just add `placeholderData: keepPreviousData` + min-height
2. **Task #4** (Attribute indicators): Add icons to existing `attribute.utils.ts` + new `AttributeTypeBadge`
3. **Task #7** (Color picker): Use react-colorful in Popover pattern
4. **Task #5** (Blog image preview): Use yet-another-react-lightbox
5. **Task #6** (Tags color dot): Simple Badge + inline color swatch
6. **Task #3** (Category tree): Integrate react-arborist for virtualized tree
7. **Task #8** (Order management): Full pages with status timeline, detail page
8. **Task #9** (Payment management): Tabbed layout with transaction, refund, gateway views
9. **Task #10** (Inventory): Stock levels, receipts, movement history
10. **Task #11** (Dashboard): KPI cards, charts, recent activity

---

**End of Research Document**
