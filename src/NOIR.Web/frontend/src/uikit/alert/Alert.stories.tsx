import type { Meta, StoryObj } from 'storybook'
import { Alert, AlertTitle, AlertDescription } from './Alert'

const meta = {
  title: 'UIKit/Alert',
  component: Alert,
  tags: ['autodocs'],
  argTypes: {
    variant: {
      control: 'select',
      options: ['default', 'destructive'],
    },
  },
} satisfies Meta<typeof Alert>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: (args) => (
    <Alert {...args}>
      <AlertTitle>Default Alert</AlertTitle>
      <AlertDescription>
        This is a default alert with a title and description.
      </AlertDescription>
    </Alert>
  ),
  args: {
    variant: 'default',
  },
}

export const Destructive: Story = {
  render: (args) => (
    <Alert {...args}>
      <AlertTitle>Error</AlertTitle>
      <AlertDescription>
        Something went wrong. Please try again later.
      </AlertDescription>
    </Alert>
  ),
  args: {
    variant: 'destructive',
  },
}

export const TitleOnly: Story = {
  render: (args) => (
    <Alert {...args}>
      <AlertTitle>Alert with title only</AlertTitle>
    </Alert>
  ),
  args: {},
}

export const DescriptionOnly: Story = {
  render: (args) => (
    <Alert {...args}>
      <AlertDescription>
        Alert with description only, no title provided.
      </AlertDescription>
    </Alert>
  ),
  args: {},
}

export const WithIcon: Story = {
  render: (args) => (
    <Alert {...args}>
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
      >
        <circle cx="12" cy="12" r="10" />
        <line x1="12" y1="8" x2="12" y2="12" />
        <line x1="12" y1="16" x2="12.01" y2="16" />
      </svg>
      <AlertTitle>Heads up!</AlertTitle>
      <AlertDescription>
        You can add icons to draw attention to the alert.
      </AlertDescription>
    </Alert>
  ),
  args: {},
}

export const DestructiveWithIcon: Story = {
  render: (args) => (
    <Alert {...args}>
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
      >
        <path d="m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3Z" />
        <line x1="12" y1="9" x2="12" y2="13" />
        <line x1="12" y1="17" x2="12.01" y2="17" />
      </svg>
      <AlertTitle>Warning</AlertTitle>
      <AlertDescription>
        This action is destructive and cannot be undone.
      </AlertDescription>
    </Alert>
  ),
  args: {
    variant: 'destructive',
  },
}

export const LongContent: Story = {
  render: (args) => (
    <Alert {...args}>
      <AlertTitle>Important Information</AlertTitle>
      <AlertDescription>
        <p>This alert contains multiple paragraphs of content.</p>
        <p>
          Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do
          eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad
          minim veniam, quis nostrud exercitation ullamco laboris.
        </p>
      </AlertDescription>
    </Alert>
  ),
  args: {},
}
