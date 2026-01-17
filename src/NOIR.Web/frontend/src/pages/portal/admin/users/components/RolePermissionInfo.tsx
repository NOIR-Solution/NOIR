import { useState, useCallback, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { Info, Loader2 } from 'lucide-react'
import { TippyTooltip } from '@/components/ui/tippy-tooltip'
import { getEffectivePermissions, getAllPermissions } from '@/services/roles'
import type { Permission, RoleListItem } from '@/types'

// Shared ref for permission details across instances (avoids module-level mutable state)
const sharedPermissionDetails = { current: null as Permission[] | null }

interface RolePermissionInfoProps {
  role: RoleListItem
  permissionsCache: Map<string, string[]>
  onPermissionsLoaded: (roleId: string, permissions: string[]) => void
}

export function RolePermissionInfo({ role, permissionsCache, onPermissionsLoaded }: RolePermissionInfoProps) {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)
  const [permissions, setPermissions] = useState<string[] | null>(null)
  const [allPermissions, setAllPermissions] = useState<Permission[]>(sharedPermissionDetails.current || [])
  // Track if we're already loading to prevent duplicate requests
  const loadingRef = useRef(false)

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

  const tooltipContent = (
    <div style={{ minWidth: '220px' }}>
      {/* Header - Primary theme color */}
      <div
        style={{
          padding: '10px 14px',
          background: 'linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)',
          fontWeight: 600,
          fontSize: '13px',
          color: '#ffffff',
          letterSpacing: '-0.01em',
        }}
      >
        {t('roles.permissions', 'Permissions')} ({totalCount})
      </div>
      {/* Content */}
      <div style={{ padding: '10px 14px' }}>
        {loading ? (
          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '8px 0' }}>
            <Loader2 className="h-4 w-4 animate-spin" style={{ color: '#6b7280' }} />
          </div>
        ) : totalCount === 0 ? (
          <p style={{ fontSize: '13px', color: '#6b7280', margin: 0 }}>
            {t('roles.noPermissions', 'No permissions assigned')}
          </p>
        ) : (
          <div style={{ maxHeight: '200px', overflowY: 'auto' }}>
            {groupedPermissions &&
              Object.entries(groupedPermissions).map(([category, perms], idx) => (
                <div key={category} style={{ marginTop: idx > 0 ? '12px' : 0 }}>
                  <div style={{
                    fontSize: '11px',
                    fontWeight: 600,
                    color: '#6b7280',
                    textTransform: 'uppercase',
                    letterSpacing: '0.05em',
                    marginBottom: '6px',
                  }}>
                    {category} ({perms.length})
                  </div>
                  <ul style={{ margin: 0, padding: 0, listStyle: 'none' }}>
                    {perms.slice(0, 5).map((perm) => (
                      <li
                        key={perm.name}
                        style={{
                          fontSize: '13px',
                          color: '#374151',
                          paddingLeft: '12px',
                          position: 'relative',
                          lineHeight: 1.6,
                        }}
                      >
                        <span style={{ position: 'absolute', left: 0, color: '#9ca3af' }}>•</span>
                        {perm.displayName}
                      </li>
                    ))}
                    {perms.length > 5 && (
                      <li style={{
                        fontSize: '12px',
                        color: '#9ca3af',
                        paddingLeft: '12px',
                        fontStyle: 'italic',
                      }}>
                        + {perms.length - 5} {t('labels.more', 'more')}...
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
