import type { Meta, StoryObj } from 'storybook'
import { Tabs, TabsList, TabsTrigger, TabsContent } from './Tabs'

const meta = {
  title: 'UIKit/Tabs',
  component: Tabs,
  tags: ['autodocs'],
} satisfies Meta<typeof Tabs>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => (
    <Tabs defaultValue="account" className="w-[400px]">
      <TabsList>
        <TabsTrigger value="account">Account</TabsTrigger>
        <TabsTrigger value="password">Password</TabsTrigger>
      </TabsList>
      <TabsContent value="account">
        <div className="space-y-2 p-4 border rounded-md mt-2">
          <h3 className="text-lg font-medium">Account</h3>
          <p className="text-sm text-muted-foreground">
            Make changes to your account here. Click save when you are done.
          </p>
          <div className="grid gap-2 pt-2">
            <label htmlFor="tab-name" className="text-sm font-medium">Name</label>
            <input
              id="tab-name"
              defaultValue="John Doe"
              className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
            />
          </div>
        </div>
      </TabsContent>
      <TabsContent value="password">
        <div className="space-y-2 p-4 border rounded-md mt-2">
          <h3 className="text-lg font-medium">Password</h3>
          <p className="text-sm text-muted-foreground">
            Change your password here. After saving, you will be logged out.
          </p>
          <div className="grid gap-2 pt-2">
            <label htmlFor="tab-current" className="text-sm font-medium">
              Current password
            </label>
            <input
              id="tab-current"
              type="password"
              className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm"
            />
          </div>
        </div>
      </TabsContent>
    </Tabs>
  ),
}

export const ThreeTabs: Story = {
  render: () => (
    <Tabs defaultValue="overview" className="w-[500px]">
      <TabsList>
        <TabsTrigger value="overview">Overview</TabsTrigger>
        <TabsTrigger value="analytics">Analytics</TabsTrigger>
        <TabsTrigger value="reports">Reports</TabsTrigger>
      </TabsList>
      <TabsContent value="overview">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">
            Overview of your project metrics and key performance indicators.
          </p>
        </div>
      </TabsContent>
      <TabsContent value="analytics">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">
            Detailed analytics showing trends and patterns over time.
          </p>
        </div>
      </TabsContent>
      <TabsContent value="reports">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">
            Generated reports ready for download and sharing.
          </p>
        </div>
      </TabsContent>
    </Tabs>
  ),
}

export const DisabledTab: Story = {
  render: () => (
    <Tabs defaultValue="active" className="w-[400px]">
      <TabsList>
        <TabsTrigger value="active">Active</TabsTrigger>
        <TabsTrigger value="draft">Draft</TabsTrigger>
        <TabsTrigger value="archived" disabled>
          Archived
        </TabsTrigger>
      </TabsList>
      <TabsContent value="active">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">
            Active items are shown here.
          </p>
        </div>
      </TabsContent>
      <TabsContent value="draft">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">
            Draft items waiting for review.
          </p>
        </div>
      </TabsContent>
    </Tabs>
  ),
}
