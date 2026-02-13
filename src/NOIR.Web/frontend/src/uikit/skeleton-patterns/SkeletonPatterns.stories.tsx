import type { Meta, StoryObj } from 'storybook'
import {
  TableRowSkeleton,
  TableSkeleton,
  CardGridSkeleton,
  PageHeaderSkeleton,
  FormSkeleton,
  StatCardSkeleton,
  StatGridSkeleton,
  ListItemSkeleton,
  ListSkeleton,
  DetailPageSkeleton,
} from './SkeletonPatterns'
import { Table, TableBody } from '../table/Table'

const meta = {
  title: 'UIKit/SkeletonPatterns',
  component: TableSkeleton,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
  },
} satisfies Meta<typeof TableSkeleton>

export default meta
type Story = StoryObj<typeof meta>

export const TableRowSkeletonDefault: Story = {
  render: () => (
    <Table>
      <TableBody>
        <TableRowSkeleton columns={5} />
      </TableBody>
    </Table>
  ),
}

export const TableSkeletonDefault: Story = {
  render: () => (
    <Table>
      <TableBody>
        <TableSkeleton rows={5} columns={4} />
      </TableBody>
    </Table>
  ),
}

export const CardGridDefault: Story = {
  render: () => <CardGridSkeleton count={6} columns={3} />,
}

export const CardGridWithoutImages: Story = {
  render: () => <CardGridSkeleton count={4} columns={2} showImage={false} />,
}

export const CardGridFourColumns: Story = {
  render: () => <CardGridSkeleton count={8} columns={4} />,
}

export const PageHeaderSkeletonDefault: Story = {
  render: () => <PageHeaderSkeleton />,
}

export const FormSkeletonDefault: Story = {
  render: () => (
    <div style={{ maxWidth: 500 }}>
      <FormSkeleton fields={4} />
    </div>
  ),
}

export const FormSkeletonFewFields: Story = {
  render: () => (
    <div style={{ maxWidth: 500 }}>
      <FormSkeleton fields={2} />
    </div>
  ),
}

export const StatCardSkeletonDefault: Story = {
  render: () => (
    <div style={{ maxWidth: 250 }}>
      <StatCardSkeleton />
    </div>
  ),
}

export const StatGridDefault: Story = {
  render: () => <StatGridSkeleton count={4} />,
}

export const ListItemSkeletonDefault: Story = {
  render: () => (
    <div style={{ maxWidth: 500 }}>
      <ListItemSkeleton />
    </div>
  ),
}

export const ListItemSkeletonNoAvatar: Story = {
  render: () => (
    <div style={{ maxWidth: 500 }}>
      <ListItemSkeleton showAvatar={false} />
    </div>
  ),
}

export const ListSkeletonDefault: Story = {
  render: () => (
    <div style={{ maxWidth: 500 }}>
      <ListSkeleton count={5} />
    </div>
  ),
}

export const DetailPageSkeletonDefault: Story = {
  render: () => (
    <div style={{ maxWidth: 900 }}>
      <DetailPageSkeleton />
    </div>
  ),
}

export const AllPatterns: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '48px' }}>
      <div>
        <h3 style={{ marginBottom: 12, fontWeight: 600, fontSize: 16 }}>Page Header</h3>
        <PageHeaderSkeleton />
      </div>
      <div>
        <h3 style={{ marginBottom: 12, fontWeight: 600, fontSize: 16 }}>Stat Grid</h3>
        <StatGridSkeleton count={4} />
      </div>
      <div>
        <h3 style={{ marginBottom: 12, fontWeight: 600, fontSize: 16 }}>Table</h3>
        <Table>
          <TableBody>
            <TableSkeleton rows={3} columns={5} />
          </TableBody>
        </Table>
      </div>
      <div>
        <h3 style={{ marginBottom: 12, fontWeight: 600, fontSize: 16 }}>Card Grid</h3>
        <CardGridSkeleton count={3} columns={3} />
      </div>
      <div style={{ maxWidth: 500 }}>
        <h3 style={{ marginBottom: 12, fontWeight: 600, fontSize: 16 }}>Form</h3>
        <FormSkeleton fields={3} />
      </div>
      <div style={{ maxWidth: 500 }}>
        <h3 style={{ marginBottom: 12, fontWeight: 600, fontSize: 16 }}>List</h3>
        <ListSkeleton count={3} />
      </div>
    </div>
  ),
}
