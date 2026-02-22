/**
 * StatsTab Component
 *
 * Displays buffer statistics including entry counts by level,
 * memory usage, and time range of logged entries.
 */
import { useTranslation } from 'react-i18next'
import { RefreshCw, Database, Clock } from 'lucide-react'
import { Badge, Button, Card, CardContent, CardHeader, CardTitle, Skeleton } from '@uikit'

import { cn } from '@/lib/utils'
import type { LogBufferStatsDto } from '@/services/developerLogs'
import { LOG_LEVELS, formatBytes, formatFullTimestamp } from './log-utils'

export interface StatsTabProps {
  stats: LogBufferStatsDto | null
  onRefresh: () => void
}

// Stats Card Component (Buffer Overview)
const StatsCard = ({
  stats,
  onRefresh,
}: {
  stats: LogBufferStatsDto | null
  onRefresh: () => void
}) => {
  const { t } = useTranslation('common')

  if (!stats) {
    return (
      <div className="space-y-3">
        <Skeleton className="h-4 w-32" />
        <Skeleton className="h-8 w-full" />
        <Skeleton className="h-8 w-full" />
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium">{t('developerLogs.bufferStats')}</span>
        <Button variant="ghost" size="sm" onClick={onRefresh} className="group">
          <RefreshCw className="h-3 w-3 transition-transform duration-300 group-hover:rotate-180" />
        </Button>
      </div>

      <div className="grid grid-cols-2 gap-3 text-sm">
        <div className="p-2 bg-muted rounded">
          <div className="text-muted-foreground text-xs">{t('developerLogs.entries')}</div>
          <div className="font-mono font-semibold">
            {stats.totalEntries.toLocaleString()} / {stats.maxCapacity.toLocaleString()}
          </div>
        </div>
        <div className="p-2 bg-muted rounded">
          <div className="text-muted-foreground text-xs">{t('developerLogs.memory')}</div>
          <div className="font-mono font-semibold">
            {formatBytes(stats.memoryUsageBytes)}
          </div>
        </div>
      </div>

      {/* Entries by level */}
      <div className="space-y-2">
        <div className="text-xs text-muted-foreground">{t('developerLogs.byLevel')}</div>
        <div className="space-y-1">
          {LOG_LEVELS.map(level => {
            const count = stats.entriesByLevel[level.value] || 0
            const percentage = stats.totalEntries > 0
              ? (count / stats.totalEntries) * 100
              : 0

            return (
              <div key={level.value} className="flex items-center gap-2 text-xs">
                <Badge
                  variant="outline"
                  className={cn('w-10 justify-center px-1 py-0', level.bgColor, level.textColor)}
                >
                  {level.label}
                </Badge>
                <div className="flex-1 h-2 bg-muted rounded overflow-hidden">
                  <div
                    className={cn('h-full transition-all', level.bgColor.replace('100', '400').replace('900/30', '600'))}
                    style={{ width: `${percentage}%` }}
                  />
                </div>
                <span className="w-12 text-right tabular-nums font-mono">
                  {count.toLocaleString()}
                </span>
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}

export const StatsTab = ({ stats, onRefresh }: StatsTabProps) => {
  const { t } = useTranslation('common')
  return (
    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <Card className="lg:col-span-2 shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Database className="h-5 w-5" />
            {t('developerLogs.bufferOverview')}
          </CardTitle>
        </CardHeader>
        <CardContent>
          <StatsCard stats={stats} onRefresh={onRefresh} />
        </CardContent>
      </Card>

      <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <Clock className="h-5 w-5" />
            {t('developerLogs.timeRange')}
          </CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          {stats ? (
            <>
              <div className="p-3 bg-muted rounded-lg">
                <div className="text-xs text-muted-foreground mb-1">{t('developerLogs.oldestEntry')}</div>
                <div className="font-mono text-sm">
                  {stats.oldestEntry ? formatFullTimestamp(stats.oldestEntry) : t('labels.notAvailable', 'N/A')}
                </div>
              </div>
              <div className="p-3 bg-muted rounded-lg">
                <div className="text-xs text-muted-foreground mb-1">{t('developerLogs.newestEntry')}</div>
                <div className="font-mono text-sm">
                  {stats.newestEntry ? formatFullTimestamp(stats.newestEntry) : t('labels.notAvailable', 'N/A')}
                </div>
              </div>
            </>
          ) : (
            <div className="space-y-3">
              <Skeleton className="h-16 w-full" />
              <Skeleton className="h-16 w-full" />
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
