import { useQuery } from '@tanstack/react-query'
import { getRoles, getAllPermissions, getPermissionTemplates } from '@/services/roles'
import { getUsers } from '@/services/users'
import { getTenants, type GetTenantsParams } from '@/services/tenants'
import { roleKeys, userKeys, tenantKeys, type RolesParams, type UsersParams } from './queryKeys'

export const useRolesQuery = (params: RolesParams = {}) =>
  useQuery({
    queryKey: roleKeys.list(params),
    queryFn: () => getRoles(params),
  })

export const usePermissionsQuery = () =>
  useQuery({
    queryKey: roleKeys.permissions(),
    queryFn: () => getAllPermissions(),
  })

export const usePermissionTemplatesQuery = () =>
  useQuery({
    queryKey: roleKeys.permissionTemplates(),
    queryFn: () => getPermissionTemplates(),
  })

export const useUsersQuery = (params: UsersParams = {}) =>
  useQuery({
    queryKey: userKeys.list(params),
    queryFn: () => getUsers(params),
  })

export const useAvailableRolesQuery = () =>
  useQuery({
    queryKey: userKeys.availableRoles(),
    queryFn: () => getRoles({ pageSize: 100 }),
    select: (data) => data.items,
  })

export const useTenantsQuery = (params: GetTenantsParams = {}) =>
  useQuery({
    queryKey: tenantKeys.list(params),
    queryFn: () => getTenants(params),
  })