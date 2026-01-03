import { useState, useEffect } from 'react'
import { Outlet } from 'react-router-dom'
import { Sidebar } from '@/components/portal/Sidebar'
import { Header } from '@/components/portal/Header'
import { cn } from '@/lib/utils'

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

  // Close mobile menu when screen becomes large
  useEffect(() => {
    const handleResize = () => {
      if (window.innerWidth >= 1024) {
        setMobileMenuOpen(false)
      }
    }

    window.addEventListener('resize', handleResize)
    return () => window.removeEventListener('resize', handleResize)
  }, [])

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Mobile Overlay */}
      {mobileMenuOpen && (
        <div
          className="fixed inset-0 z-30 bg-black/50 lg:hidden"
          onClick={() => setMobileMenuOpen(false)}
        />
      )}

      {/* Sidebar - Hidden on mobile, shown on desktop */}
      <div className={cn(
        'hidden lg:block'
      )}>
        <Sidebar 
          collapsed={sidebarCollapsed} 
          onToggle={handleSidebarToggle} 
        />
      </div>

      {/* Mobile Sidebar */}
      <div className={cn(
        'lg:hidden fixed inset-y-0 left-0 z-40 transition-transform duration-300',
        mobileMenuOpen ? 'translate-x-0' : '-translate-x-full'
      )}>
        <Sidebar 
          collapsed={false} 
          onToggle={() => setMobileMenuOpen(false)} 
        />
      </div>

      {/* Header */}
      <Header 
        sidebarCollapsed={sidebarCollapsed}
        onMobileMenuToggle={() => setMobileMenuOpen(!mobileMenuOpen)}
      />

      {/* Main Content */}
      <main 
        className={cn(
          'min-h-screen pt-16 transition-all duration-300',
          sidebarCollapsed ? 'lg:pl-16' : 'lg:pl-64'
        )}
      >
        <div className="p-4 lg:p-6">
          <Outlet />
        </div>
      </main>
    </div>
  )
}
