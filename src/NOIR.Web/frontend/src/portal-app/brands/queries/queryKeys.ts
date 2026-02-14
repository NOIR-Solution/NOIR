import type { GetBrandsParams } from '@/services/brands'

export const brandKeys = {
  all: ['brands'] as const,
  lists: () => [...brandKeys.all, 'list'] as const,
  list: (params: GetBrandsParams) => [...brandKeys.lists(), params] as const,
  active: () => [...brandKeys.all, 'active'] as const,
  details: () => [...brandKeys.all, 'detail'] as const,
  detail: (id: string) => [...brandKeys.details(), id] as const,
}