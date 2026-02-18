import type { GetCustomersParams, GetCustomerOrdersParams } from '@/services/customers'

export const customerKeys = {
  all: ['customers'] as const,
  lists: () => [...customerKeys.all, 'list'] as const,
  list: (params: GetCustomersParams) => [...customerKeys.lists(), params] as const,
  details: () => [...customerKeys.all, 'detail'] as const,
  detail: (id: string) => [...customerKeys.details(), id] as const,
  stats: () => [...customerKeys.all, 'stats'] as const,
  orders: (id: string) => [...customerKeys.all, 'orders', id] as const,
  ordersList: (id: string, params: GetCustomerOrdersParams) => [...customerKeys.orders(id), params] as const,
}
