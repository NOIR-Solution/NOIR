import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import { ImageUploadField } from './ImageUploadField'

function ImageUploadFieldDemo(props: {
  initialValue?: string
  placeholder?: string
  hint?: string
  disabled?: boolean
  aspectClass?: string
  label?: string
}) {
  const [value, setValue] = useState(props.initialValue ?? '')

  return (
    <div style={{ maxWidth: 400 }}>
      <ImageUploadField
        value={value}
        onChange={setValue}
        placeholder={props.placeholder}
        hint={props.hint}
        disabled={props.disabled}
        aspectClass={props.aspectClass}
        label={props.label}
        folder="branding"
      />
    </div>
  )
}

const meta = {
  title: 'UIKit/ImageUploadField',
  component: ImageUploadField,
  tags: ['autodocs'],
  decorators: [
    (Story) => (
      <div style={{ maxWidth: 400, padding: 16 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof ImageUploadField>

export default meta
type Story = StoryObj<typeof meta>

export const Empty: Story = {
  render: () => <ImageUploadFieldDemo label="Logo" />,
}

export const WithPlaceholder: Story = {
  render: () => <ImageUploadFieldDemo label="Favicon" placeholder="Click to upload favicon" />,
}

export const WithImage: Story = {
  render: () => (
    <ImageUploadFieldDemo
      label="Brand Logo"
      initialValue="https://placehold.co/400x200/2563eb/ffffff?text=Logo"
    />
  ),
}

export const Disabled: Story = {
  render: () => (
    <ImageUploadFieldDemo label="Logo (Disabled)" disabled />
  ),
}

export const DisabledWithImage: Story = {
  render: () => (
    <ImageUploadFieldDemo
      label="Logo (Disabled)"
      initialValue="https://placehold.co/400x200/2563eb/ffffff?text=Logo"
      disabled
    />
  ),
}

export const SquareAspect: Story = {
  render: () => (
    <ImageUploadFieldDemo label="Avatar" aspectClass="aspect-square" placeholder="Upload avatar" />
  ),
}

export const WithHint: Story = {
  render: () => (
    <ImageUploadFieldDemo
      label="Featured Image"
      placeholder="Click to upload"
      hint="JPG, PNG, GIF, WebP up to 10MB"
    />
  ),
}
