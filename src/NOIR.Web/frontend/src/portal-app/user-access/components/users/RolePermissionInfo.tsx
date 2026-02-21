import { useState, useCallback, useRef, type WheelEvent } from 'react'
import { useTranslation } from 'react-i18next'
import { Info, Loader2 } from 'lucide-react'
import { TippyTooltip } from '@uikit'
import { getEffectivePermissions, getAllPermissions } from '@/services/roles'
import type { Permission, RoleListItem } from '@/types'
import { translatePermissionCategory, translatePermissionDisplayName } from '@/portal-app/user-access/utils/permissionTranslation'
import { useAuthContext } from '@/contexts/AuthContext'
import { isPlatformAdmin } from '@/lib/roles'

// Shared ref for permission details across instances (avoids module-level mutable state)
const sharedPermissionDetails = { current: null as Permission[] | null }

interface RolePermissionInfoProps {
  role: RoleListItem
  permissionsCache: Map<string, string[]>
  onPermissionsLoaded: (roleId: string, permissions: string[]) => void
}

export const RolePermissionInfo = ({ role, permissionsCache, onPermissionsLoaded }: RolePermissionInfoProps) => {
  const { t } = useTranslation('common')
  const { user } = useAuthContext()
  const showPlatformPermissions = isPlatformAdmin(user?.roles)
  const [loading, setLoading] = useState(false)
  const [permissions, setPermissions] = useState<string[] | null>(null)
  const [allPermissions, setAllPermissions] = useState<Permission[]>(sharedPermissionDetails.current || [])
  // Track if we're already loading to prevent duplicate requests
  const loadingRef = useRef(false)
  // Ref for scroll container
  const scrollRef = useRef<HTMLDivElement>(null)

  // Handle mouse wheel scrolling explicitly (Radix Tooltip can block wheel events)
  const handleWheel = (e: WheelEvent<HTMLDivElement>) => {
    const container = scrollRef.current
    if (!container) return

    // Prevent the event from bubbling to prevent Tooltip issues
    e.stopPropagation()

    // Manually scroll the container
    container.scrollTop += e.deltaY
  }

  const loadPermissions = useCallback(async () => {
    // Check cache first
    const cached = permissionsCache.get(role.id)
    if (cached) {
      setPermissions(cached)
      return
    }

    // Already loading or already have data - use ref to avoid stale closure
    if (loadingRef.current || permissions !== null) return

    loadingRef.current = true
    setLoading(true)
    try {
      // Load all permissions details if not cached
      if (!sharedPermissionDetails.current) {
        sharedPermissionDetails.current = await getAllPermissions()
        setAllPermissions(sharedPermissionDetails.current)
      }

      // Load role's effective permissions
      const rolePermissions = await getEffectivePermissions(role.id)
      setPermissions(rolePermissions)
      onPermissionsLoaded(role.id, rolePermissions)
    } catch {
      // Error already visible in network tab - no need to console.error
      setPermissions([])
    } finally {
      loadingRef.current = false
      setLoading(false)
    }
  }, [role.id, permissionsCache, onPermissionsLoaded, permissions])

  // Group permissions by category for display (exclude platform-only for non-platform admins)
  const groupedPermissions = permissions?.reduce((groups, permName) => {
    const permDetail = allPermissions.find((p) => p.name === permName)
    if (!showPlatformPermissions && (!permDetail || !permDetail.isTenantAllowed)) return groups
    const category = permDetail?.category || t('permissions.categories.other', 'Other')
    if (!groups[category]) {
      groups[category] = []
    }
    groups[category].push({
      name: permName,
      displayName: translatePermissionDisplayName(t, permName, permDetail?.displayName || permName),
    })
    return groups
  }, {} as Record<string, { name: string; displayName: string }[]>)

  const totalCount = groupedPermissions
    ? Object.values(groupedPermissions).reduce((sum, perms) => sum + perms.length, 0)
    : 0

  const tooltipContent = (
    <div className="min-w-[200px] max-w-[280px]">
      {/* Header */}
      <div className="px-3 py-2 bg-primary text-primary-foreground font-semibold text-xs rounded-t-md -mx-3 -mt-1.5">
        {t('roles.permissions', 'Permissions')} ({totalCount})
      </div>
      {/* Content */}
      <div className="pt-2">
        {loading ? (
          <div className="flex items-center justify-center py-2">
            <Loader2 className="h-4 w-4 animate-spin text-muted-foreground" />
          </div>
        ) : totalCount === 0 ? (
          <p className="text-xs text-muted-foreground">
            {t('roles.noPermissions', 'No permissions assigned')}
          </p>
        ) : (
          <div
            ref={scrollRef}
            className="max-h-[200px] overflow-y-auto space-y-2 pr-1 overscroll-contain"
            style={{
              scrollbarWidth: 'thin',
              scrollbarColor: 'color-mix(in oklch, var(--muted-foreground) 30%, transparent) transparent',
            }}
            onWheel={handleWheel}
          >
            {groupedPermissions &&
              Object.entries(groupedPermissions).map(([category, perms]) => (
                <div key={category}>
                  <div className="text-[10px] font-semibold text-muted-foreground uppercase tracking-wide mb-1">
                    {translatePermissionCategory(t, category)} ({perms.length})
                  </div>
                  <ul className="space-y-0.5">
                    {perms.slice(0, 5).map((perm) => (
                      <li
                        key={perm.name}
                        className="text-xs text-foreground pl-3 relative"
                      >
                        <span className="absolute left-0 text-muted-foreground">•</span>
                        {perm.displayName}
                      </li>
                    ))}
                    {perms.length > 5 && (
                      <li className="text-[11px] text-muted-foreground pl-3 italic">
                        {t('labels.andMore', '+ {{count}} more...', { count: perms.length - 5 })}
                      </li>
                    )}
                  </ul>
                </div>
              ))}
          </div>
        )}
      </div>
    </div>
  )

  return (
    <TippyTooltip
      content={tooltipContent}
      placement="right"
      interactive={true}
      onShow={() => { loadPermissions() }}
      delay={[100, 0]}
      contentClassName="bg-popover text-popover-foreground border shadow-lg"
    >
      <button
        type="button"
        className="p-1 rounded hover:bg-accent/50 transition-colors"
        onClick={(e) => e.stopPropagation()}
      >
        <Info className="h-4 w-4 text-muted-foreground" />
      </button>
    </TippyTooltip>
  )
}
