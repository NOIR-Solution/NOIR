import type { Meta, StoryObj } from 'storybook'
import { Separator } from './Separator'

const meta = {
  title: 'UIKit/Separator',
  component: Separator,
  tags: ['autodocs'],
  argTypes: {
    orientation: {
      control: 'select',
      options: ['horizontal', 'vertical'],
    },
  },
} satisfies Meta<typeof Separator>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    orientation: 'horizontal',
  },
}

export const Horizontal: Story = {
  render: () => (
    <div style={{ maxWidth: 400 }}>
      <div style={{ padding: '8px 0' }}>Content Above</div>
      <Separator orientation="horizontal" />
      <div style={{ padding: '8px 0' }}>Content Below</div>
    </div>
  ),
}

export const Vertical: Story = {
  render: () => (
    <div style={{ display: 'flex', alignItems: 'center', height: 40 }}>
      <span>Left</span>
      <Separator orientation="vertical" style={{ margin: '0 16px' }} />
      <span>Right</span>
    </div>
  ),
}

export const InList: Story = {
  render: () => (
    <div style={{ maxWidth: 300 }}>
      <div style={{ padding: '12px 0' }}>Item 1</div>
      <Separator />
      <div style={{ padding: '12px 0' }}>Item 2</div>
      <Separator />
      <div style={{ padding: '12px 0' }}>Item 3</div>
      <Separator />
      <div style={{ padding: '12px 0' }}>Item 4</div>
    </div>
  ),
}

export const InToolbar: Story = {
  render: () => (
    <div style={{ display: 'flex', alignItems: 'center', gap: '8px', height: 32 }}>
      <button style={{ padding: '4px 8px', cursor: 'pointer' }}>Bold</button>
      <button style={{ padding: '4px 8px', cursor: 'pointer' }}>Italic</button>
      <Separator orientation="vertical" />
      <button style={{ padding: '4px 8px', cursor: 'pointer' }}>Left</button>
      <button style={{ padding: '4px 8px', cursor: 'pointer' }}>Center</button>
      <button style={{ padding: '4px 8px', cursor: 'pointer' }}>Right</button>
      <Separator orientation="vertical" />
      <button style={{ padding: '4px 8px', cursor: 'pointer' }}>Undo</button>
      <button style={{ padding: '4px 8px', cursor: 'pointer' }}>Redo</button>
    </div>
  ),
}

export const WithText: Story = {
  render: () => (
    <div style={{ maxWidth: 400 }}>
      <h3 style={{ margin: 0, fontSize: '16px', fontWeight: 600 }}>Section Title</h3>
      <p style={{ margin: '4px 0 12px', fontSize: '14px', color: '#666' }}>
        Description of the section above.
      </p>
      <Separator />
      <h3 style={{ marginTop: '12px', fontSize: '16px', fontWeight: 600 }}>Another Section</h3>
      <p style={{ margin: '4px 0', fontSize: '14px', color: '#666' }}>
        Description of the section below.
      </p>
    </div>
  ),
}
