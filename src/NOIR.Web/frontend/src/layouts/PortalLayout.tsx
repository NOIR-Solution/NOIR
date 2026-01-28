import { useState, Suspense } from 'react'
import { Sidebar, MobileSidebarTrigger } from '@/components/portal/Sidebar'
import { PageLoader } from '@/components/ui/page-loader'
import { Breadcrumb } from '@/components/ui/breadcrumb'
import { useBreadcrumbs } from '@/hooks/useBreadcrumbs'
import { SkipLink } from '@/components/accessibility/SkipLink'
import { OfflineIndicator } from '@/components/network/OfflineIndicator'
import { AnimatedOutlet } from '@/components/layout/AnimatedOutlet'

export function PortalLayout() {
  // Use lazy initialization to read from localStorage on mount (avoids extra render)
  const [sidebarCollapsed, setSidebarCollapsed] = useState(() => {
    const saved = localStorage.getItem('sidebar-collapsed')
    return saved !== null ? JSON.parse(saved) : false
  })
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false)
  const breadcrumbs = useBreadcrumbs()

  const handleSidebarToggle = () => {
    const newState = !sidebarCollapsed
    setSidebarCollapsed(newState)
    localStorage.setItem('sidebar-collapsed', JSON.stringify(newState))
  }

  return (
    <div className="flex h-screen w-full bg-background">
      {/* Skip Link for Accessibility */}
      <SkipLink targetId="main-content" />

      {/* Desktop Sidebar - Headerless design */}
      <Sidebar
        collapsed={sidebarCollapsed}
        onToggle={handleSidebarToggle}
      />

      {/* Main Content Area - Full height, no header */}
      <div className="flex-1 flex flex-col overflow-hidden">
        {/* Mobile header - only visible on mobile */}
        <div className="flex items-center h-14 px-4 border-b border-border bg-background lg:hidden">
          <MobileSidebarTrigger
            open={mobileMenuOpen}
            onOpenChange={setMobileMenuOpen}
          />
        </div>

        {/* Main Content - Full vertical space on desktop */}
        <main id="main-content" className="flex-1 overflow-auto p-4 lg:p-6">
          {/* Breadcrumb Navigation */}
          {breadcrumbs.length > 1 && (
            <div className="mb-4">
              <Breadcrumb items={breadcrumbs} />
            </div>
          )}
          <Suspense fallback={<PageLoader text="Loading..." />}>
            <AnimatedOutlet />
          </Suspense>
        </main>
      </div>

      {/* Offline Indicator */}
      <OfflineIndicator />
    </div>
  )
}
