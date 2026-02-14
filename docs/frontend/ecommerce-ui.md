# E-Commerce UI Components

**Last Updated:** 2026-01-29
**Status:** Production Ready

---

## Enhanced Product Card

Modern glassmorphism product card built with 21st.dev Magic Component Builder.

### Files

```
src/NOIR.Web/frontend/src/portal-app/products/components/products/
├── EnhancedProductCard.tsx      # Glassmorphism card
├── EnhancedProductGridView.tsx  # Grid wrapper
├── ProductActionsMenu.tsx       # Shared dropdown menu
├── ProductStatsCards.tsx        # Dashboard stats
├── AttributeBadges.tsx          # Attribute display
└── LowStockAlert.tsx            # Stock warnings
```

### Features

**Visual:**
- Glassmorphism with backdrop blur
- Gradient backgrounds and animated borders
- Image lazy loading with hover zoom
- Status-specific color schemes

**Interactions:**
- Hover effects with scale transforms
- Framer Motion animations
- Quick action buttons on hover
- Multi-layer interactions

**Data Display:**
- Product image with fallback
- SKU, stock, price
- Low stock warning badges
- Discount percentage

### Configuration

```typescript
// src/lib/constants/product.ts
export const LOW_STOCK_THRESHOLD = 10
```

### Usage

```tsx
import { EnhancedProductGridView } from './components/EnhancedProductGridView'

<EnhancedProductGridView
  products={products}
  onEdit={handleEdit}
  onDelete={handleDelete}
/>
```

---

## Product Stats Cards

Dashboard statistics for product overview.

```tsx
import { ProductStatsCards } from './components/ProductStatsCards'

<ProductStatsCards
  totalProducts={100}
  activeProducts={85}
  lowStockProducts={5}
  outOfStockProducts={2}
/>
```

---

## Dependencies

```json
{
  "framer-motion": "^11.x",
  "lucide-react": "^0.x"
}
```
