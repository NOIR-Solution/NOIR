import type { Meta, StoryObj } from 'storybook'
import { useForm } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import {
  Form,
  FormField,
  FormItem,
  FormLabel,
  FormControl,
  FormDescription,
  FormMessage,
} from './Form'
import { Input } from '../input/Input'
import { Button } from '../button/Button'

const meta = {
  title: 'UIKit/Form',
  component: Form,
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
  },
} satisfies Meta<typeof Form>

export default meta
type Story = StoryObj<typeof meta>

const simpleSchema = z.object({
  username: z.string().min(2, 'Username must be at least 2 characters'),
  email: z.string().email('Please enter a valid email address'),
})

type SimpleFormValues = z.infer<typeof simpleSchema>

function SimpleFormExample() {
  const form = useForm<SimpleFormValues>({
    resolver: zodResolver(simpleSchema),
    mode: 'onBlur',
    defaultValues: {
      username: '',
      email: '',
    },
  })

  const onSubmit = (data: SimpleFormValues) => {
    console.log('Form submitted:', data)
  }

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(onSubmit)} style={{ width: 400 }} className="space-y-4">
        <FormField
          control={form.control}
          name="username"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Username</FormLabel>
              <FormControl>
                <Input placeholder="Enter username" {...field} />
              </FormControl>
              <FormDescription>Your public display name.</FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="email"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Email</FormLabel>
              <FormControl>
                <Input type="email" placeholder="Enter email" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <Button type="submit">Submit</Button>
      </form>
    </Form>
  )
}

export const Default: Story = {
  render: () => <SimpleFormExample />,
}

const validationSchema = z.object({
  name: z.string().min(1, 'Name is required').max(50, 'Name must be less than 50 characters'),
  email: z.string().email('Invalid email address'),
  age: z.string().refine((val) => !isNaN(Number(val)) && Number(val) >= 18, {
    message: 'You must be at least 18 years old',
  }),
})

type ValidationFormValues = z.infer<typeof validationSchema>

function ValidationFormExample() {
  const form = useForm<ValidationFormValues>({
    resolver: zodResolver(validationSchema),
    mode: 'onBlur',
    defaultValues: {
      name: '',
      email: 'invalid-email',
      age: '15',
    },
  })

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(() => {})} style={{ width: 400 }} className="space-y-4">
        <FormField
          control={form.control}
          name="name"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Name</FormLabel>
              <FormControl>
                <Input placeholder="Your name" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="email"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Email</FormLabel>
              <FormControl>
                <Input type="email" placeholder="you@example.com" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="age"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Age</FormLabel>
              <FormControl>
                <Input type="number" placeholder="Your age" {...field} />
              </FormControl>
              <FormDescription>Must be 18 or older.</FormDescription>
              <FormMessage />
            </FormItem>
          )}
        />
        <Button type="submit">Submit</Button>
      </form>
    </Form>
  )
}

export const WithValidationErrors: Story = {
  render: () => <ValidationFormExample />,
}

function SubmittingFormExample() {
  const form = useForm<SimpleFormValues>({
    resolver: zodResolver(simpleSchema),
    mode: 'onBlur',
    defaultValues: {
      username: 'johndoe',
      email: 'john@example.com',
    },
  })

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(() => {})} style={{ width: 400 }} className="space-y-4">
        <FormField
          control={form.control}
          name="username"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Username</FormLabel>
              <FormControl>
                <Input disabled placeholder="Enter username" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="email"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Email</FormLabel>
              <FormControl>
                <Input disabled type="email" placeholder="Enter email" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <Button disabled type="submit">
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
            className="animate-spin"
          >
            <path d="M21 12a9 9 0 1 1-6.219-8.56" />
          </svg>
          Submitting...
        </Button>
      </form>
    </Form>
  )
}

export const SubmittingState: Story = {
  render: () => <SubmittingFormExample />,
}

function PrefilledFormExample() {
  const form = useForm<SimpleFormValues>({
    resolver: zodResolver(simpleSchema),
    mode: 'onBlur',
    defaultValues: {
      username: 'johndoe',
      email: 'john@example.com',
    },
  })

  return (
    <Form {...form}>
      <form onSubmit={form.handleSubmit(() => {})} style={{ width: 400 }} className="space-y-4">
        <FormField
          control={form.control}
          name="username"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Username</FormLabel>
              <FormControl>
                <Input {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <FormField
          control={form.control}
          name="email"
          render={({ field }) => (
            <FormItem>
              <FormLabel>Email</FormLabel>
              <FormControl>
                <Input type="email" {...field} />
              </FormControl>
              <FormMessage />
            </FormItem>
          )}
        />
        <Button type="submit">Update</Button>
      </form>
    </Form>
  )
}

export const Prefilled: Story = {
  render: () => <PrefilledFormExample />,
}
