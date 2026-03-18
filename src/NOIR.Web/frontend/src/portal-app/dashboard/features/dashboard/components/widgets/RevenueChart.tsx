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
import { formatCurrency } from '@/lib/utils/currency'
import type { SalesOverTime } from '@/services/dashboard'

interface RevenueChartProps {
  data: SalesOverTime[]
  isLoading: boolean
}

const CHART_TOOLTIP_STYLE = {
  borderRadius: '8px',
  border: '1px solid var(--border)',
  backgroundColor: 'var(--card)',
  color: 'var(--card-foreground)',
}

export const RevenueChart = ({ data, isLoading }: RevenueChartProps) => {
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
        <CardTitle className="text-lg">{t('dashboard.revenueChart')}</CardTitle>
        <CardDescription>{t('dashboard.salesOverTimeDesc')}</CardDescription>
      </CardHeader>
      <CardContent>
        {isLoading ? (
          <Skeleton className="h-64 w-full" />
        ) : chartData.length === 0 ? (
          <EmptyState
            icon={BarChart3}
            title={t('dashboard.noSalesData')}
            description={t('dashboard.noSalesDataDesc')}
            className="border-0 rounded-none px-4 py-12"
          />
        ) : (
          <ResponsiveContainer width="100%" height={280}>
            <AreaChart data={chartData} margin={{ top: 5, right: 10, left: 0, bottom: 0 }}>
              <defs>
                <linearGradient id="dashRevenueGradient" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="var(--primary)" stopOpacity={0.3} />
                  <stop offset="95%" stopColor="var(--primary)" stopOpacity={0} />
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
                tickFormatter={(v: number) =>
                  v >= 1_000_000
                    ? `${(v / 1_000_000).toFixed(0)}M`
                    : v >= 1_000
                    ? `${(v / 1_000).toFixed(0)}K`
                    : v.toString()
                }
              />
              <RechartsTooltip
                contentStyle={CHART_TOOLTIP_STYLE}
                formatter={(value, name) => [
                  name === 'revenue'
                    ? formatCurrency(Number(value ?? 0))
                    : Number(value ?? 0).toLocaleString(),
                  name === 'revenue' ? t('dashboard.revenue') : t('dashboard.orders'),
                ]}
                labelFormatter={(label) => String(label)}
              />
              <Area
                type="monotone"
                dataKey="revenue"
                stroke="var(--primary)"
                fill="url(#dashRevenueGradient)"
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
