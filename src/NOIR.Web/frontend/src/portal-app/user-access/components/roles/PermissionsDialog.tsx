import { useEffect, useState } from 'react'
import { useTranslation } from 'react-i18next'
import { Key, Loader2, Sparkles, Shield, Lock } from 'lucide-react'
import {
  Button,
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@uikit'

import { toast } from 'sonner'
import { usePermissionTemplatesQuery, useRoleDetailQuery } from '@/portal-app/user-access/queries'
import { assignPermissions } from '@/services/roles'
import { ApiError } from '@/services/apiClient'
import type { RoleListItem } from '@/types'
import { PermissionPicker } from '@/components/PermissionPicker'

interface PermissionsDialogProps {
  role: RoleListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: () => void
}

export const PermissionsDialog = ({ role, open, onOpenChange, onSuccess }: PermissionsDialogProps) => {
  const { t } = useTranslation('common')
  const isReadOnly = role?.isSystemRole ?? false
  const { data: templates = [], isLoading: templatesLoading } = usePermissionTemplatesQuery()

  const { data: fullRole, isLoading: loadingPermissions } = useRoleDetailQuery(role?.id, open && !!role)

  const [selectedPermissions, setSelectedPermissions] = useState<Set<string>>(new Set())
  const [loading, setLoading] = useState(false)

  // Initialize selected permissions when role detail data loads
  useEffect(() => {
    if (fullRole) {
      setSelectedPermissions(new Set(fullRole.permissions || []))
    }
  }, [fullRole])

  const applyTemplate = (templateId: string) => {
    const template = templates.find(tmpl => tmpl.id === templateId)
    if (template) {
      setSelectedPermissions(new Set(template.permissions))
      toast.success(t('roles.templateApplied', 'Template applied'))
    }
  }

  const handleSave = async () => {
    if (!role || isReadOnly) return

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

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[800px] max-h-[90vh] flex flex-col overflow-hidden">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 bg-primary/10 rounded-lg">
              <Key className="h-5 w-5 text-primary" />
            </div>
            <div className="flex-1">
              <CredenzaTitle>{t('roles.permissionsTitle', 'Manage Permissions')}</CredenzaTitle>
              <CredenzaDescription>
                {t('roles.permissionsDescription', 'Configure permissions for {{role}}.', { role: role?.name })}
              </CredenzaDescription>
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
        </CredenzaHeader>

        <CredenzaBody>
          {isReadOnly && (
            <div className="flex items-center gap-2 p-3 bg-muted/50 border rounded-md text-sm text-muted-foreground mb-2">
              <Lock className="h-4 w-4 shrink-0" />
              <span>{t('roles.systemRoleReadOnly', 'System role permissions cannot be modified.')}</span>
            </div>
          )}

          {!isReadOnly && (
            <div className="flex justify-end mb-2">
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
          )}

          <PermissionPicker
            selectedPermissions={selectedPermissions}
            onPermissionsChange={setSelectedPermissions}
            readOnly={isReadOnly}
            isExternalLoading={loadingPermissions}
            showBulkActions={!isReadOnly}
          />
        </CredenzaBody>

        <CredenzaFooter className="pt-4">
          <Button type="button" variant="outline" className="cursor-pointer" onClick={() => onOpenChange(false)}>
            {isReadOnly ? t('buttons.close', 'Close') : t('buttons.cancel', 'Cancel')}
          </Button>
          {!isReadOnly && (
            <Button onClick={handleSave} disabled={loading} className="cursor-pointer">
              {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {loading ? t('labels.saving', 'Saving...') : t('buttons.save', 'Save Permissions')}
            </Button>
          )}
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}
