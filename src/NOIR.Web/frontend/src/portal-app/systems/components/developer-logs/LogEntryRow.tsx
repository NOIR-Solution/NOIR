/**
 * LogEntryRow Component
 *
 * Renders a single log entry row in the terminal-style log viewer.
 * Includes timestamp, level badge, source context, message, and action buttons.
 * Supports expanding exceptions inline.
 */
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import {
  ChevronDown,
  ChevronRight,
  Copy,
  Check,
  Maximize2,
} from 'lucide-react'
import { Badge, LogMessageFormatter, Tooltip, TooltipContent, TooltipTrigger } from '@uikit'

import { cn } from '@/lib/utils'
import type { LogEntryDto } from '@/services/developerLogs'
import {
  getLevelConfig,
  formatTimestamp,
  formatFullTimestamp,
  formatRelativeTime,
  getDisplayMessage,
} from './log-utils'

export interface LogEntryRowProps {
  entry: LogEntryDto
  isExpanded: boolean
  onToggleExpand: () => void
  onViewDetail: () => void
}

export const LogEntryRow = ({
  entry,
  isExpanded,
  onToggleExpand,
  onViewDetail,
}: LogEntryRowProps) => {
  const { t } = useTranslation('common')
  const config = getLevelConfig(entry.level)
  const [copied, setCopied] = useState(false)
  const hasException = !!entry.exception

  const handleCopy = (e: React.MouseEvent) => {
    e.stopPropagation()
    const text = JSON.stringify(entry, null, 2)
    navigator.clipboard.writeText(text)
    setCopied(true)
    setTimeout(() => setCopied(false), 2000)
  }

  return (
    <div
      className={cn(
        'group border-l-4 px-3 py-1.5 font-mono text-xs hover:bg-muted/50 dark:hover:bg-slate-800/50 transition-colors cursor-pointer',
        config.borderColor,
        hasException && 'bg-red-50 dark:bg-red-950/30'
      )}
      onClick={onViewDetail}
    >
      <div className="flex items-center gap-2">
        {/* Expand button for entries with exceptions - fixed width */}
        {hasException ? (
          <button
            onClick={(e) => {
              e.stopPropagation()
              onToggleExpand()
            }}
            className="w-4 flex-shrink-0 p-0.5 hover:bg-muted dark:hover:bg-slate-700 rounded text-muted-foreground"
          >
            {isExpanded ? (
              <ChevronDown className="h-3 w-3" />
            ) : (
              <ChevronRight className="h-3 w-3" />
            )}
          </button>
        ) : (
          <span className="w-4 flex-shrink-0" />
        )}

        {/* Timestamp - fixed width for consistent alignment */}
        <Tooltip>
          <TooltipTrigger asChild>
            <span className="w-[155px] flex-shrink-0 text-muted-foreground tabular-nums leading-5 cursor-default">
              <span className="text-muted-foreground/70">
                {formatRelativeTime(entry.timestamp)}
              </span>
              <span className="mx-1">·</span>
              {formatTimestamp(entry.timestamp)}
            </span>
          </TooltipTrigger>
          <TooltipContent side="top" className="font-mono text-xs">
            {formatFullTimestamp(entry.timestamp)}
          </TooltipContent>
        </Tooltip>

        {/* Level badge - fixed width for consistent alignment */}
        <Badge
          variant="outline"
          className={cn(
            'w-10 flex-shrink-0 justify-center px-1.5 py-0 h-5 text-[10px] font-bold',
            config.bgColor,
            config.textColor
          )}
        >
          {config.label}
        </Badge>

        {/* Source context - fixed width for consistent alignment */}
        <span className="w-[140px] flex-shrink-0 text-muted-foreground/70 truncate leading-5">
          {entry.sourceContext ? `[${entry.sourceContext.split('.').pop()}]` : ''}
        </span>

        {/* Message - ALWAYS visible with syntax highlighting */}
        <span className="flex-1 min-w-0 text-foreground truncate leading-5">
          <LogMessageFormatter message={getDisplayMessage(entry)} />
        </span>

        {/* Action buttons - always visible */}
        <div className="flex-shrink-0 flex items-center gap-1">
          <button
            onClick={handleCopy}
            className="p-1 hover:bg-muted dark:hover:bg-slate-700 rounded text-muted-foreground hover:text-foreground"
            title={t('developerLogs.copyEntryAsJson')}
          >
            {copied ? (
              <Check className="h-3 w-3 text-green-600 dark:text-green-400" />
            ) : (
              <Copy className="h-3 w-3" />
            )}
          </button>
          <button
            onClick={(e) => {
              e.stopPropagation()
              onViewDetail()
            }}
            className="p-1 hover:bg-muted dark:hover:bg-slate-700 rounded text-muted-foreground hover:text-foreground"
            title={t('developerLogs.viewFullDetails')}
          >
            <Maximize2 className="h-3 w-3" />
          </button>
        </div>
      </div>

      {/* Exception details */}
      {isExpanded && entry.exception && (
        <div
          className="mt-2 ml-6 p-2 bg-red-100 dark:bg-red-950/50 rounded border border-red-300 dark:border-red-800"
          onClick={(e) => e.stopPropagation()}
        >
          <div className="font-semibold text-red-700 dark:text-red-300">
            {entry.exception.type}
          </div>
          <div className="text-red-600 dark:text-red-400 mt-1">
            {entry.exception.message}
          </div>
          {entry.exception.stackTrace && (
            <pre className="mt-2 text-[10px] text-red-600/80 dark:text-red-400/80 whitespace-pre-wrap overflow-x-auto">
              {entry.exception.stackTrace}
            </pre>
          )}
        </div>
      )}
    </div>
  )
}
