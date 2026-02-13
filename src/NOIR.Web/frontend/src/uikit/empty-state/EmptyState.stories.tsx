import type { Meta, StoryObj } from 'storybook'
import { MemoryRouter } from 'react-router-dom'
import { Package, Search, Users, FileText } from 'lucide-react'
import { EmptyState } from './EmptyState'

const meta = {
  title: 'UIKit/EmptyState',
  component: EmptyState,
  tags: ['autodocs'],
  decorators: [
    (Story) => (
      <MemoryRouter>
        <div style={{ maxWidth: '600px', width: '100%' }}>
          <Story />
        </div>
      </MemoryRouter>
    ),
  ],
  parameters: {
    layout: 'centered',
  },
} satisfies Meta<typeof EmptyState>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    icon: Package,
    title: 'No data found',
    description: 'Get started by creating your first item.',
  },
}

export const WithAction: Story = {
  args: {
    icon: Package,
    title: 'No products yet',
    description: 'Create your first product to get started with your catalog.',
    action: {
      label: 'Create Product',
      onClick: () => {},
    },
  },
}

export const WithBothActions: Story = {
  args: {
    icon: Users,
    title: 'No users found',
    description: 'Invite your team members to start collaborating.',
    action: {
      label: 'Invite Users',
      onClick: () => {},
    },
    secondaryAction: {
      label: 'Import CSV',
      onClick: () => {},
    },
  },
}

export const WithHelpLink: Story = {
  args: {
    icon: FileText,
    title: 'No documents',
    description: 'Upload your first document or create one from scratch.',
    action: {
      label: 'Upload Document',
      onClick: () => {},
    },
    helpLink: {
      label: 'Learn more about documents',
      href: 'https://example.com',
      external: true,
    },
  },
}

export const SearchEmpty: Story = {
  args: {
    icon: Search,
    title: 'No results found',
    description: 'Try adjusting your search or filter criteria.',
    size: 'sm',
  },
}

export const Small: Story = {
  args: {
    icon: Package,
    title: 'Empty',
    description: 'Nothing here yet.',
    size: 'sm',
  },
}

export const Large: Story = {
  args: {
    icon: Package,
    title: 'Welcome to your dashboard',
    description: 'This is where all your items will appear once you create them.',
    size: 'lg',
    action: {
      label: 'Get Started',
      onClick: () => {},
    },
  },
}

export const WithCustomIllustration: Story = {
  args: {
    illustration: (
      <div style={{
        width: 80,
        height: 80,
        borderRadius: '50%',
        background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        color: 'white',
        fontSize: 32,
      }}>
        !
      </div>
    ),
    title: 'Custom illustration',
    description: 'You can use any React node as an illustration.',
  },
}

export const WithLinkAction: Story = {
  args: {
    icon: Package,
    title: 'No items',
    description: 'Navigate to the catalog to add items.',
    action: {
      label: 'Go to Catalog',
      href: '/catalog',
    },
  },
}
