import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import {
  Credenza,
  CredenzaTrigger,
  CredenzaContent,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaDescription,
  CredenzaBody,
  CredenzaFooter,
  CredenzaClose,
} from './Credenza'
import { Button } from '../button/Button'

function CredenzaDemo(props: {
  title?: string
  description?: string
  bodyContent?: string
}) {
  return (
    <Credenza>
      <CredenzaTrigger asChild>
        <Button variant="outline" className="cursor-pointer">
          Open Credenza
        </Button>
      </CredenzaTrigger>
      <CredenzaContent>
        <CredenzaHeader>
          <CredenzaTitle>{props.title ?? 'Credenza Title'}</CredenzaTitle>
          <CredenzaDescription>
            {props.description ?? 'This is a responsive dialog that becomes a drawer on mobile.'}
          </CredenzaDescription>
        </CredenzaHeader>
        <CredenzaBody>
          <p>
            {props.bodyContent ??
              'This content area adapts based on screen size. On desktop, it appears as a centered dialog. On mobile, it slides up as a drawer from the bottom.'}
          </p>
        </CredenzaBody>
        <CredenzaFooter>
          <CredenzaClose asChild>
            <Button variant="outline" className="cursor-pointer">
              Cancel
            </Button>
          </CredenzaClose>
          <Button className="cursor-pointer">Confirm</Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}

function ControlledCredenzaDemo() {
  const [open, setOpen] = useState(false)

  return (
    <div>
      <Button variant="outline" className="cursor-pointer" onClick={() => setOpen(true)}>
        Open Controlled Credenza
      </Button>
      <p style={{ marginTop: 8, fontSize: 13, color: '#6b7280' }}>
        Open state: {open ? 'true' : 'false'}
      </p>
      <Credenza open={open} onOpenChange={setOpen}>
        <CredenzaContent>
          <CredenzaHeader>
            <CredenzaTitle>Controlled Credenza</CredenzaTitle>
            <CredenzaDescription>
              This credenza is controlled by external state.
            </CredenzaDescription>
          </CredenzaHeader>
          <CredenzaBody>
            <p>The open/close state is managed externally via useState.</p>
          </CredenzaBody>
          <CredenzaFooter>
            <Button variant="outline" className="cursor-pointer" onClick={() => setOpen(false)}>
              Close
            </Button>
          </CredenzaFooter>
        </CredenzaContent>
      </Credenza>
    </div>
  )
}

function CredenzaWithFormDemo() {
  return (
    <Credenza>
      <CredenzaTrigger asChild>
        <Button variant="outline" className="cursor-pointer">
          Open Form Credenza
        </Button>
      </CredenzaTrigger>
      <CredenzaContent>
        <CredenzaHeader>
          <CredenzaTitle>Edit Profile</CredenzaTitle>
          <CredenzaDescription>Update your profile information below.</CredenzaDescription>
        </CredenzaHeader>
        <CredenzaBody>
          <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
            <div>
              <label
                htmlFor="cred-name"
                style={{ display: 'block', fontSize: 14, fontWeight: 500, marginBottom: 4 }}
              >
                Name
              </label>
              <input
                id="cred-name"
                type="text"
                placeholder="Enter your name"
                style={{
                  width: '100%',
                  padding: '8px 12px',
                  borderRadius: 6,
                  border: '1px solid #d1d5db',
                  fontSize: 14,
                }}
              />
            </div>
            <div>
              <label
                htmlFor="cred-email"
                style={{ display: 'block', fontSize: 14, fontWeight: 500, marginBottom: 4 }}
              >
                Email
              </label>
              <input
                id="cred-email"
                type="email"
                placeholder="Enter your email"
                style={{
                  width: '100%',
                  padding: '8px 12px',
                  borderRadius: 6,
                  border: '1px solid #d1d5db',
                  fontSize: 14,
                }}
              />
            </div>
          </div>
        </CredenzaBody>
        <CredenzaFooter>
          <CredenzaClose asChild>
            <Button variant="outline" className="cursor-pointer">
              Cancel
            </Button>
          </CredenzaClose>
          <Button className="cursor-pointer">Save Changes</Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}

const meta = {
  title: 'UIKit/Credenza',
  component: Credenza,
  tags: ['autodocs'],
  decorators: [
    (Story) => (
      <div style={{ padding: 16 }}>
        <Story />
      </div>
    ),
  ],
} satisfies Meta<typeof Credenza>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => <CredenzaDemo />,
}

export const CustomContent: Story = {
  render: () => (
    <CredenzaDemo
      title="Delete Item"
      description="Are you sure you want to delete this item?"
      bodyContent="This action cannot be undone. The item and all associated data will be permanently removed."
    />
  ),
}

export const Controlled: Story = {
  render: () => <ControlledCredenzaDemo />,
}

export const WithForm: Story = {
  render: () => <CredenzaWithFormDemo />,
}

export const LongContent: Story = {
  render: () => (
    <Credenza>
      <CredenzaTrigger asChild>
        <Button variant="outline" className="cursor-pointer">
          Open with long content
        </Button>
      </CredenzaTrigger>
      <CredenzaContent>
        <CredenzaHeader>
          <CredenzaTitle>Terms of Service</CredenzaTitle>
          <CredenzaDescription>Please read and accept the following terms.</CredenzaDescription>
        </CredenzaHeader>
        <CredenzaBody>
          {Array.from({ length: 10 }, (_, i) => (
            <p key={i} style={{ marginBottom: 12, fontSize: 14, color: '#4b5563' }}>
              Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor
              incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud
              exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure
              dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.
            </p>
          ))}
        </CredenzaBody>
        <CredenzaFooter>
          <CredenzaClose asChild>
            <Button variant="outline" className="cursor-pointer">
              Decline
            </Button>
          </CredenzaClose>
          <Button className="cursor-pointer">Accept</Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  ),
}

export const MinimalNoFooter: Story = {
  render: () => (
    <Credenza>
      <CredenzaTrigger asChild>
        <Button variant="outline" className="cursor-pointer">
          Notification
        </Button>
      </CredenzaTrigger>
      <CredenzaContent>
        <CredenzaHeader>
          <CredenzaTitle>Notification</CredenzaTitle>
        </CredenzaHeader>
        <CredenzaBody>
          <p>Your changes have been saved successfully.</p>
        </CredenzaBody>
      </CredenzaContent>
    </Credenza>
  ),
}
