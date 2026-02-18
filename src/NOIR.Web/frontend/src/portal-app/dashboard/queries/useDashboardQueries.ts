import { useQuery } from '@tanstack/react-query'
import { getDashboardMetrics, type GetDashboardMetricsParams } from '@/services/dashboard'
import { dashboardKeys } from './queryKeys'

export const useDashboardMetricsQuery = (params: GetDashboardMetricsParams = {}) =>
  useQuery({
    queryKey: dashboardKeys.metrics(params),
    queryFn: () => getDashboardMetrics(params),
    staleTime: 60_000, // 1 minute
    refetchInterval: 5 * 60_000, // auto-refresh every 5 minutes
  })
