import type { Meta, StoryObj } from 'storybook'
import { Skeleton } from './Skeleton'

const meta = {
  title: 'UIKit/Skeleton',
  component: Skeleton,
  tags: ['autodocs'],
} satisfies Meta<typeof Skeleton>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    style: { width: 200, height: 20 },
  },
}

export const Circle: Story = {
  args: {
    className: 'rounded-full',
    style: { width: 48, height: 48 },
  },
}

export const Rectangle: Story = {
  args: {
    style: { width: 300, height: 150 },
  },
}

export const TextLine: Story = {
  args: {
    style: { width: '100%', height: 16 },
  },
}

export const CardSkeleton: Story = {
  render: () => (
    <div
      style={{
        maxWidth: 400,
        padding: '24px',
        borderRadius: '12px',
        border: '1px solid #e5e7eb',
      }}
    >
      <div style={{ display: 'flex', alignItems: 'center', gap: '12px', marginBottom: '16px' }}>
        <Skeleton className="rounded-full" style={{ width: 40, height: 40 }} />
        <div style={{ flex: 1 }}>
          <Skeleton style={{ width: '60%', height: 16, marginBottom: 8 }} />
          <Skeleton style={{ width: '40%', height: 12 }} />
        </div>
      </div>
      <Skeleton style={{ width: '100%', height: 16, marginBottom: 8 }} />
      <Skeleton style={{ width: '100%', height: 16, marginBottom: 8 }} />
      <Skeleton style={{ width: '75%', height: 16 }} />
    </div>
  ),
}

export const ListSkeleton: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', maxWidth: 400 }}>
      {Array.from({ length: 5 }).map((_, i) => (
        <div key={i} style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <Skeleton className="rounded-full" style={{ width: 32, height: 32 }} />
          <div style={{ flex: 1 }}>
            <Skeleton style={{ width: '70%', height: 14, marginBottom: 6 }} />
            <Skeleton style={{ width: '50%', height: 12 }} />
          </div>
        </div>
      ))}
    </div>
  ),
}

export const FormSkeleton: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', maxWidth: 400 }}>
      <div>
        <Skeleton style={{ width: 80, height: 14, marginBottom: 8 }} />
        <Skeleton style={{ width: '100%', height: 36 }} />
      </div>
      <div>
        <Skeleton style={{ width: 60, height: 14, marginBottom: 8 }} />
        <Skeleton style={{ width: '100%', height: 36 }} />
      </div>
      <div>
        <Skeleton style={{ width: 100, height: 14, marginBottom: 8 }} />
        <Skeleton style={{ width: '100%', height: 80 }} />
      </div>
      <Skeleton style={{ width: 120, height: 36 }} />
    </div>
  ),
}

export const TableSkeleton: Story = {
  render: () => (
    <div style={{ maxWidth: 600 }}>
      <div style={{ display: 'flex', gap: '16px', marginBottom: '12px' }}>
        <Skeleton style={{ flex: 2, height: 16 }} />
        <Skeleton style={{ flex: 1, height: 16 }} />
        <Skeleton style={{ flex: 1, height: 16 }} />
      </div>
      {Array.from({ length: 4 }).map((_, i) => (
        <div key={i} style={{ display: 'flex', gap: '16px', marginBottom: '8px' }}>
          <Skeleton style={{ flex: 2, height: 14 }} />
          <Skeleton style={{ flex: 1, height: 14 }} />
          <Skeleton style={{ flex: 1, height: 14 }} />
        </div>
      ))}
    </div>
  ),
}
