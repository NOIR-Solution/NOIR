import type { Meta, StoryObj } from 'storybook'
import { Switch } from './Switch'

const meta = {
  title: 'UIKit/Switch',
  component: Switch,
  tags: ['autodocs'],
  argTypes: {
    disabled: {
      control: 'boolean',
    },
    checked: {
      control: 'boolean',
    },
  },
} satisfies Meta<typeof Switch>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    'aria-label': 'Toggle setting',
  },
}

export const Checked: Story = {
  args: {
    checked: true,
    'aria-label': 'Toggle setting',
  },
}

export const Unchecked: Story = {
  args: {
    checked: false,
    'aria-label': 'Toggle setting',
  },
}

export const Disabled: Story = {
  args: {
    disabled: true,
    'aria-label': 'Toggle setting',
  },
}

export const DisabledChecked: Story = {
  args: {
    disabled: true,
    checked: true,
    'aria-label': 'Toggle setting',
  },
}

export const WithLabel: Story = {
  render: () => (
    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
      <Switch id="airplane-mode" />
      <label htmlFor="airplane-mode" style={{ fontSize: '14px', cursor: 'pointer' }}>
        Airplane Mode
      </label>
    </div>
  ),
}

export const SettingsGroup: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', maxWidth: 350 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <div style={{ fontSize: '14px', fontWeight: 500 }}>Notifications</div>
          <div style={{ fontSize: '12px', color: '#666' }}>Receive push notifications</div>
        </div>
        <Switch defaultChecked aria-label="Notifications" />
      </div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <div style={{ fontSize: '14px', fontWeight: 500 }}>Dark Mode</div>
          <div style={{ fontSize: '12px', color: '#666' }}>Toggle dark theme</div>
        </div>
        <Switch aria-label="Dark Mode" />
      </div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <div style={{ fontSize: '14px', fontWeight: 500 }}>Auto-save</div>
          <div style={{ fontSize: '12px', color: '#666' }}>Save changes automatically</div>
        </div>
        <Switch defaultChecked aria-label="Auto-save" />
      </div>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <div>
          <div style={{ fontSize: '14px', fontWeight: 500, opacity: 0.5 }}>Beta Features</div>
          <div style={{ fontSize: '12px', color: '#666', opacity: 0.5 }}>Not available yet</div>
        </div>
        <Switch disabled aria-label="Beta Features" />
      </div>
    </div>
  ),
}
