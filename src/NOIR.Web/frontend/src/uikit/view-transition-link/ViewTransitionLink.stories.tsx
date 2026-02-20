import type { Meta, StoryObj } from 'storybook'
import { MemoryRouter } from 'react-router-dom'
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'

const withRouter = (Story: React.ComponentType) => (
  <MemoryRouter initialEntries={['/portal']}>
    <Story />
  </MemoryRouter>
)

const meta = {
  title: 'UIKit/ViewTransitionLink',
  component: ViewTransitionLink,
  tags: ['autodocs'],
  decorators: [withRouter],
  argTypes: {
    to: {
      control: 'text',
      description: 'Target path for navigation',
    },
    vtDirection: {
      control: 'select',
      options: ['forward', 'back'],
      description: 'Direction hint for CSS view transition animations',
    },
  },
} satisfies Meta<typeof ViewTransitionLink>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    to: '/portal/products',
    children: 'Products',
  },
}

export const WithClassName: Story = {
  args: {
    to: '/portal/products',
    children: 'Styled Link',
    className: 'text-blue-600 hover:text-blue-800 underline font-medium',
  },
}

export const BackDirection: Story = {
  args: {
    to: '/portal',
    vtDirection: 'back',
    children: 'Go Back',
    className: 'text-blue-600 hover:text-blue-800 underline',
  },
}

export const AsNavItem: Story = {
  render: () => (
    <nav style={{ display: 'flex', gap: '16px' }}>
      <ViewTransitionLink
        to="/portal"
        className="text-sm font-medium text-foreground hover:text-primary transition-colors"
      >
        Dashboard
      </ViewTransitionLink>
      <ViewTransitionLink
        to="/portal/products"
        className="text-sm font-medium text-muted-foreground hover:text-primary transition-colors"
      >
        Products
      </ViewTransitionLink>
      <ViewTransitionLink
        to="/portal/orders"
        className="text-sm font-medium text-muted-foreground hover:text-primary transition-colors"
      >
        Orders
      </ViewTransitionLink>
      <ViewTransitionLink
        to="/portal/settings"
        className="text-sm font-medium text-muted-foreground hover:text-primary transition-colors"
      >
        Settings
      </ViewTransitionLink>
    </nav>
  ),
}

export const AsButton: Story = {
  render: () => (
    <ViewTransitionLink
      to="/portal/products/new"
      className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 hover:bg-primary/90 transition-colors"
    >
      Create Product
    </ViewTransitionLink>
  ),
}
