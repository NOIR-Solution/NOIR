import type { Meta, StoryObj } from 'storybook'
import { Avatar } from './Avatar'

const meta = {
  title: 'UIKit/Avatar',
  component: Avatar,
  tags: ['autodocs'],
  argTypes: {
    size: {
      control: 'select',
      options: ['sm', 'md', 'lg'],
    },
  },
} satisfies Meta<typeof Avatar>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    fallback: 'John Doe',
    size: 'md',
  },
}

export const WithImage: Story = {
  args: {
    src: 'https://i.pravatar.cc/150?u=avatar-story',
    alt: 'User avatar',
    size: 'md',
  },
}

export const WithBrokenImage: Story = {
  args: {
    src: 'https://broken-url.invalid/avatar.png',
    alt: 'Broken Image',
    fallback: 'BI',
    size: 'md',
  },
}

export const Small: Story = {
  args: {
    fallback: 'SM',
    size: 'sm',
  },
}

export const Medium: Story = {
  args: {
    fallback: 'MD',
    size: 'md',
  },
}

export const Large: Story = {
  args: {
    fallback: 'LG',
    size: 'lg',
  },
}

export const AllSizes: Story = {
  render: () => (
    <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
      <Avatar fallback="SM" size="sm" />
      <Avatar fallback="MD" size="md" />
      <Avatar fallback="LG" size="lg" />
    </div>
  ),
}

export const FallbackFromEmail: Story = {
  args: {
    fallback: 'jane@example.com',
    size: 'md',
  },
}

export const FallbackSingleCharacter: Story = {
  args: {
    fallback: 'X',
    size: 'md',
  },
}

export const NoFallbackOrAlt: Story = {
  args: {
    size: 'md',
  },
}

export const FallbackFromAlt: Story = {
  args: {
    alt: 'Alice Wonder',
    size: 'md',
  },
}
