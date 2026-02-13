import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import { Calendar } from './Calendar'

const meta = {
  title: 'UIKit/Calendar',
  component: Calendar,
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
  },
} satisfies Meta<typeof Calendar>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {},
}

export const WithSelectedDate: Story = {
  render: () => {
    const [date, setDate] = useState<Date | undefined>(new Date())
    return (
      <Calendar
        mode="single"
        selected={date}
        onSelect={setDate}
      />
    )
  },
}

export const WithDisabledDates: Story = {
  render: () => {
    const [date, setDate] = useState<Date | undefined>(new Date())
    return (
      <Calendar
        mode="single"
        selected={date}
        onSelect={setDate}
        disabled={(d) => d < new Date()}
      />
    )
  },
}

export const RangeSelection: Story = {
  render: () => {
    const [range, setRange] = useState<{ from: Date; to?: Date } | undefined>({
      from: new Date(2026, 1, 10),
      to: new Date(2026, 1, 20),
    })
    return (
      <Calendar
        mode="range"
        selected={range}
        onSelect={setRange}
        numberOfMonths={2}
      />
    )
  },
}

export const HideOutsideDays: Story = {
  args: {
    showOutsideDays: false,
  },
}
