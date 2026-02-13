import type { Meta, StoryObj } from 'storybook'
import { RadioGroup, RadioGroupItem } from './RadioGroup'

const meta = {
  title: 'UIKit/RadioGroup',
  component: RadioGroup,
  tags: ['autodocs'],
} satisfies Meta<typeof RadioGroup>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => (
    <RadioGroup defaultValue="comfortable">
      <div className="flex items-center space-x-2">
        <RadioGroupItem value="default" id="r1" />
        <label htmlFor="r1" className="text-sm font-medium leading-none cursor-pointer">
          Default
        </label>
      </div>
      <div className="flex items-center space-x-2">
        <RadioGroupItem value="comfortable" id="r2" />
        <label htmlFor="r2" className="text-sm font-medium leading-none cursor-pointer">
          Comfortable
        </label>
      </div>
      <div className="flex items-center space-x-2">
        <RadioGroupItem value="compact" id="r3" />
        <label htmlFor="r3" className="text-sm font-medium leading-none cursor-pointer">
          Compact
        </label>
      </div>
    </RadioGroup>
  ),
}

export const WithDescriptions: Story = {
  render: () => (
    <RadioGroup defaultValue="card">
      <div className="flex items-start space-x-3">
        <RadioGroupItem value="card" id="card" className="mt-1" />
        <div>
          <label htmlFor="card" className="text-sm font-medium leading-none cursor-pointer">
            Card
          </label>
          <p className="text-sm text-muted-foreground mt-1">
            Pay with your credit or debit card.
          </p>
        </div>
      </div>
      <div className="flex items-start space-x-3">
        <RadioGroupItem value="paypal" id="paypal" className="mt-1" />
        <div>
          <label htmlFor="paypal" className="text-sm font-medium leading-none cursor-pointer">
            PayPal
          </label>
          <p className="text-sm text-muted-foreground mt-1">
            Pay using your PayPal account.
          </p>
        </div>
      </div>
      <div className="flex items-start space-x-3">
        <RadioGroupItem value="apple" id="apple" className="mt-1" />
        <div>
          <label htmlFor="apple" className="text-sm font-medium leading-none cursor-pointer">
            Apple Pay
          </label>
          <p className="text-sm text-muted-foreground mt-1">
            Pay with Apple Pay on supported devices.
          </p>
        </div>
      </div>
    </RadioGroup>
  ),
}

export const Disabled: Story = {
  render: () => (
    <RadioGroup defaultValue="option1" disabled>
      <div className="flex items-center space-x-2">
        <RadioGroupItem value="option1" id="d1" />
        <label htmlFor="d1" className="text-sm font-medium leading-none opacity-50">
          Option 1
        </label>
      </div>
      <div className="flex items-center space-x-2">
        <RadioGroupItem value="option2" id="d2" />
        <label htmlFor="d2" className="text-sm font-medium leading-none opacity-50">
          Option 2
        </label>
      </div>
      <div className="flex items-center space-x-2">
        <RadioGroupItem value="option3" id="d3" />
        <label htmlFor="d3" className="text-sm font-medium leading-none opacity-50">
          Option 3
        </label>
      </div>
    </RadioGroup>
  ),
}

export const Horizontal: Story = {
  render: () => (
    <RadioGroup defaultValue="sm" className="flex gap-4">
      <div className="flex items-center space-x-2">
        <RadioGroupItem value="sm" id="sm" />
        <label htmlFor="sm" className="text-sm font-medium leading-none cursor-pointer">
          Small
        </label>
      </div>
      <div className="flex items-center space-x-2">
        <RadioGroupItem value="md" id="md" />
        <label htmlFor="md" className="text-sm font-medium leading-none cursor-pointer">
          Medium
        </label>
      </div>
      <div className="flex items-center space-x-2">
        <RadioGroupItem value="lg" id="lg" />
        <label htmlFor="lg" className="text-sm font-medium leading-none cursor-pointer">
          Large
        </label>
      </div>
    </RadioGroup>
  ),
}
