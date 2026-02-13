import type { Meta, StoryObj } from 'storybook'
import { Popover, PopoverTrigger, PopoverContent } from './Popover'

const meta = {
  title: 'UIKit/Popover',
  component: Popover,
  tags: ['autodocs'],
} satisfies Meta<typeof Popover>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => (
    <Popover>
      <PopoverTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 px-4 py-2 cursor-pointer">
          Open Popover
        </button>
      </PopoverTrigger>
      <PopoverContent>
        <div className="grid gap-4">
          <div className="space-y-2">
            <h4 className="font-medium leading-none">Dimensions</h4>
            <p className="text-sm text-muted-foreground">
              Set the dimensions for the layer.
            </p>
          </div>
          <div className="grid gap-2">
            <div className="grid grid-cols-3 items-center gap-4">
              <label htmlFor="width" className="text-sm">Width</label>
              <input
                id="width"
                defaultValue="100%"
                className="col-span-2 h-8 rounded-md border border-input bg-background px-3 text-sm"
              />
            </div>
            <div className="grid grid-cols-3 items-center gap-4">
              <label htmlFor="height" className="text-sm">Height</label>
              <input
                id="height"
                defaultValue="25px"
                className="col-span-2 h-8 rounded-md border border-input bg-background px-3 text-sm"
              />
            </div>
          </div>
        </div>
      </PopoverContent>
    </Popover>
  ),
}

export const AlignStart: Story = {
  render: () => (
    <Popover>
      <PopoverTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 px-4 py-2 cursor-pointer">
          Align Start
        </button>
      </PopoverTrigger>
      <PopoverContent align="start">
        <p className="text-sm">This popover is aligned to the start.</p>
      </PopoverContent>
    </Popover>
  ),
}

export const AlignEnd: Story = {
  render: () => (
    <Popover>
      <PopoverTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 px-4 py-2 cursor-pointer">
          Align End
        </button>
      </PopoverTrigger>
      <PopoverContent align="end">
        <p className="text-sm">This popover is aligned to the end.</p>
      </PopoverContent>
    </Popover>
  ),
}

export const WithCustomWidth: Story = {
  render: () => (
    <Popover>
      <PopoverTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 px-4 py-2 cursor-pointer">
          Wide Popover
        </button>
      </PopoverTrigger>
      <PopoverContent className="w-96">
        <div className="space-y-2">
          <h4 className="font-medium leading-none">Notifications</h4>
          <p className="text-sm text-muted-foreground">
            You have 3 unread messages. Configure your notification preferences
            to manage how and when you receive alerts.
          </p>
        </div>
      </PopoverContent>
    </Popover>
  ),
}
