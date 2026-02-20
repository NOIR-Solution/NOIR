import type { Meta, StoryObj } from 'storybook'
import {
  Drawer,
  DrawerTrigger,
  DrawerContent,
  DrawerHeader,
  DrawerFooter,
  DrawerTitle,
  DrawerDescription,
  DrawerClose,
} from './Drawer'

const meta = {
  title: 'UIKit/Drawer',
  component: Drawer,
  tags: ['autodocs'],
} satisfies Meta<typeof Drawer>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => (
    <Drawer>
      <DrawerTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 cursor-pointer">
          Open Drawer
        </button>
      </DrawerTrigger>
      <DrawerContent>
        <DrawerHeader>
          <DrawerTitle>Drawer Title</DrawerTitle>
          <DrawerDescription>
            This is a drawer description providing context.
          </DrawerDescription>
        </DrawerHeader>
        <div className="p-4">
          <p className="text-sm text-muted-foreground">
            Drawer body content goes here. Drawers slide up from the bottom of
            the screen and are useful for mobile interactions.
          </p>
        </div>
        <DrawerFooter>
          <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 w-full py-2 cursor-pointer">
            Submit
          </button>
          <DrawerClose asChild>
            <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 w-full py-2 cursor-pointer">
              Cancel
            </button>
          </DrawerClose>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  ),
}

export const WithForm: Story = {
  render: () => (
    <Drawer>
      <DrawerTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 cursor-pointer">
          Set Goal
        </button>
      </DrawerTrigger>
      <DrawerContent>
        <DrawerHeader>
          <DrawerTitle>Move Goal</DrawerTitle>
          <DrawerDescription>Set your daily activity goal.</DrawerDescription>
        </DrawerHeader>
        <div className="p-4 pb-0">
          <div className="flex items-center justify-center space-x-2">
            <button className="inline-flex items-center justify-center rounded-full border h-8 w-8 text-sm cursor-pointer">
              -
            </button>
            <div className="flex-1 text-center">
              <div className="text-7xl font-bold tracking-tighter">350</div>
              <div className="text-[0.70rem] uppercase text-muted-foreground">
                Calories/day
              </div>
            </div>
            <button className="inline-flex items-center justify-center rounded-full border h-8 w-8 text-sm cursor-pointer">
              +
            </button>
          </div>
        </div>
        <DrawerFooter>
          <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 w-full py-2 cursor-pointer">
            Submit
          </button>
          <DrawerClose asChild>
            <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 w-full py-2 cursor-pointer">
              Cancel
            </button>
          </DrawerClose>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  ),
}

export const NoScaleBackground: Story = {
  render: () => (
    <Drawer shouldScaleBackground={false}>
      <DrawerTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 px-4 py-2 cursor-pointer">
          Open (no scale)
        </button>
      </DrawerTrigger>
      <DrawerContent>
        <DrawerHeader>
          <DrawerTitle>No Scale Background</DrawerTitle>
          <DrawerDescription>
            This drawer does not scale the background when opened.
          </DrawerDescription>
        </DrawerHeader>
        <div className="p-4">
          <p className="text-sm text-muted-foreground">
            The background remains at full scale behind the overlay.
          </p>
        </div>
        <DrawerFooter>
          <DrawerClose asChild>
            <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 w-full py-2 cursor-pointer">
              Close
            </button>
          </DrawerClose>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  ),
}

export const LargeContent: Story = {
  render: () => (
    <Drawer>
      <DrawerTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 cursor-pointer">
          View Notifications
        </button>
      </DrawerTrigger>
      <DrawerContent>
        <DrawerHeader>
          <DrawerTitle>Notifications</DrawerTitle>
          <DrawerDescription>You have 15 unread notifications.</DrawerDescription>
        </DrawerHeader>
        <div className="max-h-[60vh] overflow-y-auto px-4">
          {Array.from({ length: 15 }, (_, i) => (
            <div
              key={i}
              className="flex items-start gap-3 py-3 border-b last:border-b-0"
            >
              <div className="h-8 w-8 rounded-full bg-muted shrink-0" />
              <div className="space-y-1">
                <p className="text-sm font-medium">Notification {i + 1}</p>
                <p className="text-xs text-muted-foreground">
                  This is a notification message with details about an event.
                </p>
                <p className="text-xs text-muted-foreground">{i + 1}h ago</p>
              </div>
            </div>
          ))}
        </div>
        <DrawerFooter>
          <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 w-full py-2 cursor-pointer">
            Mark All as Read
          </button>
          <DrawerClose asChild>
            <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 w-full py-2 cursor-pointer">
              Close
            </button>
          </DrawerClose>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  ),
}

export const NonDismissible: Story = {
  render: () => (
    <Drawer dismissible={false}>
      <DrawerTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 cursor-pointer">
          Important Action
        </button>
      </DrawerTrigger>
      <DrawerContent>
        <DrawerHeader>
          <DrawerTitle>Confirm Your Identity</DrawerTitle>
          <DrawerDescription>
            This drawer cannot be dismissed by swiping. You must complete the action.
          </DrawerDescription>
        </DrawerHeader>
        <div className="p-4">
          <div className="space-y-3">
            <div>
              <label htmlFor="drawer-code" className="text-sm font-medium">
                Verification Code
              </label>
              <input
                id="drawer-code"
                placeholder="Enter 6-digit code"
                className="mt-1 flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
              />
            </div>
          </div>
        </div>
        <DrawerFooter>
          <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 w-full py-2 cursor-pointer">
            Verify
          </button>
          <DrawerClose asChild>
            <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 w-full py-2 cursor-pointer">
              Cancel
            </button>
          </DrawerClose>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  ),
}

export const NestedContent: Story = {
  render: () => (
    <Drawer>
      <DrawerTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 cursor-pointer">
          Filter Options
        </button>
      </DrawerTrigger>
      <DrawerContent>
        <DrawerHeader>
          <DrawerTitle>Filters</DrawerTitle>
          <DrawerDescription>Refine your search results.</DrawerDescription>
        </DrawerHeader>
        <div className="px-4 space-y-4">
          <div>
            <h4 className="text-sm font-medium mb-2">Category</h4>
            <div className="flex flex-wrap gap-2">
              {['Electronics', 'Clothing', 'Books', 'Home', 'Sports'].map((cat) => (
                <button
                  key={cat}
                  className="inline-flex items-center rounded-full border px-2.5 py-0.5 text-xs font-semibold transition-colors hover:bg-accent cursor-pointer"
                >
                  {cat}
                </button>
              ))}
            </div>
          </div>
          <div>
            <h4 className="text-sm font-medium mb-2">Price Range</h4>
            <div className="flex gap-2">
              <input
                placeholder="Min"
                className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm"
              />
              <input
                placeholder="Max"
                className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm"
              />
            </div>
          </div>
          <div>
            <h4 className="text-sm font-medium mb-2">Rating</h4>
            <div className="flex gap-2">
              {[5, 4, 3, 2, 1].map((stars) => (
                <button
                  key={stars}
                  className="inline-flex items-center rounded-md border px-2 py-1 text-xs hover:bg-accent cursor-pointer"
                >
                  {stars}+
                </button>
              ))}
            </div>
          </div>
        </div>
        <DrawerFooter>
          <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 w-full py-2 cursor-pointer">
            Apply Filters
          </button>
          <DrawerClose asChild>
            <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 w-full py-2 cursor-pointer">
              Reset
            </button>
          </DrawerClose>
        </DrawerFooter>
      </DrawerContent>
    </Drawer>
  ),
}
