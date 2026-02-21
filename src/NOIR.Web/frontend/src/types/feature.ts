/** Effective feature state for a module/feature within a tenant */
export interface EffectiveFeatureState {
  isAvailable: boolean
  isEnabled: boolean
  isEffective: boolean
  isCore: boolean
}

/** A child feature within a module */
export interface FeatureDto {
  name: string
  displayNameKey: string
  descriptionKey: string
  defaultEnabled: boolean
}

/** A module definition from the catalog */
export interface ModuleDto {
  name: string
  displayNameKey: string
  descriptionKey: string
  icon: string
  sortOrder: number
  isCore: boolean
  defaultEnabled: boolean
  features: FeatureDto[]
}

/** Full module catalog response */
export interface ModuleCatalogDto {
  modules: ModuleDto[]
}

/** Feature state for a specific tenant */
export interface TenantFeatureStateDto {
  featureName: string
  isAvailable: boolean
  isEnabled: boolean
  isEffective: boolean
  isCore: boolean
}

/** Request to set module availability (platform admin) */
export interface SetModuleAvailabilityRequest {
  featureName: string
  isAvailable: boolean
}

/** Request to toggle module (tenant admin) */
export interface ToggleModuleRequest {
  featureName: string
  isEnabled: boolean
}

/** Response with all effective feature states for current tenant */
export type TenantFeatureStatesResponse = Record<string, EffectiveFeatureState>
