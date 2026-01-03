import { useNavigate } from 'react-router-dom'
import { useTranslation } from 'react-i18next'
import { LogOut, Menu, ChevronDown } from 'lucide-react'
import { useAuthContext } from '@/contexts/AuthContext'
import { cn } from '@/lib/utils'
import { themeClasses } from '@/config/theme'
import { LanguageSwitcher } from '@/i18n/LanguageSwitcher'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'

interface HeaderProps {
  onMobileMenuToggle?: () => void
  sidebarCollapsed?: boolean
}

/**
 * Professional Header Component
 * Features user dropdown with Radix UI and professional styling
 */
export function Header({ onMobileMenuToggle, sidebarCollapsed = false }: HeaderProps) {
  const { t } = useTranslation('common')
  const { user, logout } = useAuthContext()
  const navigate = useNavigate()

  const handleLogout = async () => {
    await logout()
    navigate('/login')
  }

  if (!user) return null

  return (
    <header
      className={cn(
        'fixed top-0 right-0 z-30 h-16 border-b border-gray-200 bg-white/80 backdrop-blur-md transition-all duration-300',
        // Mobile: full width
        'left-0',
        // Desktop: offset by sidebar width
        sidebarCollapsed ? 'lg:left-16' : 'lg:left-64'
      )}
    >
      <div className="flex h-full items-center justify-between px-4 lg:px-6">
        {/* Left Section - Mobile Menu */}
        <div className="flex items-center gap-4">
          {/* Mobile Menu Button */}
          <button
            onClick={onMobileMenuToggle}
            className="flex h-10 w-10 items-center justify-center rounded-lg text-gray-500 hover:bg-gray-100 hover:text-gray-700 lg:hidden transition-colors"
            aria-label="Toggle mobile menu"
          >
            <Menu className="h-5 w-5" />
          </button>

          {/* Page Title - Hidden on mobile */}
          <h1 className="hidden lg:block text-lg font-semibold text-gray-900">
            {t('dashboard.title')}
          </h1>
        </div>

        {/* Right Section - Language Switcher & User Menu */}
        <div className="flex items-center gap-3">
          {/* Language Switcher */}
          <LanguageSwitcher variant="dropdown" />

          {/* Divider */}
          <div className="h-6 w-px bg-gray-200" />

          {/* User Menu with Radix Dropdown */}
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <button
                className="flex items-center gap-3 rounded-lg px-2 py-1.5 hover:bg-gray-100 transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500"
                aria-label="User menu"
              >
                {/* Avatar */}
                <div className={cn(
                  'flex h-9 w-9 items-center justify-center rounded-full text-white font-semibold text-sm',
                  themeClasses.gradient
                )}>
                  {user.fullName?.charAt(0).toUpperCase() || user.email?.charAt(0).toUpperCase() || 'U'}
                </div>

                {/* User Info - Hidden on small screens */}
                <div className="hidden sm:block text-left">
                  <p className="text-sm font-medium text-gray-900 truncate max-w-32">
                    {user.fullName || 'User'}
                  </p>
                  <p className="text-xs text-gray-500 truncate max-w-32">
                    {user.roles?.[0] || 'Member'}
                  </p>
                </div>

                <ChevronDown className="h-4 w-4 text-gray-400 hidden sm:block" />
              </button>
            </DropdownMenuTrigger>

            <DropdownMenuContent align="end" className="w-56">
              {/* User Info Header */}
              <DropdownMenuLabel className="font-normal">
                <div className="flex flex-col space-y-1">
                  <p className="text-sm font-medium text-gray-900">{user.fullName}</p>
                  <p className="text-xs text-gray-500 truncate">{user.email}</p>
                </div>
              </DropdownMenuLabel>

              <DropdownMenuSeparator />

              {/* Logout */}
              <DropdownMenuItem
                onClick={handleLogout}
                className="text-red-600 focus:text-red-600 focus:bg-red-50 cursor-pointer"
              >
                <LogOut className="mr-2 h-4 w-4" />
                {t('auth.signOut')}
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      </div>
    </header>
  )
}
