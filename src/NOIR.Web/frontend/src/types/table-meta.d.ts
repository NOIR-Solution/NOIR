import '@tanstack/react-table'

declare module '@tanstack/react-table' {
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  interface ColumnMeta<TData extends object, TValue> {
    /** Used by DataTableToolbar to render the right filter UI */
    filterType?: 'text' | 'select' | 'date-range' | 'number-range'
    /** Text alignment within the cell */
    align?: 'left' | 'center' | 'right'
    /** Permission key — column is hidden when user lacks this permission */
    permission?: string
    /** Extra className applied to the <th> element */
    headerClassName?: string
    /** Extra className applied to every <td> in this column */
    cellClassName?: string
    /** Whether this column should be included in CSV/Excel exports */
    enableExport?: boolean
  }

  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  interface TableMeta<TData extends object> {
    locale?: string
    permissions?: Set<string>
    onRowAction?: (action: string, row: TData) => void
  }
}
