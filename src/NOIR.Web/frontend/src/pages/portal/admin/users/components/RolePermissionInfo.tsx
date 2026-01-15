import { useState, useCallback } from 'react'
import { useTranslation } from 'react-i18next'
import { Info, Loader2 } from 'lucide-react'
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from '@/components/ui/tooltip'
import { getEffectivePermissions, getAllPermissions } from '@/services/roles'
import type { Permission, RoleListItem } from '@/types'

// Cache for permission details (shared across all tooltip instances)
let permissionDetailsCache: Permission[] | null = null

interface RolePermissionInfoProps {
  role: RoleListItem
  permissionsCache: Map<string, string[]>
  onPermissionsLoaded: (roleId: string, permissions: string[]) => void
}

export function RolePermissionInfo({ role, permissionsCache, onPermissionsLoaded }: RolePermissionInfoProps) {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)
  const [permissions, setPermissions] = useState<string[] | null>(null)
  const [allPermissions, setAllPermissions] = useState<Permission[]>(permissionDetailsCache || [])

  const loadPermissions = useCallback(async () => {
    // Check cache first
    const cached = permissionsCache.get(role.id)
    if (cached) {
      setPermissions(cached)
      return
    }

    // Already loading or already have data
    if (loading || permissions !== null) return

    setLoading(true)
    try {
      // Load all permissions details if not cached
      if (!permissionDetailsCache) {
        permissionDetailsCache = await getAllPermissions()
        setAllPermissions(permissionDetailsCache)
      }

      // Load role's effective permissions
      const rolePermissions = await getEffectivePermissions(role.id)
      setPermissions(rolePermissions)
      onPermissionsLoaded(role.id, rolePermissions)
    } catch (err) {
      console.error('Failed to load permissions:', err)
      setPermissions([])
    } finally {
      setLoading(false)
    }
  }, [role.id, permissionsCache, onPermissionsLoaded, loading, permissions])

  // Group permissions by category for display
  const groupedPermissions = permissions?.reduce((groups, permName) => {
    const permDetail = allPermissions.find((p) => p.name === permName)
    const category = permDetail?.category || 'Other'
    if (!groups[category]) {
      groups[category] = []
    }
    groups[category].push({
      name: permName,
      displayName: permDetail?.displayName || permName,
    })
    return groups
  }, {} as Record<string, { name: string; displayName: string }[]>)

  const totalCount = permissions?.length || 0

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <button
          type="button"
          className="p-1 rounded hover:bg-accent/50 transition-colors"
          onClick={(e) => e.stopPropagation()}
          onMouseEnter={loadPermissions}
          onFocus={loadPermissions}
        >
          <Info className="h-4 w-4 text-muted-foreground" />
        </button>
      </TooltipTrigger>
      <TooltipContent
        side="right"
        className="max-w-[280px] p-3 bg-popover text-popover-foreground border shadow-md"
      >
        <div className="space-y-2">
          <div className="font-medium text-sm border-b pb-1">
            {t('roles.permissions', 'Permissions')} ({totalCount})
          </div>
          {loading ? (
            <div className="flex items-center justify-center py-2">
              <Loader2 className="h-4 w-4 animate-spin" />
            </div>
          ) : totalCount === 0 ? (
            <p className="text-xs text-muted-foreground">
              {t('roles.noPermissions', 'No permissions assigned')}
            </p>
          ) : (
            <div className="space-y-2 max-h-[200px] overflow-y-auto text-xs">
              {groupedPermissions &&
                Object.entries(groupedPermissions).map(([category, perms]) => (
                  <div key={category}>
                    <div className="font-medium text-muted-foreground mb-1">
                      {category} ({perms.length})
                    </div>
                    <ul className="space-y-0.5 pl-2">
                      {perms.slice(0, 5).map((perm) => (
                        <li key={perm.name} className="truncate">
                          • {perm.displayName}
                        </li>
                      ))}
                      {perms.length > 5 && (
                        <li className="text-muted-foreground">
                          + {perms.length - 5} {t('labels.more', 'more')}...
                        </li>
                      )}
                    </ul>
                  </div>
                ))}
            </div>
          )}
        </div>
      </TooltipContent>
    </Tooltip>
  )
}
