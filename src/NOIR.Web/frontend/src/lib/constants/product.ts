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

// Animation durations (milliseconds)
export const ANIMATION_DURATIONS = {
  counterAnimation: 2000,
  cardFadeIn: 700,
  cardHover: 300,
} as const

// Animation delays for stat cards (milliseconds)
export const STAT_CARD_ANIMATION_DELAYS = {
  first: 0,
  second: 100,
  third: 200,
  fourth: 300,
} as const

// Stat card gradient themes
export const STAT_CARD_THEMES = {
  total: {
    gradientFrom: '#6366f1', // indigo-500
    gradientTo: '#8b5cf6',   // purple-500
  },
  active: {
    gradientFrom: '#10b981', // emerald-500
    gradientTo: '#059669',   // emerald-600
  },
  draft: {
    gradientFrom: '#f59e0b', // amber-500
    gradientTo: '#d97706',   // amber-600
  },
  outOfStock: {
    gradientFrom: '#ef4444', // red-500
    gradientTo: '#dc2626',   // red-600
  },
} as const

// Product stat cards configuration
export const PRODUCT_STAT_CARDS_CONFIG = [
  {
    key: 'total' as const,
    title: 'Total Products',
    icon: Package,
    ...STAT_CARD_THEMES.total,
    delay: STAT_CARD_ANIMATION_DELAYS.first,
  },
  {
    key: 'active' as const,
    title: 'Active Products',
    icon: CheckCircle2,
    ...STAT_CARD_THEMES.active,
    delay: STAT_CARD_ANIMATION_DELAYS.second,
  },
  {
    key: 'draft' as const,
    title: 'Draft Products',
    icon: FileText,
    ...STAT_CARD_THEMES.draft,
    delay: STAT_CARD_ANIMATION_DELAYS.third,
  },
  {
    key: 'outOfStock' as const,
    title: 'Out of Stock',
    icon: AlertCircle,
    ...STAT_CARD_THEMES.outOfStock,
    delay: STAT_CARD_ANIMATION_DELAYS.fourth,
  },
] as const
