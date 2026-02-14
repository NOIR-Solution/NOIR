import { useMutation, useQueryClient } from '@tanstack/react-query'
import {
  configureGateway,
  updateGateway,
  toggleGatewayActive,
  testGatewayConnection,
} from '@/services/paymentGateways'
import type { ConfigureGatewayRequest, UpdateGatewayRequest } from '@/types'
import { paymentGatewayKeys } from './queryKeys'

export const useConfigureGatewayMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (request: ConfigureGatewayRequest) => configureGateway(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: paymentGatewayKeys.all })
    },
  })
}

export const useUpdateGatewayMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateGatewayRequest }) =>
      updateGateway(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: paymentGatewayKeys.all })
    },
  })
}

export const useToggleGatewayActiveMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: ({ id, isActive }: { id: string; isActive: boolean }) =>
      toggleGatewayActive(id, isActive),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: paymentGatewayKeys.gateways() })
    },
  })
}

export const useTestGatewayConnectionMutation = () =>
  useMutation({
    mutationFn: (id: string) => testGatewayConnection(id),
  })
