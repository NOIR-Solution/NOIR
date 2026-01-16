import { useEffect } from 'react'
import { Link, useLocation, useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
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
} from 'lucide-react'
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
import { usePermissions, Permissions, type PermissionKey } from '@/hooks/usePermissions'
import { useLanguage } from '@/i18n/useLanguage'
import { languageFlags } from '@/i18n/languageFlags'
import type { SupportedLanguage } from '@/i18n'
import { ThemeToggle, ThemeToggleCompact } from '@/components/ui/theme-toggle'

interface NavItem {
  titleKey: string
  icon: React.ElementType
  path: string
  /** Permission required to view this menu item (optional - always shown if not specified) */
  permission?: PermissionKey
}

// Navigation items - Settings is accessed via profile dropdown, with admin section
const navItems: NavItem[] = [
  { titleKey: 'dashboard.title', icon: LayoutDashboard, path: '/portal' },
  { titleKey: 'emailTemplates.title', icon: Mail, path: '/portal/email-templates', permission: Permissions.EmailTemplatesRead },
]

const adminNavItems: NavItem[] = [
  { titleKey: 'tenants.title', icon: Building2, path: '/portal/admin/tenants', permission: Permissions.TenantsRead },
  { titleKey: 'roles.title', icon: Shield, path: '/portal/admin/roles', permission: Permissions.RolesRead },
  { titleKey: 'users.title', icon: Users, path: '/portal/admin/users', permission: Permissions.UsersRead },
  { titleKey: 'activityTimeline.title', icon: Activity, path: '/portal/admin/activity-timeline', permission: Permissions.AuditRead },
  { titleKey: 'developerLogs.title', icon: Terminal, path: '/portal/admin/developer-logs', permission: Permissions.AuditRead },
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
  const initials = displayName.charAt(0).toUpperCase()
  const avatarUrl = user?.avatarUrl
  const { currentLanguage, languages, changeLanguage } = useLanguage()
  const { logout, checkAuth } = useAuthContext()
  const navigate = useNavigate()

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
            <div className={cn(
              'h-10 w-10 rounded-full flex-shrink-0 transition-all overflow-hidden',
              !avatarUrl && 'bg-gradient-to-br from-sidebar-primary to-sidebar-primary/60 flex items-center justify-center text-sidebar-primary-foreground font-semibold text-sm',
              isExpanded ? 'ring-2 ring-transparent group-hover:ring-4 group-hover:ring-sidebar-primary/20' : 'group-hover:ring-[6px] group-hover:ring-sidebar-primary/15'
            )}>
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
}

/**
 * SidebarContent - With Dashboard and Admin sections
 */
function SidebarContent({
  isExpanded,
  onToggle,
  onItemClick,
  t,
  pathname,
  user,
}: SidebarContentProps) {
  const isActive = (path: string) => isActivePath(pathname, path)
  const { hasPermission } = usePermissions()

  // Filter nav items based on permissions
  const visibleNavItems = navItems.filter(
    item => !item.permission || hasPermission(item.permission)
  )

  // Filter admin nav items - show admin section if user has any admin permissions
  const visibleAdminItems = adminNavItems.filter(
    item => !item.permission || hasPermission(item.permission)
  )
  const showAdminSection = visibleAdminItems.length > 0

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
            <div className="h-10 w-10 rounded-xl bg-gradient-to-br from-sidebar-primary to-sidebar-primary/60 flex items-center justify-center text-sidebar-primary-foreground shadow-lg group-hover:shadow-xl transition-all">
              <ShieldCheck className="h-5 w-5" />
            </div>
            <h2 className="text-lg font-semibold text-sidebar-foreground">NOIR</h2>
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

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto py-4">
        <div className="space-y-1 px-2">
          {visibleNavItems.map(renderNavItem)}
        </div>

        {/* Admin Section */}
        {showAdminSection && (
          <div className="mt-6">
            {isExpanded && (
              <div className="px-4 mb-2">
                <p className="text-xs font-semibold text-sidebar-foreground/50 uppercase tracking-wider">
                  {t('nav.admin')}
                </p>
              </div>
            )}
            <div className="space-y-1 px-2">
              {visibleAdminItems.map(renderNavItem)}
            </div>
          </div>
        )}
      </nav>

      {/* Theme Toggle */}
      <div className={cn(
        'p-3 border-t border-sidebar-border',
        !isExpanded && 'flex justify-center'
      )}>
        {isExpanded ? (
          <ThemeToggle className="w-full" />
        ) : (
          <TippyTooltip content={t('labels.appearance')} placement="right" delay={[0, 0]}>
            <div>
              <ThemeToggleCompact />
            </div>
          </TippyTooltip>
        )}
      </div>

      {/* User Profile */}
      <div className="p-3 border-t border-sidebar-border">
        <UserProfileDropdown isExpanded={isExpanded} t={t} user={user} />
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
      />
    </aside>
  )
}

/**
 * Mobile Sidebar Trigger - With Dashboard and Admin sections
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

  // Filter nav items based on permissions
  const visibleNavItems = navItems.filter(
    item => !item.permission || hasPermission(item.permission)
  )

  // Filter admin nav items
  const visibleAdminItems = adminNavItems.filter(
    item => !item.permission || hasPermission(item.permission)
  )
  const showAdminSection = visibleAdminItems.length > 0

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

  return (
    <Sheet open={open} onOpenChange={onOpenChange}>
      <SheetTrigger asChild>
        <Button variant="outline" size="icon" className="lg:hidden h-10 w-10">
          <Menu className="h-5 w-5" />
        </Button>
      </SheetTrigger>
      <SheetContent side="left" className="p-0 w-72">
        <SheetTitle className="sr-only">{t('nav.menu')}</SheetTitle>
        <div className="flex flex-col h-full bg-sidebar">
          {/* Mobile Header */}
          <div className="flex items-center p-4 border-b border-sidebar-border">
            <Link to="/portal" className="flex items-center gap-3" onClick={() => onOpenChange(false)}>
              <div className="h-10 w-10 rounded-xl bg-gradient-to-br from-sidebar-primary to-sidebar-primary/60 flex items-center justify-center text-sidebar-primary-foreground shadow-lg">
                <ShieldCheck className="h-5 w-5" />
              </div>
              <h2 className="text-lg font-semibold text-sidebar-foreground">NOIR</h2>
            </Link>
          </div>

          {/* Mobile Navigation */}
          <nav className="flex-1 overflow-y-auto py-4">
            <div className="space-y-1 px-2">
              {visibleNavItems.map(renderMobileNavItem)}
            </div>

            {/* Admin Section */}
            {showAdminSection && (
              <div className="mt-6">
                <div className="px-4 mb-2">
                  <p className="text-xs font-semibold text-sidebar-foreground/50 uppercase tracking-wider">
                    {t('nav.admin')}
                  </p>
                </div>
                <div className="space-y-1 px-2">
                  {visibleAdminItems.map(renderMobileNavItem)}
                </div>
              </div>
            )}
          </nav>

          {/* Mobile Theme Toggle */}
          <div className="p-3 border-t border-sidebar-border">
            <ThemeToggle className="w-full" />
          </div>

          {/* Mobile User Profile */}
          <div className="p-3 border-t border-sidebar-border">
            <UserProfileDropdown isExpanded={true} t={t} user={user} />
          </div>
        </div>
      </SheetContent>
    </Sheet>
  )
}
