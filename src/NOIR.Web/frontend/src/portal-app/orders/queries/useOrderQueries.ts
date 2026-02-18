import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { getOrders, getOrderById, type GetOrdersParams } from '@/services/orders'
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
