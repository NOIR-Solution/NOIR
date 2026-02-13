import type { Meta, StoryObj } from 'storybook'
import { JsonViewer } from './JsonViewer'

const meta = {
  title: 'UIKit/JsonViewer',
  component: JsonViewer,
  tags: ['autodocs'],
  decorators: [
    (Story) => (
      <div style={{ maxWidth: 600, padding: 16 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof JsonViewer>

export default meta
type Story = StoryObj<typeof meta>

export const SimpleObject: Story = {
  args: {
    data: {
      name: 'John Doe',
      email: 'john@example.com',
      age: 30,
      isActive: true,
    },
    title: 'User Data',
  },
}

export const NestedObject: Story = {
  args: {
    data: {
      user: {
        id: 1,
        name: 'Alice',
        address: {
          street: '123 Main St',
          city: 'Wonderland',
          zip: '12345',
        },
      },
      roles: ['admin', 'editor'],
      metadata: {
        createdAt: '2026-01-15T10:00:00Z',
        isVerified: true,
      },
    },
    title: 'Nested Data',
  },
}

export const ArrayData: Story = {
  args: {
    data: [
      { id: 1, name: 'Product A', price: 19.99 },
      { id: 2, name: 'Product B', price: 29.99 },
      { id: 3, name: 'Product C', price: 39.99 },
    ],
    title: 'Product List',
  },
}

export const CollapsedByDefault: Story = {
  args: {
    data: {
      config: {
        database: { host: 'localhost', port: 5432 },
        cache: { ttl: 3600, driver: 'redis' },
      },
      features: ['auth', 'logging', 'metrics'],
    },
    defaultExpanded: false,
    title: 'Collapsed Config',
  },
}

export const LimitedDepth: Story = {
  args: {
    data: {
      level1: {
        level2: {
          level3: {
            level4: {
              value: 'deeply nested',
            },
          },
        },
      },
    },
    maxDepth: 2,
    title: 'Max Depth = 2',
  },
}

export const WithRootName: Story = {
  args: {
    data: { host: 'localhost', port: 3000, debug: true },
    rootName: 'config',
    title: 'Named Root',
  },
}

export const FromJsonString: Story = {
  args: {
    data: '{"message":"Hello World","status":200,"items":[1,2,3]}',
    title: 'Parsed from String',
  },
}

export const PlainString: Story = {
  args: {
    data: 'This is a plain text string, not JSON.',
    title: 'Plain Text',
  },
}

export const EmptyData: Story = {
  args: {
    data: null,
    title: 'Empty',
  },
}

export const EmptyCollections: Story = {
  args: {
    data: {
      emptyObject: {},
      emptyArray: [],
      normalField: 'value',
    },
    title: 'Empty Collections',
  },
}

export const NoFullscreen: Story = {
  args: {
    data: { key: 'value', count: 42 },
    allowFullscreen: false,
    title: 'No Fullscreen Button',
  },
}

export const CustomMaxHeight: Story = {
  args: {
    data: Array.from({ length: 50 }, (_, i) => ({
      id: i + 1,
      name: `Item ${i + 1}`,
      value: Math.random().toFixed(4),
    })),
    maxHeight: '200px',
    title: 'Scrollable (200px)',
  },
}

export const MixedTypes: Story = {
  args: {
    data: {
      string: 'hello',
      number: 42,
      float: 3.14,
      boolean: true,
      nullValue: null,
      array: [1, 'two', false, null],
      object: { nested: true },
    },
    title: 'Mixed Types',
  },
}
