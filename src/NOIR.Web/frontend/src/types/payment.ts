/**
 * Payment Gateway Types
 *
 * TypeScript types for payment gateway management.
 */

// ============================================================================
// Enums
// ============================================================================

export type GatewayEnvironment = 'Sandbox' | 'Production'
export type GatewayHealthStatus = 'Unknown' | 'Healthy' | 'Degraded' | 'Unhealthy'
export type CredentialFieldType = 'text' | 'password' | 'url' | 'number' | 'select'

// ============================================================================
// Gateway DTOs
// ============================================================================

export interface PaymentGateway {
  id: string
  provider: string
  displayName: string
  isActive: boolean
  sortOrder: number
  environment: GatewayEnvironment
  hasCredentials: boolean
  webhookUrl: string | null
  minAmount: number | null
  maxAmount: number | null
  supportedCurrencies: string
  lastHealthCheck: string | null
  healthStatus: GatewayHealthStatus
  createdAt: string
  modifiedAt: string | null
}

export interface CheckoutGateway {
  id: string
  provider: string
  displayName: string
  sortOrder: number
  minAmount: number | null
  maxAmount: number | null
  supportedCurrencies: string
}

// ============================================================================
// Gateway Schema Types
// ============================================================================

export interface FieldOption {
  value: string
  label: string
  description?: string
}

export interface CredentialField {
  key: string
  label: string
  type: CredentialFieldType
  required: boolean
  default?: string
  placeholder?: string
  helpText?: string
  options?: FieldOption[]
}

export interface EnvironmentDefaults {
  sandbox: Record<string, string>
  production: Record<string, string>
}

export interface GatewaySchema {
  provider: string
  displayName: string
  description: string
  iconUrl: string
  fields: CredentialField[]
  environments: EnvironmentDefaults
  supportsCod: boolean
  documentationUrl?: string
}

export interface GatewaySchemas {
  schemas: Record<string, GatewaySchema>
}

// ============================================================================
// Request/Response Types
// ============================================================================

export interface ConfigureGatewayRequest {
  provider: string
  displayName: string
  environment: GatewayEnvironment
  credentials: Record<string, string>
  supportedMethods: string[]
  sortOrder: number
  isActive: boolean
}

export interface UpdateGatewayRequest {
  displayName?: string
  environment?: GatewayEnvironment
  credentials?: Record<string, string>
  supportedMethods?: string[]
  sortOrder?: number
  isActive?: boolean
}

export interface TestConnectionResult {
  success: boolean
  message: string
  responseTimeMs?: number
  errorCode?: string
}

// ============================================================================
// UI State Types
// ============================================================================

export interface GatewayCardState {
  gateway: PaymentGateway | null
  schema: GatewaySchema
  isConfigured: boolean
  isLoading: boolean
}

export type ConfigureDialogMode = 'create' | 'edit'
