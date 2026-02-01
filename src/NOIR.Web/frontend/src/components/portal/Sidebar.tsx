import { useEffect, useState, useMemo } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { motion, AnimatePresence } from 'framer-motion'
import {
  ShieldCheck,
  LayoutDashboard,
  ChevronLeft,
  ChevronRight,
  Settings,
  LogOut,
  Menu,
  ChevronUp,
  Languages,
  Check,
  Mail,
  Building2,
  Shield,
  Users,
  Activity,
  Terminal,
  Bell,
  BellOff,
  MessageSquare,
  Sun,
  Moon,
  Monitor,
  FileText,
  FolderTree,
  Tag,
  SlidersHorizontal,
  Palette,
  Package,
  Layers,
  Search,
  X,
  Award,
  Tags,
} from 'lucide-react'
import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'
import { Button } from '@/components/ui/button'
import { TippyTooltip } from '@/components/ui/tippy-tooltip'
import {
  Sheet,
  SheetContent,
  SheetTrigger,
  SheetTitle,
} from '@/components/ui/sheet'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
  DropdownMenuSub,
  DropdownMenuSubTrigger,
  DropdownMenuSubContent,
  DropdownMenuPortal,
} from '@/components/ui/dropdown-menu'
import { useAuthContext } from '@/contexts/AuthContext'
import { getAvatarColor, getInitials } from '@/lib/gravatar'
import { usePermissions, Permissions, type PermissionKey } from '@/hooks/usePermissions'
import { useLanguage } from '@/i18n/useLanguage'
import { languageFlags } from '@/i18n/languageFlags'
import type { SupportedLanguage } from '@/i18n'
import { useTheme } from '@/contexts/ThemeContext'
import { useNotificationContext } from '@/contexts/NotificationContext'
import { useBranding } from '@/contexts/BrandingContext'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { Badge } from '@/components/ui/badge'
import { isPlatformAdmin } from '@/lib/roles'

interface NavItem {
  titleKey: string
  icon: React.ElementType
  path: string
  /** Permission required to view this menu item (optional - always shown if not specified) */
  permission?: PermissionKey
}

interface NavSection {
  labelKey?: string // undefined means no section header (primary items)
  items: NavItem[]
}

// Task-based navigation structure following standard admin dashboard patterns:
// 1. Dashboard - Overview (always visible, no section label)
// 2. Content - Business content creation (Blog)
// 3. Users & Access - Who does what (Users, Roles, Tenants)
// 4. Settings - System configuration (Platform Settings has SMTP/Email/Legal tabs, Tenant Settings has Branding/Contact/Regional/SMTP/Email/Legal tabs)
// 5. System - Monitoring and admin tools
const navSections: NavSection[] = [
  {
    // Primary - Dashboard (no section label)
    items: [
      { titleKey: 'dashboard.title', icon: LayoutDashboard, path: '/portal' },
    ],
  },
  {
    // E-commerce - Product management
    labelKey: 'nav.ecommerce',
    items: [
      { titleKey: 'ecommerce.products', icon: Package, path: '/portal/ecommerce/products', permission: Permissions.ProductsRead },
      { titleKey: 'ecommerce.categories', icon: Layers, path: '/portal/ecommerce/categories', permission: Permissions.ProductCategoriesRead },
      { titleKey: 'ecommerce.brands', icon: Award, path: '/portal/ecommerce/brands', permission: Permissions.BrandsRead },
      { titleKey: 'ecommerce.attributes', icon: Tags, path: '/portal/ecommerce/attributes', permission: Permissions.AttributesRead },
    ],
  },
  {
    // Content - Business content creation
    labelKey: 'nav.content',
    items: [
      { titleKey: 'blog.posts', icon: FileText, path: '/portal/blog/posts', permission: Permissions.BlogPostsRead },
      { titleKey: 'blog.categories', icon: FolderTree, path: '/portal/blog/categories', permission: Permissions.BlogCategoriesRead },
      { titleKey: 'blog.tags', icon: Tag, path: '/portal/blog/tags', permission: Permissions.BlogTagsRead },
    ],
  },
  {
    // Users & Access - Who can do what
    labelKey: 'nav.usersAccess',
    items: [
      { titleKey: 'users.title', icon: Users, path: '/portal/admin/users', permission: Permissions.UsersRead },
      { titleKey: 'roles.title', icon: Shield, path: '/portal/admin/roles', permission: Permissions.RolesRead },
      { titleKey: 'tenants.title', icon: Building2, path: '/portal/admin/tenants', permission: Permissions.TenantsRead },
    ],
  },
  {
    // Settings - System configuration (consolidated with tabs)
    labelKey: 'nav.settings',
    items: [
      { titleKey: 'platformSettings.title', icon: SlidersHorizontal, path: '/portal/admin/platform-settings', permission: Permissions.PlatformSettingsRead },
      { titleKey: 'tenantSettings.title', icon: Palette, path: '/portal/admin/tenant-settings', permission: Permissions.TenantSettingsRead },
    ],
  },
  {
    // System - Monitoring and admin tools
    labelKey: 'nav.system',
    items: [
      { titleKey: 'activityTimeline.title', icon: Activity, path: '/portal/activity-timeline', permission: Permissions.AuditRead },
      { titleKey: 'developerLogs.title', icon: Terminal, path: '/portal/developer-logs', permission: Permissions.SystemAdmin },
    ],
  },
]

/**
 * Utility to check if a path is active
 */
const isActivePath = (currentPathname: string, itemPath: string): boolean => {
  if (itemPath === '/portal') {
    return currentPathname === '/portal'
  }
  return currentPathname.startsWith(itemPath)
}

// User data type
interface UserData {
  fullName?: string
  firstName?: string | null
  lastName?: string | null
  email?: string
  roles?: string[]
  avatarUrl?: string | null
}

/**
 * UserProfileDropdown - With integrated language switcher
 */
interface UserProfileDropdownProps {
  isExpanded: boolean
  t: (key: string) => string
  user?: UserData | null
}

function UserProfileDropdown({ isExpanded, t, user }: UserProfileDropdownProps) {
  const displayName = user?.fullName || 'User'
  const displayEmail = user?.email || 'user@example.com'
  // Use same initials logic as ProfileAvatar for consistency
  const initials = getInitials(user?.firstName ?? null, user?.lastName ?? null, displayEmail)
  // Use email-based color for consistency with ProfileAvatar
  const avatarColor = getAvatarColor(displayEmail)
  const avatarUrl = user?.avatarUrl
  const { currentLanguage, languages, changeLanguage } = useLanguage()
  const { logout, checkAuth } = useAuthContext()
  const navigate = useNavigate()
  const { theme, setTheme } = useTheme()

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  // Listen for avatar update events
  useEffect(() => {
    const handleAvatarUpdate = () => {
      checkAuth()
    }
    window.addEventListener('avatar-updated', handleAvatarUpdate)
    return () => window.removeEventListener('avatar-updated', handleAvatarUpdate)
  }, [checkAuth])

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          className={cn(
            'w-full h-auto p-2 transition-colors relative group',
            isExpanded ? 'hover:bg-sidebar-accent rounded-lg' : 'hover:bg-transparent p-0 justify-center'
          )}
        >
          <div className={cn(
            'flex items-center gap-3 w-full',
            !isExpanded && 'justify-center'
          )}>
            <div
              className={cn(
                'h-10 w-10 rounded-full flex-shrink-0 transition-all overflow-hidden',
                !avatarUrl && 'flex items-center justify-center text-white font-semibold text-sm',
                isExpanded ? 'ring-2 ring-transparent group-hover:ring-4 group-hover:ring-sidebar-primary/20' : 'group-hover:ring-[6px] group-hover:ring-sidebar-primary/15'
              )}
              style={!avatarUrl ? { backgroundColor: avatarColor } : undefined}
            >
              {avatarUrl ? (
                <img
                  src={avatarUrl}
                  alt={displayName}
                  className="h-full w-full object-cover"
                />
              ) : (
                initials
              )}
            </div>
            {isExpanded && (
              <>
                <div className="flex-1 min-w-0 text-left">
                  <p className="text-sm font-medium text-sidebar-foreground truncate">{displayName}</p>
                  <p className="text-xs text-sidebar-foreground/60 truncate">{displayEmail}</p>
                </div>
                <ChevronUp className="h-4 w-4 text-sidebar-foreground/60 group-hover:text-sidebar-foreground transition-colors" />
              </>
            )}
          </div>
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent
        className="w-56"
        align={isExpanded ? 'end' : 'start'}
        side="top"
        sideOffset={isExpanded ? 0 : 8}
      >
        <DropdownMenuLabel>
          <div className="flex flex-col space-y-1">
            <p className="text-sm font-medium leading-none">{displayName}</p>
            <p className="text-xs leading-none text-muted-foreground">{displayEmail}</p>
          </div>
        </DropdownMenuLabel>
        <DropdownMenuSeparator />
        <DropdownMenuItem asChild>
          <Link to="/portal/settings">
            <Settings className="mr-2 h-4 w-4" />
            <span>{t('settings.title')}</span>
          </Link>
        </DropdownMenuItem>
        {/* Language Switcher Sub-menu */}
        <DropdownMenuSub>
          <DropdownMenuSubTrigger>
            <Languages className="mr-2 h-4 w-4" />
            <span>{t('labels.language')}</span>
          </DropdownMenuSubTrigger>
          <DropdownMenuPortal>
            <DropdownMenuSubContent className="min-w-[160px]">
              {(Object.entries(languages) as [SupportedLanguage, typeof languages[SupportedLanguage]][]).map(
                ([code, lang]) => (
                  <DropdownMenuItem
                    key={code}
                    onClick={() => changeLanguage(code)}
                    className="flex items-center justify-between"
                  >
                    <div className="flex items-center gap-2">
                      <span className="text-base">{languageFlags[code]}</span>
                      <span>{lang.nativeName}</span>
                    </div>
                    {currentLanguage === code && (
                      <Check className="h-4 w-4 text-sidebar-primary" />
                    )}
                  </DropdownMenuItem>
                )
              )}
            </DropdownMenuSubContent>
          </DropdownMenuPortal>
        </DropdownMenuSub>
        {/* Theme Switcher Sub-menu */}
        <DropdownMenuSub>
          <DropdownMenuSubTrigger>
            {theme === 'dark' ? (
              <Moon className="mr-2 h-4 w-4" />
            ) : theme === 'light' ? (
              <Sun className="mr-2 h-4 w-4" />
            ) : (
              <Monitor className="mr-2 h-4 w-4" />
            )}
            <span>{t('labels.theme')}</span>
          </DropdownMenuSubTrigger>
          <DropdownMenuPortal>
            <DropdownMenuSubContent className="min-w-[140px]">
              <DropdownMenuItem
                onClick={() => setTheme('light')}
                className="flex items-center justify-between"
              >
                <div className="flex items-center gap-2">
                  <Sun className="h-4 w-4" />
                  <span>{t('theme.light')}</span>
                </div>
                {theme === 'light' && (
                  <Check className="h-4 w-4 text-sidebar-primary" />
                )}
              </DropdownMenuItem>
              <DropdownMenuItem
                onClick={() => setTheme('dark')}
                className="flex items-center justify-between"
              >
                <div className="flex items-center gap-2">
                  <Moon className="h-4 w-4" />
                  <span>{t('theme.dark')}</span>
                </div>
                {theme === 'dark' && (
                  <Check className="h-4 w-4 text-sidebar-primary" />
                )}
              </DropdownMenuItem>
              <DropdownMenuItem
                onClick={() => setTheme('system')}
                className="flex items-center justify-between"
              >
                <div className="flex items-center gap-2">
                  <Monitor className="h-4 w-4" />
                  <span>{t('theme.system')}</span>
                </div>
                {theme === 'system' && (
                  <Check className="h-4 w-4 text-sidebar-primary" />
                )}
              </DropdownMenuItem>
            </DropdownMenuSubContent>
          </DropdownMenuPortal>
        </DropdownMenuSub>
        <DropdownMenuSeparator />
        <DropdownMenuItem onClick={handleLogout} className="text-red-600 focus:text-red-600">
          <LogOut className="mr-2 h-4 w-4" />
          <span>{t('auth.signOut')}</span>
        </DropdownMenuItem>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

// Props for extracted SidebarContent component
interface SidebarContentProps {
  isExpanded: boolean
  onToggle?: () => void
  onItemClick?: (path: string) => void
  t: (key: string) => string
  pathname: string
  user?: UserData | null
  logoUrl?: string | null
  searchQuery?: string
  onSearchChange?: (query: string) => void
}


/**
 * Get time label for grouping notifications
 */
function getNotificationTimeLabel(dateString: string): string {
  const date = new Date(dateString)
  const now = new Date()
  const diffTime = Math.abs(now.getTime() - date.getTime())
  const diffDays = Math.floor(diffTime / (1000 * 60 * 60 * 24))

  if (diffDays === 0) return 'Today'
  if (diffDays === 1) return 'Yesterday'
  if (diffDays <= 7) return 'Earlier this week'
  return 'Older'
}

/**
 * Animated Empty State for Notifications
 */
function NotificationEmptyState() {
  return (
    <div className="flex flex-col items-center justify-center py-10 px-4">
      {/* Animated illustration with 3 icons */}
      <div className="flex justify-center isolate mb-5">
        <motion.div
          initial={{ opacity: 0, y: 15 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.1, type: 'spring', stiffness: 300, damping: 30 }}
          className="bg-background size-10 grid place-items-center rounded-lg relative left-2 top-1 -rotate-6 shadow-md ring-1 ring-border"
        >
          <Mail className="size-5 text-muted-foreground" />
        </motion.div>
        <motion.div
          initial={{ opacity: 0, y: 15 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.2, type: 'spring', stiffness: 300, damping: 30 }}
          className="bg-background size-10 grid place-items-center rounded-lg relative z-10 shadow-md ring-1 ring-border"
        >
          <BellOff className="size-5 text-muted-foreground" />
        </motion.div>
        <motion.div
          initial={{ opacity: 0, y: 15 }}
          animate={{ opacity: 1, y: 0 }}
          transition={{ delay: 0.3, type: 'spring', stiffness: 300, damping: 30 }}
          className="bg-background size-10 grid place-items-center rounded-lg relative right-2 top-1 rotate-6 shadow-md ring-1 ring-border"
        >
          <MessageSquare className="size-5 text-muted-foreground" />
        </motion.div>
      </div>

      <motion.h4
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ delay: 0.4 }}
        className="text-sm font-semibold text-foreground mb-1"
      >
        All caught up!
      </motion.h4>
      <motion.p
        initial={{ opacity: 0 }}
        animate={{ opacity: 1 }}
        transition={{ delay: 0.5 }}
        className="text-xs text-muted-foreground text-center max-w-[200px]"
      >
        No new notifications. We'll let you know when something arrives.
      </motion.p>
    </div>
  )
}

/**
 * Notification Sidebar Item with Dropdown
 */
function NotificationSidebarItem({ isExpanded, t, onItemClick }: { isExpanded: boolean; t: (key: string) => string; onItemClick?: (path: string) => void }) {
  const { notifications, unreadCount, isLoading, markAsRead, markAllAsRead, connectionState } = useNotificationContext()
  const { formatRelativeTime } = useRegionalSettings()
  const location = useLocation()
  const isActive = location.pathname === '/portal/notifications'
  const [open, setOpen] = useState(false)

  const displayCount = unreadCount > 99 ? '99+' : unreadCount.toString()
  const recentNotifications = notifications.slice(0, 5)

  // Group notifications by time
  const groupedNotifications = recentNotifications.reduce((groups, notification) => {
    const label = getNotificationTimeLabel(notification.createdAt)
    if (!groups[label]) groups[label] = []
    groups[label].push(notification)
    return groups
  }, {} as Record<string, typeof notifications>)

  const groupOrder = ['Today', 'Yesterday', 'Earlier this week', 'Older']
  const sortedGroups = groupOrder.filter(label => groupedNotifications[label]?.length > 0)

  const handleMarkAllAsRead = async () => {
    try {
      await markAllAsRead()
    } catch {
      // Error handled by NotificationContext
    }
  }

  const bellButton = (
    <Button
      variant="ghost"
      className={cn(
        'w-full justify-start relative overflow-hidden transition-all duration-200',
        isExpanded ? 'px-3' : 'px-0 justify-center',
        isActive && 'bg-gradient-to-r from-sidebar-primary/20 to-sidebar-primary/10 text-sidebar-primary hover:from-sidebar-primary/30 hover:to-sidebar-primary/20',
        !isActive && 'text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground'
      )}
    >
      {isActive && (
        <div className="absolute left-0 top-0 bottom-0 w-1 bg-sidebar-primary rounded-r-full" />
      )}
      <div className="relative">
        <Bell className={cn('h-5 w-5 flex-shrink-0', isExpanded && 'mr-3')} />
        {unreadCount > 0 && !isExpanded && (
          <Badge
            variant="destructive"
            className="absolute -top-2 -right-2 h-4 min-w-4 p-0 text-[9px] flex items-center justify-center"
          >
            {displayCount}
          </Badge>
        )}
        {/* Connection state indicator */}
        {connectionState === 'connecting' || connectionState === 'reconnecting' ? (
          <span className="absolute bottom-0 right-0 size-2 rounded-full bg-amber-500 animate-pulse" />
        ) : connectionState === 'disconnected' ? (
          <span className="absolute bottom-0 right-0 size-2 rounded-full bg-red-500" />
        ) : null}
      </div>
      {isExpanded && (
        <>
          <span className="flex-1 text-left">{t('notifications.title')}</span>
          {unreadCount > 0 && (
            <Badge variant="destructive" className="ml-auto h-5 min-w-5 p-0 px-1.5 text-[10px]">
              {displayCount}
            </Badge>
          )}
        </>
      )}
    </Button>
  )

  return (
    <DropdownMenu open={open} onOpenChange={setOpen}>
      <DropdownMenuTrigger asChild>
        {!isExpanded ? (
          <TippyTooltip
            content={unreadCount > 0 ? `${t('notifications.title')} (${unreadCount})` : t('notifications.title')}
            placement="right"
            delay={[0, 0]}
          >
            {bellButton}
          </TippyTooltip>
        ) : (
          bellButton
        )}
      </DropdownMenuTrigger>
      <DropdownMenuContent
        align="start"
        side="top"
        sideOffset={8}
        className="w-80 p-0 overflow-hidden"
      >
        {/* Header */}
        <div className="flex items-center justify-between p-3 border-b">
          <div>
            <h3 className="text-sm font-semibold">{t('notifications.title')}</h3>
            {unreadCount > 0 && (
              <p className="text-[11px] text-muted-foreground mt-0.5">
                {unreadCount} unread
              </p>
            )}
          </div>
          {unreadCount > 0 && (
            <Button
              variant="ghost"
              size="sm"
              className="h-7 text-xs px-2"
              onClick={handleMarkAllAsRead}
            >
              <Check className="size-3 mr-1" />
              Mark all read
            </Button>
          )}
        </div>

        {/* Notification list */}
        <div className="max-h-[360px] overflow-y-auto">
          {isLoading && notifications.length === 0 ? (
            <div className="flex flex-col items-center justify-center py-8 gap-2">
              {/* Skeleton loading */}
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="flex items-start gap-3 w-full px-3">
                  <div className="mt-1 h-2 w-2 rounded-full bg-muted animate-pulse" />
                  <div className="flex-1 space-y-1.5">
                    <div className="h-3 w-3/4 rounded bg-muted animate-pulse" />
                    <div className="h-2.5 w-1/2 rounded bg-muted animate-pulse" />
                  </div>
                </div>
              ))}
            </div>
          ) : recentNotifications.length === 0 ? (
            <NotificationEmptyState />
          ) : (
            <div className="py-1">
              <AnimatePresence>
                {sortedGroups.map((groupLabel) => (
                  <div key={groupLabel} className="mb-1 last:mb-0">
                    <div className="px-3 py-1.5">
                      <span className="text-[10px] font-semibold text-muted-foreground uppercase tracking-wider">
                        {groupLabel}
                      </span>
                    </div>
                    {groupedNotifications[groupLabel].map((notification) => (
                      <motion.div
                        key={notification.id}
                        initial={{ opacity: 0, x: -10 }}
                        animate={{ opacity: 1, x: 0 }}
                        exit={{ opacity: 0, x: 10 }}
                        transition={{ type: 'spring', stiffness: 300, damping: 30 }}
                      >
                        <DropdownMenuItem
                          className={cn(
                            'flex items-start gap-2.5 cursor-pointer py-2.5 px-3 mx-1 rounded-md',
                            !notification.isRead && 'bg-primary/5'
                          )}
                          onClick={() => {
                            if (!notification.isRead) {
                              markAsRead(notification.id)
                            }
                          }}
                        >
                          <div className={cn(
                            'mt-1.5 h-2 w-2 rounded-full flex-shrink-0',
                            notification.isRead ? 'bg-transparent' : 'bg-primary'
                          )} />
                          <div className="flex-1 min-w-0">
                            <div className="flex items-start justify-between gap-2">
                              <p className={cn(
                                'text-sm truncate',
                                !notification.isRead && 'font-medium'
                              )}>
                                {notification.title}
                              </p>
                              <span className="text-[10px] text-muted-foreground shrink-0">
                                {formatRelativeTime(notification.createdAt)}
                              </span>
                            </div>
                            <p className="text-xs text-muted-foreground line-clamp-2 mt-0.5">
                              {notification.message}
                            </p>
                          </div>
                        </DropdownMenuItem>
                      </motion.div>
                    ))}
                  </div>
                ))}
              </AnimatePresence>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="p-2 border-t bg-muted/30">
          <Button
            variant="ghost"
            className="w-full justify-center text-sm h-8"
            asChild
            onClick={() => setOpen(false)}
          >
            <Link
              to="/portal/notifications"
              onClick={() => onItemClick?.('/portal/notifications')}
            >
              {t('notifications.viewAll')}
            </Link>
          </Button>
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  )
}

/**
 * SidebarContent - Task-based navigation with sections
 */
function SidebarContent({
  isExpanded,
  onToggle,
  onItemClick,
  t,
  pathname,
  user,
  logoUrl,
  searchQuery = '',
  onSearchChange,
}: SidebarContentProps) {
  const isActive = (path: string) => isActivePath(pathname, path)
  const { hasPermission } = usePermissions()

  // Filter sections and items based on permissions and search query
  const visibleSections = useMemo(() => {
    const searchLower = searchQuery.toLowerCase().trim()

    return navSections
      .map(section => ({
        ...section,
        items: section.items.filter(item => {
          // First check permissions
          if (item.permission && !hasPermission(item.permission)) return false

          // Then check search query
          if (!searchLower) return true
          const itemLabel = t(item.titleKey).toLowerCase()
          return itemLabel.includes(searchLower)
        }),
      }))
      .filter(section => section.items.length > 0)
  }, [searchQuery, hasPermission, t])

  const renderNavItem = (item: NavItem) => {
    const Icon = item.icon
    const active = isActive(item.path)

    const buttonContent = (
      <Button
        variant="ghost"
        asChild
        className={cn(
          'w-full justify-start relative overflow-hidden transition-all duration-200',
          isExpanded ? 'px-3' : 'px-0 justify-center',
          active && 'bg-gradient-to-r from-sidebar-primary/20 to-sidebar-primary/10 text-sidebar-primary hover:from-sidebar-primary/30 hover:to-sidebar-primary/20',
          !active && 'text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground'
        )}
      >
        <Link to={item.path} onClick={() => onItemClick?.(item.path)}>
          {active && (
            <div className="absolute left-0 top-0 bottom-0 w-1 bg-sidebar-primary rounded-r-full" />
          )}
          <Icon className={cn(
            'h-5 w-5 flex-shrink-0',
            isExpanded && 'mr-3'
          )} />
          {isExpanded && (
            <span className="flex-1 text-left">{t(item.titleKey)}</span>
          )}
        </Link>
      </Button>
    )

    if (!isExpanded) {
      return (
        <TippyTooltip
          key={item.path}
          content={t(item.titleKey)}
          placement="right"
          delay={[0, 0]}
        >
          {buttonContent}
        </TippyTooltip>
      )
    }

    return <div key={item.path}>{buttonContent}</div>
  }

  return (
    <div className="flex flex-col h-full">
      {/* Header with Logo and Toggle */}
      <div className="flex items-center justify-between p-4 border-b border-sidebar-border">
        {isExpanded && (
          <Link to="/portal" className="flex items-center gap-3 group" onClick={() => onItemClick?.('/portal')}>
            {logoUrl ? (
              <img
                src={logoUrl}
                alt="Logo"
                className="h-10 max-w-[160px] object-contain"
              />
            ) : (
              <>
                <div className="h-10 w-10 rounded-xl bg-gradient-to-br from-sidebar-primary to-sidebar-primary/60 flex items-center justify-center text-sidebar-primary-foreground shadow-lg group-hover:shadow-xl transition-all">
                  <ShieldCheck className="h-5 w-5" />
                </div>
                <h2 className="text-lg font-semibold text-sidebar-foreground">NOIR</h2>
              </>
            )}
          </Link>
        )}
        <Button
          variant="ghost"
          size="icon"
          onClick={onToggle}
          aria-label={isExpanded ? t('nav.collapse') : t('nav.expand')}
          className={cn(
            'h-8 w-8 transition-all text-sidebar-foreground hover:bg-sidebar-accent',
            !isExpanded && 'mx-auto'
          )}
        >
          {isExpanded ? (
            <ChevronLeft className="h-4 w-4" />
          ) : (
            <ChevronRight className="h-4 w-4" />
          )}
        </Button>
      </div>

      {/* Search Input (only when expanded) */}
      {isExpanded && onSearchChange && (
        <div className="px-3 py-2 border-b border-sidebar-border">
          <div className="relative">
            <Search className="absolute left-2.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground pointer-events-none" />
            <Input
              type="text"
              placeholder={t('nav.searchMenu')}
              value={searchQuery}
              onChange={(e) => onSearchChange(e.target.value)}
              className="h-9 pl-8 pr-8 bg-sidebar-accent/50 border-sidebar-border text-sm placeholder:text-muted-foreground/70"
            />
            {searchQuery && (
              <Button
                variant="ghost"
                size="icon"
                className="absolute right-1 top-1/2 -translate-y-1/2 h-6 w-6 text-muted-foreground hover:text-foreground"
                onClick={() => onSearchChange('')}
              >
                <X className="h-3.5 w-3.5" />
              </Button>
            )}
          </div>
          {searchQuery && visibleSections.length === 0 && (
            <p className="text-xs text-muted-foreground mt-2 text-center">
              {t('nav.noResults')}
            </p>
          )}
        </div>
      )}

      {/* Navigation - Task-based sections */}
      <nav className="flex-1 overflow-y-auto py-4">
        {visibleSections.map((section, index) => (
          <div key={section.labelKey || 'primary'} className={cn(index > 0 && 'mt-4')}>
            {/* Section label (only if defined and expanded) */}
            {section.labelKey && isExpanded && (
              <div className="px-4 mb-2">
                <p className="text-xs font-semibold text-sidebar-foreground/50 uppercase tracking-wider">
                  {t(section.labelKey)}
                </p>
              </div>
            )}
            {/* Section divider when collapsed (except for first section) */}
            {section.labelKey && !isExpanded && index > 0 && (
              <div className="mx-3 mb-2 border-t border-sidebar-border" />
            )}
            <div className="space-y-1 px-2">
              {section.items.map(renderNavItem)}
            </div>
          </div>
        ))}
      </nav>

      {/* Footer Section */}
      <div className="border-t border-sidebar-border">
        {/*
          Notifications hidden for Platform Admin.
          Platform Admins operate at system level and don't receive tenant-scoped notifications.
        */}
        {!isPlatformAdmin(user?.roles) && (
          <div className={cn('px-2 pt-3', isExpanded ? 'pb-2' : 'pb-3')}>
            <NotificationSidebarItem isExpanded={isExpanded} t={t} onItemClick={onItemClick} />
          </div>
        )}

        {/* User Profile (includes theme toggle in dropdown) */}
        <div className={cn('p-3 pt-1', !isPlatformAdmin(user?.roles) && 'border-t border-sidebar-border')}>
          <UserProfileDropdown isExpanded={isExpanded} t={t} user={user} />
        </div>
      </div>
    </div>
  )
}

// Props for main Sidebar component (desktop only)
interface SidebarProps {
  collapsed?: boolean
  onToggle?: () => void
}

/**
 * Portal Sidebar - Simplified with Dashboard only
 */
export function Sidebar({ collapsed = false, onToggle }: SidebarProps) {
  const { t } = useTranslation('common')
  const location = useLocation()
  const { user } = useAuthContext()
  const { branding } = useBranding()
  const [searchQuery, setSearchQuery] = useState('')

  // Clear search when sidebar collapses
  useEffect(() => {
    if (collapsed) {
      setSearchQuery('')
    }
  }, [collapsed])

  return (
    <aside
      className={cn(
        'hidden lg:flex flex-col h-screen bg-sidebar border-r border-sidebar-border transition-all duration-300 ease-in-out',
        collapsed ? 'w-20' : 'w-64'
      )}
    >
      <SidebarContent
        isExpanded={!collapsed}
        onToggle={onToggle}
        t={t}
        pathname={location.pathname}
        user={user}
        logoUrl={branding?.logoUrl}
        searchQuery={searchQuery}
        onSearchChange={setSearchQuery}
      />
    </aside>
  )
}

/**
 * Mobile Sidebar Trigger - Task-based navigation sections
 */
export function MobileSidebarTrigger({
  open,
  onOpenChange,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
}) {
  const { t } = useTranslation('common')
  const location = useLocation()
  const { user } = useAuthContext()
  const { hasPermission } = usePermissions()
  const { unreadCount } = useNotificationContext()
  const { branding } = useBranding()

  // Filter sections and items based on permissions
  const visibleSections = navSections
    .map(section => ({
      ...section,
      items: section.items.filter(item => !item.permission || hasPermission(item.permission)),
    }))
    .filter(section => section.items.length > 0)

  const renderMobileNavItem = (item: NavItem) => {
    const Icon = item.icon
    const active = isActivePath(location.pathname, item.path)

    return (
      <Button
        key={item.path}
        variant="ghost"
        asChild
        className={cn(
          'w-full justify-start relative overflow-hidden transition-all duration-200 px-3',
          active && 'bg-gradient-to-r from-sidebar-primary/20 to-sidebar-primary/10 text-sidebar-primary hover:from-sidebar-primary/30 hover:to-sidebar-primary/20',
          !active && 'text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground'
        )}
      >
        <Link to={item.path} onClick={() => onOpenChange(false)}>
          {active && (
            <div className="absolute left-0 top-0 bottom-0 w-1 bg-sidebar-primary rounded-r-full" />
          )}
          <Icon className="h-5 w-5 flex-shrink-0 mr-3" />
          <span className="flex-1 text-left">{t(item.titleKey)}</span>
        </Link>
      </Button>
    )
  }

  const notificationActive = location.pathname === '/portal/notifications'
  const displayCount = unreadCount > 99 ? '99+' : unreadCount.toString()

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetTrigger asChild>
        <Button variant="outline" size="icon" className="lg:hidden h-10 w-10" aria-label={t('nav.openMenu')}>
          <Menu className="h-5 w-5" />
        </Button>
      </SheetTrigger>
      <SheetContent side="left" className="p-0 w-72">
        <SheetTitle className="sr-only">{t('nav.menu')}</SheetTitle>
        <div className="flex flex-col h-full bg-sidebar">
          {/* Mobile Header */}
          <div className="flex items-center p-4 border-b border-sidebar-border">
            <Link to="/portal" className="flex items-center gap-3" onClick={() => onOpenChange(false)}>
              {branding?.logoUrl ? (
                <img
                  src={branding.logoUrl}
                  alt="Logo"
                  className="h-10 max-w-[160px] object-contain"
                />
              ) : (
                <>
                  <div className="h-10 w-10 rounded-xl bg-gradient-to-br from-sidebar-primary to-sidebar-primary/60 flex items-center justify-center text-sidebar-primary-foreground shadow-lg">
                    <ShieldCheck className="h-5 w-5" />
                  </div>
                  <h2 className="text-lg font-semibold text-sidebar-foreground">NOIR</h2>
                </>
              )}
            </Link>
          </div>

          {/* Mobile Navigation - Task-based sections */}
          <nav className="flex-1 overflow-y-auto py-4">
            {visibleSections.map((section, index) => (
              <div key={section.labelKey || 'primary'} className={cn(index > 0 && 'mt-4')}>
                {section.labelKey && (
                  <div className="px-4 mb-2">
                    <p className="text-xs font-semibold text-sidebar-foreground/50 uppercase tracking-wider">
                      {t(section.labelKey)}
                    </p>
                  </div>
                )}
                <div className="space-y-1 px-2">
                  {section.items.map(renderMobileNavItem)}
                </div>
              </div>
            ))}
          </nav>

          {/* Footer Section */}
          <div className="border-t border-sidebar-border">
            {/*
              Notifications hidden for Platform Admin.
              Platform Admins operate at system level and don't receive tenant-scoped notifications.
            */}
            {!isPlatformAdmin(user?.roles) && (
              <div className="px-2 pt-3 pb-2">
                <Button
                  variant="ghost"
                  asChild
                  className={cn(
                    'w-full justify-start relative overflow-hidden transition-all duration-200 px-3',
                    notificationActive && 'bg-gradient-to-r from-sidebar-primary/20 to-sidebar-primary/10 text-sidebar-primary hover:from-sidebar-primary/30 hover:to-sidebar-primary/20',
                    !notificationActive && 'text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground'
                  )}
                >
                  <Link to="/portal/notifications" onClick={() => onOpenChange(false)}>
                    {notificationActive && (
                      <div className="absolute left-0 top-0 bottom-0 w-1 bg-sidebar-primary rounded-r-full" />
                    )}
                    <Bell className="h-5 w-5 flex-shrink-0 mr-3" />
                    <span className="flex-1 text-left">{t('notifications.title')}</span>
                    {unreadCount > 0 && (
                      <Badge variant="destructive" className="ml-auto h-5 min-w-5 p-0 px-1.5 text-[10px]">
                        {displayCount}
                      </Badge>
                    )}
                  </Link>
                </Button>
              </div>
            )}

            {/* Mobile User Profile (includes theme toggle in dropdown) */}
            <div className={cn('p-3 pt-1', !isPlatformAdmin(user?.roles) && 'border-t border-sidebar-border')}>
              <UserProfileDropdown isExpanded={true} t={t} user={user} />
            </div>
          </div>
        </div>
      </SheetContent>
    </Sheet>
  )
}
