import { useQuery, useQueries, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getCurrentTenantFeatures,
  getModuleCatalog,
  getTenantFeatureStates,
  setModuleAvailability,
  toggleModule,
} from '@/services/features'
import type {
  ModuleCatalogDto,
  ToggleModuleRequest,
  TenantFeatureStatesResponse,
} from '@/types'

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
    staleTime: 60 * 1000, // 1 minute — short enough to pick up platform admin changes promptly
    refetchOnWindowFocus: 'always', // Always refetch when tab regains focus (platform admin may have changed availability)
  })
}

/** Hook to check if a single feature is enabled */
export const useFeature = (featureName: string): { isEnabled: boolean; isLoading: boolean } => {
  const { data, isLoading } = useFeatures()

  const isEnabled = data?.[featureName]?.isEffective ?? false // Fail-closed: hide gated content until features load

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
            isEffective: (previous[request.featureName]?.isAvailable ?? true) && request.isEnabled,
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

/** Hook to get feature states for a specific tenant (platform admin) */
export const useTenantFeatureStates = (tenantId: string) => {
  return useQuery({
    queryKey: featureKeys.tenant(tenantId),
    queryFn: () => getTenantFeatureStates(tenantId),
    enabled: !!tenantId,
    staleTime: 5 * 60 * 1000,
  })
}

/** Hook to set module availability for a tenant (platform admin).
 *  Accepts tenantId per-call, works for both single-tenant dialog and matrix contexts. */
export const useSetModuleAvailability = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ tenantId, featureName, isAvailable }: { tenantId: string; featureName: string; isAvailable: boolean }) =>
      setModuleAvailability(tenantId, { featureName, isAvailable }),
    onMutate: async ({ tenantId, featureName, isAvailable }) => {
      await queryClient.cancelQueries({ queryKey: featureKeys.tenant(tenantId) })
      const previous = queryClient.getQueryData<ModuleCatalogDto>(featureKeys.tenant(tenantId))

      if (previous) {
        queryClient.setQueryData<ModuleCatalogDto>(featureKeys.tenant(tenantId), {
          ...previous,
          modules: previous.modules.map((m) =>
            m.name === featureName
              ? { ...m, isAvailable, isEffective: isAvailable && (m.isEnabled ?? m.defaultEnabled) }
              : m,
          ),
        })
      }

      return { previous, tenantId }
    },
    onError: (_err, _vars, context) => {
      if (context?.previous) {
        queryClient.setQueryData(featureKeys.tenant(context.tenantId), context.previous)
      }
    },
    onSettled: (_data, _err, { tenantId }) => {
      queryClient.invalidateQueries({ queryKey: featureKeys.tenant(tenantId) })
    },
  })
}

/** Hook to fetch feature states for multiple tenants in parallel (matrix view) */
export const useAllTenantFeatureStates = (tenantIds: string[]) => {
  return useQueries({
    queries: tenantIds.map((tenantId) => ({
      queryKey: featureKeys.tenant(tenantId),
      queryFn: () => getTenantFeatureStates(tenantId),
      enabled: !!tenantId,
      staleTime: 5 * 60 * 1000,
    })),
  })
}
