/**
 * ProductActivityLog Component
 *
 * Displays a compact activity timeline for a specific product.
 * Uses the existing audit system to fetch and display activity entries.
 */
import { useState, useEffect, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { ViewTransitionLink } from '@/components/navigation/ViewTransitionLink'
import {
  Activity,
  Clock,
  Plus,
  Pencil,
  Trash2,
  XCircle,
  RefreshCw,
  ExternalLink,
} from 'lucide-react'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  ScrollArea,
  Skeleton,
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@uikit'

import { cn } from '@/lib/utils'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import {
  searchActivityTimeline,
  type ActivityTimelineEntry,
} from '@/services/audit'
import { ActivityDetailsDialog } from '@/portal-app/systems/components/activity-timeline/ActivityDetailsDialog'

interface ProductActivityLogProps {
  productId: string
  productName?: string
  className?: string
  maxEntries?: number
}

// Operation type config
const operationConfig = {
  Create: { icon: Plus, color: 'text-green-600', bgColor: 'bg-green-100 dark:bg-green-900/30' },
  Update: { icon: Pencil, color: 'text-blue-600', bgColor: 'bg-blue-100 dark:bg-blue-900/30' },
  Delete: { icon: Trash2, color: 'text-red-600', bgColor: 'bg-red-100 dark:bg-red-900/30' },
}

// Compact Timeline Entry
function CompactTimelineEntry({
  entry,
  onViewDetails,
}: {
  entry: ActivityTimelineEntry
  onViewDetails: () => void
}) {
  const { formatRelativeTime } = useRegionalSettings()
  const config = operationConfig[entry.operationType as keyof typeof operationConfig] || operationConfig.Update
  const Icon = config.icon

  return (
    <button
      type="button"
      onClick={onViewDetails}
      className={cn(
        'w-full flex items-start gap-3 p-3 rounded-lg text-left transition-all',
        'hover:bg-muted/50 cursor-pointer',
        !entry.isSuccess && 'bg-red-50/50 dark:bg-red-950/20'
      )}
    >
      {/* Icon */}
      <div className={cn('p-1.5 rounded-md flex-shrink-0', config.bgColor)}>
        <Icon className={cn('h-3.5 w-3.5', config.color)} />
      </div>

      {/* Content */}
      <div className="flex-1 min-w-0">
        <div className="flex items-center gap-2">
          <span className="text-sm font-medium truncate">
            {entry.actionDescription || entry.operationType}
          </span>
          {!entry.isSuccess && (
            <XCircle className="h-3.5 w-3.5 text-red-500 flex-shrink-0" />
          )}
        </div>
        <div className="flex items-center gap-2 mt-0.5 text-xs text-muted-foreground">
          <span className="truncate">{entry.userEmail || 'System'}</span>
          <span>·</span>
          <Tooltip>
            <TooltipTrigger asChild>
              <span className="flex items-center gap-1 cursor-default">
                <Clock className="h-3 w-3" />
                {formatRelativeTime(entry.timestamp)}
              </span>
            </TooltipTrigger>
            <TooltipContent side="top" className="text-xs">
              {new Date(entry.timestamp).toLocaleString()}
            </TooltipContent>
          </Tooltip>
        </div>
      </div>

      {/* Badge */}
      <Badge variant="outline" className={cn('text-xs flex-shrink-0', config.color)}>
        {entry.operationType}
      </Badge>
    </button>
  )
}

export function ProductActivityLog({
  productId,
  productName: _productName,
  className,
  maxEntries = 10,
}: ProductActivityLogProps) {
  const { t } = useTranslation('common')
  const [entries, setEntries] = useState<ActivityTimelineEntry[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [selectedEntry, setSelectedEntry] = useState<ActivityTimelineEntry | null>(null)

  const fetchActivity = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const result = await searchActivityTimeline({
        targetId: productId,
        pageContext: 'Products',
        pageSize: maxEntries,
        page: 1,
      })
      setEntries(result.items)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load activity')
    } finally {
      setLoading(false)
    }
  }, [productId, maxEntries])

  useEffect(() => {
    fetchActivity()
  }, [fetchActivity])

  return (
    <>
      <Card className={cn('shadow-sm hover:shadow-lg transition-all duration-300', className)}>
        <CardHeader className="backdrop-blur-sm bg-background/95 rounded-t-lg pb-3">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Activity className="h-4 w-4 text-muted-foreground" />
              <CardTitle className="text-base">
                {t('products.activityLog', 'Activity Log')}
              </CardTitle>
            </div>
            <Button
              variant="ghost"
              size="icon"
              className="h-8 w-8 cursor-pointer"
              onClick={fetchActivity}
              disabled={loading}
              aria-label={t('buttons.refresh', 'Refresh')}
            >
              <RefreshCw className={cn('h-4 w-4', loading && 'animate-spin')} />
            </Button>
          </div>
          <CardDescription className="text-xs">
            {t('products.activityLogDescription', 'Recent changes to this product')}
          </CardDescription>
        </CardHeader>
        <CardContent className="pt-0">
          {loading ? (
            <div className="space-y-3">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="flex items-start gap-3 p-3">
                  <Skeleton className="h-8 w-8 rounded-md flex-shrink-0" />
                  <div className="flex-1 space-y-2">
                    <Skeleton className="h-4 w-3/4" />
                    <Skeleton className="h-3 w-1/2" />
                  </div>
                </div>
              ))}
            </div>
          ) : error ? (
            <div className="text-center py-6 text-muted-foreground">
              <XCircle className="h-8 w-8 mx-auto mb-2 text-red-500" />
              <p className="text-sm">{error}</p>
              <Button
                variant="ghost"
                size="sm"
                className="mt-2 cursor-pointer"
                onClick={fetchActivity}
              >
                {t('buttons.retry', 'Retry')}
              </Button>
            </div>
          ) : entries.length === 0 ? (
            <div className="text-center py-6 text-muted-foreground">
              <Activity className="h-8 w-8 mx-auto mb-2 opacity-50" />
              <p className="text-sm">{t('products.noActivity', 'No activity recorded yet')}</p>
            </div>
          ) : (
            <ScrollArea className="h-[280px] -mx-2">
              <div className="space-y-1 px-2">
                {entries.map((entry) => (
                  <CompactTimelineEntry
                    key={entry.id}
                    entry={entry}
                    onViewDetails={() => setSelectedEntry(entry)}
                  />
                ))}
              </div>
            </ScrollArea>
          )}

          {/* View All Link */}
          {entries.length > 0 && (
            <div className="pt-3 border-t mt-3">
              <ViewTransitionLink
                to={`/portal/admin/activity-timeline?targetId=${productId}`}
                className="flex items-center justify-center gap-1 text-xs text-muted-foreground hover:text-foreground transition-colors"
              >
                {t('products.viewAllActivity', 'View all activity')}
                <ExternalLink className="h-3 w-3" />
              </ViewTransitionLink>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Details Dialog */}
      <ActivityDetailsDialog
        entry={selectedEntry}
        open={!!selectedEntry}
        onOpenChange={(open) => !open && setSelectedEntry(null)}
      />
    </>
  )
}
