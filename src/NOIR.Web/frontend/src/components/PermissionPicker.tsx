import { useState, useMemo, useEffect, useRef } from 'react'
import { useTranslation } from 'react-i18next'
import { Key, ChevronDown, ChevronRight, Search, Check, Minus } from 'lucide-react'
import {
  Badge,
  Button,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
  EmptyState,
  Input,
} from '@uikit'
import { cn } from '@/lib/utils'

import { usePermissionsQuery } from '@/portal-app/user-access/queries'
import { useAuthContext } from '@/contexts/AuthContext'
import { isPlatformAdmin } from '@/lib/roles'
import type { Permission } from '@/types'
import {
  translatePermissionCategory,
  translatePermissionDisplayName,
  translatePermissionDescription,
  comparePermissionCategories,
  CATEGORY_ICONS,
} from '@/portal-app/user-access/utils/permissionTranslation'

/**
 * Lightweight checkbox that avoids Radix's Presence-based indicator.
 * Radix Checkbox uses Presence internally which calls setNode(setState) in a ref callback —
 * when 60+ checkboxes change state simultaneously (Select All / category toggle),
 * this causes "Maximum update depth exceeded". This component renders the same visual
 * without Presence, so bulk operations work safely.
 */
const LightCheckbox = ({
  checked,
  indeterminate,
  disabled,
  onCheckedChange,
  onClick,
  className,
}: {
  checked: boolean
  indeterminate?: boolean
  disabled?: boolean
  onCheckedChange: () => void
  onClick?: (e: React.MouseEvent) => void
  className?: string
}) => (
  <button
    type="button"
    role="checkbox"
    aria-checked={indeterminate ? 'mixed' : checked}
    disabled={disabled}
    onClick={(e) => {
      onClick?.(e)
      onCheckedChange()
    }}
    className={cn(
      'peer h-4 w-4 shrink-0 rounded-sm border border-primary ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50 cursor-pointer',
      (checked || indeterminate) && 'bg-primary text-primary-foreground',
      className
    )}
  >
    {indeterminate ? (
      <span className="flex items-center justify-center text-current">
        <Minus className="h-3 w-3" />
      </span>
    ) : checked ? (
      <span className="flex items-center justify-center text-current">
        <Check className="h-3 w-3" />
      </span>
    ) : null}
  </button>
)

interface PermissionPickerProps {
  /** Currently selected permission names */
  selectedPermissions: Set<string>
  /** Callback when permissions change */
  onPermissionsChange: (permissions: Set<string>) => void
  /** If provided, only show permissions in this set (e.g. user's own permissions for API key scoping) */
  allowedPermissions?: Set<string>
  /** Whether the picker is read-only */
  readOnly?: boolean
  /** Max height for the scrollable area */
  maxHeight?: string
  /** Whether to show Select All / Clear All buttons */
  showBulkActions?: boolean
  /** Whether the permissions are loading externally (e.g. role detail loading) */
  isExternalLoading?: boolean
}

export const PermissionPicker = ({
  selectedPermissions,
  onPermissionsChange,
  allowedPermissions,
  readOnly = false,
  maxHeight = '55vh',
  showBulkActions = true,
  isExternalLoading = false,
}: PermissionPickerProps) => {
  const { t } = useTranslation('common')
  const { user } = useAuthContext()
  const { data: allPermissions = [], isLoading: permissionsLoading } = usePermissionsQuery()

  const [expandedCategories, setExpandedCategories] = useState<Set<string>>(new Set())
  const [searchQuery, setSearchQuery] = useState('')
  const initializedRef = useRef(false)

  // Filter permissions: exclude platform-only for non-platform admins, then filter by allowed set
  const permissions = useMemo(() => {
    let filtered = isPlatformAdmin(user?.roles) ? allPermissions : allPermissions.filter(p => p.isTenantAllowed)
    if (allowedPermissions) {
      filtered = filtered.filter(p => allowedPermissions.has(p.name))
    }
    return filtered
  }, [allPermissions, user?.roles, allowedPermissions])

  // Group permissions by category, sorted by sortOrder within each group
  const permissionsByCategory = useMemo(() => {
    const grouped: Record<string, Permission[]> = {}
    for (const perm of permissions) {
      const category = perm.category || 'Other'
      if (!grouped[category]) grouped[category] = []
      grouped[category].push(perm)
    }
    for (const perms of Object.values(grouped)) {
      perms.sort((a, b) => a.sortOrder - b.sortOrder)
    }
    return grouped
  }, [permissions])

  // Auto-expand all categories on first render
  useEffect(() => {
    if (!initializedRef.current && Object.keys(permissionsByCategory).length > 0) {
      setExpandedCategories(new Set(Object.keys(permissionsByCategory)))
      initializedRef.current = true
    }
  }, [permissionsByCategory])

  // Filter permissions by search query
  const filteredPermissionsByCategory = useMemo(() => {
    if (!searchQuery) return permissionsByCategory

    const query = searchQuery.toLowerCase()
    const filtered: Record<string, Permission[]> = {}

    for (const [category, perms] of Object.entries(permissionsByCategory)) {
      const matchingPerms = perms.filter(
        p =>
          p.name.toLowerCase().includes(query) ||
          p.displayName.toLowerCase().includes(query) ||
          (p.description && p.description.toLowerCase().includes(query))
      )
      if (matchingPerms.length > 0) {
        filtered[category] = matchingPerms
      }
    }

    return filtered
  }, [permissionsByCategory, searchQuery])

  const togglePermission = (permissionName: string) => {
    const next = new Set(selectedPermissions)
    if (next.has(permissionName)) {
      next.delete(permissionName)
    } else {
      next.add(permissionName)
    }
    onPermissionsChange(next)
  }

  const toggleCategory = (category: string) => {
    const categoryPerms = permissionsByCategory[category] || []
    const allSelected = categoryPerms.every(p => selectedPermissions.has(p.name))

    const next = new Set(selectedPermissions)
    if (allSelected) {
      categoryPerms.forEach(p => next.delete(p.name))
    } else {
      categoryPerms.forEach(p => next.add(p.name))
    }
    onPermissionsChange(next)
  }

  const toggleCategoryExpand = (category: string) => {
    setExpandedCategories(prev => {
      const next = new Set(prev)
      if (next.has(category)) {
        next.delete(category)
      } else {
        next.add(category)
      }
      return next
    })
  }

  const getCategoryStats = (category: string) => {
    const categoryPerms = permissionsByCategory[category] || []
    const selectedCount = categoryPerms.filter(p => selectedPermissions.has(p.name)).length
    return { selected: selectedCount, total: categoryPerms.length }
  }

  const isLoading = permissionsLoading || isExternalLoading

  return (
    <div className="space-y-2">
      {/* Search */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          placeholder={t('roles.searchPermissions', 'Search permissions...')}
          value={searchQuery}
          onChange={(e) => setSearchQuery(e.target.value)}
          className="pl-10"
        />
      </div>

      {/* Bulk actions + count */}
      <div className="flex items-center justify-between text-sm text-muted-foreground">
        <span>
          {t('roles.selectedCount', '{{count}} permissions selected', { count: selectedPermissions.size })}
        </span>
        {!readOnly && showBulkActions && (
          <div className="flex gap-2">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => onPermissionsChange(new Set(permissions.map(p => p.name)))}
            >
              {t('buttons.selectAll', 'Select All')}
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => onPermissionsChange(new Set())}
            >
              {t('buttons.clearAll', 'Clear All')}
            </Button>
          </div>
        )}
      </div>

      {/* Permission categories */}
      <div className={`min-h-0 border rounded-md overflow-y-auto`} style={{ maxHeight }}>
        <div className="p-2 space-y-1">
          {isLoading ? (
            <div className="p-8 text-center text-muted-foreground">
              {t('labels.loading', 'Loading...')}
            </div>
          ) : Object.keys(filteredPermissionsByCategory).length === 0 ? (
            <EmptyState
              icon={Key}
              title={searchQuery
                ? t('roles.noMatchingPermissions', 'No permissions match your search.')
                : t('roles.noPermissions', 'No permissions available.')}
              description=""
              className="border-0 rounded-none px-4 py-8"
            />
          ) : (
            Object.entries(filteredPermissionsByCategory)
              .sort(([a], [b]) => comparePermissionCategories(a, b))
              .map(([category, categoryPermissions]) => {
              const stats = getCategoryStats(category)
              const isExpanded = expandedCategories.has(category)
              const allSelected = stats.selected === stats.total && stats.total > 0
              const someSelected = stats.selected > 0 && stats.selected < stats.total
              const CategoryIcon = CATEGORY_ICONS[category]

              return (
                <Collapsible
                  key={category}
                  open={isExpanded}
                  onOpenChange={() => toggleCategoryExpand(category)}
                >
                  <div className="flex items-center gap-2 p-2 hover:bg-muted rounded-md">
                    <LightCheckbox
                      checked={allSelected}
                      indeterminate={someSelected}
                      disabled={readOnly}
                      onCheckedChange={() => toggleCategory(category)}
                      onClick={(e) => e.stopPropagation()}
                    />
                    <CollapsibleTrigger asChild>
                      <Button variant="ghost" className="flex-1 justify-start p-0 h-auto hover:bg-transparent cursor-pointer">
                        {isExpanded ? (
                          <ChevronDown className="h-4 w-4 mr-2" />
                        ) : (
                          <ChevronRight className="h-4 w-4 mr-2" />
                        )}
                        {CategoryIcon && <CategoryIcon className="h-4 w-4 mr-1.5 text-muted-foreground" />}
                        <span className="font-medium">{translatePermissionCategory(t, category)}</span>
                        <Badge variant="secondary" className="ml-2">
                          {stats.selected}/{stats.total}
                        </Badge>
                      </Button>
                    </CollapsibleTrigger>
                  </div>

                  <CollapsibleContent>
                    <div className="ml-6 space-y-1">
                      {categoryPermissions.map((permission) => {
                        const description = translatePermissionDescription(t, permission.name, permission.description)
                        return (
                          <div
                            key={permission.id}
                            className={`flex items-start gap-3 p-2 hover:bg-muted/50 rounded-md ${readOnly ? '' : 'cursor-pointer'}`}
                            onClick={readOnly ? undefined : () => togglePermission(permission.name)}
                          >
                            <LightCheckbox
                              checked={selectedPermissions.has(permission.name)}
                              disabled={readOnly}
                              onCheckedChange={() => togglePermission(permission.name)}
                              onClick={(e) => e.stopPropagation()}
                              className="mt-0.5"
                            />
                            <div className="flex-1 min-w-0">
                              <div className="flex items-center gap-2">
                                <span className="font-medium text-sm">{translatePermissionDisplayName(t, permission.name, permission.displayName)}</span>
                                {selectedPermissions.has(permission.name) && (
                                  <Check className="h-3 w-3 text-primary" />
                                )}
                              </div>
                              {description && (
                                <p className="text-xs text-muted-foreground">
                                  {description}
                                </p>
                              )}
                            </div>
                          </div>
                        )
                      })}
                    </div>
                  </CollapsibleContent>
                </Collapsible>
              )
            })
          )}
        </div>
      </div>
    </div>
  )
}
