import type { Meta, StoryObj } from 'storybook'
import { DiffViewer } from './DiffViewer'

const meta = {
  title: 'UIKit/DiffViewer',
  component: DiffViewer,
  tags: ['autodocs'],
  decorators: [
    (Story) => (
      <div style={{ maxWidth: 600, padding: 16 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof DiffViewer>

export default meta
type Story = StoryObj<typeof meta>

export const ModifiedFields: Story = {
  args: {
    data: {
      name: { from: 'John Doe', to: 'Jane Doe' },
      email: { from: 'john@example.com', to: 'jane@example.com' },
      isActive: { from: true, to: false },
    },
  },
}

export const AddedFields: Story = {
  args: {
    data: {
      phoneNumber: { from: null, to: '+1-555-0199' },
      bio: { from: null, to: 'Software developer and open source contributor.' },
    },
  },
}

export const RemovedFields: Story = {
  args: {
    data: {
      nickname: { from: 'Johnny', to: null },
      altEmail: { from: 'alt@example.com', to: null },
    },
  },
}

export const MixedChanges: Story = {
  args: {
    data: {
      name: { from: 'Alice Smith', to: 'Alice Johnson' },
      role: { from: null, to: 'Admin' },
      phoneNumber: { from: '+1-555-0100', to: null },
      sortOrder: { from: 1, to: 5 },
    },
  },
}

export const InlineDiffHighlight: Story = {
  args: {
    data: {
      slug: { from: 'my-old-product-slug', to: 'my-new-product-slug' },
      url: {
        from: 'https://example.com/api/v1/products',
        to: 'https://example.com/api/v2/products',
      },
    },
  },
}

export const FromJsonString: Story = {
  args: {
    data: JSON.stringify({
      status: { from: 'Draft', to: 'Active' },
      price: { from: 9.99, to: 19.99 },
    }),
  },
}

export const NoChanges: Story = {
  args: {
    data: {
      modifiedAt: { from: '2026-01-01', to: '2026-02-01' },
      createdBy: { from: 'system', to: 'admin' },
    },
  },
}

export const NonStringModified: Story = {
  args: {
    data: {
      quantity: { from: 10, to: 25 },
      isPublished: { from: false, to: true },
      tags: { from: ['tag1', 'tag2'], to: ['tag1', 'tag2', 'tag3'] },
    },
  },
}
