import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { getOrders, getOrderById, getOrderNotes, type GetOrdersParams } from '@/services/orders'
import { orderKeys } from './queryKeys'

export const useOrdersQuery = (params: GetOrdersParams) =>
  useQuery({
    queryKey: orderKeys.list(params),
    queryFn: () => getOrders(params),
    placeholderData: keepPreviousData,
  })

export const useOrderQuery = (id: string | undefined) =>
  useQuery({
    queryKey: orderKeys.detail(id!),
    queryFn: () => getOrderById(id!),
    enabled: !!id,
  })

export const useOrderNotesQuery = (orderId: string | undefined) =>
  useQuery({
    queryKey: orderKeys.notes(orderId!),
    queryFn: () => getOrderNotes(orderId!),
    enabled: !!orderId,
  })
