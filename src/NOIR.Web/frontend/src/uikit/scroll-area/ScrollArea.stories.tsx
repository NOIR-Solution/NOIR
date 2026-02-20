import type { Meta, StoryObj } from 'storybook'
import { ScrollArea, ScrollBar } from './ScrollArea'

const meta = {
  title: 'UIKit/ScrollArea',
  component: ScrollArea,
  tags: ['autodocs'],
} satisfies Meta<typeof ScrollArea>

export default meta
type Story = StoryObj<typeof meta>

const tags = Array.from({ length: 50 }, (_, i) => `Item ${i + 1}`)

export const Default: Story = {
  render: () => (
    <ScrollArea className="h-72 w-48 rounded-md border">
      <div className="p-4">
        <h4 className="mb-4 text-sm font-medium leading-none">Tags</h4>
        {tags.map((tag) => (
          <div key={tag} className="text-sm py-2 border-b last:border-b-0">
            {tag}
          </div>
        ))}
      </div>
    </ScrollArea>
  ),
}

export const HorizontalScroll: Story = {
  render: () => (
    <ScrollArea className="w-96 whitespace-nowrap rounded-md border">
      <div className="flex w-max space-x-4 p-4">
        {Array.from({ length: 20 }, (_, i) => (
          <div
            key={i}
            className="w-[150px] shrink-0 rounded-md border p-4"
          >
            <div className="text-sm font-medium">Card {i + 1}</div>
            <p className="text-xs text-muted-foreground mt-1">
              Description for card {i + 1}
            </p>
          </div>
        ))}
      </div>
      <ScrollBar orientation="horizontal" />
    </ScrollArea>
  ),
}

export const LongTextContent: Story = {
  render: () => (
    <ScrollArea className="h-[200px] w-[350px] rounded-md border p-4">
      <div className="text-sm">
        <h4 className="mb-4 font-medium leading-none">Lorem Ipsum</h4>
        <p className="mb-4">
          Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do
          eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim
          ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut
          aliquip ex ea commodo consequat.
        </p>
        <p className="mb-4">
          Duis aute irure dolor in reprehenderit in voluptate velit esse
          cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat
          cupidatat non proident, sunt in culpa qui officia deserunt mollit
          anim id est laborum.
        </p>
        <p className="mb-4">
          Sed ut perspiciatis unde omnis iste natus error sit voluptatem
          accusantium doloremque laudantium, totam rem aperiam, eaque ipsa
          quae ab illo inventore veritatis et quasi architecto beatae vitae
          dicta sunt explicabo.
        </p>
        <p>
          Nemo enim ipsam voluptatem quia voluptas sit aspernatur aut odit
          aut fugit, sed quia consequuntur magni dolores eos qui ratione
          voluptatem sequi nesciunt.
        </p>
      </div>
    </ScrollArea>
  ),
}

export const SmallContainer: Story = {
  render: () => (
    <ScrollArea className="h-32 w-32 rounded-md border">
      <div className="p-2">
        {Array.from({ length: 20 }, (_, i) => (
          <div
            key={i}
            className="flex items-center gap-2 py-1 text-xs border-b last:border-b-0"
          >
            <span className="font-mono text-muted-foreground">{String(i + 1).padStart(2, '0')}</span>
            <span>Row {i + 1}</span>
          </div>
        ))}
      </div>
    </ScrollArea>
  ),
}

export const WithStickyHeader: Story = {
  render: () => (
    <ScrollArea className="h-[250px] w-[300px] rounded-md border">
      <div className="sticky top-0 z-10 bg-background border-b px-4 py-2">
        <h4 className="text-sm font-medium">Team Members</h4>
      </div>
      <div className="p-4">
        {Array.from({ length: 25 }, (_, i) => (
          <div
            key={i}
            className="flex items-center gap-3 py-2 border-b last:border-b-0"
          >
            <div className="h-8 w-8 rounded-full bg-muted shrink-0" />
            <div>
              <p className="text-sm font-medium">User {i + 1}</p>
              <p className="text-xs text-muted-foreground">user{i + 1}@example.com</p>
            </div>
          </div>
        ))}
      </div>
    </ScrollArea>
  ),
}

export const BothDirections: Story = {
  render: () => (
    <ScrollArea className="h-[200px] w-[300px] rounded-md border">
      <div className="w-[600px] p-4">
        <table className="w-full border-collapse">
          <thead>
            <tr>
              {['ID', 'Name', 'Email', 'Role', 'Status', 'Created'].map((header) => (
                <th
                  key={header}
                  className="border-b px-4 py-2 text-left text-xs font-medium text-muted-foreground whitespace-nowrap"
                >
                  {header}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {Array.from({ length: 20 }, (_, i) => (
              <tr key={i}>
                <td className="border-b px-4 py-2 text-sm whitespace-nowrap">{i + 1}</td>
                <td className="border-b px-4 py-2 text-sm whitespace-nowrap">User {i + 1}</td>
                <td className="border-b px-4 py-2 text-sm whitespace-nowrap">user{i + 1}@mail.com</td>
                <td className="border-b px-4 py-2 text-sm whitespace-nowrap">Member</td>
                <td className="border-b px-4 py-2 text-sm whitespace-nowrap">Active</td>
                <td className="border-b px-4 py-2 text-sm whitespace-nowrap">2026-01-{String(i + 1).padStart(2, '0')}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <ScrollBar orientation="horizontal" />
    </ScrollArea>
  ),
}
