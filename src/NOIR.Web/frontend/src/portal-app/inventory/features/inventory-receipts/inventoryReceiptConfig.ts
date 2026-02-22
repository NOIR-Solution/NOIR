import { ArrowDownToLine, ArrowUpFromLine, CheckCircle2, FileText, XCircle } from 'lucide-react'
import type { InventoryReceiptType, InventoryReceiptStatus } from '@/types/inventory'
import { getStatusBadgeClasses } from '@/utils/statusBadge'

export const RECEIPT_TYPE_CONFIG: Record<
  InventoryReceiptType,
  { color: string; icon: typeof ArrowDownToLine; label: string }
> = {
  StockIn: {
    color: getStatusBadgeClasses('green'),
    icon: ArrowDownToLine,
    label: 'stockIn',
  },
  StockOut: {
    color: getStatusBadgeClasses('orange'),
    icon: ArrowUpFromLine,
    label: 'stockOut',
  },
}

export const RECEIPT_STATUS_CONFIG: Record<
  InventoryReceiptStatus,
  { color: string; icon: typeof FileText; label: string }
> = {
  Draft: {
    color: getStatusBadgeClasses('gray'),
    icon: FileText,
    label: 'draft',
  },
  Confirmed: {
    color: getStatusBadgeClasses('blue'),
    icon: CheckCircle2,
    label: 'confirmed',
  },
  Cancelled: {
    color: getStatusBadgeClasses('red'),
    icon: XCircle,
    label: 'cancelled',
  },
}
