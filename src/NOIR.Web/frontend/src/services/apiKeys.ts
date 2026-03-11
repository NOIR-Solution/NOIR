import { apiClient } from './apiClient'
import type {
  ApiKeyDto,
  ApiKeyCreatedDto,
  ApiKeyRotatedDto,
  CreateApiKeyRequest,
  UpdateApiKeyRequest,
  RevokeApiKeyRequest,
} from '@/types/apiKey'

// ============================================================================
// User Self-Service (Profile Tab)
// ============================================================================

export const getMyApiKeys = async (): Promise<ApiKeyDto[]> => {
  return apiClient<ApiKeyDto[]>('/auth/me/api-keys')
}

export const createApiKey = async (request: CreateApiKeyRequest): Promise<ApiKeyCreatedDto> => {
  return apiClient<ApiKeyCreatedDto>('/auth/me/api-keys', {
    method: 'POST',
    body: JSON.stringify(request),
  })
}

export const updateApiKey = async (id: string, request: UpdateApiKeyRequest): Promise<ApiKeyDto> => {
  return apiClient<ApiKeyDto>(`/auth/me/api-keys/${id}`, {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export const rotateApiKey = async (id: string): Promise<ApiKeyRotatedDto> => {
  return apiClient<ApiKeyRotatedDto>(`/auth/me/api-keys/${id}/rotate`, {
    method: 'POST',
  })
}

export const revokeApiKey = async (id: string, request?: RevokeApiKeyRequest): Promise<ApiKeyDto> => {
  return apiClient<ApiKeyDto>(`/auth/me/api-keys/${id}/revoke`, {
    method: 'POST',
    body: JSON.stringify(request ?? {}),
  })
}

// ============================================================================
// Admin Management
// ============================================================================

export const getTenantApiKeys = async (): Promise<ApiKeyDto[]> => {
  return apiClient<ApiKeyDto[]>('/admin/api-keys')
}

export const adminRevokeApiKey = async (id: string, request?: RevokeApiKeyRequest): Promise<ApiKeyDto> => {
  return apiClient<ApiKeyDto>(`/admin/api-keys/${id}/revoke`, {
    method: 'POST',
    body: JSON.stringify(request ?? {}),
  })
}
