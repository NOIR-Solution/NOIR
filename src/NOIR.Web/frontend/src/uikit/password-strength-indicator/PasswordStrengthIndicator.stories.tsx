import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import { I18nextProvider } from 'react-i18next'
import i18n from 'i18next'
import { PasswordStrengthIndicator } from '@/components/forgot-password/PasswordStrengthIndicator'

// Minimal i18n instance for Storybook
const i18nInstance = i18n.createInstance()
i18nInstance.init({
  lng: 'en',
  resources: {
    en: {
      auth: {
        forgotPassword: {
          reset: {
            passwordStrength: 'Password strength',
            strength: {
              weak: 'Weak',
              fair: 'Fair',
              good: 'Good',
              strong: 'Strong',
            },
            requirements: {
              length: '12+ characters',
              lowercase: 'Lowercase letter',
              uppercase: 'Uppercase letter',
              digit: 'Number',
              special: 'Special character',
              uniqueChars: '4+ unique characters',
            },
          },
        },
      },
    },
  },
  defaultNS: 'auth',
  interpolation: { escapeValue: false },
})

const withI18n = (Story: React.ComponentType) => (
  <I18nextProvider i18n={i18nInstance}>
    <div style={{ width: '320px' }}>
      <Story />
    </div>
  </I18nextProvider>
)

const meta = {
  title: 'UIKit/PasswordStrengthIndicator',
  component: PasswordStrengthIndicator,
  tags: ['autodocs'],
  decorators: [withI18n],
  argTypes: {
    password: {
      control: 'text',
      description: 'The password to analyze',
    },
    showRequirements: {
      control: 'boolean',
      description: 'Whether to show the requirements checklist',
    },
  },
} satisfies Meta<typeof PasswordStrengthIndicator>

export default meta
type Story = StoryObj<typeof meta>

export const Interactive: Story = {
  render: () => {
    const [password, setPassword] = useState('')
    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: '12px' }}>
        <input
          type="text"
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          placeholder="Type a password..."
          style={{
            padding: '8px 12px',
            border: '1px solid #ddd',
            borderRadius: '6px',
            fontSize: '14px',
            width: '100%',
            boxSizing: 'border-box',
          }}
        />
        <PasswordStrengthIndicator password={password} />
      </div>
    )
  },
}

export const Weak: Story = {
  args: {
    password: 'ab',
  },
}

export const Fair: Story = {
  args: {
    password: 'Abc123',
  },
}

export const Good: Story = {
  args: {
    password: 'Abcdef1234',
  },
}

export const Strong: Story = {
  args: {
    password: 'MyStr0ng!Pass#2024',
  },
}

export const WithoutRequirements: Story = {
  args: {
    password: 'Abc123',
    showRequirements: false,
  },
}

export const Empty: Story = {
  render: () => (
    <div>
      <p style={{ marginBottom: '8px', fontSize: '14px' }}>
        Empty password (component renders nothing):
      </p>
      <div style={{ border: '1px dashed #ccc', padding: '8px', borderRadius: '4px', minHeight: '24px' }}>
        <PasswordStrengthIndicator password="" />
      </div>
      <p style={{ marginTop: '8px', fontSize: '12px', color: '#888' }}>
        The dashed box above is empty because no password was provided.
      </p>
    </div>
  ),
}

export const AllStrengthLevels: Story = {
  render: () => (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
      <div>
        <p style={{ marginBottom: '8px', fontSize: '14px', fontWeight: 500 }}>Weak</p>
        <PasswordStrengthIndicator password="ab" />
      </div>
      <div>
        <p style={{ marginBottom: '8px', fontSize: '14px', fontWeight: 500 }}>Fair</p>
        <PasswordStrengthIndicator password="Abc123" />
      </div>
      <div>
        <p style={{ marginBottom: '8px', fontSize: '14px', fontWeight: 500 }}>Good</p>
        <PasswordStrengthIndicator password="Abcdef1234" />
      </div>
      <div>
        <p style={{ marginBottom: '8px', fontSize: '14px', fontWeight: 500 }}>Strong</p>
        <PasswordStrengthIndicator password="MyStr0ng!Pass#2024" />
      </div>
    </div>
  ),
}
