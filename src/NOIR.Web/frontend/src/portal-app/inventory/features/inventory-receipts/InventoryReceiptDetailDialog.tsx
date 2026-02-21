import { useTranslation } from 'react-i18next'
import { Package } from 'lucide-react'
import {
  Badge,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaHeader,
  CredenzaTitle,
  Separator,
  Skeleton,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@uikit'
import { useInventoryReceiptQuery } from '@/portal-app/inventory/queries'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import { formatCurrency } from '@/lib/utils/currency'
import { RECEIPT_TYPE_CONFIG, RECEIPT_STATUS_CONFIG } from './inventoryReceiptConfig'

interface InventoryReceiptDetailDialogProps {
  receiptId: string | undefined
  open: boolean
  onOpenChange: (open: boolean) => void
}

export const InventoryReceiptDetailDialog = ({
  receiptId,
  open,
  onOpenChange,
}: InventoryReceiptDetailDialogProps) => {
  const { t } = useTranslation('common')
  const { formatDateTime } = useRegionalSettings()
  const { data: receipt, isLoading } = useInventoryReceiptQuery(receiptId)

  const typeConfig = receipt ? RECEIPT_TYPE_CONFIG[receipt.type] : null
  const statusConfig = receipt ? RECEIPT_STATUS_CONFIG[receipt.status] : null

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[700px]">
        <CredenzaHeader>
          <CredenzaTitle>{t('inventory.receiptDetail', 'Receipt Detail')}</CredenzaTitle>
          <CredenzaDescription>
            {t('inventory.receiptDetailDescription', 'View receipt items and status details.')}
          </CredenzaDescription>
        </CredenzaHeader>

        <CredenzaBody>
          {isLoading ? (
            <div className="space-y-4">
              <div className="flex items-center justify-between">
                <Skeleton className="h-6 w-48" />
                <Skeleton className="h-6 w-24 rounded-full" />
              </div>
              <div className="grid grid-cols-2 gap-4">
                {[...Array(4)].map((_, i) => (
                  <div key={i} className="space-y-1">
                    <Skeleton className="h-3 w-16" />
                    <Skeleton className="h-4 w-28" />
                  </div>
                ))}
              </div>
              <Skeleton className="h-px w-full" />
              {[...Array(3)].map((_, i) => (
                <Skeleton key={i} className="h-10 w-full" />
              ))}
            </div>
          ) : receipt ? (
            <div className="space-y-5">
              {/* Header: Receipt Number + Badges */}
              <div className="flex items-center justify-between flex-wrap gap-2">
                <span className="font-mono font-semibold text-base">{receipt.receiptNumber}</span>
                <div className="flex items-center gap-2">
                  {typeConfig && (
                    <Badge variant="outline" className={typeConfig.color}>
                      <typeConfig.icon className="h-3 w-3 mr-1.5" />
                      {t(`inventory.type.${typeConfig.label}`)}
                    </Badge>
                  )}
                  {statusConfig && (
                    <Badge variant="outline" className={statusConfig.color}>
                      <statusConfig.icon className="h-3 w-3 mr-1.5" />
                      {t(`inventory.status.${statusConfig.label}`)}
                    </Badge>
                  )}
                </div>
              </div>

              {/* Info Grid */}
              <div className="grid grid-cols-2 gap-4 text-sm">
                <div>
                  <span className="text-muted-foreground">{t('labels.date', 'Date')}</span>
                  <p className="font-medium mt-0.5">{formatDateTime(receipt.createdAt)}</p>
                </div>
                <div>
                  <span className="text-muted-foreground">{t('inventory.totalCost', 'Total Cost')}</span>
                  <p className="font-medium mt-0.5">{formatCurrency(receipt.totalCost)}</p>
                </div>
                {receipt.confirmedAt && (
                  <div>
                    <span className="text-muted-foreground">{t('inventory.confirmedAt', 'Confirmed')}</span>
                    <p className="font-medium mt-0.5">{formatDateTime(receipt.confirmedAt)}</p>
                  </div>
                )}
                {receipt.cancelledAt && (
                  <div>
                    <span className="text-muted-foreground">{t('inventory.cancelledAt', 'Cancelled')}</span>
                    <p className="font-medium mt-0.5">{formatDateTime(receipt.cancelledAt)}</p>
                  </div>
                )}
              </div>

              {/* Notes */}
              {receipt.notes && (
                <div className="bg-muted/50 border border-border/50 rounded-lg p-3">
                  <span className="text-xs font-medium text-muted-foreground">{t('labels.notes', 'Notes')}</span>
                  <p className="text-sm mt-1">{receipt.notes}</p>
                </div>
              )}

              {/* Cancellation Reason */}
              {receipt.cancellationReason && (
                <div className="bg-red-50 dark:bg-red-950/20 border border-red-200 dark:border-red-900 rounded-lg p-3">
                  <span className="text-xs font-medium text-red-600 dark:text-red-400">{t('inventory.cancellationReason', 'Cancellation Reason')}</span>
                  <p className="text-sm mt-1">{receipt.cancellationReason}</p>
                </div>
              )}

              <Separator />

              {/* Items Table */}
              <div>
                <span className="text-sm font-medium mb-3 block">
                  {t('inventory.receiptItems', 'Receipt Items')} ({receipt.items.length})
                </span>
                <div className="rounded-xl border border-border/50 overflow-hidden">
                  <Table>
                    <TableHeader>
                      <TableRow>
                        <TableHead>{t('inventory.productName', 'Product')}</TableHead>
                        <TableHead>{t('inventory.variant', 'Variant')}</TableHead>
                        <TableHead>{t('inventory.sku', 'SKU')}</TableHead>
                        <TableHead className="text-right">{t('inventory.quantity', 'Qty')}</TableHead>
                        <TableHead className="text-right">{t('inventory.unitCost', 'Unit Cost')}</TableHead>
                        <TableHead className="text-right">{t('inventory.lineTotal', 'Total')}</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {receipt.items.length === 0 ? (
                        <TableRow>
                          <TableCell colSpan={6} className="text-center py-8 text-muted-foreground">
                            <Package className="h-8 w-8 mx-auto mb-2 opacity-50" />
                            {t('inventory.noItems', 'No items')}
                          </TableCell>
                        </TableRow>
                      ) : (
                        receipt.items.map((item) => (
                          <TableRow key={item.id}>
                            <TableCell className="font-medium max-w-[160px] truncate" title={item.productName}>
                              {item.productName}
                            </TableCell>
                            <TableCell className="text-sm text-muted-foreground">
                              {item.variantName}
                            </TableCell>
                            <TableCell>
                              {item.sku ? (
                                <span className="font-mono text-xs bg-muted px-1.5 py-0.5 rounded">{item.sku}</span>
                              ) : (
                                <span className="text-muted-foreground">-</span>
                              )}
                            </TableCell>
                            <TableCell className="text-right font-medium">{item.quantity}</TableCell>
                            <TableCell className="text-right">{formatCurrency(item.unitCost)}</TableCell>
                            <TableCell className="text-right font-medium">{formatCurrency(item.lineTotal)}</TableCell>
                          </TableRow>
                        ))
                      )}
                    </TableBody>
                  </Table>
                </div>

                {/* Summary Row */}
                {receipt.items.length > 0 && (
                  <div className="flex items-center justify-between mt-3 px-2 text-sm">
                    <span className="text-muted-foreground">
                      {t('inventory.totalItems', '{{count}} items, {{qty}} total quantity', {
                        count: receipt.items.length,
                        qty: receipt.totalQuantity,
                      })}
                    </span>
                    <span className="font-semibold">{formatCurrency(receipt.totalCost)}</span>
                  </div>
                )}
              </div>
            </div>
          ) : (
            <div className="py-8 text-center text-muted-foreground">
              {t('inventory.receiptNotFound', 'Receipt not found')}
            </div>
          )}
        </CredenzaBody>
      </CredenzaContent>
    </Credenza>
  )
}
