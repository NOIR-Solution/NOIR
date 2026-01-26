import React, { useEffect, useState } from 'react'
import { Card } from '@/components/ui/card'
import { TrendingUp, TrendingDown, Info } from 'lucide-react'
import { PRODUCT_STAT_CARDS_CONFIG, ANIMATION_DURATIONS } from '@/lib/constants/product'

interface StatCardProps {
  title: string
  value: number
  trend?: number
  trendDirection?: 'up' | 'down'
  icon: React.ReactNode
  gradientFrom: string
  gradientTo: string
  delay?: number
}

const AnimatedCounter: React.FC<{ value: number; duration?: number }> = ({
  value,
  duration = ANIMATION_DURATIONS.counterAnimation
}) => {
  const [count, setCount] = useState(0)

  useEffect(() => {
    let startTime: number | null = null
    const startValue = 0
    const endValue = value

    const animate = (currentTime: number) => {
      if (!startTime) startTime = currentTime
      const progress = Math.min((currentTime - startTime) / duration, 1)

      const easeOutQuart = 1 - Math.pow(1 - progress, 4)
      const currentCount = Math.floor(startValue + (endValue - startValue) * easeOutQuart)

      setCount(currentCount)

      if (progress < 1) {
        requestAnimationFrame(animate)
      } else {
        setCount(endValue)
      }
    }

    requestAnimationFrame(animate)
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
}) => {
  const [isVisible, setIsVisible] = useState(false)

  useEffect(() => {
    const timer = setTimeout(() => setIsVisible(true), delay)
    return () => clearTimeout(timer)
  }, [delay])

  return (
    <Card
      className={`relative overflow-hidden border-border/40 backdrop-blur-xl bg-background/40 shadow-sm hover:shadow-lg transition-all duration-300 ${
        isVisible ? 'opacity-100 translate-y-0' : 'opacity-0 translate-y-4'
      }`}
      style={{
        background: `linear-gradient(135deg, ${gradientFrom}15 0%, ${gradientTo}10 100%)`,
      }}
    >
      <div className="absolute inset-0 bg-gradient-to-br from-white/5 to-transparent pointer-events-none" />
      <div className="absolute -right-8 -top-8 w-32 h-32 rounded-full blur-3xl opacity-20"
        style={{ background: gradientFrom }} />

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

interface ProductStatsCardsProps {
  stats: ProductStats
  hasActiveFilters?: boolean
}

export function ProductStatsCards({ stats, hasActiveFilters = false }: ProductStatsCardsProps) {
  const cards = PRODUCT_STAT_CARDS_CONFIG.map(config => {
    const Icon = config.icon
    return {
      ...config,
      icon: <Icon className="w-5 h-5" />,
      value: stats[config.key],
    }
  })

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        {cards.map((card, index) => (
          <StatCard key={index} {...card} />
        ))}
      </div>
      {hasActiveFilters && (
        <div className="flex items-center justify-center gap-2 text-xs text-muted-foreground">
          <Info className="h-3.5 w-3.5" />
          <p>Note: Status counts reflect the current page only. Total is accurate.</p>
        </div>
      )}
    </div>
  )
}
