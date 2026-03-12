import type { Meta, StoryObj } from 'storybook'
import { Progress } from './Progress'

const meta = {
  title: 'UIKit/Progress',
  component: Progress,
  tags: ['autodocs'],
  argTypes: {
    value: {
      control: { type: 'range', min: 0, max: 100, step: 1 },
    },
  },
} satisfies Meta<typeof Progress>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    value: 50,
    'aria-label': 'Progress',
  },
}

export const Empty: Story = {
  args: {
    value: 0,
    'aria-label': 'Progress',
  },
}

export const Quarter: Story = {
  args: {
    value: 25,
    'aria-label': 'Progress',
  },
}

export const Half: Story = {
  args: {
    value: 50,
    'aria-label': 'Progress',
  },
}

export const ThreeQuarters: Story = {
  args: {
    value: 75,
    'aria-label': 'Progress',
  },
}

export const Full: Story = {
  args: {
    value: 100,
    'aria-label': 'Progress',
  },
}

export const WithLabel: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', maxWidth: 400 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '14px' }}>
        <span>Upload Progress</span>
        <span>66%</span>
      </div>
      <Progress value={66} aria-label="Upload progress" />
    </div>
  ),
}

export const AllStages: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', maxWidth: 400 }}>
      <div>
        <div style={{ fontSize: '12px', marginBottom: '4px' }}>0%</div>
        <Progress value={0} aria-label="Progress 0%" />
      </div>
      <div>
        <div style={{ fontSize: '12px', marginBottom: '4px' }}>25%</div>
        <Progress value={25} aria-label="Progress 25%" />
      </div>
      <div>
        <div style={{ fontSize: '12px', marginBottom: '4px' }}>50%</div>
        <Progress value={50} aria-label="Progress 50%" />
      </div>
      <div>
        <div style={{ fontSize: '12px', marginBottom: '4px' }}>75%</div>
        <Progress value={75} aria-label="Progress 75%" />
      </div>
      <div>
        <div style={{ fontSize: '12px', marginBottom: '4px' }}>100%</div>
        <Progress value={100} aria-label="Progress 100%" />
      </div>
    </div>
  ),
}

export const Indeterminate: Story = {
  args: {
    value: undefined,
    'aria-label': 'Loading',
  },
}

export const CustomWidth: Story = {
  render: () => (
    <div style={{ maxWidth: 200 }}>
      <Progress value={60} aria-label="Progress" />
    </div>
  ),
}
