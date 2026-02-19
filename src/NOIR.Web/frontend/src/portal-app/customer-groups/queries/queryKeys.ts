import type { GetCustomerGroupsParams } from '@/services/customerGroups'

export const customerGroupKeys = {
  all: ['customer-groups'] as const,
  lists: () => [...customerGroupKeys.all, 'list'] as const,
  list: (params: GetCustomerGroupsParams) => [...customerGroupKeys.lists(), params] as const,
  details: () => [...customerGroupKeys.all, 'detail'] as const,
  detail: (id: string) => [...customerGroupKeys.details(), id] as const,
}
