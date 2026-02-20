import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import { I18nextProvider } from 'react-i18next'
import i18n from 'i18next'
import {
  LayoutDashboard,
  Package,
  Users,
  Shield,
  Settings,
  Search,
  Sun,
  Moon,
  Plus,
} from 'lucide-react'

/**
 * CommandPalette stories
 *
 * The real CommandPalette depends on many context providers (CommandContext,
 * ThemeContext, DensityContext, AuthContext, react-router, permissions).
 * This story renders a simplified visual replica to showcase the UI design,
 * search behavior, and keyboard hint layout.
 */

// Minimal i18n instance for Storybook
const i18nInstance = i18n.createInstance()
i18nInstance.init({
  lng: 'en',
  resources: {
    en: {
      common: {
        commandPalette: {
          placeholder: 'Type a command or search...',
          noResults: 'No results found.',
          navigation: 'Navigation',
          actions: 'Quick Actions',
          hintNavigate: 'navigate',
          hintSelect: 'select',
          hintClose: 'close',
        },
      },
    },
  },
  defaultNS: 'common',
  interpolation: { escapeValue: false },
})

const navItems = [
  { icon: LayoutDashboard, label: 'Dashboard', path: '/portal' },
  { icon: Package, label: 'Products', path: '/portal/ecommerce/products' },
  { icon: Users, label: 'Users', path: '/portal/admin/users' },
  { icon: Shield, label: 'Roles', path: '/portal/admin/roles' },
  { icon: Settings, label: 'Settings', path: '/portal/settings' },
]

const quickActions = [
  { icon: Sun, label: 'Switch to Light Mode' },
  { icon: Moon, label: 'Switch to Dark Mode' },
  { icon: Plus, label: 'Create Product' },
  { icon: Plus, label: 'Create Blog Post' },
]

/**
 * Simplified visual replica of the CommandPalette
 */
const CommandPaletteDemo = ({ withSearch }: { withSearch?: string }) => {
  const [search, setSearch] = useState(withSearch ?? '')
  const searchLower = search.toLowerCase()

  const filteredNav = navItems.filter(
    (item) => !search || item.label.toLowerCase().includes(searchLower)
  )
  const filteredActions = quickActions.filter(
    (item) => !search || item.label.toLowerCase().includes(searchLower)
  )

  return (
    <I18nextProvider i18n={i18nInstance}>
      <div style={{ width: '512px' }}>
        <div className="rounded-xl border shadow-2xl bg-popover overflow-hidden">
          {/* Search Input */}
          <div className="flex items-center border-b px-3">
            <Search className="h-4 w-4 shrink-0 text-muted-foreground" />
            <input
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Type a command or search..."
              className="flex h-12 w-full bg-transparent py-3 px-2 text-sm outline-none placeholder:text-muted-foreground"
            />
            <kbd className="inline-flex h-5 select-none items-center gap-1 rounded border bg-muted px-1.5 font-mono text-[10px] font-medium text-muted-foreground">
              Esc
            </kbd>
          </div>

          {/* Results List */}
          <div className="max-h-[300px] overflow-y-auto p-2">
            {filteredNav.length === 0 && filteredActions.length === 0 && (
              <div className="py-6 text-center text-sm text-muted-foreground">
                No results found.
              </div>
            )}

            {filteredNav.length > 0 && (
              <div>
                <div className="px-2 py-1.5 text-xs font-medium text-muted-foreground">
                  Navigation
                </div>
                {filteredNav.map((item) => (
                  <div
                    key={item.path}
                    className="flex items-center gap-2 px-2 py-2 rounded-md cursor-pointer hover:bg-accent hover:text-accent-foreground"
                  >
                    <item.icon className="h-4 w-4 text-muted-foreground" />
                    <span className="text-sm">{item.label}</span>
                  </div>
                ))}
              </div>
            )}

            {filteredActions.length > 0 && (
              <div>
                <div className="px-2 py-1.5 text-xs font-medium text-muted-foreground">
                  Quick Actions
                </div>
                {filteredActions.map((item, idx) => (
                  <div
                    key={idx}
                    className="flex items-center gap-2 px-2 py-2 rounded-md cursor-pointer hover:bg-accent hover:text-accent-foreground"
                  >
                    <item.icon className="h-4 w-4 text-muted-foreground" />
                    <span className="text-sm">{item.label}</span>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Footer with hints */}
          <div className="border-t px-3 py-2 text-xs text-muted-foreground flex items-center gap-4">
            <span className="flex items-center gap-1">
              <kbd className="px-1.5 py-0.5 rounded bg-muted font-mono">&#8593;&#8595;</kbd>
              <span>navigate</span>
            </span>
            <span className="flex items-center gap-1">
              <kbd className="px-1.5 py-0.5 rounded bg-muted font-mono">&#8629;</kbd>
              <span>select</span>
            </span>
            <span className="flex items-center gap-1">
              <kbd className="px-1.5 py-0.5 rounded bg-muted font-mono">esc</kbd>
              <span>close</span>
            </span>
            <span className="ml-auto text-muted-foreground/60">
              &#8984;K to open
            </span>
          </div>
        </div>
      </div>
    </I18nextProvider>
  )
}

const meta = {
  title: 'UIKit/CommandPalette',
  component: CommandPaletteDemo,
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
  },
} satisfies Meta<typeof CommandPaletteDemo>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => <CommandPaletteDemo />,
}

export const WithSearchQuery: Story = {
  render: () => <CommandPaletteDemo withSearch="prod" />,
}

export const NoResults: Story = {
  render: () => <CommandPaletteDemo withSearch="zzzzz" />,
}

export const WithBackdrop: Story = {
  parameters: {
    layout: 'fullscreen',
  },
  render: () => (
    <div style={{ position: 'relative', height: '500px', background: '#f5f5f5' }}>
      {/* Backdrop */}
      <div
        style={{
          position: 'absolute',
          inset: 0,
          backgroundColor: 'rgba(0,0,0,0.5)',
          backdropFilter: 'blur(4px)',
        }}
      />
      {/* Dialog */}
      <div style={{ position: 'absolute', left: '50%', top: '15%', transform: 'translateX(-50%)', width: '100%', maxWidth: '512px', padding: '0 16px' }}>
        <CommandPaletteDemo />
      </div>
    </div>
  ),
}
