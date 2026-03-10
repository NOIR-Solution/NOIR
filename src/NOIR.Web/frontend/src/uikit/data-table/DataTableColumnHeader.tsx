import { ArrowDown, ArrowUp, ChevronsUpDown } from 'lucide-react'
import type { Column } from '@tanstack/react-table'
import { cn } from '@/lib/utils'
import { Button } from '../button/Button'

interface DataTableColumnHeaderProps<TData, TValue> {
  column: Column<TData, TValue>
  title: string
  className?: string
}

export const DataTableColumnHeader = <TData, TValue>({
  column,
  title,
  className,
}: DataTableColumnHeaderProps<TData, TValue>) => {
  if (!column.getCanSort()) {
    return <span className={cn('text-sm font-medium text-muted-foreground', className)}>{title}</span>
  }

  return (
    <Button
      variant="ghost"
      size="sm"
      className={cn(
        '-ml-3 h-8 cursor-pointer select-none text-muted-foreground hover:text-foreground',
        column.getIsSorted() && 'text-foreground',
        className,
      )}
      onClick={() => column.toggleSorting(column.getIsSorted() === 'asc')}
      aria-label={
        column.getIsSorted() === 'asc'
          ? `Sort ${title} descending`
          : column.getIsSorted() === 'desc'
            ? `Clear sort for ${title}`
            : `Sort ${title} ascending`
      }
    >
      {title}
      {column.getIsSorted() === 'asc' ? (
        <ArrowUp className="ml-1.5 h-3.5 w-3.5" />
      ) : column.getIsSorted() === 'desc' ? (
        <ArrowDown className="ml-1.5 h-3.5 w-3.5" />
      ) : (
        <ChevronsUpDown className="ml-1.5 h-3.5 w-3.5 opacity-40" />
      )}
    </Button>
  )
}
