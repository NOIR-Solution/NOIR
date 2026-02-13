import type { Meta, StoryObj } from 'storybook'
import { I18nextProvider } from 'react-i18next'
import i18n from 'i18next'
import { ThemeProvider } from '@/contexts/ThemeContext'
import { ThemeToggle, ThemeToggleCompact } from './ThemeToggle'

// Minimal i18n instance for Storybook
const i18nInstance = i18n.createInstance()
i18nInstance.init({
  lng: 'en',
  resources: {
    en: {
      common: {
        labels: {
          lightMode: 'Light mode',
          darkMode: 'Dark mode',
        },
      },
    },
  },
  defaultNS: 'common',
  interpolation: { escapeValue: false },
})

function withProviders(Story: React.ComponentType) {
  return (
    <I18nextProvider i18n={i18nInstance}>
      <ThemeProvider defaultTheme="light">
        <div style={{ padding: 24 }}>
          <Story />
        </div>
      </ThemeProvider>
    </I18nextProvider>
  )
}

const meta = {
  title: 'UIKit/ThemeToggle',
  component: ThemeToggle,
  tags: ['autodocs'],
  decorators: [withProviders],
} satisfies Meta<typeof ThemeToggle>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {},
}

export const WithCustomClass: Story = {
  args: {
    className: 'w-64',
  },
}

export const Compact: Story = {
  render: () => <ThemeToggleCompact />,
}

export const CompactWithClass: Story = {
  render: () => <ThemeToggleCompact className="border-primary" />,
}

export const SideBySide: Story = {
  render: () => (
    <div style={{ display: 'flex', alignItems: 'center', gap: 24 }}>
      <ThemeToggle />
      <ThemeToggleCompact />
    </div>
  ),
}
