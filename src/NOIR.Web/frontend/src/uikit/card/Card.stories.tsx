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
