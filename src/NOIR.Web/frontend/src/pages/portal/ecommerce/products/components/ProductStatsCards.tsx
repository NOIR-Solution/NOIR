import React, { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Card } from '@/components/ui/card'
import { TrendingUp, TrendingDown, Info, Check } from 'lucide-react'
import { PRODUCT_STAT_CARDS_CONFIG, ANIMATION_DURATIONS } from '@/lib/constants/product'
import type { ProductStatus } from '@/types/product'
import { cn } from '@/lib/utils'

interface StatCardProps {
  title: string
  value: number
  trend?: number
  trendDirection?: 'up' | 'down'
  icon: React.ReactNode
  gradientFrom: string
  gradientTo: string
  delay?: number
  isActive?: boolean
  onClick?: () => void
}

const AnimatedCounter: React.FC<{ value: number; duration?: number }> = ({
  value,
  duration = ANIMATION_DURATIONS.counterAnimation
}) => {
  const [count, setCount] = useState(0)

  useEffect(() => {
    let startTime: number | null = null
    let animationId: number | null = null
    const startValue = 0
    const endValue = value

    const animate = (currentTime: number) => {
      if (!startTime) startTime = currentTime
      const progress = Math.min((currentTime - startTime) / duration, 1)

      const easeOutQuart = 1 - Math.pow(1 - progress, 4)
      const currentCount = Math.floor(startValue + (endValue - startValue) * easeOutQuart)

      setCount(currentCount)

      if (progress < 1) {
        animationId = requestAnimationFrame(animate)
      } else {
        setCount(endValue)
      }
    }

    animationId = requestAnimationFrame(animate)

    // Cleanup: cancel animation on unmount to prevent memory leaks
    return () => {
      if (animationId !== null) {
        cancelAnimationFrame(animationId)
      }
    }
  }, [value, duration])

  return <span>{count.toLocaleString()}</span>
}

const StatCard: React.FC<StatCardProps> = ({
  title,
  value,
  trend,
  trendDirection,
  icon,
  gradientFrom,
  gradientTo,
  delay = 0,
  isActive = false,
  onClick,
}) => {
  const [isVisible, setIsVisible] = useState(false)

  useEffect(() => {
    const timer = setTimeout(() => setIsVisible(true), delay)
    return () => clearTimeout(timer)
  }, [delay])

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (onClick && (e.key === 'Enter' || e.key === ' ')) {
      e.preventDefault()
      onClick()
    }
  }

  return (
    <Card
      className={cn(
        `relative overflow-hidden border-border/40 backdrop-blur-xl bg-background/40 shadow-sm transition-all duration-300`,
        isVisible ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-4',
        onClick && 'cursor-pointer hover:shadow-lg hover:scale-[1.02] focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2',
        isActive && 'ring-2 ring-primary ring-offset-2 ring-offset-background'
      )}
      style={{
        background: `linear-gradient(135deg, ${gradientFrom}15 0%, ${gradientTo}10 100%)`,
      }}
      onClick={onClick}
      onKeyDown={handleKeyDown}
      tabIndex={onClick ? 0 : undefined}
      role={onClick ? 'button' : undefined}
      aria-pressed={onClick ? isActive : undefined}
      aria-label={onClick ? `Filter by ${title}` : undefined}
    >
      <div className="absolute inset-0 bg-gradient-to-br from-white/5 to-transparent pointer-events-none" />
      <div className="absolute -right-8 -top-8 w-32 h-32 rounded-full blur-3xl opacity-20"
        style={{ background: gradientFrom }} />

      {/* Active indicator */}
      {isActive && (
        <div className="absolute top-2 right-2 p-1 rounded-full bg-primary text-primary-foreground">
          <Check className="h-3 w-3" />
        </div>
      )}

      <div className="relative p-6">
        <div className="flex items-start justify-between mb-4">
          <div className="flex items-center gap-3">
            <div
              className="p-3 rounded-xl backdrop-blur-sm shadow-lg"
              style={{
                background: `linear-gradient(135deg, ${gradientFrom}20, ${gradientTo}20)`,
              }}
            >
              <div style={{ color: gradientFrom }}>
                {icon}
              </div>
            </div>
            <div>
              <p className="text-sm font-medium text-muted-foreground">{title}</p>
            </div>
          </div>
          {trend !== undefined && trendDirection && (
            <div className={`flex items-center gap-1 px-2 py-1 rounded-full text-xs font-semibold ${
              trendDirection === 'up'
                ? 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400'
                : 'bg-red-500/10 text-red-600 dark:text-red-400'
            }`}>
              {trendDirection === 'up' ? (
                <TrendingUp className="w-3 h-3" />
              ) : (
                <TrendingDown className="w-3 h-3" />
              )}
              <span>{Math.abs(trend)}%</span>
            </div>
          )}
        </div>

        <div className="space-y-2">
          <div className="text-4xl font-bold text-foreground">
            <AnimatedCounter value={value} />
          </div>
          <div className="flex items-center gap-2">
            <div className="h-1 flex-1 bg-muted rounded-full overflow-hidden">
              <div
                className="h-full rounded-full transition-all duration-1000 ease-out"
                style={{
                  width: isVisible ? '100%' : '0%',
                  background: `linear-gradient(90deg, ${gradientFrom}, ${gradientTo})`,
                }}
              />
            </div>
          </div>
        </div>
      </div>
    </Card>
  )
}

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

export function ProductStatsCards({
  stats,
  hasActiveFilters = false,
  activeFilter,
  onFilterChange,
}: ProductStatsCardsProps) {
  const { t } = useTranslation('common')

  const cards = PRODUCT_STAT_CARDS_CONFIG.map(config => {
    const Icon = config.icon
    const statKey = config.key as StatKey
    const filterStatus = statKeyToStatus[statKey]

    return {
      ...config,
      icon: <Icon className="w-5 h-5" />,
      value: stats[statKey],
      isActive: activeFilter === filterStatus && filterStatus !== null,
      onClick: onFilterChange ? () => {
        // Toggle filter: if already active, clear it; otherwise set it
        if (activeFilter === filterStatus) {
          onFilterChange(null)
        } else {
          onFilterChange(filterStatus)
        }
      } : undefined,
    }
  })

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {cards.map((card) => (
          <StatCard key={card.key} {...card} />
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
