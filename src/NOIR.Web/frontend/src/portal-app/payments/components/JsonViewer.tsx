import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { ChevronDown, ChevronRight } from 'lucide-react'
import { Button } from '@uikit'

interface JsonViewerProps {
  data?: string
  maxHeight?: string
}

export const JsonViewer = ({ data, maxHeight = '300px' }: JsonViewerProps) => {
  const { t } = useTranslation('common')
  const [isExpanded, setIsExpanded] = useState(false)

  if (!data) return <span className="text-muted-foreground">&mdash;</span>

  const formatted = (() => {
    try {
      return JSON.stringify(JSON.parse(data), null, 2)
    } catch {
      return data
    }
  })()

  return (
    <div>
      <Button
        variant="ghost"
        size="sm"
        onClick={() => setIsExpanded(!isExpanded)}
        className="cursor-pointer h-7 px-2 text-xs"
        aria-label={isExpanded ? t('payments.detail.hideData') : t('payments.detail.showData')}
      >
        {isExpanded ? (
          <ChevronDown className="h-3 w-3 mr-1" />
        ) : (
          <ChevronRight className="h-3 w-3 mr-1" />
        )}
        {isExpanded ? t('payments.detail.hideData') : t('payments.detail.showData')}
      </Button>
      {isExpanded && (
        <pre
          className="mt-2 p-3 bg-muted rounded-md text-xs overflow-auto font-mono"
          style={{ maxHeight }}
        >
          {formatted}
        </pre>
      )}
    </div>
  )
}
