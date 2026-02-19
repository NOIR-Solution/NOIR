import { useEffect, useState, useRef, useMemo, useCallback } from 'react'
import { Command } from 'cmdk'
import { useLocation } from 'react-router-dom'
import { useViewTransitionNavigate } from '@/hooks/useViewTransition'
import { useTranslation } from 'react-i18next'
import {
  LayoutDashboard,
  Users,
  Shield,
  Building2,
  FileText,
  FolderTree,
  Tag,
  Package,
  Layers,
  Settings,
  Search,
  Truck,
  Moon,
  Sun,
  Monitor,
  Activity,
  Terminal,
  SlidersHorizontal,
  Palette,
  Plus,
  Award,
  Tags,
  ShoppingCart,
  Warehouse,
  Percent,
  Star,
  UserCheck,
  BarChart3,
  Heart,
  Clock,
  UsersRound,
} from 'lucide-react'
import { useCommand } from './CommandContext'
import { useKeyboardShortcuts, formatShortcut } from '@/hooks/useKeyboardShortcuts'
import { useTheme } from '@/contexts/ThemeContext'
import { useDensity, type Density } from '@/contexts/DensityContext'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { cn } from '@/lib/utils'

const RECENT_PAGES_KEY = 'noir-command-palette-recent'
const MAX_RECENT_PAGES = 5

interface NavigationItem {
  icon: React.ElementType
  labelKey: string
  path: string
  keywords?: string[]
  permission?: keyof typeof Permissions
}

/**
 * Navigation items mirroring the sidebar navSections for full coverage
 */
const NAVIGATION_ITEMS: NavigationItem[] = [
  // Primary
  { icon: LayoutDashboard, labelKey: 'dashboard.title', path: '/portal', keywords: ['home', 'main', 'overview'] },
  // E-commerce
  { icon: Package, labelKey: 'ecommerce.products', path: '/portal/ecommerce/products', keywords: ['shop', 'store', 'catalog'], permission: 'ProductsRead' },
  { icon: Layers, labelKey: 'ecommerce.categories', path: '/portal/ecommerce/categories', keywords: ['shop', 'organize'], permission: 'ProductCategoriesRead' },
  { icon: Award, labelKey: 'ecommerce.brands', path: '/portal/ecommerce/brands', keywords: ['manufacturer'], permission: 'BrandsRead' },
  { icon: Tags, labelKey: 'ecommerce.attributes', path: '/portal/ecommerce/attributes', keywords: ['specifications', 'filter'], permission: 'AttributesRead' },
  { icon: ShoppingCart, labelKey: 'ecommerce.orders', path: '/portal/ecommerce/orders', keywords: ['purchase', 'buy', 'checkout'], permission: 'OrdersRead' },
  { icon: Warehouse, labelKey: 'ecommerce.inventory', path: '/portal/ecommerce/inventory', keywords: ['stock', 'warehouse', 'receipt'], permission: 'InventoryRead' },
  { icon: Truck, labelKey: 'ecommerce.shipping', path: '/portal/ecommerce/shipping', keywords: ['carrier', 'delivery', 'tracking'], permission: 'OrdersRead' },
  { icon: UserCheck, labelKey: 'ecommerce.customers', path: '/portal/ecommerce/customers', keywords: ['client', 'buyer'], permission: 'CustomersRead' },
  { icon: UsersRound, labelKey: 'ecommerce.customerGroups', path: '/portal/ecommerce/customer-groups', keywords: ['segment', 'group', 'targeting'], permission: 'CustomerGroupsRead' },
  { icon: Star, labelKey: 'ecommerce.reviews', path: '/portal/ecommerce/reviews', keywords: ['rating', 'feedback'], permission: 'ReviewsRead' },
  { icon: Heart, labelKey: 'ecommerce.wishlists', path: '/portal/ecommerce/wishlists', keywords: ['favorites', 'saved'], permission: 'WishlistsRead' },
  // Marketing
  { icon: Percent, labelKey: 'ecommerce.promotions', path: '/portal/marketing/promotions', keywords: ['discount', 'coupon', 'voucher', 'sale'], permission: 'PromotionsRead' },
  { icon: BarChart3, labelKey: 'ecommerce.reports', path: '/portal/marketing/reports', keywords: ['analytics', 'statistics', 'chart'], permission: 'ReportsRead' },
  // Content
  { icon: FileText, labelKey: 'blog.posts', path: '/portal/blog/posts', keywords: ['articles', 'content', 'writing'], permission: 'BlogPostsRead' },
  { icon: FolderTree, labelKey: 'blog.categories', path: '/portal/blog/categories', keywords: ['organize'], permission: 'BlogCategoriesRead' },
  { icon: Tag, labelKey: 'blog.tags', path: '/portal/blog/tags', keywords: ['label'], permission: 'BlogTagsRead' },
  // Users & Access
  { icon: Users, labelKey: 'users.title', path: '/portal/admin/users', keywords: ['people', 'accounts'], permission: 'UsersRead' },
  { icon: Shield, labelKey: 'roles.title', path: '/portal/admin/roles', keywords: ['permissions', 'access'], permission: 'RolesRead' },
  { icon: Building2, labelKey: 'tenants.title', path: '/portal/admin/tenants', keywords: ['organizations'], permission: 'TenantsRead' },
  // Settings
  { icon: SlidersHorizontal, labelKey: 'platformSettings.title', path: '/portal/admin/platform-settings', keywords: ['smtp', 'email'], permission: 'PlatformSettingsRead' },
  { icon: Palette, labelKey: 'tenantSettings.title', path: '/portal/admin/tenant-settings', keywords: ['branding', 'theme'], permission: 'TenantSettingsRead' },
  // System
  { icon: Activity, labelKey: 'activityTimeline.title', path: '/portal/activity-timeline', keywords: ['audit', 'logs', 'history'], permission: 'AuditRead' },
  { icon: Terminal, labelKey: 'developerLogs.title', path: '/portal/developer-logs', keywords: ['debug', 'console'], permission: 'SystemAdmin' },
  // Personal
  { icon: Settings, labelKey: 'settings.title', path: '/portal/settings', keywords: ['profile', 'preferences', 'account'] },
]

interface QuickAction {
  icon: React.ElementType
  labelKey: string
  action: () => void
  keywords?: string[]
}

/**
 * Read recent pages from localStorage
 */
const getRecentPages = (): string[] => {
  try {
    const stored = localStorage.getItem(RECENT_PAGES_KEY)
    if (!stored) return []
    const parsed = JSON.parse(stored)
    return Array.isArray(parsed) ? parsed.slice(0, MAX_RECENT_PAGES) : []
  } catch {
    return []
  }
}

/**
 * Save a visited page to recent pages in localStorage
 */
const saveRecentPage = (path: string) => {
  try {
    const current = getRecentPages()
    const filtered = current.filter((p) => p !== path)
    const updated = [path, ...filtered].slice(0, MAX_RECENT_PAGES)
    localStorage.setItem(RECENT_PAGES_KEY, JSON.stringify(updated))
  } catch {
    // Silently fail if localStorage is unavailable
  }
}

/**
 * CommandPalette - Global search and quick actions (Linear/Vercel quality)
 *
 * Opened with Cmd+K (Mac) or Ctrl+K (Windows).
 * Provides:
 * - Recently visited pages
 * - Quick navigation to any page (permission-filtered)
 * - Quick actions (theme toggle, create new entities)
 * - Full keyboard navigation
 */
export const CommandPalette = () => {
  const { t } = useTranslation('common')
  const navigate = useViewTransitionNavigate()
  const location = useLocation()
  const { isOpen, close, toggle } = useCommand()
  const { setTheme, resolvedTheme } = useTheme()
  const { density, setDensity } = useDensity()
  const { hasPermission } = usePermissions()
  const [search, setSearch] = useState('')
  const inputRef = useRef<HTMLInputElement>(null)
  const [recentPages, setRecentPages] = useState<string[]>([])

  // Load recent pages when palette opens
  useEffect(() => {
    if (isOpen) {
      setRecentPages(getRecentPages())
      requestAnimationFrame(() => {
        inputRef.current?.focus()
      })
    }
  }, [isOpen])

  // Register keyboard shortcuts
  useKeyboardShortcuts([
    { key: 'k', metaKey: true, callback: toggle, description: 'Open command palette' },
  ])

  // Clear search when closed
  useEffect(() => {
    if (!isOpen) {
      setSearch('')
    }
  }, [isOpen])

  // Track page visits for recently used
  useEffect(() => {
    if (location.pathname.startsWith('/portal')) {
      saveRecentPage(location.pathname)
    }
  }, [location.pathname])

  // Close when route changes
  useEffect(() => {
    close()
  }, [location.pathname, close])

  const handleSelect = useCallback((path: string) => {
    saveRecentPage(path)
    navigate(path)
    close()
  }, [navigate, close])

  // Filter navigation items by permission
  const visibleNavItems = useMemo(() =>
    NAVIGATION_ITEMS.filter(
      (item) => !item.permission || hasPermission(Permissions[item.permission])
    ),
    [hasPermission]
  )

  // Recently visited items (only show when not searching)
  const recentNavItems = useMemo(() => {
    if (search) return []
    return recentPages
      .map((path) => visibleNavItems.find((item) => item.path === path))
      .filter((item): item is NavigationItem => item !== undefined)
  }, [recentPages, visibleNavItems, search])

  // Quick actions (permission-filtered for create actions)
  const quickActions: QuickAction[] = useMemo(() => {
    const actions: QuickAction[] = [
      {
        icon: resolvedTheme === 'dark' ? Sun : Moon,
        labelKey: resolvedTheme === 'dark'
          ? 'commandPalette.switchToLight'
          : 'commandPalette.switchToDark',
        action: () => {
          setTheme(resolvedTheme === 'dark' ? 'light' : 'dark')
          close()
        },
        keywords: ['theme', 'appearance', 'mode'],
      },
      {
        icon: Monitor,
        labelKey: 'commandPalette.useSystemTheme',
        action: () => {
          setTheme('system')
          close()
        },
        keywords: ['theme', 'auto', 'system'],
      },
    ]

    if (hasPermission(Permissions.ProductsCreate)) {
      actions.push({
        icon: Plus,
        labelKey: 'commandPalette.createProduct',
        action: () => {
          navigate('/portal/ecommerce/products/new')
          close()
        },
        keywords: ['add', 'new', 'product'],
      })
    }

    if (hasPermission(Permissions.BlogPostsCreate)) {
      actions.push({
        icon: Plus,
        labelKey: 'commandPalette.createBlogPost',
        action: () => {
          navigate('/portal/blog/posts/new')
          close()
        },
        keywords: ['add', 'write', 'article', 'blog'],
      })
    }

    // Density toggle actions (show options other than current)
    const densityLabelKeys: Record<Density, string> = {
      compact: 'commandPalette.densityCompact',
      comfortable: 'commandPalette.densityComfortable',
      spacious: 'commandPalette.densitySpacious',
    }
    const densityActions = (['compact', 'comfortable', 'spacious'] as Density[])
      .filter((d) => d !== density)
      .map((d) => ({
        icon: SlidersHorizontal,
        labelKey: densityLabelKeys[d],
        action: () => {
          setDensity(d)
          close()
        },
        keywords: ['density', 'layout', 'spacing', d],
      }))

    return [...actions, ...densityActions]
  }, [resolvedTheme, setTheme, density, setDensity, close, navigate, hasPermission])

  if (!isOpen) return null

  return (
    <div className="fixed inset-0 z-50">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-background/80 backdrop-blur-sm"
        onClick={close}
        aria-hidden="true"
      />

      {/* Command Dialog */}
      <div className="absolute left-1/2 top-[15%] -translate-x-1/2 w-full max-w-lg px-4">
        <Command
          className="rounded-xl border shadow-2xl bg-popover overflow-hidden"
          loop
          onKeyDown={(e) => {
            if (e.key === 'Escape') {
              close()
            }
          }}
        >
          {/* Search Input */}
          <div className="flex items-center border-b px-3">
            <Search className="h-4 w-4 shrink-0 text-muted-foreground" />
            <Command.Input
              ref={inputRef}
              value={search}
              onValueChange={setSearch}
              placeholder={t('commandPalette.placeholder')}
              className="flex h-12 w-full bg-transparent py-3 px-2 text-sm outline-none
                         placeholder:text-muted-foreground"
            />
            <kbd className="hidden sm:inline-flex h-5 select-none items-center gap-1 rounded border
                           bg-muted px-1.5 font-mono text-[10px] font-medium text-muted-foreground">
              Esc
            </kbd>
          </div>

          {/* Results List */}
          <Command.List className="max-h-[300px] overflow-y-auto p-2">
            <Command.Empty className="py-6 text-center text-sm text-muted-foreground">
              {search
                ? t('commandPalette.noResultsFor', { query: search })
                : t('commandPalette.noResults')}
            </Command.Empty>

            {/* Recently Visited Group */}
            {recentNavItems.length > 0 && (
              <Command.Group heading={t('commandPalette.recentlyVisited')}>
                {recentNavItems.map((item) => (
                  <Command.Item
                    key={`recent-${item.path}`}
                    value={`recent ${t(item.labelKey)} ${item.keywords?.join(' ') || ''}`}
                    onSelect={() => handleSelect(item.path)}
                    className={cn(
                      'flex items-center gap-2 px-2 py-2 rounded-md cursor-pointer',
                      'aria-selected:bg-accent aria-selected:text-accent-foreground',
                      'data-[selected=true]:bg-accent data-[selected=true]:text-accent-foreground'
                    )}
                  >
                    <Clock className="h-4 w-4 text-muted-foreground" />
                    <span>{t(item.labelKey)}</span>
                    {location.pathname === item.path && (
                      <span className="ml-auto text-xs text-muted-foreground">
                        {t('commandPalette.current')}
                      </span>
                    )}
                  </Command.Item>
                ))}
              </Command.Group>
            )}

            {/* Navigation Group */}
            <Command.Group heading={t('commandPalette.navigation')}>
              {visibleNavItems.map((item) => (
                <Command.Item
                  key={item.path}
                  value={`${t(item.labelKey)} ${item.keywords?.join(' ') || ''}`}
                  onSelect={() => handleSelect(item.path)}
                  className={cn(
                    'flex items-center gap-2 px-2 py-2 rounded-md cursor-pointer',
                    'aria-selected:bg-accent aria-selected:text-accent-foreground',
                    'data-[selected=true]:bg-accent data-[selected=true]:text-accent-foreground'
                  )}
                >
                  <item.icon className="h-4 w-4 text-muted-foreground" />
                  <span>{t(item.labelKey)}</span>
                  {location.pathname === item.path && (
                    <span className="ml-auto text-xs text-muted-foreground">
                      {t('commandPalette.current')}
                    </span>
                  )}
                </Command.Item>
              ))}
            </Command.Group>

            {/* Quick Actions Group */}
            <Command.Group heading={t('commandPalette.actions')}>
              {quickActions.map((action) => (
                <Command.Item
                  key={action.labelKey}
                  value={`${t(action.labelKey)} ${action.keywords?.join(' ') || ''}`}
                  onSelect={action.action}
                  className={cn(
                    'flex items-center gap-2 px-2 py-2 rounded-md cursor-pointer',
                    'aria-selected:bg-accent aria-selected:text-accent-foreground',
                    'data-[selected=true]:bg-accent data-[selected=true]:text-accent-foreground'
                  )}
                >
                  <action.icon className="h-4 w-4 text-muted-foreground" />
                  <span>{t(action.labelKey)}</span>
                </Command.Item>
              ))}
            </Command.Group>
          </Command.List>

          {/* Footer with hints */}
          <div className="border-t px-3 py-2 text-xs text-muted-foreground flex items-center gap-4">
            <span className="flex items-center gap-1">
              <kbd className="px-1.5 py-0.5 rounded bg-muted font-mono">↑↓</kbd>
              <span>{t('commandPalette.hintNavigate')}</span>
            </span>
            <span className="flex items-center gap-1">
              <kbd className="px-1.5 py-0.5 rounded bg-muted font-mono">↵</kbd>
              <span>{t('commandPalette.hintSelect')}</span>
            </span>
            <span className="flex items-center gap-1">
              <kbd className="px-1.5 py-0.5 rounded bg-muted font-mono">esc</kbd>
              <span>{t('commandPalette.hintClose')}</span>
            </span>
            <span className="ml-auto hidden sm:inline text-muted-foreground/60">
              {formatShortcut({ key: 'k', metaKey: true })} {t('commandPalette.hintOpen')}
            </span>
          </div>
        </Command>
      </div>
    </div>
  )
}
