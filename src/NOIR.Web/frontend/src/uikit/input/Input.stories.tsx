import type { Meta, StoryObj } from 'storybook'
import { Input } from './Input'

const meta = {
  title: 'UIKit/Input',
  component: Input,
  tags: ['autodocs'],
  argTypes: {
    type: {
      control: 'select',
      options: ['text', 'password', 'email', 'number', 'search', 'tel', 'url', 'file'],
    },
    disabled: {
      control: 'boolean',
    },
    placeholder: {
      control: 'text',
    },
  },
} satisfies Meta<typeof Input>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    placeholder: 'Enter text...',
    type: 'text',
  },
}

export const WithValue: Story = {
  args: {
    defaultValue: 'Hello, World!',
    type: 'text',
  },
}

export const Email: Story = {
  args: {
    type: 'email',
    placeholder: 'user@example.com',
  },
}

export const Password: Story = {
  args: {
    type: 'password',
    placeholder: 'Enter password',
  },
}

export const Number: Story = {
  args: {
    type: 'number',
    placeholder: '0',
  },
}

export const Search: Story = {
  args: {
    type: 'search',
    placeholder: 'Search...',
  },
}

export const File: Story = {
  args: {
    type: 'file',
  },
}

export const Disabled: Story = {
  args: {
    placeholder: 'Disabled input',
    disabled: true,
  },
}

export const DisabledWithValue: Story = {
  args: {
    defaultValue: 'Cannot edit this',
    disabled: true,
  },
}

export const Invalid: Story = {
  args: {
    'aria-invalid': true,
    defaultValue: 'Invalid value',
  },
}

export const WithLabel: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '4px', maxWidth: 300 }}>
      <label htmlFor="email-input" style={{ fontSize: '14px', fontWeight: 500 }}>
        Email
      </label>
      <Input id="email-input" type="email" placeholder="user@example.com" />
    </div>
  ),
}

export const AllTypes: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', maxWidth: 300 }}>
      <Input type="text" placeholder="Text input" />
      <Input type="email" placeholder="Email input" />
      <Input type="password" placeholder="Password input" />
      <Input type="number" placeholder="Number input" />
      <Input type="search" placeholder="Search input" />
      <Input type="tel" placeholder="Tel input" />
      <Input type="url" placeholder="URL input" />
      <Input type="file" />
    </div>
  ),
}
