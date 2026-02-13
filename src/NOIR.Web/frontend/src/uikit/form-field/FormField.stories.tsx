import type { Meta, StoryObj } from 'storybook'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { FormField, FormTextarea, FormError } from './FormField'

const schema = z.object({
  name: z.string().min(1, 'Name is required'),
  email: z.string().email('Invalid email address'),
  bio: z.string().optional(),
  password: z.string().min(8, 'Password must be at least 8 characters'),
})

type FormData = z.infer<typeof schema>

function FormFieldDemo(props: {
  name: keyof FormData
  label?: string
  placeholder?: string
  description?: string
  type?: string
  required?: boolean
  disabled?: boolean
}) {
  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    mode: 'onBlur',
    defaultValues: { name: '', email: '', bio: '', password: '' },
  })

  return (
    <div style={{ maxWidth: 400 }}>
      <FormField
        form={form}
        name={props.name}
        label={props.label}
        placeholder={props.placeholder}
        description={props.description}
        type={props.type}
        required={props.required}
        disabled={props.disabled}
      />
    </div>
  )
}

function FormTextareaDemo() {
  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    mode: 'onBlur',
    defaultValues: { name: '', email: '', bio: '', password: '' },
  })

  return (
    <div style={{ maxWidth: 400 }}>
      <FormTextarea
        form={form}
        name="bio"
        label="Bio"
        placeholder="Tell us about yourself..."
        description="Brief description for your profile."
        rows={4}
      />
    </div>
  )
}

function FullFormDemo() {
  const form = useForm<FormData>({
    resolver: zodResolver(schema),
    mode: 'onBlur',
    defaultValues: { name: '', email: '', bio: '', password: '' },
  })

  return (
    <form
      style={{ maxWidth: 400, display: 'flex', flexDirection: 'column', gap: 16 }}
      onSubmit={form.handleSubmit(() => {})}
    >
      <FormField form={form} name="name" label="Name" placeholder="Enter your name" required />
      <FormField
        form={form}
        name="email"
        label="Email"
        placeholder="Enter your email"
        type="email"
        required
        description="We will never share your email."
      />
      <FormField
        form={form}
        name="password"
        label="Password"
        placeholder="Enter a password"
        type="password"
        required
      />
      <FormTextarea form={form} name="bio" label="Bio" placeholder="Tell us about yourself..." />
      <button type="submit" style={{ padding: '8px 16px', cursor: 'pointer' }}>
        Submit
      </button>
    </form>
  )
}

const meta = {
  title: 'UIKit/FormField',
  component: FormField,
  tags: ['autodocs'],
} satisfies Meta<typeof FormField>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => (
    <FormFieldDemo name="name" label="Name" placeholder="Enter your name" />
  ),
}

export const WithDescription: Story = {
  render: () => (
    <FormFieldDemo
      name="email"
      label="Email Address"
      placeholder="user@example.com"
      type="email"
      description="We will never share your email with anyone."
    />
  ),
}

export const Required: Story = {
  render: () => (
    <FormFieldDemo name="name" label="Full Name" placeholder="Required field" required />
  ),
}

export const Disabled: Story = {
  render: () => (
    <FormFieldDemo name="name" label="Name" placeholder="Cannot edit" disabled />
  ),
}

export const PasswordType: Story = {
  render: () => (
    <FormFieldDemo name="password" label="Password" placeholder="Enter password" type="password" required />
  ),
}

export const Textarea: Story = {
  render: () => <FormTextareaDemo />,
}

export const FullForm: Story = {
  render: () => <FullFormDemo />,
}

export const ErrorMessage: Story = {
  render: () => (
    <div style={{ maxWidth: 400, display: 'flex', flexDirection: 'column', gap: 16 }}>
      <FormError message="Something went wrong. Please try again." />
      <FormError message={null} />
    </div>
  ),
}
