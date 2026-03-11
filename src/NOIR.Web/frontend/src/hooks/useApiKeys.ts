import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getMyApiKeys,
  createApiKey,
  updateApiKey,
  rotateApiKey,
  revokeApiKey,
  getTenantApiKeys,
  adminRevokeApiKey,
} from '@/services/apiKeys'
import type {
  CreateApiKeyRequest,
  UpdateApiKeyRequest,
  RevokeApiKeyRequest,
} from '@/types/apiKey'

// ============================================================================
// Query Keys
// ============================================================================

export const apiKeyKeys = {
  all: ['apiKeys'] as const,
  myKeys: () => [...apiKeyKeys.all, 'my'] as const,
  tenantKeys: () => [...apiKeyKeys.all, 'tenant'] as const,
}

// ============================================================================
// Query Hooks
// ============================================================================

export const useMyApiKeys = () =>
  useQuery({
    queryKey: apiKeyKeys.myKeys(),
    queryFn: getMyApiKeys,
    staleTime: 30_000,
  })

export const useTenantApiKeys = () =>
  useQuery({
    queryKey: apiKeyKeys.tenantKeys(),
    queryFn: getTenantApiKeys,
    staleTime: 30_000,
  })

// ============================================================================
// Mutation Hooks
// ============================================================================

export const useCreateApiKey = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: CreateApiKeyRequest) => createApiKey(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: apiKeyKeys.all })
    },
  })
}

export const useUpdateApiKey = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateApiKeyRequest }) =>
      updateApiKey(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: apiKeyKeys.all })
    },
  })
}

export const useRotateApiKey = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => rotateApiKey(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: apiKeyKeys.all })
    },
  })
}

export const useRevokeApiKey = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request?: RevokeApiKeyRequest }) =>
      revokeApiKey(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: apiKeyKeys.all })
    },
  })
}

export const useAdminRevokeApiKey = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request?: RevokeApiKeyRequest }) =>
      adminRevokeApiKey(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: apiKeyKeys.all })
    },
  })
}
