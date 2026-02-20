import { useTranslation } from 'react-i18next'
import { Card, CardContent } from '@uikit'
import { Check, Info } from 'lucide-react'
import { PRODUCT_STAT_CARDS_CONFIG } from '@/lib/constants/product'
import type { ProductStatus } from '@/types/product'
import { cn } from '@/lib/utils'

interface ProductStats {
  total: number
  active: number
  draft: number
  outOfStock: number
}

type StatKey = 'total' | 'active' | 'draft' | 'outOfStock'

interface ProductStatsCardsProps {
  stats: ProductStats
  hasActiveFilters?: boolean
  activeFilter?: ProductStatus | null
  onFilterChange?: (status: ProductStatus | null) => void
}

// Map stat keys to ProductStatus values
const statKeyToStatus: Record<StatKey, ProductStatus | null> = {
  total: null,
  active: 'Active',
  draft: 'Draft',
  outOfStock: 'OutOfStock',
}

export const ProductStatsCards = ({
  stats,
  hasActiveFilters = false,
  activeFilter,
  onFilterChange,
}: ProductStatsCardsProps) => {
  const { t } = useTranslation('common')

  const cards = PRODUCT_STAT_CARDS_CONFIG.map(config => {
    const Icon = config.icon
    const statKey = config.key as StatKey
    const filterStatus = statKeyToStatus[statKey]

    return {
      key: config.key,
      title: config.title,
      iconBg: config.iconBg,
      iconColor: config.iconColor,
      icon: <Icon className="h-5 w-5" />,
      value: stats[statKey],
      isActive: activeFilter === filterStatus && filterStatus !== null,
      onClick: onFilterChange ? () => {
        if (activeFilter === filterStatus) {
          onFilterChange(null)
        } else {
          onFilterChange(filterStatus)
        }
      } : undefined,
    }
  })

  const handleKeyDown = (e: React.KeyboardEvent, onClick?: () => void) => {
    if (onClick && (e.key === 'Enter' || e.key === ' ')) {
      e.preventDefault()
      onClick()
    }
  }

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
        {cards.map((card) => (
          <Card
            key={card.key}
            className={cn(
              'shadow-sm hover:shadow-lg transition-all duration-300 relative',
              card.onClick && 'cursor-pointer focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2',
              card.isActive && 'ring-2 ring-primary ring-offset-2 ring-offset-background'
            )}
            onClick={card.onClick}
            onKeyDown={(e) => handleKeyDown(e, card.onClick)}
            tabIndex={card.onClick ? 0 : undefined}
            role={card.onClick ? 'button' : undefined}
            aria-pressed={card.onClick ? card.isActive : undefined}
            aria-label={card.onClick ? `Filter by ${card.title}` : undefined}
          >
            {card.isActive && (
              <div className="absolute top-2 right-2 p-1 rounded-full bg-primary text-primary-foreground">
                <Check className="h-3 w-3" />
              </div>
            )}
            <CardContent className="p-4">
              <div className="flex items-center gap-3">
                <div className={cn('p-2 rounded-xl border', card.iconBg)}>
                  <div className={card.iconColor}>
                    {card.icon}
                  </div>
                </div>
                <div>
                  <p className="text-sm text-muted-foreground">{card.title}</p>
                  <p className="text-2xl font-bold">{card.value.toLocaleString()}</p>
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
      {onFilterChange && (
        <div className="flex items-center justify-center gap-2 text-xs text-muted-foreground">
          <Info className="h-3.5 w-3.5" />
          <p>{t('products.clickToFilter', 'Click a card to filter by status. Click again to clear.')}</p>
        </div>
      )}
      {hasActiveFilters && !onFilterChange && (
        <div className="flex items-center justify-center gap-2 text-xs text-muted-foreground">
          <Info className="h-3.5 w-3.5" />
          <p>{t('products.statsNote', 'Note: Status counts reflect the current page only. Total is accurate.')}</p>
        </div>
      )}
    </div>
  )
}
