import type { GetDashboardMetricsParams } from '@/services/dashboard'

export const dashboardKeys = {
  all: ['dashboard'] as const,
  metrics: (params?: GetDashboardMetricsParams) => [...dashboardKeys.all, 'metrics', params] as const,
}
