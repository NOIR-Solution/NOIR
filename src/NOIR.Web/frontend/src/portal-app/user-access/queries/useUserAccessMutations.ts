import { useMutation, useQueryClient } from '@tanstack/react-query'
import { deleteRole } from '@/services/roles'
import { deleteUser, lockUser, unlockUser } from '@/services/users'
import { deleteTenant } from '@/services/tenants'
import { roleKeys, userKeys, tenantKeys } from './queryKeys'
import { optimisticListDelete, optimisticListPatch } from '@/hooks/useOptimisticMutation'

export const useDeleteRoleMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteRole(id),
    ...optimisticListDelete(queryClient, roleKeys.lists(), roleKeys.all),
  })
}

export const useDeleteUserMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteUser(id),
    ...optimisticListDelete(queryClient, userKeys.lists(), userKeys.all),
  })
}

export const useLockUserMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => lockUser(id),
    ...optimisticListPatch(queryClient, userKeys.lists(), userKeys.all, { isLocked: true }),
  })
}

export const useUnlockUserMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => unlockUser(id),
    ...optimisticListPatch(queryClient, userKeys.lists(), userKeys.all, { isLocked: false }),
  })
}

export const useDeleteTenantMutation = () => {
  const queryClient = useQueryClient()
  return useMutation({
    mutationFn: (id: string) => deleteTenant(id),
    ...optimisticListDelete(queryClient, tenantKeys.lists(), tenantKeys.all),
  })
}
