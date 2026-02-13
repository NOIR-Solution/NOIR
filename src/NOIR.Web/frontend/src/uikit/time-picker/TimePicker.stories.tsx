import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import { TimePicker } from './TimePicker'

function TimePickerDemo(props: {
  initialValue?: string
  placeholder?: string
  interval?: number
  disabled?: boolean
}) {
  const [value, setValue] = useState<string | undefined>(props.initialValue)

  return (
    <div style={{ maxWidth: 300 }}>
      <TimePicker
        value={value}
        onChange={setValue}
        placeholder={props.placeholder}
        interval={props.interval}
        disabled={props.disabled}
      />
      {value && (
        <p style={{ marginTop: 8, fontSize: 13, color: '#6b7280' }}>
          Selected: {value}
        </p>
      )}
    </div>
  )
}

const meta = {
  title: 'UIKit/TimePicker',
  component: TimePicker,
  tags: ['autodocs'],
  decorators: [
    (Story) => (
      <div style={{ padding: 16 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof TimePicker>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => <TimePickerDemo />,
}

export const WithPreselectedTime: Story = {
  render: () => <TimePickerDemo initialValue="14:30" />,
}

export const CustomPlaceholder: Story = {
  render: () => <TimePickerDemo placeholder="Choose a time..." />,
}

export const FifteenMinuteInterval: Story = {
  render: () => <TimePickerDemo interval={15} />,
}

export const HourlyInterval: Story = {
  render: () => <TimePickerDemo interval={60} />,
}

export const Disabled: Story = {
  render: () => <TimePickerDemo disabled />,
}

export const DisabledWithValue: Story = {
  render: () => <TimePickerDemo initialValue="09:00" disabled />,
}
