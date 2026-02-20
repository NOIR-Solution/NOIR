import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import {
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  CardFooter,
  CardAction,
} from './Card'
import { Skeleton } from '../skeleton/Skeleton'
import { Input } from '../input/Input'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '../select/Select'
import { ViewModeToggle, type ViewModeOption } from '../view-mode-toggle/ViewModeToggle'
import { Users, TrendingUp, UserCheck, Crown, Search, List, LayoutGrid } from 'lucide-react'

const meta = {
  title: 'UIKit/Card',
  component: Card,
  tags: ['autodocs'],
} satisfies Meta<typeof Card>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: (args) => (
    <Card {...args} style={{ maxWidth: 400 }}>
      <CardHeader>
        <CardTitle>Card Title</CardTitle>
        <CardDescription>Card description goes here.</CardDescription>
      </CardHeader>
      <CardContent>
        <p>This is the card content area. You can put any content here.</p>
      </CardContent>
      <CardFooter>
        <p>Card Footer</p>
      </CardFooter>
    </Card>
  ),
}

export const Simple: Story = {
  render: () => (
    <Card style={{ maxWidth: 400 }}>
      <CardContent>
        <p>A simple card with content only.</p>
      </CardContent>
    </Card>
  ),
}

export const WithHeaderOnly: Story = {
  render: () => (
    <Card style={{ maxWidth: 400 }}>
      <CardHeader>
        <CardTitle>Header Only Card</CardTitle>
        <CardDescription>
          This card has a header with title and description but no content or
          footer.
        </CardDescription>
      </CardHeader>
    </Card>
  ),
}

export const WithAction: Story = {
  render: () => (
    <Card style={{ maxWidth: 400 }}>
      <CardHeader>
        <CardTitle>Card with Action</CardTitle>
        <CardDescription>
          This card has an action button in the header.
        </CardDescription>
        <CardAction>
          <button
            style={{
              padding: '4px 12px',
              borderRadius: '6px',
              border: '1px solid #ccc',
              cursor: 'pointer',
              fontSize: '14px',
            }}
          >
            Edit
          </button>
        </CardAction>
      </CardHeader>
      <CardContent>
        <p>Card content with an action button positioned in the header.</p>
      </CardContent>
    </Card>
  ),
}

export const FullComposition: Story = {
  render: () => (
    <Card style={{ maxWidth: 400 }}>
      <CardHeader>
        <CardTitle>Complete Card</CardTitle>
        <CardDescription>
          Uses all card subcomponents together.
        </CardDescription>
        <CardAction>
          <span style={{ fontSize: '12px', color: '#888' }}>Action</span>
        </CardAction>
      </CardHeader>
      <CardContent>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
          <div>Name: John Doe</div>
          <div>Email: john@example.com</div>
          <div>Role: Administrator</div>
        </div>
      </CardContent>
      <CardFooter>
        <div style={{ display: 'flex', gap: '8px', width: '100%' }}>
          <button
            style={{
              padding: '6px 16px',
              borderRadius: '6px',
              border: '1px solid #ccc',
              cursor: 'pointer',
            }}
          >
            Cancel
          </button>
          <button
            style={{
              padding: '6px 16px',
              borderRadius: '6px',
              border: 'none',
              background: '#0f172a',
              color: 'white',
              cursor: 'pointer',
            }}
          >
            Save
          </button>
        </div>
      </CardFooter>
    </Card>
  ),
}

export const MultipleCards: Story = {
  render: () => (
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: '16px', maxWidth: 900 }}>
      <Card>
        <CardHeader>
          <CardTitle>Users</CardTitle>
          <CardDescription>Manage user accounts</CardDescription>
        </CardHeader>
        <CardContent>
          <p style={{ fontSize: '32px', fontWeight: 'bold' }}>1,234</p>
        </CardContent>
      </Card>
      <Card>
        <CardHeader>
          <CardTitle>Orders</CardTitle>
          <CardDescription>Recent order activity</CardDescription>
        </CardHeader>
        <CardContent>
          <p style={{ fontSize: '32px', fontWeight: 'bold' }}>567</p>
        </CardContent>
      </Card>
      <Card>
        <CardHeader>
          <CardTitle>Revenue</CardTitle>
          <CardDescription>Monthly earnings</CardDescription>
        </CardHeader>
        <CardContent>
          <p style={{ fontSize: '32px', fontWeight: 'bold' }}>$12,345</p>
        </CardContent>
      </Card>
    </div>
  ),
}

export const ContentOnly: Story = {
  render: () => (
    <Card style={{ maxWidth: 400 }}>
      <CardContent>
        <p>Card with no header or footer, just content.</p>
      </CardContent>
    </Card>
  ),
}

export const HoverShadow: Story = {
  render: () => (
    <Card className="shadow-sm hover:shadow-lg transition-all duration-300" style={{ maxWidth: 400 }}>
      <CardHeader>
        <CardTitle className="text-lg">NOIR Standard Card</CardTitle>
        <CardDescription>Uses the standard shadow-sm hover:shadow-lg transition pattern</CardDescription>
      </CardHeader>
      <CardContent>
        <p>Hover over this card to see the shadow elevation effect.</p>
      </CardContent>
    </Card>
  ),
}

export const Clickable: Story = {
  render: () => (
    <Card
      className="shadow-sm hover:shadow-lg transition-all duration-300 cursor-pointer"
      style={{ maxWidth: 400 }}
      onClick={() => alert('Card clicked!')}
    >
      <CardHeader>
        <CardTitle className="text-lg">Clickable Card</CardTitle>
        <CardDescription>This card acts as a clickable element</CardDescription>
      </CardHeader>
      <CardContent>
        <p>Click anywhere on this card to trigger an action.</p>
      </CardContent>
    </Card>
  ),
}

export const Loading: Story = {
  render: () => (
    <Card className="shadow-sm hover:shadow-lg transition-all duration-300" style={{ maxWidth: 400 }}>
      <CardHeader>
        <Skeleton className="h-6 w-48" />
        <Skeleton className="h-4 w-64" />
      </CardHeader>
      <CardContent className="space-y-3">
        <Skeleton className="h-4 w-full" />
        <Skeleton className="h-4 w-3/4" />
        <Skeleton className="h-4 w-1/2" />
      </CardContent>
    </Card>
  ),
}

/**
 * Shared shell for list page header stories.
 * Extracts the common Card + CardHeader + CardContent structure to avoid duplication.
 */
const ListPageCardShell = ({ children }: { children: React.ReactNode }) => (
  <Card className="shadow-sm hover:shadow-lg transition-all duration-300" style={{ maxWidth: 800 }}>
    <CardHeader className="pb-4">
      <div className="space-y-3">
        {children}
      </div>
    </CardHeader>
    <CardContent>
      <div className="rounded-xl border border-border/50 p-8 text-center text-muted-foreground">
        Table / Tree content goes here
      </div>
    </CardContent>
  </Card>
)

/**
 * ListPageHeader — the standardized Card header layout used across all list pages.
 *
 * Structure:
 * - `space-y-3` stacked layout inside CardHeader
 * - Title row: `flex items-center justify-between` with CardTitle/CardDescription on the left
 *   and an optional ViewModeToggle on the right
 * - Filter bar: `flex flex-wrap items-center gap-2` with search input first (`flex-1 min-w-[200px]`),
 *   followed by Select filter dropdowns (`w-36 h-9 cursor-pointer`)
 *
 * Card class variants:
 * - Standard: `shadow-sm hover:shadow-lg transition-all duration-300`
 * - Enhanced (ProductsPage): adds `border-border/50 backdrop-blur-sm bg-card/95`
 *
 * Reference: Activity Timeline page layout (gold standard)
 * Used by: Products, Product Categories, Blog Categories, Product Attributes, Reviews, Inventory Receipts, etc.
 */
export const ListPageHeader: Story = {
  render: () => {
    const [status, setStatus] = useState('all')
    const [category, setCategory] = useState('all')
    const [viewMode, setViewMode] = useState<'table' | 'grid'>('table')
    const viewModeOptions: ViewModeOption<'table' | 'grid'>[] = [
      { value: 'table', label: 'List', icon: List, ariaLabel: 'Table view' },
      { value: 'grid', label: 'Grid', icon: LayoutGrid, ariaLabel: 'Grid view' },
    ]

    return (
      <ListPageCardShell>
        {/* Title row: title + optional ViewModeToggle */}
        <div className="flex items-center justify-between">
          <div>
            <CardTitle>All Products</CardTitle>
            <CardDescription>24 products total</CardDescription>
          </div>
          <ViewModeToggle options={viewModeOptions} value={viewMode} onChange={setViewMode} />
        </div>
        {/* Filter bar: search first, then Select dropdowns */}
        <div className="flex flex-wrap items-center gap-2">
          <div className="relative flex-1 min-w-[200px]">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground pointer-events-none" />
            <Input
              placeholder="Search products..."
              className="pl-9 h-9"
              aria-label="Search products"
            />
          </div>
          <Select value={status} onValueChange={setStatus}>
            <SelectTrigger className="w-36 h-9 cursor-pointer" aria-label="Filter by status">
              <SelectValue placeholder="Status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Status</SelectItem>
              <SelectItem value="active">Active</SelectItem>
              <SelectItem value="draft">Draft</SelectItem>
              <SelectItem value="archived">Archived</SelectItem>
            </SelectContent>
          </Select>
          <Select value={category} onValueChange={setCategory}>
            <SelectTrigger className="w-40 h-9 cursor-pointer" aria-label="Filter by category">
              <SelectValue placeholder="Category" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Categories</SelectItem>
              <SelectItem value="clothing">Clothing</SelectItem>
              <SelectItem value="electronics">Electronics</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </ListPageCardShell>
    )
  },
}

/**
 * ListPageHeaderDropdownOnly — variant for pages with only dropdown filters and no view-mode toggle.
 *
 * When to use this variant vs ListPageHeader:
 * - Use ListPageHeader when the page has a search input and/or a ViewModeToggle (table/grid, table/tree)
 * - Use this variant when the page has only dropdown filters and no mode switching
 * - The filter bar uses `justify-end` since there is no flex-1 search input to push content right
 *
 * Used by: Inventory Receipts and similar pages.
 */
export const ListPageHeaderDropdownOnly: Story = {
  render: () => {
    const [type, setType] = useState('all')
    const [status, setStatus] = useState('all')

    return (
      <ListPageCardShell>
        <div>
          <CardTitle>Inventory Receipts</CardTitle>
          <CardDescription>12 receipts total</CardDescription>
        </div>
        {/* Filter bar: dropdown-only, right-aligned */}
        <div className="flex flex-wrap items-center justify-end gap-2">
          <Select value={type} onValueChange={setType}>
            <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label="Filter by type">
              <SelectValue placeholder="Type" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Types</SelectItem>
              <SelectItem value="stockIn">Stock In</SelectItem>
              <SelectItem value="stockOut">Stock Out</SelectItem>
            </SelectContent>
          </Select>
          <Select value={status} onValueChange={setStatus}>
            <SelectTrigger className="w-[140px] h-9 cursor-pointer" aria-label="Filter by status">
              <SelectValue placeholder="Status" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">All Status</SelectItem>
              <SelectItem value="confirmed">Confirmed</SelectItem>
              <SelectItem value="draft">Draft</SelectItem>
            </SelectContent>
          </Select>
        </div>
      </ListPageCardShell>
    )
  },
}

/**
 * Stat cards — the standard pattern used across Dashboard, Products, and Customers pages.
 * Each card has a colored icon container, title, and bold value.
 */
export const StatCards: Story = {
  render: () => {
    const stats = [
      { title: 'Total Customers', value: '1,234', icon: Users, iconBg: 'bg-primary/10 border-primary/20', iconColor: 'text-primary' },
      { title: 'Active Customers', value: '892', icon: UserCheck, iconBg: 'bg-green-500/10 border-green-500/20', iconColor: 'text-green-500' },
      { title: 'VIP Customers', value: '56', icon: Crown, iconBg: 'bg-purple-500/10 border-purple-500/20', iconColor: 'text-purple-500' },
      { title: 'Growth', value: '+12%', icon: TrendingUp, iconBg: 'bg-amber-500/10 border-amber-500/20', iconColor: 'text-amber-500' },
    ]

    return (
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: '16px', maxWidth: 900 }}>
        {stats.map((stat) => {
          const Icon = stat.icon
          return (
            <Card key={stat.title} className="shadow-sm hover:shadow-lg transition-all duration-300">
              <CardContent className="p-4">
                <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                  <div className={`p-2 rounded-xl border ${stat.iconBg}`}>
                    <Icon className={`h-5 w-5 ${stat.iconColor}`} />
                  </div>
                  <div>
                    <p className="text-sm text-muted-foreground">{stat.title}</p>
                    <p className="text-2xl font-bold">{stat.value}</p>
                  </div>
                </div>
              </CardContent>
            </Card>
          )
        })}
      </div>
    )
  },
}
