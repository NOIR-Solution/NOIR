/**
 * Product UI configuration constants
 * Shared across all product-related pages and components
 */

import type { ProductStatus } from '@/types/product'
import { CheckCircle2, XCircle, AlertCircle, Archive, Package, FileText } from 'lucide-react'
import type { FC } from 'react'
import { getStatusBadgeClasses } from '@/utils/statusBadge'

export const PRODUCT_STATUS_CONFIG: Record<
  ProductStatus,
  { color: string; icon: FC<{ className?: string }>; label: string }
> = {
  Draft: {
    color: getStatusBadgeClasses('gray'),
    icon: AlertCircle,
    label: 'Draft'
  },
  Active: {
    color: getStatusBadgeClasses('green'),
    icon: CheckCircle2,
    label: 'Active'
  },
  Archived: {
    color: getStatusBadgeClasses('yellow'),
    icon: Archive,
    label: 'Archived'
  },
  OutOfStock: {
    color: getStatusBadgeClasses('red'),
    icon: XCircle,
    label: 'Out of Stock'
  },
}

// Default page size for product lists
export const DEFAULT_PRODUCT_PAGE_SIZE = 10

// Low stock threshold - products with stock below this value show warning
export const LOW_STOCK_THRESHOLD = 10

// Stat card theme classes (Tailwind)
export const STAT_CARD_THEMES = {
  total: {
    iconBg: 'bg-primary/10 border-primary/20',
    iconColor: 'text-primary',
  },
  active: {
    iconBg: 'bg-green-500/10 border-green-500/20',
    iconColor: 'text-green-500',
  },
  draft: {
    iconBg: 'bg-amber-500/10 border-amber-500/20',
    iconColor: 'text-amber-500',
  },
  outOfStock: {
    iconBg: 'bg-red-500/10 border-red-500/20',
    iconColor: 'text-red-500',
  },
} as const

// Product stat cards configuration
export const PRODUCT_STAT_CARDS_CONFIG = [
  {
    key: 'total' as const,
    title: 'Total Products',
    icon: Package,
    ...STAT_CARD_THEMES.total,
  },
  {
    key: 'active' as const,
    title: 'Active Products',
    icon: CheckCircle2,
    ...STAT_CARD_THEMES.active,
  },
  {
    key: 'draft' as const,
    title: 'Draft Products',
    icon: FileText,
    ...STAT_CARD_THEMES.draft,
  },
  {
    key: 'outOfStock' as const,
    title: 'Out of Stock',
    icon: AlertCircle,
    ...STAT_CARD_THEMES.outOfStock,
  },
] as const
