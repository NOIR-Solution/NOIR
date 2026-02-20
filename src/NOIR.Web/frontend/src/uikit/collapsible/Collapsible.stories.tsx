import type { Meta, StoryObj } from 'storybook'
import { useState } from 'react'
import { ChevronsUpDown } from 'lucide-react'
import { Collapsible, CollapsibleTrigger, CollapsibleContent } from './Collapsible'

const meta = {
  title: 'UIKit/Collapsible',
  component: Collapsible,
  tags: ['autodocs'],
} satisfies Meta<typeof Collapsible>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => (
    <Collapsible className="w-[350px] space-y-2">
      <div className="flex items-center justify-between space-x-4 px-4">
        <h4 className="text-sm font-semibold">Starred repositories</h4>
        <CollapsibleTrigger asChild>
          <button className="inline-flex items-center justify-center rounded-md text-sm font-medium ring-offset-background transition-colors hover:bg-accent hover:text-accent-foreground h-9 w-9 p-0 cursor-pointer">
            <ChevronsUpDown className="h-4 w-4" />
            <span className="sr-only">Toggle</span>
          </button>
        </CollapsibleTrigger>
      </div>
      <div className="rounded-md border px-4 py-3 font-mono text-sm">
        @radix-ui/primitives
      </div>
      <CollapsibleContent className="space-y-2">
        <div className="rounded-md border px-4 py-3 font-mono text-sm">
          @radix-ui/colors
        </div>
        <div className="rounded-md border px-4 py-3 font-mono text-sm">
          @radix-ui/react-collapsible
        </div>
      </CollapsibleContent>
    </Collapsible>
  ),
}

export const DefaultOpen: Story = {
  render: () => (
    <Collapsible defaultOpen className="w-[350px] space-y-2">
      <div className="flex items-center justify-between space-x-4 px-4">
        <h4 className="text-sm font-semibold">Settings</h4>
        <CollapsibleTrigger asChild>
          <button className="inline-flex items-center justify-center rounded-md text-sm font-medium ring-offset-background transition-colors hover:bg-accent hover:text-accent-foreground h-9 w-9 p-0 cursor-pointer">
            <ChevronsUpDown className="h-4 w-4" />
            <span className="sr-only">Toggle</span>
          </button>
        </CollapsibleTrigger>
      </div>
      <CollapsibleContent className="space-y-2">
        <div className="rounded-md border px-4 py-3 text-sm">
          Notification preferences
        </div>
        <div className="rounded-md border px-4 py-3 text-sm">
          Privacy settings
        </div>
        <div className="rounded-md border px-4 py-3 text-sm">
          Account details
        </div>
      </CollapsibleContent>
    </Collapsible>
  ),
}

export const Controlled: Story = {
  render: function ControlledCollapsible() {
    const [isOpen, setIsOpen] = useState(false)

    return (
      <Collapsible
        open={isOpen}
        onOpenChange={setIsOpen}
        className="w-[350px] space-y-2"
      >
        <div className="flex items-center justify-between space-x-4 px-4">
          <h4 className="text-sm font-semibold">
            {isOpen ? 'Expanded' : 'Collapsed'}
          </h4>
          <CollapsibleTrigger asChild>
            <button className="inline-flex items-center justify-center rounded-md text-sm font-medium ring-offset-background transition-colors hover:bg-accent hover:text-accent-foreground h-9 w-9 p-0 cursor-pointer">
              <ChevronsUpDown className="h-4 w-4" />
              <span className="sr-only">Toggle</span>
            </button>
          </CollapsibleTrigger>
        </div>
        <CollapsibleContent className="space-y-2">
          <div className="rounded-md border px-4 py-3 text-sm">
            This section is controlled externally.
          </div>
        </CollapsibleContent>
      </Collapsible>
    )
  },
}

export const Disabled: Story = {
  render: () => (
    <Collapsible disabled className="w-[350px] space-y-2">
      <div className="flex items-center justify-between space-x-4 px-4">
        <h4 className="text-sm font-semibold text-muted-foreground">
          Locked section
        </h4>
        <CollapsibleTrigger asChild>
          <button
            className="inline-flex items-center justify-center rounded-md text-sm font-medium ring-offset-background transition-colors h-9 w-9 p-0 opacity-50 cursor-not-allowed"
            disabled
          >
            <ChevronsUpDown className="h-4 w-4" />
            <span className="sr-only">Toggle</span>
          </button>
        </CollapsibleTrigger>
      </div>
      <div className="rounded-md border px-4 py-3 font-mono text-sm">
        This item is always visible
      </div>
      <CollapsibleContent className="space-y-2">
        <div className="rounded-md border px-4 py-3 font-mono text-sm">
          Hidden content (unreachable)
        </div>
      </CollapsibleContent>
    </Collapsible>
  ),
}

export const Nested: Story = {
  render: () => (
    <Collapsible defaultOpen className="w-[350px] space-y-2">
      <div className="flex items-center justify-between space-x-4 px-4">
        <h4 className="text-sm font-semibold">Project Files</h4>
        <CollapsibleTrigger asChild>
          <button className="inline-flex items-center justify-center rounded-md text-sm font-medium ring-offset-background transition-colors hover:bg-accent hover:text-accent-foreground h-9 w-9 p-0 cursor-pointer">
            <ChevronsUpDown className="h-4 w-4" />
            <span className="sr-only">Toggle</span>
          </button>
        </CollapsibleTrigger>
      </div>
      <CollapsibleContent className="space-y-2">
        <div className="rounded-md border px-4 py-3 font-mono text-sm">
          README.md
        </div>
        <Collapsible className="space-y-2 ml-4">
          <div className="flex items-center justify-between space-x-4 px-4">
            <h4 className="text-sm font-semibold">src/</h4>
            <CollapsibleTrigger asChild>
              <button className="inline-flex items-center justify-center rounded-md text-sm font-medium ring-offset-background transition-colors hover:bg-accent hover:text-accent-foreground h-9 w-9 p-0 cursor-pointer">
                <ChevronsUpDown className="h-4 w-4" />
                <span className="sr-only">Toggle</span>
              </button>
            </CollapsibleTrigger>
          </div>
          <CollapsibleContent className="space-y-2">
            <div className="rounded-md border px-4 py-3 font-mono text-sm">
              index.ts
            </div>
            <div className="rounded-md border px-4 py-3 font-mono text-sm">
              App.tsx
            </div>
            <Collapsible className="space-y-2 ml-4">
              <div className="flex items-center justify-between space-x-4 px-4">
                <h4 className="text-sm font-semibold">components/</h4>
                <CollapsibleTrigger asChild>
                  <button className="inline-flex items-center justify-center rounded-md text-sm font-medium ring-offset-background transition-colors hover:bg-accent hover:text-accent-foreground h-9 w-9 p-0 cursor-pointer">
                    <ChevronsUpDown className="h-4 w-4" />
                    <span className="sr-only">Toggle</span>
                  </button>
                </CollapsibleTrigger>
              </div>
              <CollapsibleContent className="space-y-2">
                <div className="rounded-md border px-4 py-3 font-mono text-sm">
                  Button.tsx
                </div>
                <div className="rounded-md border px-4 py-3 font-mono text-sm">
                  Card.tsx
                </div>
              </CollapsibleContent>
            </Collapsible>
          </CollapsibleContent>
        </Collapsible>
        <div className="rounded-md border px-4 py-3 font-mono text-sm">
          package.json
        </div>
      </CollapsibleContent>
    </Collapsible>
  ),
}
