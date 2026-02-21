import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getCurrentTenantFeatures,
  getModuleCatalog,
  toggleModule,
} from '@/services/features'
import type { ToggleModuleRequest, TenantFeatureStatesResponse } from '@/types'

/** Query keys for feature management */
export const featureKeys = {
  all: ['features'] as const,
  currentTenant: () => [...featureKeys.all, 'current-tenant'] as const,
  catalog: () => [...featureKeys.all, 'catalog'] as const,
  tenant: (tenantId: string) => [...featureKeys.all, 'tenant', tenantId] as const,
}

/** Hook to get all effective feature states for the current tenant */
export const useFeatures = () => {
  return useQuery({
    queryKey: featureKeys.currentTenant(),
    queryFn: getCurrentTenantFeatures,
    staleTime: 5 * 60 * 1000, // 5 minutes (matches backend cache)
  })
}

/** Hook to check if a single feature is enabled */
export const useFeature = (featureName: string): { isEnabled: boolean; isLoading: boolean } => {
  const { data, isLoading } = useFeatures()

  const isEnabled = data?.[featureName]?.isEffective ?? true // Default to enabled if not loaded

  return { isEnabled, isLoading }
}

/** Hook to get the full module catalog */
export const useModuleCatalog = () => {
  return useQuery({
    queryKey: featureKeys.catalog(),
    queryFn: getModuleCatalog,
    staleTime: 30 * 60 * 1000, // 30 minutes (catalog rarely changes)
  })
}

/** Hook to toggle a module for the current tenant */
export const useToggleModule = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: ToggleModuleRequest) => toggleModule(request),
    onMutate: async (request) => {
      // Optimistic update
      await queryClient.cancelQueries({ queryKey: featureKeys.currentTenant() })
      const previous = queryClient.getQueryData<TenantFeatureStatesResponse>(featureKeys.currentTenant())

      if (previous) {
        queryClient.setQueryData<TenantFeatureStatesResponse>(featureKeys.currentTenant(), {
          ...previous,
          [request.featureName]: {
            ...previous[request.featureName],
            isEnabled: request.isEnabled,
            isEffective: previous[request.featureName]?.isAvailable && request.isEnabled,
          },
        })
      }

      return { previous }
    },
    onError: (_err, _request, context) => {
      // Rollback on error
      if (context?.previous) {
        queryClient.setQueryData(featureKeys.currentTenant(), context.previous)
      }
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: featureKeys.currentTenant() })
    },
  })
}
