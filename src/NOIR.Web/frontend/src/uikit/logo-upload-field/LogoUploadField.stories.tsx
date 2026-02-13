import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import { I18nextProvider } from 'react-i18next'
import i18n from 'i18next'
import { LogoUploadField } from './LogoUploadField'

// Minimal i18n instance for Storybook
const i18nInstance = i18n.createInstance()
i18nInstance.init({
  lng: 'en',
  resources: {
    en: {
      common: {
        errors: {
          invalidFileType: 'Invalid file type',
          fileTooLarge: 'File must be less than {{maxSizeMB}}MB',
          uploadFailed: 'Upload failed',
        },
        labels: {
          logoUploaded: 'Logo uploaded',
          removeLogo: 'Remove logo',
          uploading: 'Uploading...',
          uploadLogo: 'Upload logo',
          dragOrClick: 'Drag & drop or click to select',
        },
      },
    },
  },
  defaultNS: 'common',
  interpolation: { escapeValue: false },
})

function LogoUploadFieldDemo(props: {
  initialValue?: string | null
  disabled?: boolean
  placeholder?: string
  simulateDelay?: number
  simulateError?: boolean
}) {
  const [value, setValue] = useState<string | null>(props.initialValue ?? null)

  const handleUpload = async (_file: File): Promise<string> => {
    // Simulate upload delay
    await new Promise((resolve) => setTimeout(resolve, props.simulateDelay ?? 1000))
    if (props.simulateError) {
      throw new Error('Simulated upload error')
    }
    return 'https://placehold.co/200x200/2563eb/ffffff?text=Logo'
  }

  return (
    <I18nextProvider i18n={i18nInstance}>
      <div style={{ maxWidth: 400 }}>
        <LogoUploadField
          value={value}
          onChange={setValue}
          onUpload={handleUpload}
          disabled={props.disabled}
          placeholder={props.placeholder}
        />
      </div>
    </I18nextProvider>
  )
}

const meta = {
  title: 'UIKit/LogoUploadField',
  component: LogoUploadField,
  tags: ['autodocs'],
} satisfies Meta<typeof LogoUploadField>

export default meta
type Story = StoryObj<typeof meta>

export const Empty: Story = {
  render: () => <LogoUploadFieldDemo />,
}

export const WithPlaceholder: Story = {
  render: () => <LogoUploadFieldDemo placeholder="Upload your company logo" />,
}

export const WithImage: Story = {
  render: () => (
    <LogoUploadFieldDemo initialValue="https://placehold.co/200x200/2563eb/ffffff?text=Logo" />
  ),
}

export const Disabled: Story = {
  render: () => <LogoUploadFieldDemo disabled />,
}

export const DisabledWithImage: Story = {
  render: () => (
    <LogoUploadFieldDemo
      initialValue="https://placehold.co/200x200/2563eb/ffffff?text=Logo"
      disabled
    />
  ),
}

export const SimulatedError: Story = {
  render: () => <LogoUploadFieldDemo simulateError />,
}
