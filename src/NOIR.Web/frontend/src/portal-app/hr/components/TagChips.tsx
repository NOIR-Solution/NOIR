import { X } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import { Badge, Tooltip, TooltipContent, TooltipTrigger } from '@uikit'
import type { TagBriefDto } from '@/types/hr'

interface TagChipsProps {
  tags: TagBriefDto[]
  maxVisible?: number
  onRemove?: (tagId: string) => void
  size?: 'sm' | 'md'
}

export const TagChips = ({ tags, maxVisible = 3, onRemove, size = 'sm' }: TagChipsProps) => {
  const { t } = useTranslation('common')
  const safeTags = tags ?? []
  const visibleTags = safeTags.slice(0, maxVisible)
  const hiddenCount = safeTags.length - maxVisible

  if (safeTags.length === 0) return null

  return (
    <div className="flex flex-wrap gap-1 items-center">
      {visibleTags.map((tag) => (
        <Badge
          key={tag.id}
          variant="outline"
          className={`${size === 'sm' ? 'text-xs px-1.5 py-0' : 'text-xs px-2 py-0.5'} ${onRemove ? 'pr-0.5' : ''}`}
          style={{ borderColor: tag.color, color: tag.color }}
        >
          <span
            className="inline-block h-1.5 w-1.5 rounded-full mr-1 flex-shrink-0"
            style={{ backgroundColor: tag.color }}
          />
          {tag.name}
          {onRemove && (
            <button
              type="button"
              className="ml-1 p-0.5 rounded-full hover:bg-muted/50 cursor-pointer transition-colors"
              onClick={(e) => {
                e.stopPropagation()
                onRemove(tag.id)
              }}
              aria-label={t('hr.tags.removeTags')}
            >
              <X className="h-3 w-3" />
            </button>
          )}
        </Badge>
      ))}
      {hiddenCount > 0 && (
        <Tooltip>
          <TooltipTrigger asChild>
            <Badge variant="outline" className="text-xs px-1.5 py-0 cursor-default">
              +{hiddenCount}
            </Badge>
          </TooltipTrigger>
          <TooltipContent>
            <div className="space-y-1">
              {safeTags.slice(maxVisible).map((tag) => (
                <div key={tag.id} className="flex items-center gap-1.5">
                  <span
                    className="inline-block h-2 w-2 rounded-full flex-shrink-0"
                    style={{ backgroundColor: tag.color }}
                  />
                  <span className="text-xs">{tag.name}</span>
                </div>
              ))}
            </div>
          </TooltipContent>
        </Tooltip>
      )}
    </div>
  )
}
