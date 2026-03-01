import { useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip as RechartsTooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Legend,
} from 'recharts'
import { BarChart3, Users, Building2 } from 'lucide-react'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
  EmptyState,
  PageHeader,
  Skeleton,
} from '@uikit'
import { useHrReportsQuery } from '@/portal-app/hr/queries'

const CHART_COLORS = [
  'hsl(var(--chart-1))',
  'hsl(var(--chart-2))',
  'hsl(var(--chart-3))',
  'hsl(var(--chart-4))',
  'hsl(var(--chart-5))',
]

const CHART_TOOLTIP_STYLE = {
  borderRadius: '8px',
  border: '1px solid hsl(var(--border))',
  backgroundColor: 'hsl(var(--card))',
  color: 'hsl(var(--card-foreground))',
}

export const HrReportsPage = () => {
  const { t } = useTranslation('common')
  const { data: reports, isLoading } = useHrReportsQuery()

  const headcountData = useMemo(() =>
    reports?.headcountByDepartment?.map(d => ({
      name: d.departmentName,
      count: d.count,
    })) ?? [],
    [reports]
  )

  const tagData = useMemo(() =>
    reports?.tagDistribution?.map(d => ({
      name: d.tagName,
      count: d.count,
      color: d.color,
    })) ?? [],
    [reports]
  )

  const employmentTypeData = useMemo(() =>
    reports?.employmentTypeBreakdown?.map(d => ({
      name: t(`hr.employmentTypes.${d.type.charAt(0).toLowerCase() + d.type.slice(1).replace(/([A-Z])/g, m => m.toLowerCase())}`),
      value: d.count,
    })) ?? [],
    [reports, t]
  )

  const statusData = useMemo(() =>
    reports?.statusBreakdown?.map(d => ({
      name: t(`hr.statuses.${d.status.toLowerCase()}`),
      value: d.count,
    })) ?? [],
    [reports, t]
  )

  return (
    <div className="space-y-6">
      <PageHeader
        icon={BarChart3}
        title={t('hr.reports.title')}
        description={t('hr.reports.description')}
        responsive
      />

      {/* Summary Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardContent className="flex items-center gap-4 pt-6">
            <div className="p-3 rounded-xl bg-primary/10">
              <Users className="h-6 w-6 text-primary" />
            </div>
            <div>
              <p className="text-sm text-muted-foreground">{t('hr.reports.totalActiveEmployees')}</p>
              {isLoading ? (
                <Skeleton className="h-8 w-16 mt-1" />
              ) : (
                <p className="text-2xl font-bold">{reports?.totalActiveEmployees ?? 0}</p>
              )}
            </div>
          </CardContent>
        </Card>
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardContent className="flex items-center gap-4 pt-6">
            <div className="p-3 rounded-xl bg-blue-100 dark:bg-blue-900/30">
              <Building2 className="h-6 w-6 text-blue-600 dark:text-blue-400" />
            </div>
            <div>
              <p className="text-sm text-muted-foreground">{t('hr.reports.totalDepartments')}</p>
              {isLoading ? (
                <Skeleton className="h-8 w-16 mt-1" />
              ) : (
                <p className="text-2xl font-bold">{reports?.totalDepartments ?? 0}</p>
              )}
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Charts Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Headcount by Department - Horizontal Bar */}
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300 lg:col-span-2">
          <CardHeader>
            <CardTitle className="text-lg">{t('hr.reports.headcountByDepartment')}</CardTitle>
            <CardDescription>{t('hr.reports.description')}</CardDescription>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-64 w-full" />
            ) : headcountData.length === 0 ? (
              <EmptyState
                icon={BarChart3}
                title={t('hr.orgChart.noData')}
                description={t('hr.orgChart.noDataDescription')}
                className="border-0 rounded-none px-4 py-12"
              />
            ) : (
              <ResponsiveContainer width="100%" height={Math.max(200, headcountData.length * 40)}>
                <BarChart data={headcountData} layout="vertical" margin={{ top: 5, right: 30, left: 0, bottom: 5 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
                  <XAxis type="number" tick={{ fontSize: 11 }} stroke="hsl(var(--muted-foreground))" />
                  <YAxis dataKey="name" type="category" tick={{ fontSize: 11 }} stroke="hsl(var(--muted-foreground))" width={120} />
                  <RechartsTooltip contentStyle={CHART_TOOLTIP_STYLE} />
                  <Bar dataKey="count" fill="hsl(var(--chart-1))" radius={[0, 4, 4, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>

        {/* Tag Distribution - Horizontal Bar */}
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300 lg:col-span-2">
          <CardHeader>
            <CardTitle className="text-lg">{t('hr.reports.tagDistribution')}</CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-64 w-full" />
            ) : tagData.length === 0 ? (
              <EmptyState
                icon={BarChart3}
                title={t('hr.orgChart.noData')}
                description={t('hr.orgChart.noDataDescription')}
                className="border-0 rounded-none px-4 py-12"
              />
            ) : (
              <ResponsiveContainer width="100%" height={Math.max(200, tagData.length * 36)}>
                <BarChart data={tagData} layout="vertical" margin={{ top: 5, right: 30, left: 0, bottom: 5 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="hsl(var(--border))" />
                  <XAxis type="number" tick={{ fontSize: 11 }} stroke="hsl(var(--muted-foreground))" />
                  <YAxis dataKey="name" type="category" tick={{ fontSize: 11 }} stroke="hsl(var(--muted-foreground))" width={120} />
                  <RechartsTooltip contentStyle={CHART_TOOLTIP_STYLE} />
                  <Bar dataKey="count" radius={[0, 4, 4, 0]}>
                    {tagData.map((entry, index) => (
                      <Cell key={index} fill={entry.color || CHART_COLORS[index % CHART_COLORS.length]} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>

        {/* Employment Type - Donut */}
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardHeader>
            <CardTitle className="text-lg">{t('hr.reports.employmentTypeBreakdown')}</CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-64 w-full" />
            ) : employmentTypeData.length === 0 ? (
              <EmptyState
                icon={BarChart3}
                title={t('hr.orgChart.noData')}
                description={t('hr.orgChart.noDataDescription')}
                className="border-0 rounded-none px-4 py-12"
              />
            ) : (
              <ResponsiveContainer width="100%" height={280}>
                <PieChart>
                  <Pie
                    data={employmentTypeData}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={100}
                    dataKey="value"
                    paddingAngle={2}
                    label={({ name, percent }) => `${name} (${((percent ?? 0) * 100).toFixed(0)}%)`}
                    labelLine={false}
                  >
                    {employmentTypeData.map((_, index) => (
                      <Cell key={index} fill={CHART_COLORS[index % CHART_COLORS.length]} />
                    ))}
                  </Pie>
                  <RechartsTooltip contentStyle={CHART_TOOLTIP_STYLE} />
                  <Legend />
                </PieChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>

        {/* Status Breakdown - Donut */}
        <Card className="shadow-sm hover:shadow-lg transition-all duration-300">
          <CardHeader>
            <CardTitle className="text-lg">{t('hr.reports.statusBreakdown')}</CardTitle>
          </CardHeader>
          <CardContent>
            {isLoading ? (
              <Skeleton className="h-64 w-full" />
            ) : statusData.length === 0 ? (
              <EmptyState
                icon={BarChart3}
                title={t('hr.orgChart.noData')}
                description={t('hr.orgChart.noDataDescription')}
                className="border-0 rounded-none px-4 py-12"
              />
            ) : (
              <ResponsiveContainer width="100%" height={280}>
                <PieChart>
                  <Pie
                    data={statusData}
                    cx="50%"
                    cy="50%"
                    innerRadius={60}
                    outerRadius={100}
                    dataKey="value"
                    paddingAngle={2}
                    label={({ name, percent }) => `${name} (${((percent ?? 0) * 100).toFixed(0)}%)`}
                    labelLine={false}
                  >
                    {statusData.map((_, index) => (
                      <Cell key={index} fill={CHART_COLORS[index % CHART_COLORS.length]} />
                    ))}
                  </Pie>
                  <RechartsTooltip contentStyle={CHART_TOOLTIP_STYLE} />
                  <Legend />
                </PieChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  )
}

export default HrReportsPage
