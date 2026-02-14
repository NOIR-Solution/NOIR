import { useState, useEffect, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { Shield, Loader2, Check } from 'lucide-react'
import {
  Badge,
  Button,
  Checkbox,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Label,
  ScrollArea,
} from '@uikit'

import { toast } from 'sonner'
import { getUserRoles, assignRolesToUser } from '@/services/users'
import { useAvailableRolesQuery } from '@/portal-app/user-access/queries'
import { RolePermissionInfo } from './RolePermissionInfo'
import type { UserListItem } from '@/types'

interface AssignRolesDialogProps {
  user: UserListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: () => void
}

export const AssignRolesDialog = ({ user, open, onOpenChange, onSuccess }: AssignRolesDialogProps) => {
  const { t } = useTranslation('common')
  const { data: availableRoles = [], isLoading: loadingRoles } = useAvailableRolesQuery()
  const [selectedRoles, setSelectedRoles] = useState<Set<string>>(new Set())
  const [loading, setLoading] = useState(false)
  const [loadingUserRoles, setLoadingUserRoles] = useState(false)
  const [permissionsCache] = useState(() => new Map<string, string[]>())

  // Callback to cache loaded permissions
  const handlePermissionsLoaded = useCallback((roleId: string, permissions: string[]) => {
    permissionsCache.set(roleId, permissions)
  }, [permissionsCache])

  // Load user's current roles when dialog opens
  useEffect(() => {
    if (user && open) {
      setLoadingUserRoles(true)
      getUserRoles(user.id)
        .then((roles) => {
          setSelectedRoles(new Set(roles))
        })
        .catch(() => {
          // Fall back to roles from user list item
          setSelectedRoles(new Set(user.roles))
        })
        .finally(() => {
          setLoadingUserRoles(false)
        })
    }
  }, [user, open])

  const handleToggleRole = (roleName: string) => {
    setSelectedRoles((prev) => {
      const next = new Set(prev)
      if (next.has(roleName)) {
        next.delete(roleName)
      } else {
        next.add(roleName)
      }
      return next
    })
  }

  const handleSelectAll = () => {
    setSelectedRoles(new Set(availableRoles.map((r) => r.name)))
  }

  const handleClearAll = () => {
    setSelectedRoles(new Set())
  }

  const handleSubmit = async () => {
    if (!user) return

    setLoading(true)
    try {
      await assignRolesToUser({
        userId: user.id,
        roleNames: Array.from(selectedRoles),
      })
      toast.success(t('users.rolesAssigned', 'Roles assigned successfully'))
      onSuccess()
      onOpenChange(false)
    } catch {
      toast.error(t('messages.operationFailed', 'Operation failed. Please try again.'))
    } finally {
      setLoading(false)
    }
  }

  const isLoading = loadingRoles || loadingUserRoles

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[500px]">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Shield className="h-5 w-5" />
            {t('users.assignRolesTitle', 'Assign Roles')}
          </DialogTitle>
          <DialogDescription>
            {t('users.assignRolesDescription', 'Select roles to assign to "{{email}}"', {
              email: user?.email || '',
            })}
          </DialogDescription>
        </DialogHeader>

        {isLoading ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
          </div>
        ) : (
          <>
            <div className="flex items-center justify-between py-2">
              <div className="text-sm text-muted-foreground">
                {t('users.rolesSelected', '{{count}} roles selected', {
                  count: selectedRoles.size,
                })}
              </div>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleSelectAll}
                >
                  {t('roles.selectAll', 'Select All')}
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleClearAll}
                >
                  {t('roles.clearAll', 'Clear All')}
                </Button>
              </div>
            </div>

            <ScrollArea className="h-[300px] rounded-md border p-4">
              <div className="space-y-3">
                {availableRoles.length === 0 ? (
                  <div className="text-center py-8 text-muted-foreground">
                    {t('users.noRolesAvailable', 'No roles available')}
                  </div>
                ) : (
                  availableRoles.map((role) => (
                    <div
                      key={role.id}
                      className="flex items-start space-x-3 rounded-lg border p-3 hover:bg-accent/50 transition-colors cursor-pointer"
                      onClick={() => handleToggleRole(role.name)}
                    >
                      <Checkbox
                        id={`role-${role.id}`}
                        checked={selectedRoles.has(role.name)}
                        onCheckedChange={() => handleToggleRole(role.name)}
                        className="mt-0.5"
                      />
                      <div className="flex-1 space-y-1">
                        <div className="flex items-center gap-2">
                          <Label
                            htmlFor={`role-${role.id}`}
                            className="font-medium cursor-pointer"
                          >
                            {role.name}
                          </Label>
                          {role.isSystemRole && (
                            <Badge variant="secondary" className="text-xs">
                              {t('roles.system', 'System')}
                            </Badge>
                          )}
                          <RolePermissionInfo
                            role={role}
                            permissionsCache={permissionsCache}
                            onPermissionsLoaded={handlePermissionsLoaded}
                          />
                          {selectedRoles.has(role.name) && (
                            <Check className="h-4 w-4 text-green-600 ml-auto" />
                          )}
                        </div>
                        {role.description && (
                          <p className="text-xs text-muted-foreground">
                            {role.description}
                          </p>
                        )}
                        <p className="text-xs text-muted-foreground">
                          {t('roles.userCount', '{{count}} users', { count: role.userCount })}
                        </p>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </ScrollArea>
          </>
        )}

        <DialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={loading}
          >
            {t('buttons.cancel', 'Cancel')}
          </Button>
          <Button onClick={handleSubmit} disabled={loading || isLoading}>
            {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {t('buttons.save', 'Save')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
