import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useRegionalSettings } from '@/contexts/RegionalSettingsContext'
import {
  AreaChart,
  Area,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  ResponsiveContainer,
} from 'recharts'
import { BarChart3 } from 'lucide-react'
import { Card, CardContent, CardDescription, CardHeader, CardTitle, EmptyState, Skeleton } from '@uikit'
import type { PublishingTrendDto } from '@/services/dashboard'

interface PublishingTrendChartProps {
  data: PublishingTrendDto[]
  isLoading: boolean
}

const CHART_TOOLTIP_STYLE = {
  borderRadius: '8px',
  border: '1px solid var(--border)',
  backgroundColor: 'var(--card)',
  color: 'var(--card-foreground)',
}

export const PublishingTrendChart = ({ data, isLoading }: PublishingTrendChartProps) => {
  const { t } = useTranslation('common')
  const { timezone } = useRegionalSettings()

  const chartData = useMemo(
    () =>
      data.map((d) => ({
        ...d,
        dateLabel: new Date(d.date).toLocaleDateString(undefined, { month: 'short', day: 'numeric', timeZone: timezone }),
      })),
    [data, timezone]
  )

  return (
    <Card className="shadow-sm hover:shadow-lg transition-all duration-300 md:col-span-2">
      <CardHeader>
        <CardTitle className="text-lg">{t('dashboard.publishingTrend')}</CardTitle>
        <CardDescription>{t('dashboard.publishingTrendDesc', 'Posts published over time')}</CardDescription>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <Skeleton className="h-64 w-full" />
        ) : chartData.length === 0 ? (
          <EmptyState
            icon={BarChart3}
            title={t('labels.noData', 'No data available')}
            size="sm"
            className="border-0 rounded-none py-6"
          />
        ) : (
          <ResponsiveContainer width="100%" height={240}>
            <AreaChart data={chartData} margin={{ top: 5, right: 10, left: 0, bottom: 0 }}>
              <defs>
                <linearGradient id="dashBlogGradient" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="var(--chart-2)" stopOpacity={0.3} />
                  <stop offset="95%" stopColor="var(--chart-2)" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" />
              <XAxis
                dataKey="dateLabel"
                tick={{ fontSize: 11, fill: 'var(--muted-foreground)' }}
                stroke="var(--muted-foreground)"
                interval="preserveStartEnd"
              />
              <YAxis
                tick={{ fontSize: 11, fill: 'var(--muted-foreground)' }}
                stroke="var(--muted-foreground)"
                allowDecimals={false}
              />
              <RechartsTooltip
                contentStyle={CHART_TOOLTIP_STYLE}
                formatter={(value) => [Number(value ?? 0), t('dashboard.blogStats')]}
                labelFormatter={(label) => String(label)}
              />
              <Area
                type="monotone"
                dataKey="postCount"
                stroke="var(--chart-2)"
                fill="url(#dashBlogGradient)"
                strokeWidth={2}
                dot={false}
                activeDot={{ r: 4 }}
              />
            </AreaChart>
          </ResponsiveContainer>
        )}
      </CardContent>
    </Card>
  )
}
