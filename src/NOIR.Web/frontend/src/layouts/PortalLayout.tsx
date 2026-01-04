import { useState } from 'react'
import { Outlet } from 'react-router-dom'
import { Sidebar, MobileSidebarTrigger } from '@/components/portal/Sidebar'

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
        {/* Mobile Header - Only shows trigger on mobile */}
        <div className="lg:hidden flex items-center h-16 px-4 border-b border-border bg-background">
          <MobileSidebarTrigger
            open={mobileMenuOpen}
            onOpenChange={setMobileMenuOpen}
          />
        </div>

        {/* Main Content */}
        <main className="flex-1 overflow-auto p-4 lg:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
