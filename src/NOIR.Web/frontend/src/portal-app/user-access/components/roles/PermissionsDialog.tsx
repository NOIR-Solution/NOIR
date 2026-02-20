import { useEffect, useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { Key, ChevronDown, ChevronRight, Loader2, Search, Sparkles, Shield, Check } from 'lucide-react'
import {
  Badge,
  Button,
  Checkbox,
  Collapsible,
  CollapsibleContent,
  CollapsibleTrigger,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  Input,
} from '@uikit'

import { toast } from 'sonner'
import { usePermissionsQuery, usePermissionTemplatesQuery } from '@/portal-app/user-access/queries'
import { assignPermissions, getRoleById } from '@/services/roles'
import { ApiError } from '@/services/apiClient'
import type { RoleListItem, Permission } from '@/types'
import { translatePermissionCategory, translatePermissionDisplayName, translatePermissionDescription } from '@/portal-app/user-access/utils/permissionTranslation'

interface PermissionsDialogProps {
  role: RoleListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: () => void
}

export const PermissionsDialog = ({ role, open, onOpenChange, onSuccess }: PermissionsDialogProps) => {
  const { t } = useTranslation('common')
  const { data: permissions = [], isLoading: permissionsLoading } = usePermissionsQuery()
  const { data: templates = [], isLoading: templatesLoading } = usePermissionTemplatesQuery()

  // Group permissions by category
  const permissionsByCategory = useMemo(() => {
    const grouped: Record<string, Permission[]> = {}
    for (const perm of permissions) {
      const category = perm.category || 'Other'
      if (!grouped[category]) grouped[category] = []
      grouped[category].push(perm)
    }
    return grouped
  }, [permissions])

  const [selectedPermissions, setSelectedPermissions] = useState<Set<string>>(new Set())
  const [expandedCategories, setExpandedCategories] = useState<Set<string>>(new Set())
  const [searchQuery, setSearchQuery] = useState('')
  const [loading, setLoading] = useState(false)
  const [loadingPermissions, setLoadingPermissions] = useState(false)

  // Initialize selected permissions when role changes - fetch role's current permissions
  useEffect(() => {
    if (role && open) {
      setExpandedCategories(new Set(Object.keys(permissionsByCategory)))

      // Fetch the role's current permissions
      setLoadingPermissions(true)
      getRoleById(role.id)
        .then((fullRole) => {
          // Use the role's direct permissions (not inherited)
          setSelectedPermissions(new Set(fullRole.permissions || []))
        })
        .catch(() => {
          // Start with empty if we can't load
          setSelectedPermissions(new Set())
        })
        .finally(() => {
          setLoadingPermissions(false)
        })
    }
  }, [role, open, permissionsByCategory])

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
    setSelectedPermissions(prev => {
      const next = new Set(prev)
      if (next.has(permissionName)) {
        next.delete(permissionName)
      } else {
        next.add(permissionName)
      }
      return next
    })
  }

  const toggleCategory = (category: string) => {
    const categoryPerms = permissionsByCategory[category] || []
    const allSelected = categoryPerms.every(p => selectedPermissions.has(p.name))

    setSelectedPermissions(prev => {
      const next = new Set(prev)
      if (allSelected) {
        categoryPerms.forEach(p => next.delete(p.name))
      } else {
        categoryPerms.forEach(p => next.add(p.name))
      }
      return next
    })
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

  const applyTemplate = (templateId: string) => {
    const template = templates.find(tmpl => tmpl.id === templateId)
    if (template) {
      setSelectedPermissions(new Set(template.permissions))
      toast.success(t('roles.templateApplied', 'Template applied'))
    }
  }

  const handleSave = async () => {
    if (!role) return

    setLoading(true)
    try {
      await assignPermissions(role.id, Array.from(selectedPermissions))

      toast.success(t('roles.permissionsUpdated', 'Permissions updated'))

      onOpenChange(false)
      onSuccess()
    } catch (err) {
      const message = err instanceof ApiError
        ? err.message
        : t('roles.permissionsError', 'Failed to update permissions')
      toast.error(message)
    } finally {
      setLoading(false)
    }
  }

  const getCategoryStats = (category: string) => {
    const categoryPerms = permissionsByCategory[category] || []
    const selectedCount = categoryPerms.filter(p => selectedPermissions.has(p.name)).length
    return { selected: selectedCount, total: categoryPerms.length }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[800px] max-h-[90vh] flex flex-col overflow-hidden">
        <DialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary/10 rounded-lg">
              <Key className="h-5 w-5 text-primary" />
            </div>
            <div className="flex-1">
              <DialogTitle>{t('roles.permissionsTitle', 'Manage Permissions')}</DialogTitle>
              <DialogDescription>
                {t('roles.permissionsDescription', 'Configure permissions for {{role}}.', { role: role?.name })}
              </DialogDescription>
            </div>
            {role && (
              <div
                className="w-8 h-8 rounded-full flex items-center justify-center"
                style={{ backgroundColor: role.color || '#6b7280' }}
              >
                <Shield className="h-4 w-4 text-white" />
              </div>
            )}
          </div>
        </DialogHeader>

        <div className="flex items-center gap-2 py-2">
          <div className="relative flex-1">
            <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
            <Input
              placeholder={t('roles.searchPermissions', 'Search permissions...')}
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="pl-10"
            />
          </div>

          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="outline" className="cursor-pointer" disabled={templatesLoading}>
                <Sparkles className="mr-2 h-4 w-4" />
                {t('roles.applyTemplate', 'Apply Template')}
              </Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end" className="w-56">
              {templates.map((template) => (
                <DropdownMenuItem
                  key={template.id}
                  onClick={() => applyTemplate(template.id)}
                >
                  <div className="flex flex-col">
                    <span>{template.name}</span>
                    <span className="text-xs text-muted-foreground">
                      {template.permissions.length} {t('labels.permissions', 'permissions')}
                    </span>
                  </div>
                </DropdownMenuItem>
              ))}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>

        <div className="flex items-center justify-between text-sm text-muted-foreground pb-2">
          <span>
            {t('roles.selectedCount', '{{count}} permissions selected', { count: selectedPermissions.size })}
          </span>
          <div className="flex gap-2">
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setSelectedPermissions(new Set(permissions.map(p => p.name)))}
            >
              {t('buttons.selectAll', 'Select All')}
            </Button>
            <Button
              variant="ghost"
              size="sm"
              onClick={() => setSelectedPermissions(new Set())}
            >
              {t('buttons.clearAll', 'Clear All')}
            </Button>
          </div>
        </div>

        <div className="flex-1 min-h-0 max-h-[55vh] border rounded-md overflow-y-auto">
          <div className="p-2 space-y-1">
            {permissionsLoading || loadingPermissions ? (
              <div className="p-8 text-center text-muted-foreground">
                {t('labels.loading', 'Loading...')}
              </div>
            ) : Object.keys(filteredPermissionsByCategory).length === 0 ? (
              <div className="p-8 text-center text-muted-foreground">
                {searchQuery
                  ? t('roles.noMatchingPermissions', 'No permissions match your search.')
                  : t('roles.noPermissions', 'No permissions available.')}
              </div>
            ) : (
              Object.entries(filteredPermissionsByCategory).map(([category, categoryPermissions]) => {
                const stats = getCategoryStats(category)
                const isExpanded = expandedCategories.has(category)
                const allSelected = stats.selected === stats.total && stats.total > 0
                const someSelected = stats.selected > 0 && stats.selected < stats.total

                return (
                  <Collapsible
                    key={category}
                    open={isExpanded}
                    onOpenChange={() => toggleCategoryExpand(category)}
                  >
                    <div className="flex items-center gap-2 p-2 hover:bg-muted rounded-md">
                      <Checkbox
                        checked={allSelected}
                        ref={(el) => {
                          if (el) {
                            (el as HTMLButtonElement & { indeterminate?: boolean }).indeterminate = someSelected
                          }
                        }}
                        onCheckedChange={() => toggleCategory(category)}
                        onClick={(e) => e.stopPropagation()}
                      />
                      <CollapsibleTrigger asChild>
                        <Button variant="ghost" className="flex-1 justify-start p-0 h-auto hover:bg-transparent">
                          {isExpanded ? (
                            <ChevronDown className="h-4 w-4 mr-2" />
                          ) : (
                            <ChevronRight className="h-4 w-4 mr-2" />
                          )}
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
                              className="flex items-start gap-3 p-2 hover:bg-muted/50 rounded-md cursor-pointer"
                              onClick={() => togglePermission(permission.name)}
                            >
                              <Checkbox
                                checked={selectedPermissions.has(permission.name)}
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
                                <p className="text-xs text-muted-foreground truncate">
                                  {permission.name}
                                </p>
                                {description && (
                                  <p className="text-xs text-muted-foreground mt-1">
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

        <DialogFooter className="pt-4">
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
            {t('buttons.cancel', 'Cancel')}
          </Button>
          <Button onClick={handleSave} disabled={loading} className="cursor-pointer">
            {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {loading ? t('labels.saving', 'Saving...') : t('buttons.save', 'Save Permissions')}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
