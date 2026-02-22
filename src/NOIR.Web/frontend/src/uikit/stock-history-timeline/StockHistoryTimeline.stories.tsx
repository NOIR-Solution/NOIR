import type { Meta, StoryObj } from 'storybook'
import { StockHistoryTimeline } from '@/components/products/StockHistoryTimeline'
import type { StockMovement } from '@/components/products/StockHistoryTimeline'

// StockHistoryTimeline uses react-i18next and date-fns.
// The component is imported directly — react-i18next gracefully falls back to
// the `defaultValue`-style keys when no i18n provider is present in Storybook.

const now = new Date()
const hoursAgo = (h: number) => new Date(now.getTime() - h * 60 * 60 * 1000).toISOString()

const SAMPLE_MOVEMENTS: StockMovement[] = [
  {
    id: '1',
    type: 'initial',
    quantity: 100,
    previousStock: 0,
    newStock: 100,
    reason: 'Initial stock entry',
    createdAt: hoursAgo(72),
    createdBy: 'admin@noir.local',
  },
  {
    id: '2',
    type: 'restock',
    quantity: 50,
    previousStock: 80,
    newStock: 130,
    reason: 'Purchase order PO-2024-001',
    createdAt: hoursAgo(48),
    createdBy: 'warehouse@noir.local',
  },
  {
    id: '3',
    type: 'sale',
    quantity: -5,
    previousStock: 130,
    newStock: 125,
    orderId: 'ORD-2024-0042',
    createdAt: hoursAgo(36),
  },
  {
    id: '4',
    type: 'reserved',
    quantity: -10,
    previousStock: 125,
    newStock: 115,
    reason: 'Cart reservation — pending checkout',
    createdAt: hoursAgo(24),
  },
  {
    id: '5',
    type: 'released',
    quantity: 10,
    previousStock: 115,
    newStock: 125,
    reason: 'Cart abandoned — stock released',
    createdAt: hoursAgo(23),
  },
  {
    id: '6',
    type: 'return',
    quantity: 2,
    previousStock: 125,
    newStock: 127,
    orderId: 'ORD-2024-0031',
    reason: 'Customer return — item undamaged',
    createdAt: hoursAgo(12),
    createdBy: 'support@noir.local',
  },
  {
    id: '7',
    type: 'adjustment',
    quantity: -3,
    previousStock: 127,
    newStock: 124,
    reason: 'Inventory count correction',
    createdAt: hoursAgo(2),
    createdBy: 'admin@noir.local',
  },
]

// --- Meta ---

const meta = {
  title: 'UIKit/StockHistoryTimeline',
  component: StockHistoryTimeline,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component:
          'Displays a chronological timeline of stock movements for a product variant. ' +
          'Supports 7 movement types: initial, restock, sale, return, reserved, released, adjustment.',
      },
    },
  },
  decorators: [
    (Story) => (
      <div style={{ maxWidth: 520 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof StockHistoryTimeline>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  args: {
    movements: SAMPLE_MOVEMENTS,
    currentStock: 124,
    variantName: 'Classic Black / Size M',
  },
}

export const Empty: Story = {
  parameters: {
    docs: {
      description: { story: 'No stock movements yet — shows empty state with hint.' },
    },
  },
  args: {
    movements: [],
    currentStock: 0,
    variantName: 'Classic White / Size S',
  },
}

export const SingleMovement: Story = {
  parameters: {
    docs: {
      description: { story: 'Only the initial stock entry.' },
    },
  },
  args: {
    movements: [SAMPLE_MOVEMENTS[0]],
    currentStock: 100,
    variantName: 'Navy Blue / Size L',
  },
}

export const AllMovementTypes: Story = {
  parameters: {
    docs: {
      description: { story: 'One of each movement type to showcase all icon/colour combinations.' },
    },
  },
  args: {
    movements: SAMPLE_MOVEMENTS,
    currentStock: 124,
  },
}

export const WithoutVariantName: Story = {
  parameters: {
    docs: {
      description: { story: 'When variantName is omitted the header shows generic "Current Stock" label.' },
    },
  },
  args: {
    movements: SAMPLE_MOVEMENTS.slice(0, 4),
    currentStock: 115,
  },
}

export const LowStock: Story = {
  parameters: {
    docs: {
      description: { story: 'Product with critically low stock after a large sale.' },
    },
  },
  args: {
    movements: [
      {
        id: '1',
        type: 'initial',
        quantity: 10,
        previousStock: 0,
        newStock: 10,
        createdAt: hoursAgo(48),
      },
      {
        id: '2',
        type: 'sale',
        quantity: -8,
        previousStock: 10,
        newStock: 2,
        orderId: 'ORD-2024-9001',
        createdAt: hoursAgo(1),
      },
    ],
    currentStock: 2,
    variantName: 'Limited Edition / One Size',
  },
}
