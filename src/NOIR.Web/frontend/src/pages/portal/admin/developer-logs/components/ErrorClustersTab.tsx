/**
 * ErrorClustersTab Component
 *
 * Displays error pattern analysis with collapsible cluster details.
 * Shows severity, count, first/last seen timestamps for each error pattern.
 */
import { RefreshCw, ChevronRight, AlertCircle } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Badge } from '@/components/ui/badge'
import {
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
} from '@/components/ui/collapsible'
import type { ErrorClusterDto } from '@/services/developerLogs'

export interface ErrorClustersTabProps {
  clusters: ErrorClusterDto[]
  onRefresh: () => void
}

// Error Clusters Component
function ErrorClusters({
  clusters,
  onRefresh,
}: {
  clusters: ErrorClusterDto[]
  onRefresh: () => void
}) {
  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <span className="text-sm font-medium">Error Patterns</span>
        <Button variant="ghost" size="sm" onClick={onRefresh}>
          <RefreshCw className="h-3 w-3" />
        </Button>
      </div>

      {clusters.length === 0 ? (
        <div className="text-center py-6 text-muted-foreground text-sm">
          No error patterns detected
        </div>
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
                    <span className="text-muted-foreground">First seen:</span>
                    <span className="font-mono">{new Date(cluster.firstSeen).toLocaleString()}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Last seen:</span>
                    <span className="font-mono">{new Date(cluster.lastSeen).toLocaleString()}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Severity:</span>
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

export function ErrorClustersTab({ clusters, onRefresh }: ErrorClustersTabProps) {
  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <AlertCircle className="h-5 w-5 text-red-500" />
          Error Pattern Analysis
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
