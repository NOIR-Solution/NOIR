import type { Meta, StoryObj } from 'storybook'
import { Label } from './Label'

const meta = {
  title: 'UIKit/Label',
  component: Label,
  tags: ['autodocs'],
} satisfies Meta<typeof Label>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    children: 'Email Address',
  },
}

export const WithHtmlFor: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
      <Label htmlFor="username">Username</Label>
      <input
        id="username"
        type="text"
        placeholder="Enter username"
        style={{
          padding: '6px 12px',
          borderRadius: '6px',
          border: '1px solid #ccc',
          fontSize: '14px',
        }}
      />
    </div>
  ),
}

export const Required: Story = {
  render: () => (
    <Label>
      Password <span style={{ color: 'red' }}>*</span>
    </Label>
  ),
}

export const DisabledState: Story = {
  render: () => (
    <div data-disabled="true" className="group">
      <Label>Disabled Label</Label>
    </div>
  ),
}

export const WithPeerDisabled: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
      <Label htmlFor="disabled-input">Disabled Field</Label>
      <input
        id="disabled-input"
        type="text"
        disabled
        placeholder="Cannot edit"
        className="peer"
        style={{
          padding: '6px 12px',
          borderRadius: '6px',
          border: '1px solid #ccc',
          fontSize: '14px',
          opacity: 0.5,
        }}
      />
    </div>
  ),
}

export const MultipleLabels: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
        <Label htmlFor="first-name">First Name</Label>
        <input
          id="first-name"
          type="text"
          placeholder="John"
          style={{
            padding: '6px 12px',
            borderRadius: '6px',
            border: '1px solid #ccc',
            fontSize: '14px',
          }}
        />
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
        <Label htmlFor="last-name">Last Name</Label>
        <input
          id="last-name"
          type="text"
          placeholder="Doe"
          style={{
            padding: '6px 12px',
            borderRadius: '6px',
            border: '1px solid #ccc',
            fontSize: '14px',
          }}
        />
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
        <Label htmlFor="email-addr">Email</Label>
        <input
          id="email-addr"
          type="email"
          placeholder="john@example.com"
          style={{
            padding: '6px 12px',
            borderRadius: '6px',
            border: '1px solid #ccc',
            fontSize: '14px',
          }}
        />
      </div>
    </div>
  ),
}
