import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import {
  Plus,
  Pencil,
  Trash2,
  Activity,
  ArrowRight,
  Clock,
  CheckCircle2,
  type LucideIcon,
} from 'lucide-react'
import {
  Avatar,
  Badge,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  EmptyState,
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@uikit'
import { cn } from '@/lib/utils'
import { translateAuditDescription } from '@/lib/utils/auditDescriptionTranslator'
import { useRegionalSettings, getLocaleForFormat } from '@/contexts/RegionalSettingsContext'
import type { ActivityFeedItemDto } from '@/services/dashboard'

interface ActivityFeedProps {
  items: ActivityFeedItemDto[]
}

const OPERATION_CONFIG: Record<string, { icon: LucideIcon; textColor: string; bgColor: string }> = {
  Create: { icon: Plus, textColor: 'text-green-700 dark:text-green-400', bgColor: 'bg-green-100 dark:bg-green-900/30' },
  Update: { icon: Pencil, textColor: 'text-blue-700 dark:text-blue-400', bgColor: 'bg-blue-100 dark:bg-blue-900/30' },
  Delete: { icon: Trash2, textColor: 'text-red-700 dark:text-red-400', bgColor: 'bg-red-100 dark:bg-red-900/30' },
}

const DEFAULT_CONFIG = { icon: Activity, textColor: 'text-gray-700 dark:text-gray-400', bgColor: 'bg-gray-100 dark:bg-gray-900/30' }

const formatTimeOnly = (timestamp: string, timezone: string, locale: string): string => {
  const date = new Date(timestamp)
  try {
    return date.toLocaleTimeString(locale, {
      hour12: false,
      hour: '2-digit',
      minute: '2-digit',
      timeZone: timezone,
    })
  } catch {
    return date.toLocaleTimeString(locale, {
      hour12: false,
      hour: '2-digit',
      minute: '2-digit',
    })
  }
}

export const ActivityFeed = ({ items }: ActivityFeedProps) => {
  const { t } = useTranslation('common')
  const navigate = useNavigate()
  const { formatRelativeTime, formatDateTime, timezone, dateFormat } = useRegionalSettings()
  const locale = getLocaleForFormat(dateFormat)

  return (
    <Card className="gap-0 shadow-sm hover:shadow-lg transition-all duration-300">
      <CardHeader className="pb-2">
        <div className="flex items-center justify-between">
          <CardTitle className="text-lg">{t('dashboard.activityFeed')}</CardTitle>
          <button
            type="button"
            onClick={() => navigate('/portal/activity-timeline')}
            className="flex items-center gap-1 text-xs text-muted-foreground hover:text-primary transition-colors cursor-pointer"
          >
            {t('dashboard.viewAll')}
            <ArrowRight className="h-3 w-3" />
          </button>
        </div>
      </CardHeader>
      <CardContent>
        {items.length === 0 ? (
          <EmptyState
            icon={Activity}
            title={t('dashboard.noActivity')}
            size="sm"
            className="border-0 rounded-none py-6"
          />
        ) : (
          <div className="max-h-[400px] overflow-y-auto pl-1 pt-1" tabIndex={0} role="region" aria-label={t('dashboard.activityFeed')}>
            {items.map((item, i) => {
              const config = OPERATION_CONFIG[item.type] ?? DEFAULT_CONFIG
              const Icon = config.icon
              const isLast = i === items.length - 1

              return (
                <div key={`${item.timestamp}-${item.entityId ?? i}`} className="relative flex gap-3">
                  {/* Timeline line */}
                  {!isLast && (
                    <div className="absolute left-[19px] top-[44px] bottom-0 w-0.5 bg-border" />
                  )}

                  {/* Avatar with page context initials + success indicator */}
                  <div className="relative z-10 shrink-0 h-10 w-10">
                    <Avatar
                      fallback={item.title || 'System'}
                      size="md"
                      className="ring-4 ring-background"
                    />
                    <span className="absolute bottom-0 right-0 h-3.5 w-3.5 rounded-full border-2 border-background flex items-center justify-center bg-green-500">
                      <CheckCircle2 className="h-2 w-2 text-white" />
                    </span>
                  </div>

                  {/* Content card - mirrors TimelineEntry from Activity Timeline page */}
                  <div
                    className={cn(
                      'flex-1 min-w-0 rounded-lg border transition-all mb-2.5 text-left',
                      'hover:shadow-sm hover:border-primary/20'
                    )}
                  >
                    <div className="p-3 flex items-start gap-2.5">
                      {/* Operation Icon */}
                      <div className={cn('p-1.5 rounded-lg shrink-0', config.bgColor)}>
                        <Icon className={cn('h-3.5 w-3.5', config.textColor)} />
                      </div>

                      {/* Main Content */}
                      <div className="flex-1 min-w-0">
                        <div className="flex items-center gap-1.5 flex-wrap">
                          <span className="font-medium text-sm leading-tight">
                            {translateAuditDescription(t, item.description)}
                          </span>
                          <Badge variant="outline" className={cn('text-[10px] px-1.5 py-0 leading-relaxed', config.textColor)}>
                            {t(`activityTimeline.operations.${item.type.toLowerCase()}`, item.type)}
                          </Badge>
                        </div>

                        {item.targetDisplayName && (
                          <div className="text-xs text-foreground/80 mt-0.5 truncate">
                            → <span className="font-medium">{item.targetDisplayName}</span>
                          </div>
                        )}

                        <div className="flex items-center gap-2 mt-1 text-xs text-muted-foreground">
                          {item.userEmail && (
                            <>
                              <span className="truncate max-w-[120px]">{item.userEmail}</span>
                              <span className="text-muted-foreground">·</span>
                            </>
                          )}
                          <Tooltip>
                            <TooltipTrigger asChild>
                              <span className="flex items-center gap-1 cursor-default tabular-nums">
                                <Clock className="h-3 w-3" />
                                <span className="text-muted-foreground">
                                  {formatRelativeTime(item.timestamp)}
                                </span>
                                <span>·</span>
                                {formatTimeOnly(item.timestamp, timezone, locale)}
                              </span>
                            </TooltipTrigger>
                            <TooltipContent side="top" className="font-mono text-xs">
                              {formatDateTime(item.timestamp)}
                            </TooltipContent>
                          </Tooltip>
                        </div>
                      </div>
                    </div>
                  </div>
                </div>
              )
            })}
          </div>
        )}
      </CardContent>
    </Card>
  )
}
