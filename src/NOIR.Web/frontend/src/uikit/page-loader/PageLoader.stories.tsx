import type { Meta, StoryObj } from 'storybook'
import { PageLoader, PageSkeleton } from './PageLoader'

const meta = {
  title: 'UIKit/PageLoader',
  component: PageLoader,
  tags: ['autodocs'],
  parameters: {
    layout: 'padded',
  },
  argTypes: {
    fullScreen: { control: 'boolean' },
  },
} satisfies Meta<typeof PageLoader>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    text: 'Loading...',
  },
}

export const WithoutText: Story = {
  args: {},
}

export const CustomText: Story = {
  args: {
    text: 'Fetching your data, please wait...',
  },
}

export const Skeleton: Story = {
  render: () => (
    <div style={{ maxWidth: 900 }}>
      <PageSkeleton />
    </div>
  ),
}
