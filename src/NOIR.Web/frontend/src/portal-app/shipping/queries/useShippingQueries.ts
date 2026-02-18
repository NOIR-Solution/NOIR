import { useQuery } from '@tanstack/react-query'
import {
  getShippingProviders,
  getShippingProviderById,
  getShippingOrderByTracking,
  getShippingOrderByOrderId,
  getShippingTracking,
} from '@/services/shipping'
import { shippingKeys } from './queryKeys'

export const useShippingProvidersQuery = () =>
  useQuery({
    queryKey: shippingKeys.providers(),
    queryFn: () => getShippingProviders(),
  })

export const useShippingProviderQuery = (id: string | undefined) =>
  useQuery({
    queryKey: shippingKeys.provider(id!),
    queryFn: () => getShippingProviderById(id!),
    enabled: !!id,
  })

export const useShippingOrderByTrackingQuery = (trackingNumber: string | undefined) =>
  useQuery({
    queryKey: shippingKeys.orderByTracking(trackingNumber!),
    queryFn: () => getShippingOrderByTracking(trackingNumber!),
    enabled: !!trackingNumber,
  })

export const useShippingOrderByOrderIdQuery = (orderId: string | undefined) =>
  useQuery({
    queryKey: shippingKeys.orderByOrderId(orderId!),
    queryFn: () => getShippingOrderByOrderId(orderId!),
    enabled: !!orderId,
  })

export const useShippingTrackingQuery = (trackingNumber: string | undefined) =>
  useQuery({
    queryKey: shippingKeys.tracking(trackingNumber!),
    queryFn: () => getShippingTracking(trackingNumber!),
    enabled: !!trackingNumber,
  })
