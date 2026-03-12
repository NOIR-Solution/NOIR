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
    'aria-label': 'Text input',
  },
}

export const WithValue: Story = {
  args: {
    defaultValue: 'Hello, World!',
    type: 'text',
    'aria-label': 'Text input',
  },
}

export const Email: Story = {
  args: {
    type: 'email',
    placeholder: 'user@example.com',
    'aria-label': 'Email input',
  },
}

export const Password: Story = {
  args: {
    type: 'password',
    placeholder: 'Enter password',
    'aria-label': 'Password input',
  },
}

export const Number: Story = {
  args: {
    type: 'number',
    placeholder: '0',
    'aria-label': 'Number input',
  },
}

export const Search: Story = {
  args: {
    type: 'search',
    placeholder: 'Search...',
    'aria-label': 'Search input',
  },
}

export const File: Story = {
  args: {
    type: 'file',
    'aria-label': 'File input',
  },
}

export const Disabled: Story = {
  args: {
    placeholder: 'Disabled input',
    disabled: true,
    'aria-label': 'Disabled input',
  },
}

export const DisabledWithValue: Story = {
  args: {
    defaultValue: 'Cannot edit this',
    disabled: true,
    'aria-label': 'Disabled input with value',
  },
}

export const Invalid: Story = {
  args: {
    'aria-invalid': true,
    defaultValue: 'Invalid value',
    'aria-label': 'Invalid input',
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

export const Loading: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', maxWidth: 300 }}>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
        <label htmlFor="loading-email-input" style={{ fontSize: '14px', fontWeight: 500 }}>Email</label>
        <div className="relative">
          <Input id="loading-email-input" disabled placeholder="Loading..." className="pr-8" />
          <svg
            xmlns="http://www.w3.org/2000/svg"
            width="16"
            height="16"
            viewBox="0 0 24 24"
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
            className="animate-spin absolute right-2.5 top-2.5 text-muted-foreground"
          >
            <path d="M21 12a9 9 0 1 1-6.219-8.56" />
          </svg>
        </div>
      </div>
      <div style={{ display: 'flex', flexDirection: 'column', gap: '4px' }}>
        <div className="h-4 w-16 animate-pulse rounded bg-muted" />
        <div className="h-10 w-full animate-pulse rounded-md bg-muted" />
      </div>
    </div>
  ),
}

export const AllTypes: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', maxWidth: 300 }}>
      <Input type="text" placeholder="Text input" aria-label="Text input" />
      <Input type="email" placeholder="Email input" aria-label="Email input" />
      <Input type="password" placeholder="Password input" aria-label="Password input" />
      <Input type="number" placeholder="Number input" aria-label="Number input" />
      <Input type="search" placeholder="Search input" aria-label="Search input" />
      <Input type="tel" placeholder="Tel input" aria-label="Tel input" />
      <Input type="url" placeholder="URL input" aria-label="URL input" />
      <Input type="file" aria-label="File input" />
    </div>
  ),
}
