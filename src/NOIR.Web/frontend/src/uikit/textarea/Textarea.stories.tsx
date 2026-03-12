import type { Meta, StoryObj } from 'storybook'
import { Textarea } from './Textarea'

const meta = {
  title: 'UIKit/Textarea',
  component: Textarea,
  tags: ['autodocs'],
  argTypes: {
    disabled: {
      control: 'boolean',
    },
    placeholder: {
      control: 'text',
    },
    rows: {
      control: 'number',
    },
  },
} satisfies Meta<typeof Textarea>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    placeholder: 'Type your message here...',
    'aria-label': 'Message',
  },
}

export const WithValue: Story = {
  args: {
    defaultValue:
      'Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.',
    'aria-label': 'Message',
  },
}

export const Disabled: Story = {
  args: {
    placeholder: 'This textarea is disabled',
    disabled: true,
    'aria-label': 'Disabled textarea',
  },
}

export const DisabledWithValue: Story = {
  args: {
    defaultValue: 'This content cannot be edited.',
    disabled: true,
    'aria-label': 'Disabled textarea',
  },
}

export const CustomRows: Story = {
  args: {
    placeholder: 'Textarea with 8 rows',
    rows: 8,
    'aria-label': 'Large textarea',
  },
}

export const SmallRows: Story = {
  args: {
    placeholder: 'Textarea with 2 rows',
    rows: 2,
    'aria-label': 'Small textarea',
  },
}

export const WithLabel: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', maxWidth: 400 }}>
      <label htmlFor="bio" style={{ fontSize: '14px', fontWeight: 500 }}>
        Bio
      </label>
      <Textarea id="bio" placeholder="Tell us about yourself..." />
      <span style={{ fontSize: '12px', color: '#666' }}>
        Maximum 500 characters.
      </span>
    </div>
  ),
}

export const WithMaxLength: Story = {
  args: {
    placeholder: 'Limited to 100 characters',
    maxLength: 100,
    'aria-label': 'Limited textarea',
  },
}

export const ReadOnly: Story = {
  args: {
    defaultValue: 'This is read-only content that cannot be modified by the user.',
    readOnly: true,
    'aria-label': 'Read-only textarea',
  },
}

export const Required: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', maxWidth: 400 }}>
      <label htmlFor="required-field" style={{ fontSize: '14px', fontWeight: 500 }}>
        Description <span style={{ color: 'red' }}>*</span>
      </label>
      <Textarea id="required-field" placeholder="This field is required" required />
    </div>
  ),
}

export const FormExample: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px', maxWidth: 400 }}>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
        <label htmlFor="form-subject" style={{ fontSize: '14px', fontWeight: 500 }}>Subject</label>
        <input
          id="form-subject"
          type="text"
          placeholder="Enter subject"
          style={{
            padding: '6px 12px',
            borderRadius: '6px',
            border: '1px solid #ccc',
            fontSize: '14px',
          }}
        />
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
        <label htmlFor="form-message" style={{ fontSize: '14px', fontWeight: 500 }}>Message</label>
        <Textarea id="form-message" placeholder="Write your message..." rows={6} />
      </div>
      <button
        style={{
          padding: '8px 16px',
          borderRadius: '6px',
          border: 'none',
          background: '#0f172a',
          color: 'white',
          cursor: 'pointer',
          alignSelf: 'flex-end',
        }}
      >
        Send
      </button>
    </div>
  ),
}
