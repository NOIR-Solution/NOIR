import type { Table } from '@tanstack/react-table'
import { Pagination } from '../pagination/Pagination'

interface DataTablePaginationProps<TData> {
  table: Table<TData>
  /** Override total item count (falls back to table.getRowCount()) */
  totalItems?: number
  showPageSizeSelector?: boolean
  pageSizeOptions?: number[]
  className?: string
}

/**
 * Bridges TanStack Table's pagination state to the existing UIKit Pagination component.
 * Place below <DataTable /> in page layouts.
 */
export const DataTablePagination = <TData,>({
  table,
  totalItems,
  showPageSizeSelector = true,
  pageSizeOptions,
  className,
}: DataTablePaginationProps<TData>) => {
  const { pageIndex, pageSize } = table.getState().pagination
  const totalCount = totalItems ?? table.getRowCount()
  const totalPages = table.getPageCount()

  if (totalPages <= 0) return null

  return (
    <Pagination
      currentPage={pageIndex + 1}
      totalPages={totalPages}
      totalItems={totalCount}
      pageSize={pageSize}
      onPageChange={(page) => table.setPageIndex(page - 1)}
      onPageSizeChange={(size) => {
        table.setPageSize(size)
        table.setPageIndex(0)
      }}
      showPageSizeSelector={showPageSizeSelector}
      pageSizeOptions={pageSizeOptions}
      className={className}
    />
  )
}
