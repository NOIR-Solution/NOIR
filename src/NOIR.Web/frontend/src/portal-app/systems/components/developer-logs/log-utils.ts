/**
 * Shared utilities, types, and constants for Developer Logs components.
 */
import type { LucideIcon } from 'lucide-react'
import {
  Bug,
  Info,
  AlertTriangle,
  AlertCircle,
  Skull,
  MessageSquare,
} from 'lucide-react'
import { format, parseISO } from 'date-fns'
import type { LogEntryDto, DevLogLevel } from '@/services/developerLogs'

// Configuration constants
export const LOG_STREAM_CONFIG = {
  MAX_ENTRIES: 1000,
  HISTORY_PAGE_SIZE: 100,
  AUTO_CONNECT: true,
} as const

// Log level configuration with colors and icons
export interface LogLevelConfig {
  value: DevLogLevel
  label: string
  icon: LucideIcon
  bgColor: string
  textColor: string
  borderColor: string
}

export const LOG_LEVELS: LogLevelConfig[] = [
  {
    value: 'Verbose',
    label: 'VRB',
    icon: MessageSquare,
    bgColor: 'bg-slate-100 dark:bg-slate-800',
    textColor: 'text-slate-500 dark:text-slate-400',
    borderColor: 'border-slate-300 dark:border-slate-600',
  },
  {
    value: 'Debug',
    label: 'DBG',
    icon: Bug,
    bgColor: 'bg-purple-100 dark:bg-purple-900/30',
    textColor: 'text-purple-600 dark:text-purple-400',
    borderColor: 'border-purple-300 dark:border-purple-700',
  },
  {
    value: 'Information',
    label: 'INF',
    icon: Info,
    bgColor: 'bg-blue-100 dark:bg-blue-900/30',
    textColor: 'text-blue-600 dark:text-blue-400',
    borderColor: 'border-blue-300 dark:border-blue-700',
  },
  {
    value: 'Warning',
    label: 'WRN',
    icon: AlertTriangle,
    bgColor: 'bg-amber-100 dark:bg-amber-900/30',
    textColor: 'text-amber-600 dark:text-amber-400',
    borderColor: 'border-amber-300 dark:border-amber-700',
  },
  {
    value: 'Error',
    label: 'ERR',
    icon: AlertCircle,
    bgColor: 'bg-red-100 dark:bg-red-900/30',
    textColor: 'text-red-600 dark:text-red-400',
    borderColor: 'border-red-300 dark:border-red-700',
  },
  {
    value: 'Fatal',
    label: 'FTL',
    icon: Skull,
    bgColor: 'bg-red-200 dark:bg-red-900/50',
    textColor: 'text-red-700 dark:text-red-300',
    borderColor: 'border-red-500 dark:border-red-600',
  },
]

export const getLevelConfig = (level: DevLogLevel): LogLevelConfig => {
  return LOG_LEVELS.find(l => l.value === level) || LOG_LEVELS[2] // Default to Information
}

// Format timestamp for display
export const formatTimestamp = (timestamp: string): string => {
  const date = new Date(timestamp)
  return date.toLocaleTimeString('en-US', {
    hour12: false,
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    fractionalSecondDigits: 3,
  })
}

// Format full timestamp with date
export const formatFullTimestamp = (timestamp: string): string => {
  const date = new Date(timestamp)
  return date.toLocaleString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour12: false,
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  })
}

// Format relative time (e.g., "2m ago")
export const formatRelativeTime = (timestamp: string): string => {
  const date = new Date(timestamp)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffSecs = Math.floor(diffMs / 1000)
  const diffMins = Math.floor(diffSecs / 60)
  const diffHours = Math.floor(diffMins / 60)
  const diffDays = Math.floor(diffHours / 24)

  if (diffSecs < 5) return 'now'
  if (diffSecs < 60) return `${diffSecs}s ago`
  if (diffMins < 60) return `${diffMins}m ago`
  if (diffHours < 24) return `${diffHours}h ago`
  return `${diffDays}d ago`
}

// Format bytes to human-readable
export const formatBytes = (bytes: number): string => {
  const sizes = ['B', 'KB', 'MB', 'GB']
  if (bytes === 0) return '0 B'
  const i = Math.floor(Math.log(bytes) / Math.log(1024))
  return `${(bytes / Math.pow(1024, i)).toFixed(1)} ${sizes[i]}`
}

// Format date string for display
export const formatDateDisplay = (dateStr: string): string => {
  try {
    const date = parseISO(dateStr)
    return format(date, 'MMM d, yyyy')
  } catch {
    return dateStr
  }
}

// Get displayable message from log entry (handles empty message field)
export const getDisplayMessage = (entry: LogEntryDto): string => {
  // If message exists, use it
  if (entry.message && entry.message.trim()) {
    return entry.message
  }

  // Fallback to messageTemplate
  if (entry.messageTemplate && entry.messageTemplate.trim()) {
    return entry.messageTemplate
  }

  // Try to extract MessageTemplate from properties (Serilog format)
  if (entry.properties) {
    const props = entry.properties as Record<string, unknown>
    if (typeof props.MessageTemplate === 'string' && props.MessageTemplate.trim()) {
      return props.MessageTemplate
    }
    // Some formats nest it under Properties
    if (props.Properties && typeof props.Properties === 'object') {
      const nestedProps = props.Properties as Record<string, unknown>
      if (typeof nestedProps.MessageTemplate === 'string') {
        return nestedProps.MessageTemplate
      }
    }
  }

  return '(no message)'
}
