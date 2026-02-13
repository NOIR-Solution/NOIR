import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import { InlineEditInput } from './InlineEditInput'

const meta = {
  title: 'UIKit/InlineEditInput',
  component: InlineEditInput,
  tags: ['autodocs'],
  argTypes: {
    align: {
      control: 'select',
      options: ['left', 'center', 'right'],
    },
    type: {
      control: 'select',
      options: ['text', 'number', 'email'],
    },
    disabled: { control: 'boolean' },
  },
} satisfies Meta<typeof InlineEditInput>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    placeholder: 'Enter text...',
    value: 'Editable text',
  },
}

export const WithError: Story = {
  args: {
    value: '',
    error: 'This field is required',
    hasError: true,
    placeholder: 'Required field',
  },
}

export const NumberInput: Story = {
  args: {
    type: 'number',
    value: '42',
    align: 'right',
    placeholder: '0',
  },
}

export const CenterAligned: Story = {
  args: {
    value: 'Centered',
    align: 'center',
  },
}

export const Disabled: Story = {
  args: {
    value: 'Cannot edit this',
    disabled: true,
  },
}

export const Interactive: Story = {
  render: () => {
    const [value, setValue] = useState('Edit me')
    const [saved, setSaved] = useState('')
    return (
      <div style={{ width: 300 }}>
        <InlineEditInput
          value={value}
          onChange={setValue}
          onEnterPress={() => setSaved(value)}
          onEscapePress={() => setValue(saved || 'Edit me')}
          placeholder="Type and press Enter to save"
        />
        {saved && (
          <p style={{ marginTop: 8, fontSize: 12, color: '#666' }}>
            Last saved: {saved}
          </p>
        )}
      </div>
    )
  },
}

export const WithValidation: Story = {
  render: () => {
    const [value, setValue] = useState('')
    const error = value.length > 0 && value.length < 3
      ? 'Must be at least 3 characters'
      : undefined
    return (
      <div style={{ width: 300 }}>
        <InlineEditInput
          value={value}
          onChange={setValue}
          error={error}
          hasError={!!error}
          placeholder="Min 3 characters"
        />
      </div>
    )
  },
}
