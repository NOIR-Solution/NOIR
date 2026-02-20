import { useQuery, keepPreviousData } from '@tanstack/react-query'
import { getOrders, getOrderById, getOrderNotes, searchProductVariants, type GetOrdersParams } from '@/services/orders'
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

export const useSearchProductVariantsQuery = (search: string, categoryId?: string) =>
  useQuery({
    queryKey: ['productVariants', 'search', search, categoryId] as const,
    queryFn: () => searchProductVariants({ search, pageSize: 20, categoryId }),
    enabled: search.length >= 2,
    placeholderData: keepPreviousData,
  })
