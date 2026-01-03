import { Link, useLocation } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { ShieldCheck, LayoutDashboard, ChevronLeft, ChevronRight } from 'lucide-react'
import { cn } from '@/lib/utils'
import { themeClasses } from '@/config/theme'

interface SidebarProps {
  collapsed?: boolean
  onToggle?: () => void
}

interface NavItem {
  titleKey: string
  icon: React.ElementType
  path: string
}

const navItems: NavItem[] = [
  { titleKey: 'dashboard.title', icon: LayoutDashboard, path: '/portal' },
]

/**
 * Professional Sidebar Component
 * Inspired by shadcn-admin with smooth animations and professional styling
 */
export function Sidebar({ collapsed = false, onToggle }: SidebarProps) {
  const { t } = useTranslation('common')
  const location = useLocation()

  const isActive = (path: string) => {
    if (path === '/portal') {
      return location.pathname === '/portal'
    }
    return location.pathname.startsWith(path)
  }

  return (
    <aside
      className={cn(
        'fixed left-0 top-0 z-40 flex h-screen flex-col border-r border-gray-200 bg-white transition-all duration-300 ease-in-out',
        collapsed ? 'w-16' : 'w-64'
      )}
    >
      {/* Logo Section */}
      <div className={cn(
        'flex h-16 items-center border-b border-gray-200 transition-all duration-300',
        collapsed ? 'justify-center px-2' : 'px-4'
      )}>
        <Link
          to="/portal"
          className={cn(
            'flex items-center transition-all duration-300',
            collapsed ? 'justify-center gap-0' : 'gap-3'
          )}
        >
          <div className={cn(
            'flex items-center justify-center rounded-xl transition-all duration-300',
            themeClasses.iconContainer,
            themeClasses.iconContainerShadow,
            collapsed ? 'h-9 w-9' : 'h-10 w-10'
          )}>
            <ShieldCheck className="h-5 w-5 text-white" />
          </div>
          <span className={cn(
            'font-bold text-xl text-gray-900 whitespace-nowrap transition-all duration-300',
            collapsed && 'w-0 overflow-hidden opacity-0'
          )}>
            NOIR
          </span>
        </Link>
      </div>

      {/* Navigation */}
      <nav className={cn(
        'flex-1 overflow-y-auto py-3 transition-all duration-300',
        collapsed ? 'px-2' : 'px-3'
      )}>
        {/* Section Header */}
        <div className={cn(
          'mb-3 px-3 text-xs font-semibold uppercase tracking-wider text-gray-400 transition-all duration-300',
          collapsed && 'opacity-0 h-0 mb-0 overflow-hidden'
        )}>
          {t('nav.overview')}
        </div>

        <ul className="space-y-1">
          {navItems.map((item) => {
            const active = isActive(item.path)
            const Icon = item.icon

            return (
              <li key={item.path}>
                <Link
                  to={item.path}
                  className={cn(
                    'group relative flex items-center rounded-lg py-2.5 text-sm font-medium transition-all duration-200',
                    active
                      ? `${themeClasses.bgPrimary} text-white shadow-md ${themeClasses.shadowPrimaryLight}`
                      : 'text-gray-600 hover:bg-gray-100 hover:text-gray-900',
                    collapsed ? 'justify-center gap-0 px-0' : 'gap-3 px-3'
                  )}
                  title={collapsed ? t(item.titleKey) : undefined}
                >
                  <Icon className={cn(
                    'h-5 w-5 flex-shrink-0 transition-transform duration-200',
                    active ? 'text-white' : 'text-gray-500 group-hover:text-gray-700',
                    'group-hover:scale-110'
                  )} />
                  <span className={cn(
                    'whitespace-nowrap transition-all duration-300',
                    collapsed && 'w-0 overflow-hidden opacity-0'
                  )}>
                    {t(item.titleKey)}
                  </span>

                  {/* Active indicator */}
                  {active && !collapsed && (
                    <div className="absolute right-2 h-2 w-2 rounded-full bg-white/30" />
                  )}
                </Link>
              </li>
            )
          })}
        </ul>
      </nav>

      {/* Toggle Button */}
      <div className={cn(
        'border-t border-gray-200 py-3 transition-all duration-300',
        collapsed ? 'px-2' : 'px-3'
      )}>
        <button
          onClick={onToggle}
          className={cn(
            'flex w-full items-center rounded-lg py-2 text-sm text-gray-500 transition-all duration-200',
            'hover:bg-gray-100 hover:text-gray-900',
            collapsed ? 'justify-center gap-0 px-0' : 'gap-2 px-3'
          )}
          aria-label={collapsed ? 'Expand sidebar' : 'Collapse sidebar'}
        >
          {collapsed ? (
            <ChevronRight className="h-4 w-4" />
          ) : (
            <>
              <ChevronLeft className="h-4 w-4" />
              <span className="text-xs">{t('nav.collapse')}</span>
            </>
          )}
        </button>
      </div>
    </aside>
  )
}
