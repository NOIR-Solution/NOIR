/**
 * Dashboard TanStack Query hook
 *
 * Fetches dashboard metrics with caching and refetch support.
 */
import { useQuery } from '@tanstack/react-query'
import { getDashboardMetrics, type DashboardMetricsParams } from '@/services/dashboard'

export const dashboardKeys = {
  all: ['dashboard'] as const,
  metrics: (params?: DashboardMetricsParams) => [...dashboardKeys.all, 'metrics', params] as const,
}

export const useDashboardMetrics = (params: DashboardMetricsParams = {}) =>
  useQuery({
    queryKey: dashboardKeys.metrics(params),
    queryFn: () => getDashboardMetrics(params),
    staleTime: 60_000,
    refetchInterval: 5 * 60_000,
  })
