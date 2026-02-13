import { useState } from 'react'
import type { Meta, StoryObj } from 'storybook'
import { I18nextProvider } from 'react-i18next'
import i18n from 'i18next'
import { Pagination } from './Pagination'

// Minimal i18n instance for Storybook
const i18nInstance = i18n.createInstance()
i18nInstance.init({
  lng: 'en',
  resources: {
    en: {
      common: {
        labels: {
          showingOfItems: 'Showing {{from}} to {{to}} of {{total}} items',
          rowsPerPage: 'Rows per page:',
          goToFirstPage: 'Go to first page',
          goToPreviousPage: 'Go to previous page',
          goToNextPage: 'Go to next page',
          goToLastPage: 'Go to last page',
        },
      },
    },
  },
})

const meta = {
  title: 'UIKit/Pagination',
  component: Pagination,
  tags: ['autodocs'],
  decorators: [
    (Story) => (
      <I18nextProvider i18n={i18nInstance}>
        <Story />
      </I18nextProvider>
    ),
  ],
  parameters: {
    layout: 'padded',
  },
} satisfies Meta<typeof Pagination>

export default meta
type Story = StoryObj<typeof meta>

export const Default: Story = {
  render: () => {
    const [page, setPage] = useState(1)
    return (
      <Pagination
        currentPage={page}
        totalPages={10}
        totalItems={100}
        pageSize={10}
        onPageChange={setPage}
      />
    )
  },
}

export const WithPageSizeSelector: Story = {
  render: () => {
    const [page, setPage] = useState(1)
    const [pageSize, setPageSize] = useState(10)
    const totalItems = 250
    const totalPages = Math.ceil(totalItems / pageSize)
    return (
      <Pagination
        currentPage={page}
        totalPages={totalPages}
        totalItems={totalItems}
        pageSize={pageSize}
        onPageChange={setPage}
        onPageSizeChange={(size) => {
          setPageSize(size)
          setPage(1)
        }}
        showPageSizeSelector
      />
    )
  },
}

export const FewPages: Story = {
  render: () => {
    const [page, setPage] = useState(1)
    return (
      <Pagination
        currentPage={page}
        totalPages={3}
        totalItems={25}
        pageSize={10}
        onPageChange={setPage}
      />
    )
  },
}

export const ManyPages: Story = {
  render: () => {
    const [page, setPage] = useState(5)
    return (
      <Pagination
        currentPage={page}
        totalPages={50}
        totalItems={500}
        pageSize={10}
        onPageChange={setPage}
      />
    )
  },
}

export const FirstPage: Story = {
  render: () => {
    const [page, setPage] = useState(1)
    return (
      <Pagination
        currentPage={page}
        totalPages={20}
        totalItems={200}
        pageSize={10}
        onPageChange={setPage}
      />
    )
  },
}

export const LastPage: Story = {
  render: () => {
    const [page, setPage] = useState(20)
    return (
      <Pagination
        currentPage={page}
        totalPages={20}
        totalItems={200}
        pageSize={10}
        onPageChange={setPage}
      />
    )
  },
}

export const SinglePage: Story = {
  render: () => (
    <Pagination
      currentPage={1}
      totalPages={1}
      totalItems={5}
      pageSize={10}
      onPageChange={() => {}}
    />
  ),
}

export const NoPageSizeSelector: Story = {
  render: () => {
    const [page, setPage] = useState(1)
    return (
      <Pagination
        currentPage={page}
        totalPages={10}
        totalItems={100}
        pageSize={10}
        onPageChange={setPage}
        showPageSizeSelector={false}
      />
    )
  },
}
