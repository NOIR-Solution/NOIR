import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import { MemoryRouter } from 'react-router-dom'
import {
  ShieldCheck,
  LayoutDashboard,
  ChevronLeft,
  ChevronRight,
  Package,
  Layers,
  ShoppingCart,
  Users,
  Shield,
  Settings,
  FileText,
  Tag,
  Activity,
  Bell,
  ChevronUp,
} from 'lucide-react'
import { cn } from '@/lib/utils'

/**
 * Sidebar stories
 *
 * The real Sidebar component depends on AuthContext, BrandingContext,
 * NotificationContext, PermissionsHook, i18n, and React Router.
 * This story renders a simplified visual replica to showcase the
 * sidebar layout, collapse/expand behavior, and navigation structure.
 */

interface NavItem {
  icon: React.ElementType
  label: string
  path: string
  active?: boolean
}

interface NavSection {
  label?: string
  items: NavItem[]
}

const navSections: NavSection[] = [
  {
    items: [
      { icon: LayoutDashboard, label: 'Dashboard', path: '/portal', active: true },
    ],
  },
  {
    label: 'E-COMMERCE',
    items: [
      { icon: Package, label: 'Products', path: '/portal/ecommerce/products' },
      { icon: Layers, label: 'Categories', path: '/portal/ecommerce/categories' },
      { icon: ShoppingCart, label: 'Orders', path: '/portal/ecommerce/orders' },
    ],
  },
  {
    label: 'CONTENT',
    items: [
      { icon: FileText, label: 'Blog Posts', path: '/portal/blog/posts' },
      { icon: Tag, label: 'Tags', path: '/portal/blog/tags' },
    ],
  },
  {
    label: 'USERS & ACCESS',
    items: [
      { icon: Users, label: 'Users', path: '/portal/admin/users' },
      { icon: Shield, label: 'Roles', path: '/portal/admin/roles' },
    ],
  },
  {
    label: 'SYSTEM',
    items: [
      { icon: Activity, label: 'Activity Timeline', path: '/portal/activity-timeline' },
      { icon: Settings, label: 'Settings', path: '/portal/settings' },
    ],
  },
]

const SidebarDemo = ({ initialCollapsed = false }: { initialCollapsed?: boolean }) => {
  const [collapsed, setCollapsed] = useState(initialCollapsed)
  const isExpanded = !collapsed

  return (
    <div
      className={cn(
        'flex flex-col h-[600px] bg-sidebar border-r border-sidebar-border transition-all duration-300 ease-in-out',
        collapsed ? 'w-20' : 'w-64'
      )}
    >
      {/* Header */}
      <div className="flex items-center justify-between p-4 border-b border-sidebar-border">
        {isExpanded && (
          <div className="flex items-center gap-3">
            <div className="h-10 w-10 rounded-xl bg-gradient-to-br from-sidebar-primary to-sidebar-primary/60 flex items-center justify-center text-sidebar-primary-foreground shadow-lg">
              <ShieldCheck className="h-5 w-5" />
            </div>
            <h2 className="text-lg font-semibold text-sidebar-foreground">NOIR</h2>
          </div>
        )}
        <button
          onClick={() => setCollapsed(!collapsed)}
          className={cn(
            'h-8 w-8 inline-flex items-center justify-center rounded-md transition-all text-sidebar-foreground hover:bg-sidebar-accent',
            !isExpanded && 'mx-auto'
          )}
        >
          {isExpanded ? <ChevronLeft className="h-4 w-4" /> : <ChevronRight className="h-4 w-4" />}
        </button>
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto py-4">
        {navSections.map((section, index) => (
          <div key={section.label || 'primary'} className={cn(index > 0 && 'mt-4')}>
            {section.label && isExpanded && (
              <div className="px-4 mb-2">
                <p className="text-xs font-semibold text-sidebar-foreground/70 uppercase tracking-wider">
                  {section.label}
                </p>
              </div>
            )}
            {section.label && !isExpanded && index > 0 && (
              <div className="mx-3 mb-2 border-t border-sidebar-border" />
            )}
            <div className="space-y-1 px-2">
              {section.items.map((item) => {
                const Icon = item.icon
                return (
                  <button
                    key={item.path}
                    className={cn(
                      'w-full flex items-center rounded-md text-sm h-9 transition-all duration-200 cursor-pointer',
                      isExpanded ? 'px-3' : 'px-0 justify-center',
                      item.active
                        ? 'bg-gradient-to-r from-sidebar-primary/20 to-sidebar-primary/10 text-sidebar-primary'
                        : 'text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground'
                    )}
                  >
                    {item.active && (
                      <div className="absolute left-0 top-0 bottom-0 w-1 bg-sidebar-primary rounded-r-full" />
                    )}
                    <Icon className={cn('h-5 w-5 flex-shrink-0', isExpanded && 'mr-3')} />
                    {isExpanded && <span className="flex-1 text-left">{item.label}</span>}
                  </button>
                )
              })}
            </div>
          </div>
        ))}
      </nav>

      {/* Footer */}
      <div className="border-t border-sidebar-border">
        {/* Notification button */}
        <div className={cn('px-2 pt-3', isExpanded ? 'pb-2' : 'pb-3')}>
          <button
            className={cn(
              'w-full flex items-center rounded-md text-sm h-9 transition-all duration-200 cursor-pointer',
              isExpanded ? 'px-3' : 'px-0 justify-center',
              'text-sidebar-foreground hover:bg-sidebar-accent hover:text-sidebar-accent-foreground'
            )}
          >
            <div className="relative">
              <Bell className={cn('h-5 w-5 flex-shrink-0', isExpanded && 'mr-3')} />
              {!isExpanded && (
                <span className="absolute -top-2 -right-2 h-4 min-w-4 p-0 text-[9px] flex items-center justify-center bg-destructive text-destructive-foreground rounded-full">
                  3
                </span>
              )}
            </div>
            {isExpanded && (
              <>
                <span className="flex-1 text-left">Notifications</span>
                <span className="ml-auto h-5 min-w-5 px-1.5 text-[10px] flex items-center justify-center bg-destructive text-destructive-foreground rounded-full">
                  3
                </span>
              </>
            )}
          </button>
        </div>

        {/* User profile */}
        <div className="p-3 pt-1 border-t border-sidebar-border">
          <button
            className={cn(
              'w-full flex items-center gap-3 transition-colors cursor-pointer',
              isExpanded ? 'p-2 hover:bg-sidebar-accent rounded-lg' : 'justify-center'
            )}
          >
            <div className="h-10 w-10 rounded-full flex-shrink-0 flex items-center justify-center text-white font-semibold text-sm bg-blue-600">
              JD
            </div>
            {isExpanded && (
              <>
                <div className="flex-1 min-w-0 text-left">
                  <p className="text-sm font-medium text-sidebar-foreground truncate">John Doe</p>
                  <p className="text-xs text-sidebar-foreground/60 truncate">john@example.com</p>
                </div>
                <ChevronUp className="h-4 w-4 text-sidebar-foreground/60" />
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  )
}

const withRouter = (Story: React.ComponentType) => (
  <MemoryRouter>
    <Story />
  </MemoryRouter>
)

const meta = {
  title: 'UIKit/Sidebar',
  component: SidebarDemo,
  tags: ['autodocs'],
  decorators: [withRouter],
  parameters: {
    layout: 'fullscreen',
  },
} satisfies Meta<typeof SidebarDemo>

export default meta
type Story = StoryObj<typeof meta>

export const Expanded: Story = {
  render: () => <SidebarDemo />,
}

export const Collapsed: Story = {
  render: () => <SidebarDemo initialCollapsed />,
}

export const Interactive: Story = {
  render: () => (
    <div style={{ display: 'flex', height: '600px' }}>
      <SidebarDemo />
      <div style={{ flex: 1, padding: '24px', background: '#fafafa' }}>
        <h1 style={{ fontSize: '24px', fontWeight: 600, marginBottom: '8px' }}>Dashboard</h1>
        <p style={{ color: '#666' }}>Click the collapse/expand button to toggle the sidebar.</p>
      </div>
    </div>
  ),
}

export const SideBySide: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: '24px', padding: '24px', background: '#f5f5f5' }}>
      <div>
        <p style={{ marginBottom: '8px', fontSize: '14px', fontWeight: 500 }}>Expanded</p>
        <SidebarDemo />
      </div>
      <div>
        <p style={{ marginBottom: '8px', fontSize: '14px', fontWeight: 500 }}>Collapsed</p>
        <SidebarDemo initialCollapsed />
      </div>
    </div>
  ),
}
