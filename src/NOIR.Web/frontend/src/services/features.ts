import { apiClient } from './apiClient'
import type {
  ModuleCatalogDto,
  TenantFeatureStatesResponse,
  TenantFeatureStateDto,
  SetModuleAvailabilityRequest,
  ToggleModuleRequest,
} from '@/types'

/** Get effective feature states for the current tenant */
export const getCurrentTenantFeatures = async (): Promise<TenantFeatureStatesResponse> => {
  return apiClient<TenantFeatureStatesResponse>('/features/current-tenant')
}

/** Get the full module catalog (code-defined modules) */
export const getModuleCatalog = async (): Promise<ModuleCatalogDto> => {
  return apiClient<ModuleCatalogDto>('/features/catalog')
}

/** Get module catalog with tenant-specific states (platform admin) */
export const getTenantFeatureStates = async (tenantId: string): Promise<ModuleCatalogDto> => {
  return apiClient<ModuleCatalogDto>(`/features/tenant/${tenantId}`)
}

/** Set module availability for a tenant (platform admin) */
export const setModuleAvailability = async (
  tenantId: string,
  request: SetModuleAvailabilityRequest,
): Promise<TenantFeatureStateDto> => {
  return apiClient<TenantFeatureStateDto>(`/features/tenant/${tenantId}/availability`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

/** Toggle module enabled state for current tenant (tenant admin) */
export const toggleModule = async (request: ToggleModuleRequest): Promise<TenantFeatureStateDto> => {
  return apiClient<TenantFeatureStateDto>('/features/toggle', {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}
