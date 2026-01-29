/**
 * LowStockAlert Component
 *
 * Displays a dismissible alert banner when there are products with low stock.
 * Shows a count and quick link to filter to low stock items.
 */
import { useState, useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { motion, AnimatePresence } from 'framer-motion'
import { AlertTriangle, X, ArrowRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import { cn } from '@/lib/utils'

interface LowStockAlertProps {
  lowStockCount: number
  onViewLowStock?: () => void
  className?: string
}

export function LowStockAlert({
  lowStockCount,
  onViewLowStock,
  className,
}: LowStockAlertProps) {
  const { t } = useTranslation('common')
  const [isDismissed, setIsDismissed] = useState(false)
  const hasSeenCountRef = useRef(0)

  // Reset dismissed state if count increases significantly (5+ new items)
  useEffect(() => {
    if (lowStockCount > hasSeenCountRef.current + 5) {
      setIsDismissed(false)
      hasSeenCountRef.current = lowStockCount // Only update when state changes
    }
  }, [lowStockCount])

  if (lowStockCount === 0 || isDismissed) {
    return null
  }

  return (
    <AnimatePresence>
      <motion.div
        initial={{ opacity: 0, y: -10, height: 0 }}
        animate={{ opacity: 1, y: 0, height: 'auto' }}
        exit={{ opacity: 0, y: -10, height: 0 }}
        transition={{ duration: 0.3 }}
        className={cn(
          'rounded-lg border overflow-hidden',
          'bg-amber-500/10 border-amber-500/30',
          'dark:bg-amber-950/30 dark:border-amber-500/20',
          className
        )}
      >
        <div className="flex items-center gap-3 p-4">
          {/* Icon */}
          <div className="flex-shrink-0 p-2 rounded-lg bg-amber-500/20 dark:bg-amber-500/10">
            <AlertTriangle className="h-5 w-5 text-amber-600 dark:text-amber-400" />
          </div>

          {/* Content */}
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <p className="font-medium text-amber-800 dark:text-amber-200">
                {t('products.lowStockAlert.title', 'Low Stock Alert')}
              </p>
              <Badge
                variant="secondary"
                className="bg-amber-500/20 text-amber-700 dark:text-amber-300 border-amber-500/30"
              >
                {lowStockCount} {t('products.lowStockAlert.items', 'items')}
              </Badge>
            </div>
            <p className="text-sm text-amber-700/80 dark:text-amber-300/80 mt-0.5">
              {t('products.lowStockAlert.description', 'Some products are running low on stock and may need restocking soon.')}
            </p>
          </div>

          {/* Actions */}
          <div className="flex items-center gap-2 flex-shrink-0">
            {onViewLowStock && (
              <Button
                variant="outline"
                size="sm"
                onClick={onViewLowStock}
                className="cursor-pointer border-amber-500/30 text-amber-700 dark:text-amber-300 hover:bg-amber-500/10"
              >
                {t('products.lowStockAlert.viewAll', 'View All')}
                <ArrowRight className="h-4 w-4 ml-1" />
              </Button>
            )}
            <Button
              variant="ghost"
              size="icon"
              onClick={() => setIsDismissed(true)}
              className="cursor-pointer h-8 w-8 text-amber-600 dark:text-amber-400 hover:bg-amber-500/10"
              aria-label={t('buttons.dismiss', 'Dismiss')}
            >
              <X className="h-4 w-4" />
            </Button>
          </div>
        </div>
      </motion.div>
    </AnimatePresence>
  )
}
