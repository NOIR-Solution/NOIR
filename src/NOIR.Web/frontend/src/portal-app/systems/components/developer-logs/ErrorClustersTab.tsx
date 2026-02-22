/**
 * ErrorClustersTab Component
 *
 * Displays error pattern analysis with collapsible cluster details.
 * Shows severity, count, first/last seen timestamps for each error pattern.
 */
import { useTranslation } from 'react-i18next'
import { RefreshCw, ChevronRight, AlertCircle } from 'lucide-react'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import {
  Badge,
  Button,
  Card,
  CardContent,
  CardHeader,
  CardTitle,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
  EmptyState,
} from '@uikit'

import type { ErrorClusterDto } from '@/services/developerLogs'

export interface ErrorClustersTabProps {
  clusters: ErrorClusterDto[]
  onRefresh: () => void
}

// Error Clusters Component
const ErrorClusters = ({
  clusters,
  onRefresh,
}: {
  clusters: ErrorClusterDto[]
  onRefresh: () => void
}) => {
  const { t } = useTranslation('common')
  const { formatDateTime } = useRegionalSettings()

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium">{t('developerLogs.errorPatterns')}</span>
        <Button variant="ghost" size="sm" onClick={onRefresh} className="group">
          <RefreshCw className="h-3 w-3 transition-transform duration-300 group-hover:rotate-180" />
        </Button>
      </div>

      {clusters.length === 0 ? (
        <EmptyState
          icon={AlertCircle}
          title={t('developerLogs.noErrorPatterns')}
          description=""
          className="border-0 rounded-none px-4 py-8"
        />
      ) : (
        <div className="space-y-2">
          {clusters.map(cluster => (
            <Collapsible key={cluster.id}>
              <CollapsibleTrigger className="w-full">
                <div className="flex items-center gap-2 p-2 bg-red-50 dark:bg-red-950/30 rounded border border-red-200 dark:border-red-800 text-left hover:bg-red-100 dark:hover:bg-red-950/50 transition-colors">
                  <Badge
                    variant={
                      cluster.severity === 'critical'
                        ? 'destructive'
                        : cluster.severity === 'high'
                        ? 'default'
                        : 'secondary'
                    }
                    className="flex-shrink-0"
                  >
                    {cluster.count}x
                  </Badge>
                  <span className="flex-1 text-xs font-mono truncate">
                    {cluster.pattern}
                  </span>
                  <ChevronRight className="h-4 w-4 flex-shrink-0" />
                </div>
              </CollapsibleTrigger>
              <CollapsibleContent>
                <div className="mt-1 p-2 bg-muted/50 rounded text-xs space-y-1">
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('developerLogs.firstSeen')}</span>
                    <span className="font-mono">{formatDateTime(cluster.firstSeen)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('developerLogs.lastSeen')}</span>
                    <span className="font-mono">{formatDateTime(cluster.lastSeen)}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">{t('developerLogs.severity')}</span>
                    <Badge variant="outline" className="capitalize">{cluster.severity}</Badge>
                  </div>
                </div>
              </CollapsibleContent>
            </Collapsible>
          ))}
        </div>
      )}
    </div>
  )
}

export const ErrorClustersTab = ({ clusters, onRefresh }: ErrorClustersTabProps) => {
  const { t } = useTranslation('common')
  return (
    <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <AlertCircle className="h-5 w-5 text-red-500" />
          {t('developerLogs.errorPatternAnalysis')}
        </CardTitle>
      </CardHeader>
      <CardContent>
        <ErrorClusters
          clusters={clusters}
          onRefresh={onRefresh}
        />
      </CardContent>
    </Card>
  )
}
