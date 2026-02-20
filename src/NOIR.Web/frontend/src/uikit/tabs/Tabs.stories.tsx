import type { Meta, StoryObj } from 'storybook'
import { User, CreditCard, Bell, Shield } from 'lucide-react'
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

export const ManyTabs: Story = {
  render: () => (
    <Tabs defaultValue="general" className="w-[600px]">
      <TabsList>
        <TabsTrigger value="general">General</TabsTrigger>
        <TabsTrigger value="security">Security</TabsTrigger>
        <TabsTrigger value="billing">Billing</TabsTrigger>
        <TabsTrigger value="notifications">Notifications</TabsTrigger>
        <TabsTrigger value="integrations">Integrations</TabsTrigger>
        <TabsTrigger value="advanced">Advanced</TabsTrigger>
      </TabsList>
      <TabsContent value="general">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">General account settings and preferences.</p>
        </div>
      </TabsContent>
      <TabsContent value="security">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">Password, two-factor auth, and session management.</p>
        </div>
      </TabsContent>
      <TabsContent value="billing">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">Subscription plans and payment methods.</p>
        </div>
      </TabsContent>
      <TabsContent value="notifications">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">Email and push notification preferences.</p>
        </div>
      </TabsContent>
      <TabsContent value="integrations">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">Connected apps and third-party services.</p>
        </div>
      </TabsContent>
      <TabsContent value="advanced">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">Advanced configuration and developer options.</p>
        </div>
      </TabsContent>
    </Tabs>
  ),
}

export const WithIcons: Story = {
  render: () => (
    <Tabs defaultValue="profile" className="w-[500px]">
      <TabsList>
        <TabsTrigger value="profile" className="gap-1.5">
          <User className="h-4 w-4" />
          Profile
        </TabsTrigger>
        <TabsTrigger value="billing" className="gap-1.5">
          <CreditCard className="h-4 w-4" />
          Billing
        </TabsTrigger>
        <TabsTrigger value="notifications" className="gap-1.5">
          <Bell className="h-4 w-4" />
          Notifications
        </TabsTrigger>
        <TabsTrigger value="security" className="gap-1.5">
          <Shield className="h-4 w-4" />
          Security
        </TabsTrigger>
      </TabsList>
      <TabsContent value="profile">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">Manage your profile information and avatar.</p>
        </div>
      </TabsContent>
      <TabsContent value="billing">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">View invoices and manage payment methods.</p>
        </div>
      </TabsContent>
      <TabsContent value="notifications">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">Configure notification channels and frequency.</p>
        </div>
      </TabsContent>
      <TabsContent value="security">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">Two-factor authentication and login history.</p>
        </div>
      </TabsContent>
    </Tabs>
  ),
}

export const FullWidth: Story = {
  render: () => (
    <Tabs defaultValue="all" className="w-full">
      <TabsList className="w-full">
        <TabsTrigger value="all" className="flex-1">All</TabsTrigger>
        <TabsTrigger value="active" className="flex-1">Active</TabsTrigger>
        <TabsTrigger value="completed" className="flex-1">Completed</TabsTrigger>
        <TabsTrigger value="archived" className="flex-1">Archived</TabsTrigger>
      </TabsList>
      <TabsContent value="all">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">Showing all items regardless of status.</p>
        </div>
      </TabsContent>
      <TabsContent value="active">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">Only active items are displayed.</p>
        </div>
      </TabsContent>
      <TabsContent value="completed">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">Items that have been completed.</p>
        </div>
      </TabsContent>
      <TabsContent value="archived">
        <div className="p-4 border rounded-md mt-2">
          <p className="text-sm text-muted-foreground">Archived items stored for reference.</p>
        </div>
      </TabsContent>
    </Tabs>
  ),
}
