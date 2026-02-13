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
