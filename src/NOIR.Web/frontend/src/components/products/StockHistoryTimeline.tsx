/**
 * StockHistoryTimeline Component
 *
 * Displays a timeline of stock changes for a product variant.
 * Shows adjustments, sales, restocks, and reservations.
 */
import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { formatDistanceToNow } from 'date-fns'
import {
  Package,
  TrendingUp,
  ShoppingCart,
  RotateCcw,
  History,
  Lock,
  Unlock,
} from 'lucide-react'
import { ScrollArea } from '@/components/ui/scroll-area'
import { cn } from '@/lib/utils'

export type StockMovementType =
  | 'adjustment'
  | 'sale'
  | 'restock'
  | 'return'
  | 'reserved'
  | 'released'
  | 'initial'

export interface StockMovement {
  id: string
  type: StockMovementType
  quantity: number
  previousStock: number
  newStock: number
  reason?: string
  orderId?: string
  createdAt: string
  createdBy?: string
}

interface StockHistoryTimelineProps {
  movements: StockMovement[]
  currentStock: number
  variantName?: string
  maxHeight?: string
}

const movementConfig: Record<
  StockMovementType,
  {
    icon: typeof Package
    color: string
    bgColor: string
    labelKey: string
  }
> = {
  initial: {
    icon: Package,
    color: 'text-blue-600',
    bgColor: 'bg-blue-100 dark:bg-blue-900/30',
    labelKey: 'initial',
  },
  adjustment: {
    icon: History,
    color: 'text-purple-600',
    bgColor: 'bg-purple-100 dark:bg-purple-900/30',
    labelKey: 'adjustment',
  },
  sale: {
    icon: ShoppingCart,
    color: 'text-orange-600',
    bgColor: 'bg-orange-100 dark:bg-orange-900/30',
    labelKey: 'sale',
  },
  restock: {
    icon: TrendingUp,
    color: 'text-green-600',
    bgColor: 'bg-green-100 dark:bg-green-900/30',
    labelKey: 'restock',
  },
  return: {
    icon: RotateCcw,
    color: 'text-teal-600',
    bgColor: 'bg-teal-100 dark:bg-teal-900/30',
    labelKey: 'return',
  },
  reserved: {
    icon: Lock,
    color: 'text-amber-600',
    bgColor: 'bg-amber-100 dark:bg-amber-900/30',
    labelKey: 'reserved',
  },
  released: {
    icon: Unlock,
    color: 'text-cyan-600',
    bgColor: 'bg-cyan-100 dark:bg-cyan-900/30',
    labelKey: 'released',
  },
}

export function StockHistoryTimeline({
  movements,
  currentStock,
  variantName,
  maxHeight = '400px',
}: StockHistoryTimelineProps) {
  const { t } = useTranslation()

  // Sort movements by date (newest first)
  const sortedMovements = useMemo(() => {
    return [...movements].sort(
      (a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime()
    )
  }, [movements])

  if (movements.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-8 text-center text-muted-foreground">
        <History className="h-12 w-12 mb-3 opacity-50" />
        <p className="text-sm">{t('products.stock.noHistory')}</p>
        <p className="text-xs mt-1">{t('products.stock.noHistoryHint')}</p>
      </div>
    )
  }

  return (
    <div className="space-y-3">
      {/* Current stock header */}
      <div className="flex items-center justify-between p-3 rounded-lg bg-muted/50">
        <div>
          <p className="text-sm text-muted-foreground">
            {variantName
              ? t('products.stock.currentStockFor', { name: variantName })
              : t('products.stock.currentStock')}
          </p>
          <p className="text-2xl font-bold">{currentStock}</p>
        </div>
        <Package className="h-8 w-8 text-muted-foreground" />
      </div>

      {/* Timeline */}
      <ScrollArea style={{ maxHeight }}>
        <div className="relative pl-6">
          {/* Vertical line */}
          <div className="absolute left-[11px] top-0 bottom-0 w-0.5 bg-border" />

          <div className="space-y-4">
            {sortedMovements.map((movement) => {
              const config = movementConfig[movement.type]
              const Icon = config.icon
              const delta = movement.newStock - movement.previousStock

              return (
                <div key={movement.id} className="relative flex gap-3">
                  {/* Icon circle */}
                  <div
                    className={cn(
                      'absolute -left-6 flex h-6 w-6 items-center justify-center rounded-full',
                      config.bgColor
                    )}
                  >
                    <Icon className={cn('h-3.5 w-3.5', config.color)} />
                  </div>

                  {/* Content */}
                  <div className="flex-1 rounded-lg border bg-card p-3">
                    <div className="flex items-start justify-between gap-2">
                      <div>
                        <p className="font-medium text-sm">
                          {t(`products.stock.types.${config.labelKey}`)}
                        </p>
                        {movement.reason && (
                          <p className="text-xs text-muted-foreground mt-0.5">
                            {movement.reason}
                          </p>
                        )}
                        {movement.orderId && (
                          <p className="text-xs text-muted-foreground mt-0.5">
                            {t('products.stock.orderRef', { id: movement.orderId })}
                          </p>
                        )}
                      </div>
                      <div className="text-right">
                        <p
                          className={cn(
                            'font-semibold text-sm',
                            delta > 0
                              ? 'text-green-600'
                              : delta < 0
                              ? 'text-red-600'
                              : 'text-muted-foreground'
                          )}
                        >
                          {delta > 0 ? '+' : ''}
                          {delta}
                        </p>
                        <p className="text-xs text-muted-foreground">
                          {movement.previousStock} &rarr; {movement.newStock}
                        </p>
                      </div>
                    </div>
                    <div className="flex items-center justify-between mt-2 pt-2 border-t text-xs text-muted-foreground">
                      <span>
                        {formatDistanceToNow(new Date(movement.createdAt), { addSuffix: true })}
                      </span>
                      {movement.createdBy && <span>{movement.createdBy}</span>}
                    </div>
                  </div>
                </div>
              )
            })}
          </div>
        </div>
      </ScrollArea>
    </div>
  )
}
