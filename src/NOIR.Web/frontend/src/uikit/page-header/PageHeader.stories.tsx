import type { Meta, StoryObj } from 'storybook'
import { Package, Users, Settings, LayoutDashboard, Plus, Download, Filter } from 'lucide-react'
import { PageHeader } from './PageHeader'
import { Button } from '../button/Button'
import { Skeleton } from '../skeleton/Skeleton'

const meta = {
  title: 'UIKit/PageHeader',
  component: PageHeader,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
  },
  argTypes: {
    responsive: { control: 'boolean' },
  },
} satisfies Meta<typeof PageHeader>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    icon: Package,
    title: 'Products',
    description: 'Manage your product catalog',
  },
}

export const WithAction: Story = {
  args: {
    icon: Users,
    title: 'Users',
    description: 'Manage users and their roles',
    action: <Button>Add User</Button>,
  },
}

export const WithoutDescription: Story = {
  args: {
    icon: Settings,
    title: 'Settings',
  },
}

export const Responsive: Story = {
  args: {
    icon: LayoutDashboard,
    title: 'Dashboard',
    description: 'Overview of your business metrics',
    responsive: true,
    action: (
      <div style={{ display: 'flex', gap: '8px' }}>
        <Button variant="outline">Export</Button>
        <Button>Create Report</Button>
      </div>
    ),
  },
}

export const LongTitle: Story = {
  args: {
    icon: Package,
    title: 'Product Inventory Management System',
    description: 'View and manage all products, variants, and stock levels across your warehouses.',
    action: <Button>Add Product</Button>,
  },
}

export const WithMultipleActions: Story = {
  args: {
    icon: Users,
    title: 'Users',
    description: 'Manage users and their roles',
    action: (
      <div style={{ display: 'flex', gap: '8px' }}>
        <Button variant="outline" size="sm">
          <Filter className="h-4 w-4 mr-2" />
          Filter
        </Button>
        <Button variant="outline" size="sm">
          <Download className="h-4 w-4 mr-2" />
          Export
        </Button>
        <Button size="sm">
          <Plus className="h-4 w-4 mr-2" />
          Add User
        </Button>
      </div>
    ),
  },
}

export const LoadingState: Story = {
  render: () => (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
        <Skeleton className="h-10 w-10 rounded-xl" />
        <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
          <Skeleton className="h-7 w-48" />
          <Skeleton className="h-4 w-64" />
        </div>
      </div>
      <Skeleton className="h-9 w-28" />
    </div>
  ),
}
