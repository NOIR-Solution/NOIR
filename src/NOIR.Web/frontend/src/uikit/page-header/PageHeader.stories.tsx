import type { Meta, StoryObj } from 'storybook'
import { Package, Users, Settings, LayoutDashboard } from 'lucide-react'
import { PageHeader } from './PageHeader'
import { Button } from '../button/Button'

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
