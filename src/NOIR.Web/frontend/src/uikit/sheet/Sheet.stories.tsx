import type { Meta, StoryObj } from 'storybook'
import {
  Sheet,
  SheetTrigger,
  SheetContent,
  SheetHeader,
  SheetFooter,
  SheetTitle,
  SheetDescription,
  SheetClose,
} from './Sheet'

const meta = {
  title: 'UIKit/Sheet',
  component: Sheet,
  tags: ['autodocs'],
} satisfies Meta<typeof Sheet>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => (
    <Sheet>
      <SheetTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 cursor-pointer">
          Open Sheet
        </button>
      </SheetTrigger>
      <SheetContent>
        <SheetHeader>
          <SheetTitle>Sheet Title</SheetTitle>
          <SheetDescription>
            This is a sheet description. Make changes and click save when done.
          </SheetDescription>
        </SheetHeader>
        <div className="grid gap-4 py-4 px-4">
          <div className="grid grid-cols-4 items-center gap-4">
            <label htmlFor="name" className="text-right text-sm font-medium">
              Name
            </label>
            <input
              id="name"
              defaultValue="John Doe"
              className="col-span-3 flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
            />
          </div>
          <div className="grid grid-cols-4 items-center gap-4">
            <label htmlFor="username" className="text-right text-sm font-medium">
              Username
            </label>
            <input
              id="username"
              defaultValue="@johndoe"
              className="col-span-3 flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
            />
          </div>
        </div>
        <SheetFooter>
          <SheetClose asChild>
            <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 cursor-pointer">
              Save changes
            </button>
          </SheetClose>
        </SheetFooter>
      </SheetContent>
    </Sheet>
  ),
}

export const LeftSide: Story = {
  render: () => (
    <Sheet>
      <SheetTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 px-4 py-2 cursor-pointer">
          Open Left
        </button>
      </SheetTrigger>
      <SheetContent side="left">
        <SheetHeader>
          <SheetTitle>Navigation</SheetTitle>
          <SheetDescription>Browse through sections.</SheetDescription>
        </SheetHeader>
        <nav className="grid gap-2 py-4 px-4">
          {['Dashboard', 'Products', 'Orders', 'Customers', 'Settings'].map(
            (item) => (
              <div
                key={item}
                className="rounded-md px-3 py-2 text-sm hover:bg-accent cursor-pointer"
              >
                {item}
              </div>
            )
          )}
        </nav>
      </SheetContent>
    </Sheet>
  ),
}

export const TopSide: Story = {
  render: () => (
    <Sheet>
      <SheetTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 px-4 py-2 cursor-pointer">
          Open Top
        </button>
      </SheetTrigger>
      <SheetContent side="top">
        <SheetHeader>
          <SheetTitle>Notification Bar</SheetTitle>
          <SheetDescription>
            You have 3 unread notifications.
          </SheetDescription>
        </SheetHeader>
        <div className="px-4 pb-4">
          <p className="text-sm text-muted-foreground">
            This sheet slides in from the top of the screen.
          </p>
        </div>
      </SheetContent>
    </Sheet>
  ),
}

export const BottomSide: Story = {
  render: () => (
    <Sheet>
      <SheetTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 px-4 py-2 cursor-pointer">
          Open Bottom
        </button>
      </SheetTrigger>
      <SheetContent side="bottom">
        <SheetHeader>
          <SheetTitle>Quick Actions</SheetTitle>
          <SheetDescription>
            Select an action to perform.
          </SheetDescription>
        </SheetHeader>
        <div className="grid grid-cols-3 gap-4 px-4 pb-4">
          {['Copy', 'Move', 'Delete', 'Share', 'Download', 'Archive'].map(
            (action) => (
              <button
                key={action}
                className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 px-4 py-2 cursor-pointer"
              >
                {action}
              </button>
            )
          )}
        </div>
      </SheetContent>
    </Sheet>
  ),
}
