import type { TFunction } from 'i18next'
import {
  BarChart3, ShoppingCart, UserCheck, Package, FileText,
  Users, Building2, Settings as SettingsIcon, Activity,
  SlidersHorizontal, type LucideIcon,
} from 'lucide-react'

/** Category display order matching sidebar navigation sections. */
const CATEGORY_SORT_ORDER: Record<string, number> = {
  'Marketing': 1,
  'Orders': 2,
  'Customers': 3,
  'Catalog': 4,
  'Content': 5,
  'Users & Access': 6,
  'Tenant Management': 7,
  'Settings': 8,
  'System': 9,
  'Platform': 10,
}

/** Category icons matching sidebar navigation. */
export const CATEGORY_ICONS: Record<string, LucideIcon> = {
  'Marketing': BarChart3,
  'Orders': ShoppingCart,
  'Customers': UserCheck,
  'Catalog': Package,
  'Content': FileText,
  'Users & Access': Users,
  'Tenant Management': Building2,
  'Settings': SettingsIcon,
  'System': Activity,
  'Platform': SlidersHorizontal,
}

/** Sort comparator for permission categories matching sidebar menu order. */
export const comparePermissionCategories = (a: string, b: string): number =>
  (CATEGORY_SORT_ORDER[a] ?? 99) - (CATEGORY_SORT_ORDER[b] ?? 99)

/**
 * Translate a permission category name using locale keys.
 * Falls back to the raw category string if no translation exists.
 */
export const translatePermissionCategory = (t: TFunction, category: string): string => {
  const categoryKey = category.toLowerCase().replace(/[^a-z0-9]/g, '')
  return t(`permissions.categories.${categoryKey}`, category)
}

/**
 * Translate a permission displayName using locale keys.
 * Key format: permissions.displayNames.{resource}.{action}
 * Permission name format: "resource:action" (e.g., "users:read")
 */
export const translatePermissionDisplayName = (t: TFunction, permName: string, fallback: string): string => {
  const key = permName.replace(':', '.')
  return t(`permissions.displayNames.${key}`, fallback)
}

/**
 * Translate a permission description using locale keys.
 * Returns undefined if the original description is falsy.
 */
export const translatePermissionDescription = (t: TFunction, permName: string, description?: string | null): string | undefined => {
  if (!description) return undefined
  const key = permName.replace(':', '.')
  return t(`permissions.descriptions.${key}`, description)
}
