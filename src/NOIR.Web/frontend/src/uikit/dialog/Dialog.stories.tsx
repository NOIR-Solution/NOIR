import type { Meta, StoryObj } from 'storybook'
import {
  Dialog,
  DialogTrigger,
  DialogContent,
  DialogHeader,
  DialogFooter,
  DialogTitle,
  DialogDescription,
  DialogClose,
} from './Dialog'

const meta = {
  title: 'UIKit/Dialog',
  component: Dialog,
  tags: ['autodocs'],
} satisfies Meta<typeof Dialog>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => (
    <Dialog>
      <DialogTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 cursor-pointer">
          Open Dialog
        </button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Dialog Title</DialogTitle>
          <DialogDescription>
            This is a dialog description that provides additional context about
            the dialog content.
          </DialogDescription>
        </DialogHeader>
        <div className="py-4">
          <p className="text-sm text-muted-foreground">
            Dialog body content goes here.
          </p>
        </div>
        <DialogFooter>
          <DialogClose asChild>
            <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 px-4 py-2 cursor-pointer">
              Cancel
            </button>
          </DialogClose>
          <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 cursor-pointer">
            Confirm
          </button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  ),
}

export const WithForm: Story = {
  render: () => (
    <Dialog>
      <DialogTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 cursor-pointer">
          Edit Profile
        </button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>Edit Profile</DialogTitle>
          <DialogDescription>
            Make changes to your profile here. Click save when you are done.
          </DialogDescription>
        </DialogHeader>
        <div className="grid gap-4 py-4">
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
            <label htmlFor="email" className="text-right text-sm font-medium">
              Email
            </label>
            <input
              id="email"
              defaultValue="john@example.com"
              className="col-span-3 flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
            />
          </div>
        </div>
        <DialogFooter>
          <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 cursor-pointer">
            Save changes
          </button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  ),
}

export const LoadingContent: Story = {
  render: () => (
    <Dialog>
      <DialogTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 cursor-pointer">
          Open Loading Dialog
        </button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Loading Data</DialogTitle>
          <DialogDescription>
            Please wait while we fetch your data...
          </DialogDescription>
        </DialogHeader>
        <div className="space-y-4 py-4">
          <div className="space-y-2">
            <div className="h-4 w-24 animate-pulse rounded bg-muted" />
            <div className="h-10 w-full animate-pulse rounded-md bg-muted" />
          </div>
          <div className="space-y-2">
            <div className="h-4 w-32 animate-pulse rounded bg-muted" />
            <div className="h-10 w-full animate-pulse rounded-md bg-muted" />
          </div>
          <div className="space-y-2">
            <div className="h-4 w-20 animate-pulse rounded bg-muted" />
            <div className="h-20 w-full animate-pulse rounded-md bg-muted" />
          </div>
        </div>
        <DialogFooter>
          <button
            disabled
            className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 opacity-50"
          >
            Save
          </button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  ),
}

export const ScrollableContent: Story = {
  render: () => (
    <Dialog>
      <DialogTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 cursor-pointer">
          Open Scrollable Dialog
        </button>
      </DialogTrigger>
      <DialogContent className="max-h-[80vh] flex flex-col">
        <DialogHeader>
          <DialogTitle>Terms of Service</DialogTitle>
          <DialogDescription>
            Please read the following terms carefully.
          </DialogDescription>
        </DialogHeader>
        <div className="flex-1 overflow-y-auto py-4 space-y-4">
          {Array.from({ length: 10 }, (_, i) => (
            <div key={i}>
              <h4 className="text-sm font-semibold mb-1">Section {i + 1}</h4>
              <p className="text-sm text-muted-foreground">
                Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do
                eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim
                ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut
                aliquip ex ea commodo consequat. Duis aute irure dolor in
                reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla
                pariatur.
              </p>
            </div>
          ))}
        </div>
        <DialogFooter>
          <DialogClose asChild>
            <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 px-4 py-2 cursor-pointer">
              Decline
            </button>
          </DialogClose>
          <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 cursor-pointer">
            Accept
          </button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  ),
}

export const CustomWidth: Story = {
  render: () => (
    <Dialog>
      <DialogTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 cursor-pointer">
          Open Wide Dialog
        </button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-[700px]">
        <DialogHeader>
          <DialogTitle>Data Preview</DialogTitle>
          <DialogDescription>
            This dialog uses a wider layout for content that needs more space.
          </DialogDescription>
        </DialogHeader>
        <div className="py-4">
          <div className="rounded-md border">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/50">
                  <th className="p-2 text-left font-medium">Name</th>
                  <th className="p-2 text-left font-medium">Email</th>
                  <th className="p-2 text-left font-medium">Role</th>
                  <th className="p-2 text-left font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {[
                  { name: 'Alice Johnson', email: 'alice@example.com', role: 'Admin', status: 'Active' },
                  { name: 'Bob Smith', email: 'bob@example.com', role: 'Editor', status: 'Active' },
                  { name: 'Charlie Brown', email: 'charlie@example.com', role: 'Viewer', status: 'Inactive' },
                ].map((row) => (
                  <tr key={row.email} className="border-b last:border-0">
                    <td className="p-2">{row.name}</td>
                    <td className="p-2 text-muted-foreground">{row.email}</td>
                    <td className="p-2">{row.role}</td>
                    <td className="p-2">
                      <span
                        className={`inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ${
                          row.status === 'Active'
                            ? 'bg-green-100 text-green-700'
                            : 'bg-gray-100 text-gray-700'
                        }`}
                      >
                        {row.status}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
        <DialogFooter>
          <DialogClose asChild>
            <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 px-4 py-2 cursor-pointer">
              Close
            </button>
          </DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  ),
}

export const NestedDialog: Story = {
  render: () => (
    <Dialog>
      <DialogTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-primary text-primary-foreground h-10 px-4 py-2 cursor-pointer">
          Open Parent Dialog
        </button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Parent Dialog</DialogTitle>
          <DialogDescription>
            This dialog can open another dialog on top of it.
          </DialogDescription>
        </DialogHeader>
        <div className="py-4">
          <p className="text-sm text-muted-foreground mb-4">
            Click the button below to open a nested confirmation dialog.
          </p>
          <Dialog>
            <DialogTrigger asChild>
              <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-destructive text-destructive-foreground h-10 px-4 py-2 cursor-pointer">
                Delete Item
              </button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Confirm Deletion</DialogTitle>
                <DialogDescription>
                  Are you sure you want to delete this item? This action cannot be
                  undone.
                </DialogDescription>
              </DialogHeader>
              <DialogFooter>
                <DialogClose asChild>
                  <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 px-4 py-2 cursor-pointer">
                    Cancel
                  </button>
                </DialogClose>
                <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-destructive text-destructive-foreground h-10 px-4 py-2 cursor-pointer">
                  Delete
                </button>
              </DialogFooter>
            </DialogContent>
          </Dialog>
        </div>
        <DialogFooter>
          <DialogClose asChild>
            <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 px-4 py-2 cursor-pointer">
              Close
            </button>
          </DialogClose>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  ),
}

export const DestructiveAction: Story = {
  render: () => (
    <Dialog>
      <DialogTrigger asChild>
        <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-destructive text-destructive-foreground h-10 px-4 py-2 cursor-pointer">
          Delete Account
        </button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Are you sure?</DialogTitle>
          <DialogDescription>
            This action cannot be undone. This will permanently delete your
            account and remove your data from our servers.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <DialogClose asChild>
            <button className="inline-flex items-center justify-center rounded-md text-sm font-medium border border-input bg-background h-10 px-4 py-2 cursor-pointer">
              Cancel
            </button>
          </DialogClose>
          <button className="inline-flex items-center justify-center rounded-md text-sm font-medium bg-destructive text-destructive-foreground h-10 px-4 py-2 cursor-pointer">
            Delete
          </button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  ),
}
