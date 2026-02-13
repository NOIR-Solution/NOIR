import type { Meta, StoryObj } from 'storybook'
import { TooltipProvider } from '../tooltip/Tooltip'
import { TippyTooltip, RichTooltip } from './TippyTooltip'
import { Button } from '../button/Button'

const meta = {
  title: 'UIKit/TippyTooltip',
  component: TippyTooltip,
  tags: ['autodocs'],
  decorators: [
    (Story) => (
      <TooltipProvider>
        <div style={{ padding: 80, display: 'flex', justifyContent: 'center' }}>
          <Story />
        </div>
      </TooltipProvider>
    ),
  ],
} satisfies Meta<typeof TippyTooltip>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    content: 'This is a tooltip',
    children: <Button variant="outline">Hover me</Button>,
  },
}

export const PlacementTop: Story = {
  args: {
    content: 'Tooltip on top',
    placement: 'top',
    children: <Button variant="outline">Top</Button>,
  },
}

export const PlacementBottom: Story = {
  args: {
    content: 'Tooltip on bottom',
    placement: 'bottom',
    children: <Button variant="outline">Bottom</Button>,
  },
}

export const PlacementLeft: Story = {
  args: {
    content: 'Tooltip on left',
    placement: 'left',
    children: <Button variant="outline">Left</Button>,
  },
}

export const PlacementRight: Story = {
  args: {
    content: 'Tooltip on right',
    placement: 'right',
    children: <Button variant="outline">Right</Button>,
  },
}

export const LongContent: Story = {
  args: {
    content: 'This is a longer tooltip message that provides more detail about the element.',
    placement: 'top',
    children: <Button variant="outline">Long tooltip</Button>,
  },
}

export const CustomDelay: Story = {
  args: {
    content: 'Appears after 500ms',
    delay: 500,
    children: <Button variant="outline">Delayed tooltip</Button>,
  },
}

export const RichDefault: Story = {
  render: () => (
    <RichTooltip
      title="Search Tips"
      items={[
        'Use quotes for exact match',
        'Prefix with @ to search users',
        'Use - to exclude terms',
      ]}
      placement="bottom"
    >
      <Button variant="outline">Rich Tooltip</Button>
    </RichTooltip>
  ),
}

export const RichTitleOnly: Story = {
  render: () => (
    <RichTooltip title="Quick Info" placement="top">
      <Button variant="outline">Title only</Button>
    </RichTooltip>
  ),
}

export const RichItemsOnly: Story = {
  render: () => (
    <RichTooltip
      items={['First item', 'Second item', 'Third item']}
      placement="bottom"
    >
      <Button variant="outline">Items only</Button>
    </RichTooltip>
  ),
}

export const RichCustomContent: Story = {
  render: () => (
    <RichTooltip
      content={
        <div style={{ padding: 4 }}>
          <strong>Custom content</strong>
          <p style={{ margin: '4px 0 0', fontSize: 13, color: '#6b7280' }}>
            This tooltip uses fully custom content instead of title/items.
          </p>
        </div>
      }
      placement="bottom"
    >
      <Button variant="outline">Custom content</Button>
    </RichTooltip>
  ),
}

export const AllPlacements: Story = {
  render: () => (
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: 16 }}>
      {(['top', 'right', 'bottom', 'left'] as const).map((placement) => (
        <TippyTooltip key={placement} content={`Tooltip: ${placement}`} placement={placement}>
          <Button variant="outline" className="w-full">
            {placement}
          </Button>
        </TippyTooltip>
      ))}
    </div>
  ),
}
