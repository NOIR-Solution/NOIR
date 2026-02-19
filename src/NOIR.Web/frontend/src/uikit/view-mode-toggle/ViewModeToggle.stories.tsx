import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import { List, GitBranch, LayoutGrid, Table2, Kanban } from 'lucide-react'
import { ViewModeToggle, type ViewModeOption } from './ViewModeToggle'

const meta = {
  title: 'UIKit/ViewModeToggle',
  component: ViewModeToggle,
  tags: ['autodocs'],
  parameters: {
    layout: 'centered',
  },
} satisfies Meta<typeof ViewModeToggle>

export default meta
type Story = StoryObj<typeof meta>

const listTreeOptions: ViewModeOption[] = [
  { value: 'list', label: 'List', icon: List, ariaLabel: 'Table view' },
  { value: 'tree', label: 'Tree', icon: GitBranch, ariaLabel: 'Tree view' },
]

const listGridOptions: ViewModeOption[] = [
  { value: 'list', label: 'List', icon: List, ariaLabel: 'List view' },
  { value: 'grid', label: 'Grid', icon: LayoutGrid, ariaLabel: 'Grid view' },
]

const threeOptions: ViewModeOption[] = [
  { value: 'table', label: 'Table', icon: Table2, ariaLabel: 'Table view' },
  { value: 'grid', label: 'Grid', icon: LayoutGrid, ariaLabel: 'Grid view' },
  { value: 'kanban', label: 'Kanban', icon: Kanban, ariaLabel: 'Kanban view' },
]

export const ListTree: Story = {
  render: () => {
    const [value, setValue] = useState('list')
    return (
      <ViewModeToggle
        options={listTreeOptions}
        value={value}
        onChange={setValue}
      />
    )
  },
}

export const ListGrid: Story = {
  render: () => {
    const [value, setValue] = useState('list')
    return (
      <ViewModeToggle
        options={listGridOptions}
        value={value}
        onChange={setValue}
      />
    )
  },
}

export const ThreeOptions: Story = {
  render: () => {
    const [value, setValue] = useState('table')
    return (
      <ViewModeToggle
        options={threeOptions}
        value={value}
        onChange={setValue}
      />
    )
  },
}

export const TreeActive: Story = {
  render: () => {
    const [value, setValue] = useState('tree')
    return (
      <ViewModeToggle
        options={listTreeOptions}
        value={value}
        onChange={setValue}
      />
    )
  },
}

export const GridActive: Story = {
  render: () => {
    const [value, setValue] = useState('grid')
    return (
      <ViewModeToggle
        options={listGridOptions}
        value={value}
        onChange={setValue}
      />
    )
  },
}
