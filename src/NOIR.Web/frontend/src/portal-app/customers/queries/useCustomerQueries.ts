import { useQuery, keepPreviousData } from '@tanstack/react-query'
import {
  getCustomers,
  getCustomerById,
  getCustomerStats,
  getCustomerOrders,
  type GetCustomersParams,
  type GetCustomerOrdersParams,
} from '@/services/customers'
import { customerKeys } from './queryKeys'

export const useCustomersQuery = (params: GetCustomersParams) =>
  useQuery({
    queryKey: customerKeys.list(params),
    queryFn: () => getCustomers(params),
    placeholderData: keepPreviousData,
  })

export const useCustomerQuery = (id: string | undefined) =>
  useQuery({
    queryKey: customerKeys.detail(id!),
    queryFn: () => getCustomerById(id!),
    enabled: !!id,
  })

export const useCustomerStatsQuery = () =>
  useQuery({
    queryKey: customerKeys.stats(),
    queryFn: () => getCustomerStats(),
  })

export const useCustomerOrdersQuery = (id: string | undefined, params: GetCustomerOrdersParams) =>
  useQuery({
    queryKey: customerKeys.ordersList(id!, params),
    queryFn: () => getCustomerOrders(id!, params),
    enabled: !!id,
    placeholderData: keepPreviousData,
  })
