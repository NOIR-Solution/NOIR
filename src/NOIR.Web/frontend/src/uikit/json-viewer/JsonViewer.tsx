import { useState, useMemo, useCallback } from 'react'
import { ChevronRight, ChevronDown, Copy, Check, Maximize2 } from 'lucide-react'
import { cn } from '@/lib/utils'
import { Button } from '../button/Button'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '../dialog/Dialog'

interface JsonViewerProps {
  data: unknown
  className?: string
  defaultExpanded?: boolean
  maxDepth?: number
  rootName?: string
  title?: string
  allowFullscreen?: boolean
  maxHeight?: string
}

type JsonValue = string | number | boolean | null | JsonObject | JsonArray
interface JsonObject {
  [key: string]: JsonValue
}
type JsonArray = JsonValue[]

// Color classes for different JSON types
const typeColors = {
  string: 'text-green-600 dark:text-green-400',
  number: 'text-blue-600 dark:text-blue-400',
  boolean: 'text-purple-600 dark:text-purple-400',
  null: 'text-gray-500 dark:text-gray-400',
  key: 'text-rose-600 dark:text-rose-400',
  bracket: 'text-gray-600 dark:text-gray-400',
}

const JsonNode = ({
  name,
  value,
  depth,
  defaultExpanded,
  maxDepth,
}: {
  name?: string
  value: JsonValue
  depth: number
  defaultExpanded: boolean
  maxDepth: number
}) => {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded && depth < maxDepth)

  const valueType = useMemo(() => {
    if (value === null) return 'null'
    if (Array.isArray(value)) return 'array'
    return typeof value
  }, [value])

  const isExpandable = valueType === 'object' || valueType === 'array'
  const isEmpty =
    (valueType === 'object' && Object.keys(value as JsonObject).length === 0) ||
    (valueType === 'array' && (value as JsonArray).length === 0)

  const toggleExpand = useCallback(() => {
    if (isExpandable && !isEmpty) {
      setIsExpanded((prev) => !prev)
    }
  }, [isExpandable, isEmpty])

  const renderValue = () => {
    switch (valueType) {
      case 'string':
        return (
          <span className={typeColors.string}>
            &quot;{String(value).length > 100 ? `${String(value).slice(0, 100)}...` : String(value)}
            &quot;
          </span>
        )
      case 'number':
        return <span className={typeColors.number}>{String(value)}</span>
      case 'boolean':
        return <span className={typeColors.boolean}>{String(value)}</span>
      case 'null':
        return <span className={typeColors.null}>null</span>
      case 'array':
        if (isEmpty) {
          return <span className={typeColors.bracket}>[]</span>
        }
        return (
          <>
            <span className={typeColors.bracket}>[</span>
            <span className="text-muted-foreground text-xs ml-1">
              {(value as JsonArray).length} items
            </span>
          </>
        )
      case 'object':
        if (isEmpty) {
          return <span className={typeColors.bracket}>{'{}'}</span>
        }
        return (
          <>
            <span className={typeColors.bracket}>{'{'}</span>
            <span className="text-muted-foreground text-xs ml-1">
              {Object.keys(value as JsonObject).length} keys
            </span>
          </>
        )
      default:
        return <span>{String(value)}</span>
    }
  }

  const renderChildren = () => {
    if (!isExpandable || isEmpty || !isExpanded) return null

    if (valueType === 'array') {
      return (value as JsonArray).map((item, index) => (
        <JsonNode
          key={index}
          name={String(index)}
          value={item}
          depth={depth + 1}
          defaultExpanded={defaultExpanded}
          maxDepth={maxDepth}
        />
      ))
    }

    if (valueType === 'object') {
      return Object.entries(value as JsonObject).map(([key, val]) => (
        <JsonNode
          key={key}
          name={key}
          value={val}
          depth={depth + 1}
          defaultExpanded={defaultExpanded}
          maxDepth={maxDepth}
        />
      ))
    }

    return null
  }

  return (
    <div className="font-mono text-sm">
      <div
        className={cn(
          'flex items-start gap-1 py-0.5 rounded hover:bg-muted/50 transition-colors',
          isExpandable && !isEmpty && 'cursor-pointer'
        )}
        style={{ paddingLeft: `${depth * 16}px` }}
        onClick={toggleExpand}
      >
        {/* Expand/collapse icon */}
        <span className="w-4 h-4 flex items-center justify-center flex-shrink-0 mt-0.5">
          {isExpandable && !isEmpty ? (
            isExpanded ? (
              <ChevronDown className="h-3 w-3 text-muted-foreground" />
            ) : (
              <ChevronRight className="h-3 w-3 text-muted-foreground" />
            )
          ) : null}
        </span>

        {/* Key name */}
        {name !== undefined && (
          <>
            <span className={typeColors.key}>&quot;{name}&quot;</span>
            <span className={typeColors.bracket}>:</span>
          </>
        )}

        {/* Value */}
        <span className="break-all">{renderValue()}</span>
      </div>

      {/* Children */}
      {isExpanded && renderChildren()}

      {/* Closing bracket */}
      {isExpanded && isExpandable && !isEmpty && (
        <div
          className="py-0.5"
          style={{ paddingLeft: `${depth * 16}px` }}
        >
          <span className="w-4 inline-block" />
          <span className={typeColors.bracket}>
            {valueType === 'array' ? ']' : '}'}
          </span>
        </div>
      )}
    </div>
  )
}

export const JsonViewer = ({
  data,
  className,
  defaultExpanded = true,
  maxDepth = 5,
  rootName,
  title,
  allowFullscreen = true,
  maxHeight = '300px',
}: JsonViewerProps) => {
  const [copied, setCopied] = useState(false)
  const [isFullscreen, setIsFullscreen] = useState(false)

  const parsedData = useMemo(() => {
    if (typeof data === 'string') {
      try {
        return JSON.parse(data)
      } catch {
        return data
      }
    }
    return data
  }, [data])

  const formattedJson = useMemo(() => {
    try {
      return typeof data === 'string' ? data : JSON.stringify(data, null, 2)
    } catch {
      return String(data)
    }
  }, [data])

  const handleCopy = useCallback(async () => {
    try {
      await navigator.clipboard.writeText(formattedJson)
      setCopied(true)
      setTimeout(() => setCopied(false), 2000)
    } catch {
      // Clipboard not available (e.g., insecure context)
    }
  }, [formattedJson])

  if (parsedData === undefined || parsedData === null || parsedData === '') {
    return (
      <div className={cn('p-3 bg-muted/50 rounded-lg text-sm text-muted-foreground', className)}>
        No data
      </div>
    )
  }

  // If it's a simple string that couldn't be parsed as JSON
  if (typeof parsedData === 'string') {
    return (
      <div className={cn('relative group', className)}>
        <div className="absolute top-2 right-2 flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity z-10">
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7"
            onClick={handleCopy}
          >
            {copied ? (
              <Check className="h-3.5 w-3.5 text-green-500" />
            ) : (
              <Copy className="h-3.5 w-3.5" />
            )}
          </Button>
        </div>
        <pre className="p-3 bg-muted/50 rounded-lg text-sm overflow-auto font-mono whitespace-pre-wrap break-all">
          {parsedData}
        </pre>
      </div>
    )
  }

  const JsonContent = ({ expanded, depth, height }: { expanded: boolean; depth: number; height?: string }) => (
    <div className="p-3 bg-muted/50 rounded-lg overflow-auto" style={{ maxHeight: height }}>
      <JsonNode
        name={rootName}
        value={parsedData as JsonValue}
        depth={0}
        defaultExpanded={expanded}
        maxDepth={depth}
      />
    </div>
  )

  return (
    <>
      <div className={cn('relative group', className)}>
        {/* Action buttons */}
        <div className="absolute top-2 right-2 flex gap-1 opacity-0 group-hover:opacity-100 transition-opacity z-10">
          <Button
            variant="ghost"
            size="icon"
            className="h-7 w-7"
            onClick={handleCopy}
            title="Copy to clipboard"
          >
            {copied ? (
              <Check className="h-3.5 w-3.5 text-green-500" />
            ) : (
              <Copy className="h-3.5 w-3.5" />
            )}
          </Button>
          {allowFullscreen && (
            <Button
              variant="ghost"
              size="icon"
              className="h-7 w-7"
              onClick={() => setIsFullscreen(true)}
              title="View fullscreen"
            >
              <Maximize2 className="h-3.5 w-3.5" />
            </Button>
          )}
        </div>
        <JsonContent expanded={defaultExpanded} depth={maxDepth} height={maxHeight} />
      </div>

      {/* Fullscreen Dialog */}
      <Dialog open={isFullscreen} onOpenChange={setIsFullscreen}>
        <DialogContent className="max-w-4xl max-h-[90vh] flex flex-col">
          <DialogHeader className="flex-shrink-0">
            <DialogTitle className="flex items-center justify-between">
              <span>{title || 'JSON Data'}</span>
              <Button
                variant="ghost"
                size="icon"
                className="h-8 w-8"
                onClick={handleCopy}
              >
                {copied ? (
                  <Check className="h-4 w-4 text-green-500" />
                ) : (
                  <Copy className="h-4 w-4" />
                )}
              </Button>
            </DialogTitle>
          </DialogHeader>
          <div className="flex-1 overflow-hidden">
            <JsonContent expanded={true} depth={10} height="calc(90vh - 120px)" />
          </div>
        </DialogContent>
      </Dialog>
    </>
  )
}
