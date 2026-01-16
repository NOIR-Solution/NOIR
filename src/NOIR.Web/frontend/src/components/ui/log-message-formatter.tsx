/**
 * Log Message Formatter
 *
 * Provides syntax highlighting and visual enhancements for log messages.
 * Inspired by tools like Datadog, Grafana, tailspin, and hl.
 *
 * Features:
 * - HTTP method color coding (GET=blue, POST=green, PUT=orange, DELETE=red)
 * - Status code coloring (2xx=green, 3xx=blue, 4xx=yellow, 5xx=red)
 * - Response time highlighting with thresholds (<100ms=green, 100-500ms=yellow, >500ms=red)
 * - Handler/Command/Query name highlighting (teal for names)
 * - Handler keywords (Handling=blue, Handled=teal, successfully=green, failed=red)
 * - UUID highlighting
 * - URL/path highlighting
 * - Quoted string highlighting
 * - Number highlighting
 * - Key-value pair highlighting
 */
import React, { useMemo } from 'react'
import { cn } from '@/lib/utils'
import { Badge } from '@/components/ui/badge'
import { Clock, Zap, AlertTriangle, Play, CheckCircle2, XCircle } from 'lucide-react'

// Response time thresholds (in milliseconds)
const RESPONSE_TIME_THRESHOLDS = {
  fast: 100,    // < 100ms = fast (green)
  normal: 500,  // 100-500ms = normal (yellow)
  slow: 1000,   // 500-1000ms = slow (orange)
  // > 1000ms = very slow (red)
} as const

// HTTP method styles
const HTTP_METHOD_STYLES: Record<string, string> = {
  GET: 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300',
  POST: 'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300',
  PUT: 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300',
  PATCH: 'bg-purple-100 text-purple-700 dark:bg-purple-900/40 dark:text-purple-300',
  DELETE: 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300',
  OPTIONS: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
  HEAD: 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300',
}

// Status code styles
const getStatusCodeStyle = (code: number): string => {
  if (code >= 200 && code < 300) {
    return 'bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-300'
  }
  if (code >= 300 && code < 400) {
    return 'bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300'
  }
  if (code >= 400 && code < 500) {
    return 'bg-amber-100 text-amber-700 dark:bg-amber-900/40 dark:text-amber-300'
  }
  if (code >= 500) {
    return 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300'
  }
  return 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300'
}

// Response time styles
const getResponseTimeStyle = (ms: number): { className: string; icon: typeof Clock | null; label: string } => {
  if (ms < RESPONSE_TIME_THRESHOLDS.fast) {
    return {
      className: 'text-green-600 dark:text-green-400',
      icon: Zap,
      label: 'fast',
    }
  }
  if (ms < RESPONSE_TIME_THRESHOLDS.normal) {
    return {
      className: 'text-amber-600 dark:text-amber-400',
      icon: Clock,
      label: 'normal',
    }
  }
  if (ms < RESPONSE_TIME_THRESHOLDS.slow) {
    return {
      className: 'text-orange-600 dark:text-orange-400 font-medium',
      icon: Clock,
      label: 'slow',
    }
  }
  return {
    className: 'text-red-600 dark:text-red-400 font-semibold',
    icon: AlertTriangle,
    label: 'very slow',
  }
}

// Regex patterns for log message parsing
const PATTERNS = {
  // HTTP request pattern: HTTP "METHOD" "PATH" responded STATUS in DURATION ms
  httpRequest: /HTTP\s+"(GET|POST|PUT|PATCH|DELETE|OPTIONS|HEAD)"\s+"([^"]+)"\s+responded\s+(\d{3})\s+in\s+([\d.]+)\s*ms/gi,
  // Handler patterns: Handling "QueryName" or Handled "QueryName" [successfully|failed]
  handlerStart: /\bHandling\s+"([^"]+)"/gi,
  handlerComplete: /\bHandled\s+"([^"]+)"(?:\s+(successfully|failed))?/gi,
  // Success/failure keywords (standalone)
  successKeyword: /\b(successfully|completed|success)\b/gi,
  failureKeyword: /\b(failed|error|failure|exception)\b/gi,
  // UUID pattern
  uuid: /\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/gi,
  // Quoted strings
  quotedString: /"([^"]+)"/g,
  // Numbers with units (e.g., 123ms, 456.78s)
  numberWithUnit: /\b(\d+(?:\.\d+)?)\s*(ms|s|kb|mb|gb|bytes?)\b/gi,
  // Standalone numbers
  number: /\b(\d+(?:\.\d+)?)\b/g,
  // Key-value pairs (e.g., key: value, key=value)
  keyValue: /\b(\w+)[:=]\s*("[^"]*"|\S+)/g,
  // URL paths
  urlPath: /(?:\/[\w\-./]+)+(?:\?[\w\-=&]+)?/g,
  // IP addresses
  ipAddress: /\b(?:\d{1,3}\.){3}\d{1,3}(?::\d+)?\b/g,
  // Correlation IDs
  correlationId: /CorrelationId:\s*"?([^"\s]+)"?/gi,
  // Duration patterns like "in 5ms" or "successfully in 47ms"
  duration: /\b(?:in|took)\s+(\d+(?:\.\d+)?)\s*ms\b/gi,
}

interface FormattedSegment {
  type: 'text' | 'http-method' | 'status-code' | 'duration' | 'uuid' | 'path' | 'quoted' | 'number' | 'correlation-id' | 'handler-start' | 'handler-complete' | 'handler-name' | 'success-keyword' | 'failure-keyword'
  content: string
  metadata?: {
    method?: string
    statusCode?: number
    durationMs?: number
    path?: string
    handlerName?: string
    isSuccess?: boolean
  }
}

/**
 * Parse HTTP request log messages
 */
function parseHttpRequest(message: string): FormattedSegment[] | null {
  const match = PATTERNS.httpRequest.exec(message)
  if (!match) return null

  PATTERNS.httpRequest.lastIndex = 0 // Reset regex

  const [fullMatch, method, path, status, duration] = match
  const startIndex = message.indexOf(fullMatch)
  const segments: FormattedSegment[] = []

  // Add text before the match
  if (startIndex > 0) {
    segments.push({ type: 'text', content: message.slice(0, startIndex) })
  }

  // Add HTTP method
  segments.push({
    type: 'http-method',
    content: method,
    metadata: { method },
  })

  segments.push({ type: 'text', content: ' ' })

  // Add path
  segments.push({
    type: 'path',
    content: `"${path}"`,
    metadata: { path },
  })

  segments.push({ type: 'text', content: ' responded ' })

  // Add status code
  segments.push({
    type: 'status-code',
    content: status,
    metadata: { statusCode: parseInt(status, 10) },
  })

  segments.push({ type: 'text', content: ' in ' })

  // Add duration
  segments.push({
    type: 'duration',
    content: `${duration}ms`,
    metadata: { durationMs: parseFloat(duration) },
  })

  // Add remaining text
  const endIndex = startIndex + fullMatch.length
  if (endIndex < message.length) {
    segments.push({ type: 'text', content: message.slice(endIndex) })
  }

  return segments
}

/**
 * Parse Handler log messages (Handling/Handled patterns)
 *
 * Supported formats:
 * - "Handling 'QueryName'"
 * - "Handled 'QueryName' successfully"
 * - "Handled 'QueryName' successfully in 47ms"
 * - "Handled 'QueryName' failed"
 * - "Handled 'QueryName'" (no status indicator - neutral display)
 *
 * Remaining text after the match is delegated to parseGenericMessage()
 * for secondary highlighting (durations, UUIDs, correlation IDs, etc.)
 */
function parseHandlerMessage(message: string): FormattedSegment[] | null {
  // Try to match "Handled X successfully/failed" first (more specific)
  const completeRegex = new RegExp(PATTERNS.handlerComplete.source, 'gi')
  const completeMatch = completeRegex.exec(message)

  if (completeMatch) {
    const [fullMatch, handlerName, status] = completeMatch
    const startIndex = message.indexOf(fullMatch)
    const segments: FormattedSegment[] = []
    // isSuccess: true = success, false = failed, undefined = no status keyword
    const isSuccess = status ? status.toLowerCase() === 'successfully' : undefined

    // Add text before the match
    if (startIndex > 0) {
      segments.push({ type: 'text', content: message.slice(0, startIndex) })
    }

    // Add "Handled" keyword
    segments.push({
      type: 'handler-complete',
      content: 'Handled',
      metadata: { isSuccess },
    })

    segments.push({ type: 'text', content: ' ' })

    // Add handler name with quotes
    segments.push({
      type: 'handler-name',
      content: `"${handlerName}"`,
      metadata: { handlerName },
    })

    // Add success/failure keyword if present
    if (status) {
      segments.push({ type: 'text', content: ' ' })

      segments.push({
        type: isSuccess ? 'success-keyword' : 'failure-keyword',
        content: status,
        metadata: { isSuccess },
      })
    }

    // Add remaining text and continue parsing
    const endIndex = startIndex + fullMatch.length
    if (endIndex < message.length) {
      const remainingSegments = parseGenericMessage(message.slice(endIndex))
      segments.push(...remainingSegments)
    }

    return segments
  }

  // Try to match "Handling X" (start pattern)
  const startRegex = new RegExp(PATTERNS.handlerStart.source, 'gi')
  const startMatch = startRegex.exec(message)

  if (startMatch) {
    const [fullMatch, handlerName] = startMatch
    const startIndex = message.indexOf(fullMatch)
    const segments: FormattedSegment[] = []

    // Add text before the match
    if (startIndex > 0) {
      segments.push({ type: 'text', content: message.slice(0, startIndex) })
    }

    // Add "Handling" keyword
    segments.push({
      type: 'handler-start',
      content: 'Handling',
    })

    segments.push({ type: 'text', content: ' ' })

    // Add handler name with quotes
    segments.push({
      type: 'handler-name',
      content: `"${handlerName}"`,
      metadata: { handlerName },
    })

    // Add remaining text and continue parsing
    const endIndex = startIndex + fullMatch.length
    if (endIndex < message.length) {
      const remainingSegments = parseGenericMessage(message.slice(endIndex))
      segments.push(...remainingSegments)
    }

    return segments
  }

  return null
}

/**
 * Parse generic log messages with highlighting
 */
function parseGenericMessage(message: string): FormattedSegment[] {
  const segments: FormattedSegment[] = []
  let lastIndex = 0

  // Find and highlight UUIDs
  const uuidMatches = [...message.matchAll(new RegExp(PATTERNS.uuid.source, 'gi'))]

  // Find and highlight durations
  const durationMatches = [...message.matchAll(new RegExp(PATTERNS.duration.source, 'gi'))]

  // Find and highlight correlation IDs
  const correlationMatches = [...message.matchAll(new RegExp(PATTERNS.correlationId.source, 'gi'))]

  // Combine all matches and sort by index
  const allMatches: Array<{ index: number; length: number; type: FormattedSegment['type']; content: string; metadata?: FormattedSegment['metadata'] }> = []

  for (const match of uuidMatches) {
    if (match.index !== undefined) {
      allMatches.push({
        index: match.index,
        length: match[0].length,
        type: 'uuid',
        content: match[0],
      })
    }
  }

  for (const match of durationMatches) {
    if (match.index !== undefined) {
      const durationMs = parseFloat(match[1])
      allMatches.push({
        index: match.index,
        length: match[0].length,
        type: 'duration',
        content: match[0],
        metadata: { durationMs },
      })
    }
  }

  for (const match of correlationMatches) {
    if (match.index !== undefined) {
      allMatches.push({
        index: match.index,
        length: match[0].length,
        type: 'correlation-id',
        content: match[0],
      })
    }
  }

  // Sort by index
  allMatches.sort((a, b) => a.index - b.index)

  // Build segments
  for (const match of allMatches) {
    // Add text before this match
    if (match.index > lastIndex) {
      segments.push({
        type: 'text',
        content: message.slice(lastIndex, match.index),
      })
    }

    // Add the matched segment
    segments.push({
      type: match.type,
      content: match.content,
      metadata: match.metadata,
    })

    lastIndex = match.index + match.length
  }

  // Add remaining text
  if (lastIndex < message.length) {
    segments.push({
      type: 'text',
      content: message.slice(lastIndex),
    })
  }

  // If no matches, return single text segment
  if (segments.length === 0) {
    segments.push({ type: 'text', content: message })
  }

  return segments
}

/**
 * Parse and format a log message
 *
 * Pattern Precedence (first match wins):
 * 1. HTTP Request - "HTTP 'METHOD' 'PATH' responded STATUS in DURATIONms"
 * 2. Handler - "Handling 'Name'" or "Handled 'Name' [successfully|failed]"
 * 3. Generic - UUIDs, durations, correlation IDs, etc.
 *
 * Note: Only one primary pattern type is matched per message. If a message
 * contains multiple patterns, the higher priority pattern takes precedence.
 * Remaining text is parsed generically for secondary highlights.
 */
function parseLogMessage(message: string): FormattedSegment[] {
  // Try HTTP request pattern first (highest priority)
  const httpSegments = parseHttpRequest(message)
  if (httpSegments) {
    return httpSegments
  }

  // Try Handler patterns (Handling/Handled)
  const handlerSegments = parseHandlerMessage(message)
  if (handlerSegments) {
    return handlerSegments
  }

  // Fall back to generic parsing
  return parseGenericMessage(message)
}

/**
 * Render a formatted segment
 */
function renderSegment(segment: FormattedSegment, index: number): React.ReactNode {
  switch (segment.type) {
    case 'http-method': {
      const style = HTTP_METHOD_STYLES[segment.metadata?.method || 'GET']
      return (
        <Badge
          key={index}
          variant="outline"
          className={cn('px-1.5 py-0 h-5 text-[10px] font-bold', style)}
        >
          {segment.content}
        </Badge>
      )
    }

    case 'status-code': {
      const code = segment.metadata?.statusCode || 200
      const style = getStatusCodeStyle(code)
      return (
        <Badge
          key={index}
          variant="outline"
          className={cn('px-1.5 py-0 h-5 text-[10px] font-bold', style)}
        >
          {segment.content}
        </Badge>
      )
    }

    case 'duration': {
      const ms = segment.metadata?.durationMs || 0
      const { className, icon: Icon } = getResponseTimeStyle(ms)
      return (
        <span key={index} className={cn('inline-flex items-center gap-0.5', className)}>
          {Icon && <Icon className="h-3 w-3" />}
          <span className="tabular-nums">{segment.content}</span>
        </span>
      )
    }

    case 'uuid': {
      return (
        <span
          key={index}
          className="text-purple-600 dark:text-purple-400 font-mono text-[11px]"
          title="UUID"
        >
          {segment.content}
        </span>
      )
    }

    case 'path': {
      return (
        <span
          key={index}
          className="text-cyan-600 dark:text-cyan-400"
        >
          {segment.content}
        </span>
      )
    }

    case 'correlation-id': {
      return (
        <span
          key={index}
          className="text-indigo-600 dark:text-indigo-400"
          title="Correlation ID"
        >
          {segment.content}
        </span>
      )
    }

    case 'quoted': {
      return (
        <span
          key={index}
          className="text-emerald-600 dark:text-emerald-400"
        >
          {segment.content}
        </span>
      )
    }

    case 'number': {
      return (
        <span
          key={index}
          className="text-amber-600 dark:text-amber-400 tabular-nums"
        >
          {segment.content}
        </span>
      )
    }

    case 'handler-start': {
      return (
        <Badge
          key={index}
          variant="outline"
          className="px-1.5 py-0 h-5 text-[10px] font-semibold bg-blue-100 text-blue-700 dark:bg-blue-900/40 dark:text-blue-300 gap-1"
        >
          <Play className="h-2.5 w-2.5" />
          {segment.content}
        </Badge>
      )
    }

    case 'handler-complete': {
      const isSuccess = segment.metadata?.isSuccess
      const isFailure = isSuccess === false
      return (
        <Badge
          key={index}
          variant="outline"
          className={cn(
            'px-1.5 py-0 h-5 text-[10px] font-semibold gap-1',
            isSuccess === true
              ? 'bg-teal-100 text-teal-700 dark:bg-teal-900/40 dark:text-teal-300'
              : isFailure
              ? 'bg-red-100 text-red-700 dark:bg-red-900/40 dark:text-red-300'
              : 'bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-300' // neutral for unknown
          )}
        >
          {isSuccess === true ? (
            <CheckCircle2 className="h-2.5 w-2.5" />
          ) : isFailure ? (
            <XCircle className="h-2.5 w-2.5" />
          ) : (
            <CheckCircle2 className="h-2.5 w-2.5" /> // completed but status unknown
          )}
          {segment.content}
        </Badge>
      )
    }

    case 'handler-name': {
      return (
        <span
          key={index}
          className="text-teal-600 dark:text-teal-400 font-medium"
          title="Handler/Query/Command"
        >
          {segment.content}
        </span>
      )
    }

    case 'success-keyword': {
      return (
        <span
          key={index}
          className="text-green-600 dark:text-green-400 font-medium"
        >
          {segment.content}
        </span>
      )
    }

    case 'failure-keyword': {
      return (
        <span
          key={index}
          className="text-red-600 dark:text-red-400 font-semibold"
        >
          {segment.content}
        </span>
      )
    }

    default:
      return <span key={index}>{segment.content}</span>
  }
}

export interface LogMessageFormatterProps {
  message: string
  className?: string
}

/**
 * Format and highlight a log message with syntax highlighting
 */
export function LogMessageFormatter({ message, className }: LogMessageFormatterProps) {
  const segments = useMemo(() => parseLogMessage(message), [message])

  return (
    <span className={cn('break-all', className)}>
      {segments.map((segment, index) => renderSegment(segment, index))}
    </span>
  )
}

/**
 * Get response time style info for external use
 */
export { getResponseTimeStyle, getStatusCodeStyle, HTTP_METHOD_STYLES, RESPONSE_TIME_THRESHOLDS }
