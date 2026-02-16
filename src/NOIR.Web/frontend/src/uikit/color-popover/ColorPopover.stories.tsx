import type { Meta, StoryObj } from 'storybook'
import { ColorPopover } from './ColorPopover'

const meta = {
  title: 'UIKit/ColorPopover',
  component: ColorPopover,
  tags: ['autodocs'],
  argTypes: {
    color: { control: 'color' },
    size: {
      control: 'select',
      options: ['sm', 'md'],
    },
  },
} satisfies Meta<typeof ColorPopover>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    color: '#3B82F6',
    size: 'sm',
  },
}

export const MediumSize: Story = {
  args: {
    color: '#EF4444',
    size: 'md',
  },
}

export const DarkColor: Story = {
  args: {
    color: '#1E293B',
    size: 'sm',
  },
}

export const AllSizes: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: '16px', alignItems: 'center' }}>
      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '4px' }}>
        <ColorPopover color="#3B82F6" size="sm" />
        <span style={{ fontSize: 11, color: '#6b7280' }}>sm</span>
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '4px' }}>
        <ColorPopover color="#3B82F6" size="md" />
        <span style={{ fontSize: 11, color: '#6b7280' }}>md</span>
      </div>
    </div>
  ),
}

export const ColorPalette: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
      <ColorPopover color="#EF4444" />
      <ColorPopover color="#F97316" />
      <ColorPopover color="#EAB308" />
      <ColorPopover color="#22C55E" />
      <ColorPopover color="#3B82F6" />
      <ColorPopover color="#8B5CF6" />
      <ColorPopover color="#EC4899" />
      <ColorPopover color="#6B7280" />
    </div>
  ),
}

export const InTableContext: Story = {
  render: () => (
    <table style={{ borderCollapse: 'collapse', width: '100%', maxWidth: 400 }}>
      <thead>
        <tr>
          <th style={{ textAlign: 'left', padding: '8px', borderBottom: '1px solid #e5e7eb', fontSize: 13 }}>Color</th>
          <th style={{ textAlign: 'left', padding: '8px', borderBottom: '1px solid #e5e7eb', fontSize: 13 }}>Name</th>
        </tr>
      </thead>
      <tbody>
        {[
          { color: '#EF4444', name: 'Red' },
          { color: '#3B82F6', name: 'Blue' },
          { color: '#22C55E', name: 'Green' },
          { color: '#F97316', name: 'Orange' },
        ].map(({ color, name }) => (
          <tr key={color}>
            <td style={{ padding: '8px', borderBottom: '1px solid #f3f4f6' }}>
              <ColorPopover color={color} />
            </td>
            <td style={{ padding: '8px', borderBottom: '1px solid #f3f4f6', fontSize: 13 }}>{name}</td>
          </tr>
        ))}
      </tbody>
    </table>
  ),
}
