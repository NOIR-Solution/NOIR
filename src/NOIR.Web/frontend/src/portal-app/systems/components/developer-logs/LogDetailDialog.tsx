/**
 * LogDetailDialog Component
 *
 * A dialog that shows detailed information about a single log entry.
 * Includes message, metadata grid, exception details, properties, and raw JSON.
 */
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Copy, Check } from 'lucide-react'
import {
  Badge,
  Button,
  Credenza,
  CredenzaContent,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
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
  const { t } = useTranslation('common')
  const [copied, setCopied] = useState(false)

  if (!entry) return null

  const config = getLevelConfig(entry.level)

  const handleCopyMessage = () => {
    navigator.clipboard.writeText(getDisplayMessage(entry))
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="max-w-4xl max-h-[90vh] overflow-hidden flex flex-col">
        <CredenzaHeader className="flex-shrink-0">
          <CredenzaTitle className="flex items-center gap-3">
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
          </CredenzaTitle>
        </CredenzaHeader>

        <CredenzaBody>
          <div className="flex-1 overflow-y-auto space-y-4 pr-2">
            {/* Message */}
            <div className="space-y-2">
              <div className="flex items-center justify-between">
                <label className="text-sm font-medium text-muted-foreground">{t('developerLogs.message')}</label>
                <Button variant="ghost" size="sm" onClick={handleCopyMessage} className="h-7 gap-1.5">
                  {copied ? <Check className="h-3 w-3" /> : <Copy className="h-3 w-3" />}
                  {t('developerLogs.copy')}
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
                  <label className="text-xs font-medium text-muted-foreground">{t('developerLogs.source')}</label>
                  <div className="p-2 bg-muted rounded text-sm font-mono truncate">
                    {entry.sourceContext}
                  </div>
                </div>
              )}
              {entry.requestId && (
                <div className="space-y-1">
                  <label className="text-xs font-medium text-muted-foreground">{t('developerLogs.requestId')}</label>
                  <div className="p-2 bg-muted rounded text-sm font-mono truncate">
                    {entry.requestId}
                  </div>
                </div>
              )}
              {entry.traceId && (
                <div className="space-y-1">
                  <label className="text-xs font-medium text-muted-foreground">{t('developerLogs.traceId')}</label>
                  <div className="p-2 bg-muted rounded text-sm font-mono truncate">
                    {entry.traceId}
                  </div>
                </div>
              )}
              {entry.userId && (
                <div className="space-y-1">
                  <label className="text-xs font-medium text-muted-foreground">{t('developerLogs.userId')}</label>
                  <div className="p-2 bg-muted rounded text-sm font-mono truncate">
                    {entry.userId}
                  </div>
                </div>
              )}
              {entry.tenantId && (
                <div className="space-y-1">
                  <label className="text-xs font-medium text-muted-foreground">{t('developerLogs.tenantId')}</label>
                  <div className="p-2 bg-muted rounded text-sm font-mono truncate">
                    {entry.tenantId}
                  </div>
                </div>
              )}
            </div>

            {/* Exception */}
            {entry.exception && (
              <div className="space-y-2">
                <label className="text-sm font-medium text-red-600 dark:text-red-400">{t('developerLogs.exception')}</label>
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
                <label className="text-sm font-medium text-muted-foreground">{t('developerLogs.properties')}</label>
                <JsonViewer
                  data={entry.properties}
                  defaultExpanded={true}
                  maxDepth={4}
                  maxHeight="200px"
                  allowFullscreen={true}
                  title={t('developerLogs.logProperties')}
                />
              </div>
            )}

            {/* Raw JSON */}
            <div className="space-y-2">
              <label className="text-sm font-medium text-muted-foreground">{t('developerLogs.rawJson')}</label>
              <JsonViewer
                data={entry}
                defaultExpanded={true}
                maxDepth={4}
                maxHeight="250px"
                allowFullscreen={true}
                title={t('developerLogs.logEntry')}
              />
            </div>
          </div>
        </CredenzaBody>
      </CredenzaContent>
    </Credenza>
  )
}
