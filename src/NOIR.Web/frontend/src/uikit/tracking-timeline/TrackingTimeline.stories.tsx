import type { Meta, StoryObj } from 'storybook'
import { TrackingTimeline } from '@/portal-app/shipping/components/TrackingTimeline'

// TrackingTimeline uses react-i18next, RegionalSettingsContext, and lucide icons.
// react-i18next gracefully falls back to the defaultValue-style keys when no
// i18n provider is present in Storybook.

// Inline mock types matching the component's expected shape so the story is
// self-contained and doesn't import from @/types/shipping.
type ShippingStatus =
  | 'Draft'
  | 'AwaitingPickup'
  | 'PickedUp'
  | 'InTransit'
  | 'OutForDelivery'
  | 'Delivered'
  | 'DeliveryFailed'
  | 'Cancelled'
  | 'Returning'
  | 'Returned'

interface TrackingEvent {
  eventType: string
  status: ShippingStatus
  description: string
  location?: string | null
  eventDate: string
}

const now = new Date()
const hoursAgo = (h: number) => new Date(now.getTime() - h * 60 * 60 * 1000).toISOString()

const SAMPLE_EVENTS: TrackingEvent[] = [
  {
    eventType: 'StatusUpdate',
    status: 'Draft',
    description: 'Shipment created — awaiting label generation',
    location: null,
    eventDate: hoursAgo(96),
  },
  {
    eventType: 'StatusUpdate',
    status: 'AwaitingPickup',
    description: 'Label generated — package ready for carrier pickup',
    location: 'Warehouse A, Ho Chi Minh City',
    eventDate: hoursAgo(72),
  },
  {
    eventType: 'StatusUpdate',
    status: 'PickedUp',
    description: 'Package picked up by carrier',
    location: 'Warehouse A, Ho Chi Minh City',
    eventDate: hoursAgo(68),
  },
  {
    eventType: 'StatusUpdate',
    status: 'InTransit',
    description: 'Package in transit to regional sorting facility',
    location: 'Sorting Center, Binh Duong',
    eventDate: hoursAgo(48),
  },
  {
    eventType: 'StatusUpdate',
    status: 'InTransit',
    description: 'Package departed sorting facility',
    location: 'Sorting Center, Da Nang',
    eventDate: hoursAgo(24),
  },
  {
    eventType: 'StatusUpdate',
    status: 'OutForDelivery',
    description: 'Package out for delivery',
    location: 'District 1, Ho Chi Minh City',
    eventDate: hoursAgo(4),
  },
  {
    eventType: 'StatusUpdate',
    status: 'Delivered',
    description: 'Package delivered — signed by recipient',
    location: '123 Nguyen Hue, District 1, Ho Chi Minh City',
    eventDate: hoursAgo(2),
  },
]

// --- Meta ---

const meta = {
  title: 'UIKit/TrackingTimeline',
  component: TrackingTimeline,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
    docs: {
      description: {
        component:
          'Displays a chronological timeline of shipping tracking events. ' +
          'Supports 10 shipping statuses with distinct icons and colour coding. ' +
          'Used in both the Shipping and Orders domains.',
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
} satisfies Meta<typeof TrackingTimeline>

export default meta
type Story = StoryObj<typeof meta>

// --- Stories ---

export const Default: Story = {
  args: {
    events: SAMPLE_EVENTS,
  },
}

export const Empty: Story = {
  parameters: {
    docs: {
      description: { story: 'No tracking events yet — shows empty state with helpful message.' },
    },
  },
  args: {
    events: [],
  },
}

export const SingleEvent: Story = {
  parameters: {
    docs: {
      description: { story: 'A shipment that was just created with only one tracking event.' },
    },
  },
  args: {
    events: [SAMPLE_EVENTS[0]],
  },
}

export const ManyEvents: Story = {
  parameters: {
    docs: {
      description: { story: 'Full delivery lifecycle with all tracking milestones.' },
    },
  },
  args: {
    events: SAMPLE_EVENTS,
  },
}

export const DeliveryFailed: Story = {
  parameters: {
    docs: {
      description: { story: 'A shipment where the delivery attempt failed.' },
    },
  },
  args: {
    events: [
      ...SAMPLE_EVENTS.slice(0, 5),
      {
        eventType: 'StatusUpdate',
        status: 'OutForDelivery' as ShippingStatus,
        description: 'First delivery attempt',
        location: 'District 7, Ho Chi Minh City',
        eventDate: hoursAgo(6),
      },
      {
        eventType: 'StatusUpdate',
        status: 'DeliveryFailed' as ShippingStatus,
        description: 'Delivery failed — recipient not available',
        location: 'District 7, Ho Chi Minh City',
        eventDate: hoursAgo(5),
      },
    ],
  },
}

export const CancelledShipment: Story = {
  parameters: {
    docs: {
      description: { story: 'A shipment that was cancelled before delivery.' },
    },
  },
  args: {
    events: [
      SAMPLE_EVENTS[0],
      SAMPLE_EVENTS[1],
      {
        eventType: 'StatusUpdate',
        status: 'Cancelled' as ShippingStatus,
        description: 'Shipment cancelled by customer request',
        location: null,
        eventDate: hoursAgo(60),
      },
    ],
  },
}

export const ReturnFlow: Story = {
  parameters: {
    docs: {
      description: { story: 'A delivered package being returned to sender.' },
    },
  },
  args: {
    events: [
      ...SAMPLE_EVENTS,
      {
        eventType: 'StatusUpdate',
        status: 'Returning' as ShippingStatus,
        description: 'Return initiated — package being shipped back',
        location: 'District 1, Ho Chi Minh City',
        eventDate: hoursAgo(1),
      },
      {
        eventType: 'StatusUpdate',
        status: 'Returned' as ShippingStatus,
        description: 'Package returned to warehouse',
        location: 'Warehouse A, Ho Chi Minh City',
        eventDate: new Date().toISOString(),
      },
    ],
  },
}

export const WithoutLocations: Story = {
  parameters: {
    docs: {
      description: { story: 'Tracking events without location data — only descriptions and timestamps shown.' },
    },
  },
  args: {
    events: [
      {
        eventType: 'StatusUpdate',
        status: 'AwaitingPickup' as ShippingStatus,
        description: 'Label generated',
        eventDate: hoursAgo(48),
      },
      {
        eventType: 'StatusUpdate',
        status: 'PickedUp' as ShippingStatus,
        description: 'Package picked up by carrier',
        eventDate: hoursAgo(44),
      },
      {
        eventType: 'StatusUpdate',
        status: 'InTransit' as ShippingStatus,
        description: 'Package in transit',
        eventDate: hoursAgo(24),
      },
    ],
  },
}
