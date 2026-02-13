import type { Meta, StoryObj } from 'storybook'
import {
  ResponsiveDataView,
  MobileCard,
  MobileCardHeader,
  MobileCardActions,
  MobileCardField,
} from './ResponsiveDataView'
import { Button } from '../button/Button'
import { Badge } from '../badge/Badge'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '../table/Table'

interface SampleItem {
  id: number
  name: string
  email: string
  role: string
  status: 'Active' | 'Inactive'
}

const sampleData: SampleItem[] = [
  { id: 1, name: 'Alice Johnson', email: 'alice@example.com', role: 'Admin', status: 'Active' },
  { id: 2, name: 'Bob Smith', email: 'bob@example.com', role: 'User', status: 'Active' },
  { id: 3, name: 'Charlie Brown', email: 'charlie@example.com', role: 'Editor', status: 'Inactive' },
  { id: 4, name: 'Diana Prince', email: 'diana@example.com', role: 'Admin', status: 'Active' },
]

function renderSampleTable() {
  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Name</TableHead>
          <TableHead>Email</TableHead>
          <TableHead>Role</TableHead>
          <TableHead>Status</TableHead>
          <TableHead>Actions</TableHead>
        </TableRow>
      </TableHeader>
      <TableBody>
        {sampleData.map((item) => (
          <TableRow key={item.id}>
            <TableCell className="font-medium">{item.name}</TableCell>
            <TableCell>{item.email}</TableCell>
            <TableCell>{item.role}</TableCell>
            <TableCell>
              <Badge variant={item.status === 'Active' ? 'default' : 'secondary'}>
                {item.status}
              </Badge>
            </TableCell>
            <TableCell>
              <Button variant="ghost" size="sm">Edit</Button>
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}

function renderSampleCard(item: SampleItem) {
  return (
    <MobileCard key={item.id}>
      <MobileCardHeader
        title={item.name}
        subtitle={item.email}
        badge={
          <Badge variant={item.status === 'Active' ? 'default' : 'secondary'}>
            {item.status}
          </Badge>
        }
      />
      <MobileCardField label="Role" value={item.role} />
      <MobileCardActions>
        <Button variant="outline" size="sm">Edit</Button>
        <Button variant="ghost" size="sm">Delete</Button>
      </MobileCardActions>
    </MobileCard>
  )
}

const meta = {
  title: 'UIKit/ResponsiveDataView',
  component: ResponsiveDataView,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
  },
} satisfies Meta<typeof ResponsiveDataView>

export default meta
type Story = StoryObj<typeof meta>

export const TableView: Story = {
  render: () => (
    <ResponsiveDataView
      data={sampleData}
      renderTable={renderSampleTable}
      renderCard={renderSampleCard}
      forceView="table"
    />
  ),
}

export const CardView: Story = {
  render: () => (
    <ResponsiveDataView
      data={sampleData}
      renderTable={renderSampleTable}
      renderCard={renderSampleCard}
      forceView="cards"
    />
  ),
}

export const Loading: Story = {
  render: () => (
    <ResponsiveDataView
      data={[]}
      renderTable={renderSampleTable}
      renderCard={renderSampleCard}
      loading={true}
      skeletonCount={4}
      forceView="cards"
    />
  ),
}

export const LoadingTable: Story = {
  render: () => (
    <ResponsiveDataView
      data={[]}
      renderTable={renderSampleTable}
      renderCard={renderSampleCard}
      loading={true}
      forceView="table"
    />
  ),
}

export const SmallGap: Story = {
  render: () => (
    <ResponsiveDataView
      data={sampleData}
      renderTable={renderSampleTable}
      renderCard={renderSampleCard}
      forceView="cards"
      cardGap="sm"
    />
  ),
}

export const LargeGap: Story = {
  render: () => (
    <ResponsiveDataView
      data={sampleData}
      renderTable={renderSampleTable}
      renderCard={renderSampleCard}
      forceView="cards"
      cardGap="lg"
    />
  ),
}

export const MobileCardStandalone: Story = {
  render: () => (
    <div style={{ maxWidth: 400 }}>
      <MobileCard onClick={() => {}}>
        <MobileCardHeader
          title="John Doe"
          subtitle="john@example.com"
          badge={<Badge>Active</Badge>}
        />
        <MobileCardField label="Role" value="Administrator" />
        <MobileCardField label="Last Login" value="2 hours ago" />
        <MobileCardActions>
          <Button variant="outline" size="sm">View</Button>
          <Button variant="ghost" size="sm">Edit</Button>
        </MobileCardActions>
      </MobileCard>
    </div>
  ),
}
