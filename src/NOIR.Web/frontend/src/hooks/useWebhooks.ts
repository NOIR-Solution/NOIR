/**
 * Webhooks TanStack Query hooks
 *
 * Provides data fetching and mutation hooks for webhook subscription management.
 */
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  getWebhooks,
  getWebhookById,
  getWebhookDeliveries,
  getWebhookEventTypes,
  createWebhook,
  updateWebhook,
  deleteWebhook,
  activateWebhook,
  deactivateWebhook,
  testWebhook,
  rotateWebhookSecret,
  type GetWebhooksParams,
  type GetWebhookDeliveriesParams,
} from '@/services/webhooks'
import type { CreateWebhookRequest, UpdateWebhookRequest } from '@/types/webhook'

// ============================================================================
// Query Keys
// ============================================================================

export const webhookKeys = {
  all: ['webhooks'] as const,
  lists: () => [...webhookKeys.all, 'list'] as const,
  list: (params?: GetWebhooksParams) => [...webhookKeys.lists(), params] as const,
  details: () => [...webhookKeys.all, 'detail'] as const,
  detail: (id: string) => [...webhookKeys.details(), id] as const,
  deliveries: (id: string, params?: GetWebhookDeliveriesParams) =>
    [...webhookKeys.all, 'deliveries', id, params] as const,
  eventTypes: () => [...webhookKeys.all, 'event-types'] as const,
}

// ============================================================================
// Query Hooks
// ============================================================================

/**
 * Hook to fetch a paginated list of webhook subscriptions
 */
export const useWebhooks = (params: GetWebhooksParams = {}) =>
  useQuery({
    queryKey: webhookKeys.list(params),
    queryFn: () => getWebhooks(params),
    staleTime: 30_000,
  })

/**
 * Hook to fetch a single webhook subscription by ID
 */
export const useWebhookById = (id: string | undefined) =>
  useQuery({
    queryKey: webhookKeys.detail(id!),
    queryFn: () => getWebhookById(id!),
    enabled: !!id,
    staleTime: 30_000,
  })

/**
 * Hook to fetch delivery logs for a webhook subscription
 */
export const useWebhookDeliveryLogs = (id: string | undefined, params: GetWebhookDeliveriesParams = {}) =>
  useQuery({
    queryKey: webhookKeys.deliveries(id!, params),
    queryFn: () => getWebhookDeliveries(id!, params),
    enabled: !!id,
    staleTime: 15_000,
  })

/**
 * Hook to fetch all available webhook event types
 */
export const useWebhookEventTypes = () =>
  useQuery({
    queryKey: webhookKeys.eventTypes(),
    queryFn: getWebhookEventTypes,
    staleTime: 10 * 60_000, // 10 minutes — event types rarely change
  })

// ============================================================================
// Mutation Hooks
// ============================================================================

/**
 * Hook to create a new webhook subscription
 */
export const useCreateWebhook = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (request: CreateWebhookRequest) => createWebhook(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: webhookKeys.lists() })
    },
  })
}

/**
 * Hook to update an existing webhook subscription
 */
export const useUpdateWebhook = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateWebhookRequest }) =>
      updateWebhook(id, request),
    onSuccess: (_data, { id }) => {
      queryClient.invalidateQueries({ queryKey: webhookKeys.lists() })
      queryClient.invalidateQueries({ queryKey: webhookKeys.detail(id) })
    },
  })
}

/**
 * Hook to delete a webhook subscription
 */
export const useDeleteWebhook = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => deleteWebhook(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: webhookKeys.lists() })
    },
  })
}

/**
 * Hook to activate a webhook subscription
 */
export const useActivateWebhook = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => activateWebhook(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: webhookKeys.lists() })
      queryClient.invalidateQueries({ queryKey: webhookKeys.detail(id) })
    },
  })
}

/**
 * Hook to deactivate a webhook subscription
 */
export const useDeactivateWebhook = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => deactivateWebhook(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: webhookKeys.lists() })
      queryClient.invalidateQueries({ queryKey: webhookKeys.detail(id) })
    },
  })
}

/**
 * Hook to send a test delivery to a webhook subscription
 */
export const useTestWebhook = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => testWebhook(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: [...webhookKeys.all, 'deliveries', id] })
    },
  })
}

/**
 * Hook to rotate the HMAC secret for a webhook subscription
 */
export const useRotateWebhookSecret = () => {
  const queryClient = useQueryClient()

  return useMutation({
    mutationFn: (id: string) => rotateWebhookSecret(id),
    onSuccess: (_data, id) => {
      queryClient.invalidateQueries({ queryKey: webhookKeys.detail(id) })
    },
  })
}
