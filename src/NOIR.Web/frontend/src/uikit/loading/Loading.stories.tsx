import type { Meta, StoryObj } from 'storybook'
import {
  Spinner,
  RefreshSpinner,
  ButtonSpinner,
  PageSpinner,
  TableRowSkeleton,
  CardSkeleton,
  TimelineEntrySkeleton,
  ListItemSkeleton,
  FormSkeleton,
  InlineContentSkeleton,
} from './Loading'
import { Button } from '../button/Button'

const meta = {
  title: 'UIKit/Loading',
  component: Spinner,
  tags: ['autodocs'],
} satisfies Meta<typeof Spinner>

export default meta
type Story = StoryObj<typeof meta>

export const SpinnerDefault: Story = {
  render: () => <Spinner />,
}

export const SpinnerSizes: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
      <Spinner size="xs" />
      <Spinner size="sm" />
      <Spinner size="md" />
      <Spinner size="lg" />
    </div>
  ),
}

export const RefreshSpinnerDefault: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
      <RefreshSpinner size="sm" isRefreshing={false} />
      <RefreshSpinner size="sm" isRefreshing={true} />
    </div>
  ),
}

export const ButtonSpinnerExample: Story = {
  render: () => (
    <Button disabled>
      <ButtonSpinner />
      Saving...
    </Button>
  ),
}

export const PageSpinnerDefault: Story = {
  render: () => <PageSpinner text="Loading page..." />,
}

export const PageSpinnerWithoutText: Story = {
  render: () => <PageSpinner />,
}

export const TableRowSkeletonDefault: Story = {
  render: () => (
    <div style={{ width: 600 }}>
      <TableRowSkeleton columns={4} rows={3} />
    </div>
  ),
}

export const TableRowSkeletonWithAvatar: Story = {
  render: () => (
    <div style={{ width: 600 }}>
      <TableRowSkeleton columns={4} rows={3} showAvatar />
    </div>
  ),
}

export const CardSkeletonDefault: Story = {
  render: () => (
    <div style={{ width: 400, border: '1px solid #e5e7eb', borderRadius: 8 }}>
      <CardSkeleton />
    </div>
  ),
}

export const CardSkeletonNoIcon: Story = {
  render: () => (
    <div style={{ width: 400, border: '1px solid #e5e7eb', borderRadius: 8 }}>
      <CardSkeleton showIcon={false} lines={3} />
    </div>
  ),
}

export const TimelineEntrySkeletonDefault: Story = {
  render: () => (
    <div style={{ width: 500 }}>
      <TimelineEntrySkeleton count={3} />
    </div>
  ),
}

export const ListItemSkeletonDefault: Story = {
  render: () => (
    <div style={{ width: 400 }}>
      <ListItemSkeleton count={4} />
    </div>
  ),
}

export const ListItemSkeletonNoIcons: Story = {
  render: () => (
    <div style={{ width: 400 }}>
      <ListItemSkeleton count={3} showIcon={false} />
    </div>
  ),
}

export const FormSkeletonDefault: Story = {
  render: () => (
    <div style={{ width: 400 }}>
      <FormSkeleton fields={4} />
    </div>
  ),
}

export const InlineContentSkeletonDefault: Story = {
  render: () => (
    <p>
      There are <InlineContentSkeleton width="w-[40px]" /> items in the list,
      totaling <InlineContentSkeleton width="w-[80px]" /> in value.
    </p>
  ),
}

export const AllLoadingStates: Story = {
  parameters: {
    layout: 'padded',
  },
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '32px', maxWidth: 600 }}>
      <div>
        <h3 style={{ marginBottom: 8, fontWeight: 600 }}>Spinners</h3>
        <div style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
          <Spinner size="xs" />
          <Spinner size="sm" />
          <Spinner size="md" />
          <Spinner size="lg" />
          <RefreshSpinner isRefreshing />
        </div>
      </div>
      <div>
        <h3 style={{ marginBottom: 8, fontWeight: 600 }}>Button with spinner</h3>
        <Button disabled>
          <ButtonSpinner />
          Processing...
        </Button>
      </div>
      <div>
        <h3 style={{ marginBottom: 8, fontWeight: 600 }}>Page spinner</h3>
        <PageSpinner text="Loading data..." />
      </div>
      <div>
        <h3 style={{ marginBottom: 8, fontWeight: 600 }}>Card skeleton</h3>
        <div style={{ border: '1px solid #e5e7eb', borderRadius: 8 }}>
          <CardSkeleton />
        </div>
      </div>
      <div>
        <h3 style={{ marginBottom: 8, fontWeight: 600 }}>Form skeleton</h3>
        <FormSkeleton fields={3} />
      </div>
    </div>
  ),
}
