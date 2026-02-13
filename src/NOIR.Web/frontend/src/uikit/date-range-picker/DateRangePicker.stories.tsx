import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import type { DateRange } from 'react-day-picker'
import { DateRangePicker } from './DateRangePicker'

function DateRangePickerDemo(props: {
  initialValue?: DateRange
  placeholder?: string
  showClear?: boolean
  disabled?: boolean
  numberOfMonths?: number
  align?: 'start' | 'center' | 'end'
}) {
  const [value, setValue] = useState<DateRange | undefined>(props.initialValue)

  return (
    <div style={{ maxWidth: 400 }}>
      <DateRangePicker
        value={value}
        onChange={setValue}
        placeholder={props.placeholder}
        showClear={props.showClear}
        disabled={props.disabled}
        numberOfMonths={props.numberOfMonths}
        align={props.align}
      />
      {value?.from && (
        <p style={{ marginTop: 8, fontSize: 13, color: '#6b7280' }}>
          From: {value.from.toLocaleDateString()}
          {value.to && ` - To: ${value.to.toLocaleDateString()}`}
        </p>
      )}
    </div>
  )
}

const meta = {
  title: 'UIKit/DateRangePicker',
  component: DateRangePicker,
  tags: ['autodocs'],
  decorators: [
    (Story) => (
      <div style={{ padding: 16 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof DateRangePicker>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => <DateRangePickerDemo />,
}

export const WithPreselectedRange: Story = {
  render: () => (
    <DateRangePickerDemo
      initialValue={{
        from: new Date(2026, 1, 1),
        to: new Date(2026, 1, 14),
      }}
    />
  ),
}

export const CustomPlaceholder: Story = {
  render: () => <DateRangePickerDemo placeholder="Select date range..." />,
}

export const NoClearButton: Story = {
  render: () => (
    <DateRangePickerDemo
      showClear={false}
      initialValue={{
        from: new Date(2026, 0, 15),
        to: new Date(2026, 1, 15),
      }}
    />
  ),
}

export const SingleMonth: Story = {
  render: () => <DateRangePickerDemo numberOfMonths={1} />,
}

export const AlignStart: Story = {
  render: () => <DateRangePickerDemo align="start" />,
}

export const AlignCenter: Story = {
  render: () => <DateRangePickerDemo align="center" />,
}

export const Disabled: Story = {
  render: () => <DateRangePickerDemo disabled />,
}

export const DisabledWithValue: Story = {
  render: () => (
    <DateRangePickerDemo
      initialValue={{
        from: new Date(2026, 2, 1),
        to: new Date(2026, 2, 31),
      }}
      disabled
    />
  ),
}

export const OnlyFromDate: Story = {
  render: () => (
    <DateRangePickerDemo
      initialValue={{
        from: new Date(2026, 3, 10),
      }}
    />
  ),
}
