import type { Meta, StoryObj } from 'storybook'
import { I18nextProvider } from 'react-i18next'
import i18n from 'i18next'
import { CategoryTreeView, type TreeCategory } from './CategoryTreeView'
import { action } from 'storybook/actions'

// Minimal i18n instance for Storybook
const i18nInstance = i18n.createInstance()
i18nInstance.init({
  lng: 'en',
  resources: {
    en: {
      common: {
        nav: { collapse: 'Collapse', expand: 'Expand' },
        labels: {
          children: 'children',
          edit: 'Edit',
          delete: 'Delete',
        },
        buttons: {
          create: 'Create',
          expandAll: 'Expand All',
          collapseAll: 'Collapse All',
        },
      },
    },
  },
  defaultNS: 'common',
  interpolation: { escapeValue: false },
})

const sampleCategories: TreeCategory[] = [
  {
    id: '1',
    name: 'Electronics',
    slug: 'electronics',
    description: 'Electronic devices and accessories',
    sortOrder: 0,
    parentId: null,
    childCount: 2,
    itemCount: 150,
  },
  {
    id: '2',
    name: 'Smartphones',
    slug: 'smartphones',
    description: 'Mobile phones and accessories',
    sortOrder: 0,
    parentId: '1',
    childCount: 0,
    itemCount: 75,
  },
  {
    id: '3',
    name: 'Laptops',
    slug: 'laptops',
    description: 'Notebooks and ultrabooks',
    sortOrder: 1,
    parentId: '1',
    childCount: 0,
    itemCount: 45,
  },
  {
    id: '4',
    name: 'Clothing',
    slug: 'clothing',
    description: 'Apparel and fashion',
    sortOrder: 1,
    parentId: null,
    childCount: 3,
    itemCount: 300,
  },
  {
    id: '5',
    name: "Men's Wear",
    slug: 'mens-wear',
    sortOrder: 0,
    parentId: '4',
    childCount: 1,
    itemCount: 120,
  },
  {
    id: '6',
    name: "Women's Wear",
    slug: 'womens-wear',
    sortOrder: 1,
    parentId: '4',
    childCount: 0,
    itemCount: 150,
  },
  {
    id: '7',
    name: "Kids' Wear",
    slug: 'kids-wear',
    sortOrder: 2,
    parentId: '4',
    childCount: 0,
    itemCount: 30,
  },
  {
    id: '8',
    name: 'T-Shirts',
    slug: 't-shirts',
    sortOrder: 0,
    parentId: '5',
    childCount: 0,
    itemCount: 60,
  },
  {
    id: '9',
    name: 'Home & Garden',
    slug: 'home-garden',
    description: 'Home improvement and garden supplies',
    sortOrder: 2,
    parentId: null,
    childCount: 0,
    itemCount: 80,
  },
]

function withI18n(Story: React.ComponentType) {
  return (
    <I18nextProvider i18n={i18nInstance}>
      <div style={{ maxWidth: 700, padding: 16 }}>
        <Story />
      </div>
    </I18nextProvider>
  )
}

const meta = {
  title: 'UIKit/CategoryTreeView',
  component: CategoryTreeView,
  tags: ['autodocs'],
  decorators: [withI18n],
} satisfies Meta<typeof CategoryTreeView>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  args: {
    categories: sampleCategories,
    onEdit: action('edit'),
    onDelete: action('delete'),
    itemCountLabel: 'products',
  },
}

export const DragAndDrop: Story = {
  args: {
    categories: sampleCategories,
    onEdit: action('edit'),
    onDelete: action('delete'),
    onReorder: action('reorder'),
    itemCountLabel: 'products',
  },
}

export const BlogCategories: Story = {
  args: {
    categories: [
      {
        id: '1',
        name: 'Technology',
        slug: 'technology',
        description: 'Tech news and tutorials',
        sortOrder: 0,
        parentId: null,
        childCount: 2,
        itemCount: 42,
      },
      {
        id: '2',
        name: 'Web Development',
        slug: 'web-development',
        sortOrder: 0,
        parentId: '1',
        childCount: 0,
        itemCount: 25,
      },
      {
        id: '3',
        name: 'Mobile Development',
        slug: 'mobile-development',
        sortOrder: 1,
        parentId: '1',
        childCount: 0,
        itemCount: 17,
      },
      {
        id: '4',
        name: 'Lifestyle',
        slug: 'lifestyle',
        sortOrder: 1,
        parentId: null,
        childCount: 0,
        itemCount: 15,
      },
    ],
    onEdit: action('edit'),
    onDelete: action('delete'),
    itemCountLabel: 'posts',
  },
}

export const Empty: Story = {
  args: {
    categories: [],
    emptyMessage: 'No categories yet',
    emptyDescription: 'Create your first category to get started.',
    onCreateClick: action('create'),
  },
}

export const Loading: Story = {
  args: {
    categories: [],
    loading: true,
  },
}

export const ReadOnly: Story = {
  args: {
    categories: sampleCategories,
    canEdit: false,
    canDelete: false,
    itemCountLabel: 'products',
  },
}

export const FlatList: Story = {
  args: {
    categories: [
      { id: '1', name: 'Category A', slug: 'cat-a', sortOrder: 0, parentId: null, childCount: 0, itemCount: 10 },
      { id: '2', name: 'Category B', slug: 'cat-b', sortOrder: 1, parentId: null, childCount: 0, itemCount: 20 },
      { id: '3', name: 'Category C', slug: 'cat-c', sortOrder: 2, parentId: null, childCount: 0, itemCount: 5 },
      { id: '4', name: 'Category D', slug: 'cat-d', sortOrder: 3, parentId: null, childCount: 0, itemCount: 0 },
    ],
    onEdit: action('edit'),
    onDelete: action('delete'),
    itemCountLabel: 'items',
  },
}
