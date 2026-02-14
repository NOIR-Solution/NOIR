/**
 * LogDetailDialog Component
 *
 * A dialog that shows detailed information about a single log entry.
 * Includes message, metadata grid, exception details, properties, and raw JSON.
 */
import { useState } from 'react'
import { Copy, Check } from 'lucide-react'
import {
  Badge,
  Button,
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  JsonViewer,
  LogMessageFormatter,
} from '@uikit'

import { cn } from '@/lib/utils'
import type { LogEntryDto } from '@/services/developerLogs'
import { getLevelConfig, formatFullTimestamp, getDisplayMessage } from './log-utils'

export interface LogDetailDialogProps {
  entry: LogEntryDto | null
  open: boolean
  onOpenChange: (open: boolean) => void
}

export const LogDetailDialog = ({
  entry,
  open,
  onOpenChange,
}: LogDetailDialogProps) => {
  const [copied, setCopied] = useState(false)

  if (!entry) return null

  const config = getLevelConfig(entry.level)

  const handleCopyMessage = () => {
    navigator.clipboard.writeText(getDisplayMessage(entry))
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl max-h-[90vh] overflow-hidden flex flex-col">
        <DialogHeader className="flex-shrink-0">
          <DialogTitle className="flex items-center gap-3">
            <Badge
              variant="outline"
              className={cn(
                'px-2 py-0.5 text-xs font-bold',
                config.bgColor,
                config.textColor
              )}
            >
              {config.label}
            </Badge>
            <span className="text-sm font-mono text-muted-foreground">
              {formatFullTimestamp(entry.timestamp)}
            </span>
          </DialogTitle>
        </DialogHeader>

        <div className="flex-1 overflow-y-auto space-y-4 pr-2">
          {/* Message */}
          <div className="space-y-2">
            <div className="flex items-center justify-between">
              <label className="text-sm font-medium text-muted-foreground">Message</label>
              <Button variant="ghost" size="sm" onClick={handleCopyMessage} className="h-7 gap-1.5">
                {copied ? <Check className="h-3 w-3" /> : <Copy className="h-3 w-3" />}
                Copy
              </Button>
            </div>
            <div className="p-3 bg-muted rounded-lg font-mono text-sm">
              <LogMessageFormatter message={getDisplayMessage(entry)} />
            </div>
          </div>

          {/* Metadata Grid */}
          <div className="grid grid-cols-2 gap-4">
            {entry.sourceContext && (
              <div className="space-y-1">
                <label className="text-xs font-medium text-muted-foreground">Source</label>
                <div className="p-2 bg-muted rounded text-sm font-mono truncate">
                  {entry.sourceContext}
                </div>
              </div>
            )}
            {entry.requestId && (
              <div className="space-y-1">
                <label className="text-xs font-medium text-muted-foreground">Request ID</label>
                <div className="p-2 bg-muted rounded text-sm font-mono truncate">
                  {entry.requestId}
                </div>
              </div>
            )}
            {entry.traceId && (
              <div className="space-y-1">
                <label className="text-xs font-medium text-muted-foreground">Trace ID</label>
                <div className="p-2 bg-muted rounded text-sm font-mono truncate">
                  {entry.traceId}
                </div>
              </div>
            )}
            {entry.userId && (
              <div className="space-y-1">
                <label className="text-xs font-medium text-muted-foreground">User ID</label>
                <div className="p-2 bg-muted rounded text-sm font-mono truncate">
                  {entry.userId}
                </div>
              </div>
            )}
            {entry.tenantId && (
              <div className="space-y-1">
                <label className="text-xs font-medium text-muted-foreground">Tenant ID</label>
                <div className="p-2 bg-muted rounded text-sm font-mono truncate">
                  {entry.tenantId}
                </div>
              </div>
            )}
          </div>

          {/* Exception */}
          {entry.exception && (
            <div className="space-y-2">
              <label className="text-sm font-medium text-red-600 dark:text-red-400">Exception</label>
              <div className="p-3 bg-red-50 dark:bg-red-950/50 rounded-lg border border-red-200 dark:border-red-800">
                <div className="font-semibold text-red-700 dark:text-red-300">
                  {entry.exception.type}
                </div>
                <div className="text-red-600 dark:text-red-400 mt-1">
                  {entry.exception.message}
                </div>
                {entry.exception.stackTrace && (
                  <pre className="mt-3 p-2 bg-red-100 dark:bg-red-900/50 rounded text-[11px] text-red-600/90 dark:text-red-400/90 whitespace-pre-wrap overflow-x-auto max-h-[300px] overflow-y-auto">
                    {entry.exception.stackTrace}
                  </pre>
                )}
              </div>
            </div>
          )}

          {/* Properties */}
          {entry.properties && Object.keys(entry.properties).length > 0 && (
            <div className="space-y-2">
              <label className="text-sm font-medium text-muted-foreground">Properties</label>
              <JsonViewer
                data={entry.properties}
                defaultExpanded={true}
                maxDepth={4}
                maxHeight="200px"
                allowFullscreen={true}
                title="Log Properties"
              />
            </div>
          )}

          {/* Raw JSON */}
          <div className="space-y-2">
            <label className="text-sm font-medium text-muted-foreground">Raw JSON</label>
            <JsonViewer
              data={entry}
              defaultExpanded={true}
              maxDepth={4}
              maxHeight="250px"
              allowFullscreen={true}
              title="Log Entry"
            />
          </div>
        </div>
      </DialogContent>
    </Dialog>
  )
}
