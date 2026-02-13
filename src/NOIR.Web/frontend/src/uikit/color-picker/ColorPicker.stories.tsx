import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import { ColorPicker, ColorSwatch } from './ColorPicker'

const meta = {
  title: 'UIKit/ColorPicker',
  component: ColorPicker,
  tags: ['autodocs'],
  argTypes: {
    value: { control: 'color' },
    showCustomInput: { control: 'boolean' },
  },
} satisfies Meta<typeof ColorPicker>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => {
    const [color, setColor] = useState('#3B82F6')
    return <ColorPicker value={color} onChange={setColor} />
  },
}

export const WithoutCustomInput: Story = {
  render: () => {
    const [color, setColor] = useState('#EF4444')
    return <ColorPicker value={color} onChange={setColor} showCustomInput={false} />
  },
}

export const CustomColors: Story = {
  render: () => {
    const [color, setColor] = useState('#FF6B6B')
    const customColors = [
      '#FF6B6B', '#4ECDC4', '#45B7D1', '#96CEB4',
      '#FFEAA7', '#DDA0DD', '#98D8C8', '#F7DC6F',
    ]
    return <ColorPicker value={color} onChange={setColor} colors={customColors} />
  },
}

export const PreselectedCustomColor: Story = {
  render: () => {
    const [color, setColor] = useState('#A1C4FD')
    return <ColorPicker value={color} onChange={setColor} />
  },
}

export const SingleSwatch: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: '8px' }}>
      <ColorSwatch color="#3B82F6" isSelected={true} onClick={() => {}} />
      <ColorSwatch color="#EF4444" isSelected={false} onClick={() => {}} />
      <ColorSwatch color="#10B981" isSelected={false} onClick={() => {}} />
    </div>
  ),
}
