import {
  BadgeCheck,
  BarChart3,
  Award,
  Box,
  CreditCard,
  FileText,
  FolderTree,
  Heart,
  Layers,
  Mail,
  Package,
  Palette,
  Percent,
  Scale,
  Settings,
  ShoppingBag,
  ShoppingCart,
  Star,
  Tag,
  Tags,
  UserCheck,
  UsersRound,
  Warehouse,
  type LucideIcon,
} from 'lucide-react'

import type { ModuleDto } from '@/types'

/** Map of Lucide icon name (from backend) → React component */
export const iconMap: Record<string, LucideIcon> = {
  BadgeCheck,
  BarChart3,
  Award,
  Box,
  CreditCard,
  FileText,
  FolderTree,
  Heart,
  Layers,
  Mail,
  Package,
  Palette,
  Percent,
  Scale,
  Settings,
  ShoppingBag,
  ShoppingCart,
  Star,
  Tag,
  Tags,
  UserCheck,
  UsersRound,
  Warehouse,
}

/**
 * Module item definition for the settings tab.
 * - `titleKey`: sidebar-matching localization key for the display name
 * - `descKey`: optional override for description; falls back to module.descriptionKey
 */
export interface ModuleItem {
  name: string
  titleKey: string
  descKey?: string
  /** Additional backend modules toggled together with this one */
  linkedModules?: string[]
  /** Override backend icon name (e.g. 'Settings' instead of module's own icon) */
  iconOverride?: string
}

export interface SidebarGroup {
  key: string
  labelKey: string
  modules: ModuleItem[]
}

/**
 * Sidebar-aligned module groups.
 * Each module uses the SAME localization key as its sidebar nav item
 * so names match 100% between sidebar and settings tab.
 *
 * Modules not in sidebar (Cart, Checkout) use their backend module keys.
 * Core modules and platform-only modules (Tenants, DeveloperLogs) are excluded.
 */
export const SIDEBAR_GROUPS: SidebarGroup[] = [
  {
    key: 'marketing',
    labelKey: 'featureManagement.categories.marketing',
    modules: [
      { name: 'Analytics.Reports', titleKey: 'ecommerce.reports' },
      { name: 'Ecommerce.Promotions', titleKey: 'ecommerce.promotions' },
    ],
  },
  {
    key: 'orders',
    labelKey: 'featureManagement.categories.orders',
    modules: [
      { name: 'Ecommerce.Orders', titleKey: 'ecommerce.orders' },
      { name: 'Ecommerce.Payments', titleKey: 'ecommerce.payments' },
      { name: 'Ecommerce.Cart', titleKey: 'modules.ecommerce.cart' },
      { name: 'Ecommerce.Checkout', titleKey: 'modules.ecommerce.checkout' },
      { name: 'Ecommerce.Inventory', titleKey: 'ecommerce.inventory' },
    ],
  },
  {
    key: 'customers',
    labelKey: 'featureManagement.categories.customers',
    modules: [
      { name: 'Ecommerce.Customers', titleKey: 'ecommerce.customers' },
      { name: 'Ecommerce.CustomerGroups', titleKey: 'ecommerce.customerGroups' },
      { name: 'Ecommerce.Reviews', titleKey: 'ecommerce.reviews' },
      { name: 'Ecommerce.Wishlist', titleKey: 'ecommerce.wishlists' },
    ],
  },
  {
    key: 'catalog',
    labelKey: 'featureManagement.categories.catalog',
    modules: [
      { name: 'Ecommerce.Products', titleKey: 'ecommerce.products' },
      { name: 'Ecommerce.Categories', titleKey: 'ecommerce.categories' },
      { name: 'Ecommerce.Brands', titleKey: 'ecommerce.brands' },
      { name: 'Ecommerce.Attributes', titleKey: 'ecommerce.attributes' },
    ],
  },
  {
    key: 'content',
    labelKey: 'featureManagement.categories.content',
    modules: [
      { name: 'Content.Blog', titleKey: 'blog.posts' },
      { name: 'Content.BlogCategories', titleKey: 'blog.categories' },
      { name: 'Content.BlogTags', titleKey: 'blog.tags' },
    ],
  },
  {
    key: 'settings',
    labelKey: 'featureManagement.categories.tenantSettings',
    modules: [
      {
        name: 'Platform.EmailTemplates',
        titleKey: 'featureManagement.tenantSettingsModule',
        descKey: 'featureManagement.tenantSettingsModuleDesc',
        linkedModules: ['Platform.LegalPages'],
        iconOverride: 'Settings',
      },
    ],
  },
]

export interface ResolvedItem {
  module: ModuleDto
  titleKey: string
  descKey?: string
  /** Resolved linked backend modules (toggled together with primary) */
  linkedModules: ModuleDto[]
  /** Icon name override */
  iconOverride?: string
}

export interface ResolvedGroup {
  key: string
  labelKey: string
  items: ResolvedItem[]
}

/** Get all ModuleDto objects for a resolved item (primary + linked) */
export const getItemModules = (item: ResolvedItem): ModuleDto[] =>
  [item.module, ...item.linkedModules]

/** Get all module names for a resolved item (primary + linked) */
export const getItemModuleNames = (item: ResolvedItem): string[] =>
  getItemModules(item).map(m => m.name)

/** Resolve modules from catalog into sidebar-aligned groups */
export const buildSidebarGroups = (modules: ModuleDto[]): ResolvedGroup[] => {
  const moduleMap = new Map(modules.map(m => [m.name, m]))

  return SIDEBAR_GROUPS
    .map(group => ({
      key: group.key,
      labelKey: group.labelKey,
      items: group.modules.reduce<ResolvedItem[]>((acc, item) => {
        const module = moduleMap.get(item.name)
        if (module) acc.push({
          module,
          titleKey: item.titleKey,
          descKey: item.descKey,
          linkedModules: (item.linkedModules ?? [])
            .map(name => moduleMap.get(name))
            .filter((m): m is ModuleDto => !!m),
          iconOverride: item.iconOverride,
        })
        return acc
      }, []),
    }))
    .filter(group => group.items.length > 0)
}
