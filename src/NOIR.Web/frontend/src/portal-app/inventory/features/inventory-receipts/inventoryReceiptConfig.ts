import { ArrowDownToLine, ArrowUpFromLine, CheckCircle2, FileText, XCircle } from 'lucide-react'
import type { InventoryReceiptType, InventoryReceiptStatus } from '@/types/inventory'

export const RECEIPT_TYPE_CONFIG: Record<
  InventoryReceiptType,
  { color: string; icon: typeof ArrowDownToLine; label: string }
> = {
  StockIn: {
    color: 'bg-green-100 text-green-800 border-green-200 dark:bg-green-900/30 dark:text-green-400 dark:border-green-800',
    icon: ArrowDownToLine,
    label: 'stockIn',
  },
  StockOut: {
    color: 'bg-orange-100 text-orange-800 border-orange-200 dark:bg-orange-900/30 dark:text-orange-400 dark:border-orange-800',
    icon: ArrowUpFromLine,
    label: 'stockOut',
  },
}

export const RECEIPT_STATUS_CONFIG: Record<
  InventoryReceiptStatus,
  { color: string; icon: typeof FileText; label: string }
> = {
  Draft: {
    color: 'bg-gray-100 text-gray-800 border-gray-200 dark:bg-gray-900/30 dark:text-gray-400 dark:border-gray-800',
    icon: FileText,
    label: 'draft',
  },
  Confirmed: {
    color: 'bg-blue-100 text-blue-800 border-blue-200 dark:bg-blue-900/30 dark:text-blue-400 dark:border-blue-800',
    icon: CheckCircle2,
    label: 'confirmed',
  },
  Cancelled: {
    color: 'bg-red-100 text-red-800 border-red-200 dark:bg-red-900/30 dark:text-red-400 dark:border-red-800',
    icon: XCircle,
    label: 'cancelled',
  },
}
