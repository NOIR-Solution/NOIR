import type { Meta, StoryObj } from 'storybook'
import { MemoryRouter } from 'react-router-dom'
import { I18nextProvider } from 'react-i18next'
import i18n from 'i18next'
import { Package, Settings } from 'lucide-react'
import { Breadcrumb, type BreadcrumbItem } from './Breadcrumb'

// Minimal i18n instance for Storybook
const i18nInstance = i18n.createInstance()
i18nInstance.init({
  lng: 'en',
  resources: {
    en: {
      common: {
        labels: {
          breadcrumb: 'Breadcrumb',
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
      <MemoryRouter>
        <Story />
      </MemoryRouter>
    </I18nextProvider>
  )
}

const meta = {
  title: 'UIKit/Breadcrumb',
  component: Breadcrumb,
  tags: ['autodocs'],
  decorators: [withProviders],
} satisfies Meta<typeof Breadcrumb>

export default meta
type Story = StoryObj<typeof meta>

const defaultItems: BreadcrumbItem[] = [
  { label: 'Home', href: '/' },
  { label: 'Products', href: '/products' },
  { label: 'Electronics', href: '/products/electronics' },
  { label: 'Laptops' },
]

export const Default: Story = {
  args: {
    items: defaultItems,
  },
}

export const TwoLevels: Story = {
  args: {
    items: [
      { label: 'Dashboard', href: '/dashboard' },
      { label: 'Settings' },
    ],
  },
}

export const SingleItem: Story = {
  args: {
    items: [{ label: 'Home' }],
  },
}

export const WithCustomIcons: Story = {
  args: {
    items: [
      { label: 'Home', href: '/' },
      { label: 'Products', href: '/products', icon: Package },
      { label: 'Settings', icon: Settings },
    ],
  },
}

export const NoHomeIcon: Story = {
  args: {
    items: defaultItems,
    showHomeIcon: false,
  },
}

export const LongLabels: Story = {
  args: {
    items: [
      { label: 'Home', href: '/' },
      { label: 'Product Categories', href: '/categories' },
      { label: 'Electronics & Computers', href: '/categories/electronics' },
      { label: 'A Very Long Product Name That Should Truncate' },
    ],
  },
}
