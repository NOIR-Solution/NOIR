import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import { DatePicker } from './DatePicker'

function DatePickerDemo(props: {
  initialValue?: Date
  placeholder?: string
  minDate?: Date
  maxDate?: Date
  disabled?: boolean
}) {
  const [value, setValue] = useState<Date | undefined>(props.initialValue)

  return (
    <div style={{ maxWidth: 300 }}>
      <DatePicker
        value={value}
        onChange={setValue}
        placeholder={props.placeholder}
        minDate={props.minDate}
        maxDate={props.maxDate}
        disabled={props.disabled}
      />
      {value && (
        <p style={{ marginTop: 8, fontSize: 13, color: '#6b7280' }}>
          Selected: {value.toLocaleDateString()}
        </p>
      )}
    </div>
  )
}

const meta = {
  title: 'UIKit/DatePicker',
  component: DatePicker,
  tags: ['autodocs'],
  decorators: [
    (Story) => (
      <div style={{ padding: 16 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof DatePicker>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => <DatePickerDemo />,
}

export const WithPreselectedDate: Story = {
  render: () => <DatePickerDemo initialValue={new Date(2026, 1, 13)} />,
}

export const CustomPlaceholder: Story = {
  render: () => <DatePickerDemo placeholder="Pick a date..." />,
}

export const WithMinDate: Story = {
  render: () => (
    <DatePickerDemo
      placeholder="No past dates"
      minDate={new Date()}
    />
  ),
}

export const WithMaxDate: Story = {
  render: () => (
    <DatePickerDemo
      placeholder="No future dates"
      maxDate={new Date()}
    />
  ),
}

export const WithDateRange: Story = {
  render: () => (
    <DatePickerDemo
      placeholder="Select within range"
      minDate={new Date(2026, 0, 1)}
      maxDate={new Date(2026, 11, 31)}
    />
  ),
}

export const Disabled: Story = {
  render: () => <DatePickerDemo disabled />,
}

export const DisabledWithValue: Story = {
  render: () => <DatePickerDemo initialValue={new Date(2026, 5, 15)} disabled />,
}
