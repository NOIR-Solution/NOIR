import type { Meta, StoryObj } from 'storybook'
import { I18nextProvider } from 'react-i18next'
import i18n from 'i18next'
import { CountdownTimer } from '@/components/forgot-password/CountdownTimer'

// Minimal i18n instance for Storybook
const i18nInstance = i18n.createInstance()
i18nInstance.init({
  lng: 'en',
  resources: {
    en: {
      auth: {
        forgotPassword: {
          verify: {
            resendIn: 'Resend in {{time}}',
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
    <Story />
  </I18nextProvider>
)

const meta = {
  title: 'UIKit/CountdownTimer',
  component: CountdownTimer,
  tags: ['autodocs'],
  decorators: [withI18n],
} satisfies Meta<typeof CountdownTimer>

export default meta
type Story = StoryObj<typeof meta>

export const CountingDown: Story = {
  render: () => {
    // Target 60 seconds from now
    const target = new Date(Date.now() + 60 * 1000)
    return (
      <div style={{ padding: '16px' }}>
        <p style={{ marginBottom: '8px', fontSize: '14px' }}>Countdown (60 seconds):</p>
        <CountdownTimer
          targetTime={target}
          onComplete={() => console.log('Countdown complete!')}
        />
      </div>
    )
  },
}

export const ShortCountdown: Story = {
  render: () => {
    const target = new Date(Date.now() + 10 * 1000)
    return (
      <div style={{ padding: '16px' }}>
        <p style={{ marginBottom: '8px', fontSize: '14px' }}>Short countdown (10 seconds):</p>
        <CountdownTimer
          targetTime={target}
          onComplete={() => console.log('Countdown complete!')}
        />
      </div>
    )
  },
}

export const LongCountdown: Story = {
  render: () => {
    const target = new Date(Date.now() + 5 * 60 * 1000)
    return (
      <div style={{ padding: '16px' }}>
        <p style={{ marginBottom: '8px', fontSize: '14px' }}>Long countdown (5 minutes):</p>
        <CountdownTimer
          targetTime={target}
          onComplete={() => console.log('Countdown complete!')}
        />
      </div>
    )
  },
}

export const Expired: Story = {
  render: () => {
    // Target in the past = already expired, component returns null
    const target = new Date(Date.now() - 1000)
    return (
      <div style={{ padding: '16px' }}>
        <p style={{ marginBottom: '8px', fontSize: '14px' }}>
          Expired timer (renders nothing when countdown reaches 0):
        </p>
        <div style={{ border: '1px dashed #ccc', padding: '8px', borderRadius: '4px', minHeight: '24px' }}>
          <CountdownTimer targetTime={target} />
        </div>
        <p style={{ marginTop: '8px', fontSize: '12px', color: '#888' }}>
          The dashed box above is empty because the timer has expired.
        </p>
      </div>
    )
  },
}
