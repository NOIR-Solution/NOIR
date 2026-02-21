import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
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
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Skeleton,
  Switch,
} from '@uikit'

import { useFeatures, useModuleCatalog, useToggleModule } from '@/hooks/useFeatures'
import type { ModuleDto } from '@/types'

export interface ModulesSettingsTabProps {
  canEdit: boolean
}

/** Map of Lucide icon name (from backend) → React component */
const iconMap: Record<string, LucideIcon> = {
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
interface ModuleItem {
  name: string
  titleKey: string
  descKey?: string
}

/**
 * Sidebar-aligned module groups.
 * Each module uses the SAME localization key as its sidebar nav item
 * so names match 100% between sidebar and settings tab.
 *
 * Modules not in sidebar (Cart, Checkout) use their backend module keys.
 * Core modules and platform-only modules (Tenants, DeveloperLogs) are excluded.
 */
const SIDEBAR_GROUPS: { key: string; labelKey: string; modules: ModuleItem[] }[] = [
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
    labelKey: 'featureManagement.categories.settings',
    modules: [
      { name: 'Platform.EmailTemplates', titleKey: 'tenantSettings.title', descKey: 'tenantSettings.description' },
    ],
  },
]

interface ResolvedItem {
  module: ModuleDto
  titleKey: string
  descKey?: string
}

interface ResolvedGroup {
  key: string
  labelKey: string
  items: ResolvedItem[]
}

/** Resolve modules from catalog into sidebar-aligned groups */
const buildSidebarGroups = (modules: ModuleDto[]): ResolvedGroup[] => {
  const moduleMap = new Map(modules.map(m => [m.name, m]))

  return SIDEBAR_GROUPS
    .map(group => ({
      key: group.key,
      labelKey: group.labelKey,
      items: group.modules.reduce<ResolvedItem[]>((acc, item) => {
        const module = moduleMap.get(item.name)
        if (module) acc.push({ module, titleKey: item.titleKey, descKey: item.descKey })
        return acc
      }, []),
    }))
    .filter(group => group.items.length > 0)
}

/**
 * ModulesSettingsTab - Shows tenant-configurable modules grouped by sidebar sections.
 * Module names use the same localization keys as sidebar nav items for 100% consistency.
 * Core modules and platform-only modules (Tenants, Developer Logs) are hidden.
 */
export const ModulesSettingsTab = ({ canEdit }: ModulesSettingsTabProps) => {
  const { t } = useTranslation('common')
  const { data: catalog, isLoading: catalogLoading } = useModuleCatalog()
  const { data: features, isLoading: featuresLoading } = useFeatures()
  const toggleMutation = useToggleModule()

  const isLoading = catalogLoading || featuresLoading

  const groups = useMemo(
    () => buildSidebarGroups(catalog?.modules ?? []),
    [catalog],
  )

  const handleToggle = async (featureName: string, isEnabled: boolean) => {
    try {
      await toggleMutation.mutateAsync({ featureName, isEnabled })
      toast.success(t('featureManagement.toggleSuccess'))
    } catch {
      toast.error(t('featureManagement.toggleError'))
    }
  }

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="space-y-2">
          <Skeleton className="h-6 w-48" />
          <Skeleton className="h-4 w-96" />
        </div>
        {SIDEBAR_GROUPS.map((group, i) => (
          <Card key={i}>
            <CardHeader>
              <Skeleton className="h-5 w-32" />
            </CardHeader>
            <CardContent className="space-y-4">
              {Array.from({ length: group.modules.length }).map((_, j) => (
                <div key={j} className="flex items-center justify-between py-3">
                  <div className="flex items-center gap-3">
                    <Skeleton className="h-9 w-9 rounded-lg" />
                    <div className="space-y-1.5">
                      <Skeleton className="h-4 w-40" />
                      <Skeleton className="h-3 w-64" />
                    </div>
                  </div>
                  <Skeleton className="h-5 w-9 rounded-full" />
                </div>
              ))}
            </CardContent>
          </Card>
        ))}
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h3 className="text-lg font-medium">{t('featureManagement.title')}</h3>
        <p className="text-sm text-muted-foreground">{t('featureManagement.description')}</p>
      </div>

      {groups.map(group => (
        <Card key={group.key}>
          <CardHeader className="pb-3">
            <CardTitle className="text-base">
              {t(group.labelKey)}
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-1 pt-0">
            {group.items.map(({ module, titleKey, descKey }) => {
              const state = features?.[module.name]
              const isEffective = state?.isEffective ?? module.defaultEnabled
              const Icon = iconMap[module.icon] ?? Box
              const displayName = t(titleKey)

              return (
                <div
                  key={module.name}
                  className="flex items-center justify-between py-3 px-2 -mx-2 rounded-lg transition-colors hover:bg-muted/50"
                >
                  <div className="flex items-center gap-3 min-w-0">
                    <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-muted/80 text-muted-foreground">
                      <Icon className="h-[18px] w-[18px]" />
                    </div>
                    <div className="min-w-0 space-y-0.5">
                      <span className="font-medium text-sm leading-none">
                        {displayName}
                      </span>
                      <p className="text-xs text-muted-foreground leading-snug">
                        {t(descKey ?? module.descriptionKey)}
                      </p>
                    </div>
                  </div>
                  <Switch
                    checked={isEffective}
                    disabled={!canEdit || toggleMutation.isPending}
                    onCheckedChange={(checked) => handleToggle(module.name, checked)}
                    className="cursor-pointer shrink-0 ml-4"
                    aria-label={t('featureManagement.toggleModule', { module: displayName })}
                  />
                </div>
              )
            })}
          </CardContent>
        </Card>
      ))}
    </div>
  )
}
