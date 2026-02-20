import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import { OtpInput } from '@/components/forgot-password/OtpInput'

const meta = {
  title: 'UIKit/OtpInput',
  component: OtpInput,
  tags: ['autodocs'],
  argTypes: {
    length: {
      control: { type: 'number', min: 4, max: 8 },
      description: 'Number of OTP digits',
    },
    disabled: {
      control: 'boolean',
    },
    error: {
      control: 'boolean',
    },
    autoFocus: {
      control: 'boolean',
    },
  },
} satisfies Meta<typeof OtpInput>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => {
    const [value, setValue] = useState('')
    return (
      <OtpInput
        value={value}
        onChange={setValue}
        onComplete={(otp) => console.log('OTP completed:', otp)}
      />
    )
  },
}

export const PartiallyFilled: Story = {
  render: () => {
    const [value, setValue] = useState('123')
    return (
      <OtpInput
        value={value}
        onChange={setValue}
        onComplete={(otp) => console.log('OTP completed:', otp)}
      />
    )
  },
}

export const Complete: Story = {
  render: () => {
    const [value, setValue] = useState('123456')
    return (
      <OtpInput
        value={value}
        onChange={setValue}
      />
    )
  },
}

export const ErrorState: Story = {
  render: () => {
    const [value, setValue] = useState('123456')
    return (
      <OtpInput
        value={value}
        onChange={setValue}
        error
      />
    )
  },
}

export const Disabled: Story = {
  render: () => {
    const [value, setValue] = useState('123456')
    return (
      <OtpInput
        value={value}
        onChange={setValue}
        disabled
      />
    )
  },
}

export const FourDigits: Story = {
  render: () => {
    const [value, setValue] = useState('')
    return (
      <OtpInput
        length={4}
        value={value}
        onChange={setValue}
        onComplete={(otp) => console.log('OTP completed:', otp)}
      />
    )
  },
}

export const AllStates: Story = {
  render: () => {
    const [defaultValue, setDefaultValue] = useState('')
    const [errorValue, setErrorValue] = useState('123456')
    const [disabledValue, setDisabledValue] = useState('123456')

    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: '32px' }}>
        <div>
          <p style={{ marginBottom: '8px', fontSize: '14px', fontWeight: 500 }}>Default (empty)</p>
          <OtpInput value={defaultValue} onChange={setDefaultValue} autoFocus={false} />
        </div>
        <div>
          <p style={{ marginBottom: '8px', fontSize: '14px', fontWeight: 500, color: 'red' }}>Error State</p>
          <OtpInput value={errorValue} onChange={setErrorValue} error autoFocus={false} />
        </div>
        <div>
          <p style={{ marginBottom: '8px', fontSize: '14px', fontWeight: 500, color: 'gray' }}>Disabled</p>
          <OtpInput value={disabledValue} onChange={setDisabledValue} disabled autoFocus={false} />
        </div>
      </div>
    )
  },
}
