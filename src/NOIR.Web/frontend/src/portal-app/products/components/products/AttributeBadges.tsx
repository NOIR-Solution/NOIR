import { Badge, Tooltip, TooltipContent, TooltipProvider, TooltipTrigger } from '@uikit'

import type { ProductAttributeDisplay } from '@/types/product'

interface AttributeBadgesProps {
  displayAttributes: ProductAttributeDisplay[]
  maxColors?: number
}

/**
 * Displays product attributes as compact badges
 * - Color attributes: Circular color swatches with overflow indicator
 * - Text/other attributes: Pills with value and optional unit
 */
export const AttributeBadges = ({
  displayAttributes,
  maxColors = 5,
}: AttributeBadgesProps) => {
  if (!displayAttributes || displayAttributes.length === 0) {
    return null
  }

  // Separate color attributes from others
  const colorAttributes = displayAttributes.filter(
    (attr) => attr.type === 'Color' && attr.colorCode
  )
  const otherAttributes = displayAttributes.filter(
    (attr) => attr.type !== 'Color' || !attr.colorCode
  )

  // Calculate visible colors and overflow
  const visibleColors = colorAttributes.slice(0, maxColors)
  const overflowCount = colorAttributes.length - maxColors

  return (
    <div className="flex flex-wrap gap-1 items-center">
      {/* Color swatches */}
      {visibleColors.length > 0 && (
        <TooltipProvider>
          <div className="flex items-center gap-1">
            {visibleColors.map((attr) => (
              <Tooltip key={`${attr.code}-${attr.colorCode}`}>
                <TooltipTrigger asChild>
                  <div
                    className="w-5 h-5 rounded-full border border-border/60 shadow-sm cursor-default transition-transform hover:scale-110"
                    style={{ backgroundColor: attr.colorCode ?? undefined }}
                    aria-label={attr.displayValue ?? attr.name}
                  />
                </TooltipTrigger>
                <TooltipContent side="top" className="text-xs">
                  <p>
                    {attr.name}: {attr.displayValue}
                  </p>
                </TooltipContent>
              </Tooltip>
            ))}
            {overflowCount > 0 && (
              <Tooltip>
                <TooltipTrigger asChild>
                  <div className="w-5 h-5 rounded-full border border-border/60 bg-muted flex items-center justify-center cursor-default">
                    <span className="text-[10px] font-medium text-muted-foreground">
                      +{overflowCount}
                    </span>
                  </div>
                </TooltipTrigger>
                <TooltipContent side="top" className="text-xs">
                  <p>
                    {overflowCount} more color{overflowCount > 1 ? 's' : ''}
                  </p>
                </TooltipContent>
              </Tooltip>
            )}
          </div>
        </TooltipProvider>
      )}

      {/* Text/other attribute badges */}
      {otherAttributes.map((attr) => (
        <Badge
          key={`${attr.code}-${attr.displayValue}`}
          variant="secondary"
          className="text-xs px-2 py-0.5"
        >
          {attr.displayValue}
          {(attr as any).unit && (
            <span className="text-muted-foreground ml-0.5">{(attr as any).unit}</span>
          )}
        </Badge>
      ))}
    </div>
  )
}
