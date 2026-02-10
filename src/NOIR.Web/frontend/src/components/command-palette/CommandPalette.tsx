import { useEffect, useState, useRef } from 'react'
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
  Moon,
  Sun,
  Monitor,
  Activity,
  Terminal,
  SlidersHorizontal,
  Palette,
  Plus,
} from 'lucide-react'
import { useCommand } from './CommandContext'
import { useKeyboardShortcuts, formatShortcut } from '@/hooks/useKeyboardShortcuts'
import { useTheme } from '@/contexts/ThemeContext'
import { usePermissions, Permissions } from '@/hooks/usePermissions'
import { cn } from '@/lib/utils'

interface NavigationItem {
  icon: React.ElementType
  label: string
  path: string
  keywords?: string[]
  permission?: keyof typeof Permissions
}

/**
 * Navigation items for the command palette
 */
const NAVIGATION_ITEMS: NavigationItem[] = [
  { icon: LayoutDashboard, label: 'Dashboard', path: '/portal', keywords: ['home', 'main'] },
  { icon: Package, label: 'Products', path: '/portal/ecommerce/products', keywords: ['shop', 'store'], permission: 'ProductsRead' },
  { icon: Layers, label: 'Product Categories', path: '/portal/ecommerce/categories', keywords: ['shop'], permission: 'ProductCategoriesRead' },
  { icon: FileText, label: 'Blog Posts', path: '/portal/blog/posts', keywords: ['articles', 'content'], permission: 'BlogPostsRead' },
  { icon: FolderTree, label: 'Blog Categories', path: '/portal/blog/categories', permission: 'BlogCategoriesRead' },
  { icon: Tag, label: 'Blog Tags', path: '/portal/blog/tags', permission: 'BlogTagsRead' },
  { icon: Users, label: 'Users', path: '/portal/admin/users', keywords: ['people', 'accounts'], permission: 'UsersRead' },
  { icon: Shield, label: 'Roles', path: '/portal/admin/roles', keywords: ['permissions', 'access'], permission: 'RolesRead' },
  { icon: Building2, label: 'Tenants', path: '/portal/admin/tenants', keywords: ['organizations'], permission: 'TenantsRead' },
  { icon: SlidersHorizontal, label: 'Platform Settings', path: '/portal/admin/platform-settings', permission: 'PlatformSettingsRead' },
  { icon: Palette, label: 'Tenant Settings', path: '/portal/admin/tenant-settings', keywords: ['branding'], permission: 'TenantSettingsRead' },
  { icon: Activity, label: 'Activity Timeline', path: '/portal/activity-timeline', keywords: ['audit', 'logs'], permission: 'AuditRead' },
  { icon: Terminal, label: 'Developer Logs', path: '/portal/developer-logs', keywords: ['debug'], permission: 'SystemAdmin' },
  { icon: Settings, label: 'Settings', path: '/portal/settings', keywords: ['profile', 'preferences'] },
]

interface QuickAction {
  icon: React.ElementType
  label: string
  action: () => void
  keywords?: string[]
}

/**
 * CommandPalette - Global search and quick actions
 *
 * Opened with Cmd+K (Mac) or Ctrl+K (Windows).
 * Provides:
 * - Quick navigation to any page
 * - Quick actions (theme toggle, create new)
 * - Keyboard navigation
 */
export function CommandPalette() {
  const { t } = useTranslation('common')
  const navigate = useViewTransitionNavigate()
  const location = useLocation()
  const { isOpen, close, toggle } = useCommand()
  const { setTheme, resolvedTheme } = useTheme()
  const { hasPermission } = usePermissions()
  const [search, setSearch] = useState('')
  const inputRef = useRef<HTMLInputElement>(null)

  // Auto-focus input when palette opens
  useEffect(() => {
    if (isOpen) {
      // Small delay to ensure DOM is ready
      requestAnimationFrame(() => {
        inputRef.current?.focus()
      })
    }
  }, [isOpen])

  // Register keyboard shortcuts
  useKeyboardShortcuts([
    { key: 'k', metaKey: true, callback: toggle, description: 'Open command palette' },
  ])

  // Close on Escape (handled by cmdk, but we also want to clear search)
  useEffect(() => {
    if (!isOpen) {
      setSearch('')
    }
  }, [isOpen])

  // Close when route changes
  useEffect(() => {
    close()
  }, [location.pathname, close])

  const handleSelect = (path: string) => {
    navigate(path)
    close()
  }

  // Filter navigation items by permission
  const visibleNavItems = NAVIGATION_ITEMS.filter(
    (item) => !item.permission || hasPermission(Permissions[item.permission])
  )

  // Quick actions
  const quickActions: QuickAction[] = [
    {
      icon: resolvedTheme === 'dark' ? Sun : Moon,
      label: resolvedTheme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode',
      action: () => {
        setTheme(resolvedTheme === 'dark' ? 'light' : 'dark')
        close()
      },
      keywords: ['theme', 'appearance'],
    },
    {
      icon: Monitor,
      label: 'Use system theme',
      action: () => {
        setTheme('system')
        close()
      },
      keywords: ['theme', 'auto'],
    },
    {
      icon: Plus,
      label: 'Create new product',
      action: () => {
        navigate('/portal/ecommerce/products/new')
        close()
      },
      keywords: ['add', 'new'],
    },
    {
      icon: Plus,
      label: 'Create new blog post',
      action: () => {
        navigate('/portal/blog/posts/new')
        close()
      },
      keywords: ['add', 'write', 'article'],
    },
  ]

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
              placeholder={t('commandPalette.placeholder', 'Type a command or search...')}
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
              {t('commandPalette.noResults', 'No results found.')}
            </Command.Empty>

            {/* Navigation Group */}
            <Command.Group heading={t('commandPalette.navigation', 'Navigation')}>
              {visibleNavItems.map((item) => (
                <Command.Item
                  key={item.path}
                  value={`${item.label} ${item.keywords?.join(' ') || ''}`}
                  onSelect={() => handleSelect(item.path)}
                  className={cn(
                    'flex items-center gap-2 px-2 py-2 rounded-md cursor-pointer',
                    'aria-selected:bg-accent aria-selected:text-accent-foreground',
                    'data-[selected=true]:bg-accent data-[selected=true]:text-accent-foreground'
                  )}
                >
                  <item.icon className="h-4 w-4 text-muted-foreground" />
                  <span>{item.label}</span>
                  {location.pathname === item.path && (
                    <span className="ml-auto text-xs text-muted-foreground">Current</span>
                  )}
                </Command.Item>
              ))}
            </Command.Group>

            {/* Quick Actions Group */}
            <Command.Group heading={t('commandPalette.actions', 'Quick Actions')}>
              {quickActions.map((action, index) => (
                <Command.Item
                  key={index}
                  value={`${action.label} ${action.keywords?.join(' ') || ''}`}
                  onSelect={action.action}
                  className={cn(
                    'flex items-center gap-2 px-2 py-2 rounded-md cursor-pointer',
                    'aria-selected:bg-accent aria-selected:text-accent-foreground',
                    'data-[selected=true]:bg-accent data-[selected=true]:text-accent-foreground'
                  )}
                >
                  <action.icon className="h-4 w-4 text-muted-foreground" />
                  <span>{action.label}</span>
                </Command.Item>
              ))}
            </Command.Group>
          </Command.List>

          {/* Footer with hints */}
          <div className="border-t px-3 py-2 text-xs text-muted-foreground flex items-center gap-4">
            <span className="flex items-center gap-1">
              <kbd className="px-1.5 py-0.5 rounded bg-muted font-mono">↑↓</kbd>
              <span>navigate</span>
            </span>
            <span className="flex items-center gap-1">
              <kbd className="px-1.5 py-0.5 rounded bg-muted font-mono">↵</kbd>
              <span>select</span>
            </span>
            <span className="flex items-center gap-1">
              <kbd className="px-1.5 py-0.5 rounded bg-muted font-mono">esc</kbd>
              <span>close</span>
            </span>
            <span className="ml-auto hidden sm:inline text-muted-foreground/60">
              {formatShortcut({ key: 'k', metaKey: true })} to open
            </span>
          </div>
        </Command>
      </div>
    </div>
  )
}
