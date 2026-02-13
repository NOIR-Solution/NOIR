import type { Meta, StoryObj } from 'storybook'
import { Checkbox } from './Checkbox'

const meta = {
  title: 'UIKit/Checkbox',
  component: Checkbox,
  tags: ['autodocs'],
  argTypes: {
    disabled: {
      control: 'boolean',
    },
    checked: {
      control: 'boolean',
    },
  },
} satisfies Meta<typeof Checkbox>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {},
}

export const Checked: Story = {
  args: {
    checked: true,
  },
}

export const Unchecked: Story = {
  args: {
    checked: false,
  },
}

export const Disabled: Story = {
  args: {
    disabled: true,
  },
}

export const DisabledChecked: Story = {
  args: {
    disabled: true,
    checked: true,
  },
}

export const WithLabel: Story = {
  render: () => (
    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
      <Checkbox id="terms" />
      <label
        htmlFor="terms"
        style={{ fontSize: '14px', cursor: 'pointer' }}
      >
        Accept terms and conditions
      </label>
    </div>
  ),
}

export const CheckboxGroup: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
        <Checkbox id="option1" />
        <label htmlFor="option1" style={{ fontSize: '14px', cursor: 'pointer' }}>
          Option 1
        </label>
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
        <Checkbox id="option2" defaultChecked />
        <label htmlFor="option2" style={{ fontSize: '14px', cursor: 'pointer' }}>
          Option 2 (pre-checked)
        </label>
      </div>
      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
        <Checkbox id="option3" disabled />
        <label htmlFor="option3" style={{ fontSize: '14px', cursor: 'pointer', opacity: 0.5 }}>
          Option 3 (disabled)
        </label>
      </div>
    </div>
  ),
}
