import type { Meta, StoryObj } from 'storybook'
import { HttpMethodBadge } from './HttpMethodBadge'

const meta = {
  title: 'UIKit/HttpMethodBadge',
  component: HttpMethodBadge,
  tags: ['autodocs'],
  argTypes: {
    method: {
      control: 'select',
      options: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'OPTIONS', 'HEAD'],
    },
  },
} satisfies Meta<typeof HttpMethodBadge>

export default meta
type Story = StoryObj<typeof meta>

export const Get: Story = {
  args: {
    method: 'GET',
  },
}

export const Post: Story = {
  args: {
    method: 'POST',
  },
}

export const Put: Story = {
  args: {
    method: 'PUT',
  },
}

export const Patch: Story = {
  args: {
    method: 'PATCH',
  },
}

export const Delete: Story = {
  args: {
    method: 'DELETE',
  },
}

export const Options: Story = {
  args: {
    method: 'OPTIONS',
  },
}

export const Head: Story = {
  args: {
    method: 'HEAD',
  },
}

export const AllMethods: Story = {
  render: () => (
    <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
      <HttpMethodBadge method="GET" />
      <HttpMethodBadge method="POST" />
      <HttpMethodBadge method="PUT" />
      <HttpMethodBadge method="PATCH" />
      <HttpMethodBadge method="DELETE" />
      <HttpMethodBadge method="OPTIONS" />
      <HttpMethodBadge method="HEAD" />
    </div>
  ),
}

export const UnknownMethod: Story = {
  args: {
    method: 'CUSTOM',
  },
}
