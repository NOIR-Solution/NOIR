import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import { Combobox } from './Combobox'
import type { ComboboxOption } from './Combobox'

const meta = {
  title: 'UIKit/Combobox',
  component: Combobox,
  tags: ['autodocs'],
  argTypes: {
    disabled: { control: 'boolean' },
  },
} satisfies Meta<typeof Combobox>

export default meta
type Story = StoryObj<typeof meta>

const fruitOptions: ComboboxOption[] = [
  { value: 'apple', label: 'Apple' },
  { value: 'banana', label: 'Banana' },
  { value: 'cherry', label: 'Cherry' },
  { value: 'grape', label: 'Grape' },
  { value: 'mango', label: 'Mango' },
  { value: 'orange', label: 'Orange' },
  { value: 'peach', label: 'Peach' },
  { value: 'strawberry', label: 'Strawberry' },
]

export const Default: Story = {
  render: () => {
    const [value, setValue] = useState('')
    return (
      <div style={{ width: 300 }}>
        <Combobox
          options={fruitOptions}
          value={value}
          onValueChange={setValue}
          placeholder="Select a fruit..."
          searchPlaceholder="Search fruits..."
        />
      </div>
    )
  },
}

export const WithDescriptions: Story = {
  render: () => {
    const [value, setValue] = useState('')
    const options: ComboboxOption[] = [
      { value: 'react', label: 'React', description: 'A JavaScript library for building user interfaces' },
      { value: 'vue', label: 'Vue', description: 'The Progressive JavaScript Framework' },
      { value: 'angular', label: 'Angular', description: 'Platform for building mobile and desktop web apps' },
      { value: 'svelte', label: 'Svelte', description: 'Cybernetically enhanced web apps' },
      { value: 'solid', label: 'SolidJS', description: 'Simple and performant reactivity for building user interfaces' },
    ]
    return (
      <div style={{ width: 350 }}>
        <Combobox
          options={options}
          value={value}
          onValueChange={setValue}
          placeholder="Select a framework..."
          searchPlaceholder="Search frameworks..."
        />
      </div>
    )
  },
}

export const WithCountLabel: Story = {
  render: () => {
    const [value, setValue] = useState('')
    return (
      <div style={{ width: 300 }}>
        <Combobox
          options={fruitOptions}
          value={value}
          onValueChange={setValue}
          placeholder="Select a fruit..."
          countLabel="fruits"
        />
      </div>
    )
  },
}

export const Preselected: Story = {
  render: () => {
    const [value, setValue] = useState('cherry')
    return (
      <div style={{ width: 300 }}>
        <Combobox
          options={fruitOptions}
          value={value}
          onValueChange={setValue}
          placeholder="Select a fruit..."
        />
      </div>
    )
  },
}

export const Disabled: Story = {
  render: () => (
    <div style={{ width: 300 }}>
      <Combobox
        options={fruitOptions}
        value="apple"
        placeholder="Select a fruit..."
        disabled
      />
    </div>
  ),
}

export const EmptyOptions: Story = {
  render: () => {
    const [value, setValue] = useState('')
    return (
      <div style={{ width: 300 }}>
        <Combobox
          options={[]}
          value={value}
          onValueChange={setValue}
          placeholder="Select an option..."
          emptyText="No options available."
        />
      </div>
    )
  },
}

export const ManyOptions: Story = {
  render: () => {
    const [value, setValue] = useState('')
    const countries: ComboboxOption[] = Array.from({ length: 50 }, (_, i) => ({
      value: `country-${i + 1}`,
      label: `Country ${i + 1}`,
      description: `Description for country ${i + 1}`,
    }))
    return (
      <div style={{ width: 300 }}>
        <Combobox
          options={countries}
          value={value}
          onValueChange={setValue}
          placeholder="Select a country..."
          searchPlaceholder="Search countries..."
          countLabel="countries"
        />
      </div>
    )
  },
}
