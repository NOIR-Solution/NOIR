import type { GetTenantsParams } from '@/services/tenants'

export interface RolesParams {
  page?: number
  pageSize?: number
  search?: string
}

export interface UsersParams {
  page?: number
  pageSize?: number
  search?: string
  role?: string
  isLocked?: boolean
}

export const roleKeys = {
  all: ['roles'] as const,
  lists: () => [...roleKeys.all, 'list'] as const,
  list: (params: RolesParams) => [...roleKeys.lists(), params] as const,
  permissions: () => [...roleKeys.all, 'permissions'] as const,
  permissionTemplates: () => [...roleKeys.all, 'permissionTemplates'] as const,
}

export const userKeys = {
  all: ['users'] as const,
  lists: () => [...userKeys.all, 'list'] as const,
  list: (params: UsersParams) => [...userKeys.lists(), params] as const,
  availableRoles: () => [...userKeys.all, 'availableRoles'] as const,
}

export const tenantKeys = {
  all: ['tenants'] as const,
  lists: () => [...tenantKeys.all, 'list'] as const,
  list: (params: GetTenantsParams) => [...tenantKeys.lists(), params] as const,
}