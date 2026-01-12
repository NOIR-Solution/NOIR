import { useState, Suspense } from 'react'
import { Outlet } from 'react-router-dom'
import { Sidebar, MobileSidebarTrigger } from '@/components/portal/Sidebar'
import { NotificationDropdown } from '@/components/notifications'
import { PageLoader } from '@/components/ui/page-loader'

export function PortalLayout() {
  // Use lazy initialization to read from localStorage on mount (avoids extra render)
  const [sidebarCollapsed, setSidebarCollapsed] = useState(() => {
    const saved = localStorage.getItem('sidebar-collapsed')
    return saved !== null ? JSON.parse(saved) : false
  })
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)

  const handleSidebarToggle = () => {
    const newState = !sidebarCollapsed
    setSidebarCollapsed(newState)
    localStorage.setItem('sidebar-collapsed', JSON.stringify(newState))
  }

  return (
    <div className="flex h-screen w-full bg-background">
      {/* Desktop Sidebar - 21st.dev flex layout */}
      <Sidebar
        collapsed={sidebarCollapsed}
        onToggle={handleSidebarToggle}
      />

      {/* Main Content Area */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Header */}
        <div className="flex items-center justify-between h-16 px-4 border-b border-border bg-background">
          {/* Mobile sidebar trigger */}
          <div className="lg:hidden">
            <MobileSidebarTrigger
              open={mobileMenuOpen}
              onOpenChange={setMobileMenuOpen}
            />
          </div>

          {/* Spacer for desktop */}
          <div className="hidden lg:block" />

          {/* Right side actions */}
          <div className="flex items-center gap-2">
            <NotificationDropdown />
          </div>
        </div>

        {/* Main Content */}
        <main className="flex-1 overflow-auto p-4 lg:p-6">
          <Suspense fallback={<PageLoader text="Loading..." />}>
            <Outlet />
          </Suspense>
        </main>
      </div>
    </div>
  )
}
