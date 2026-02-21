import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle, Loader2, Shield } from 'lucide-react'
import {
  Button,
  Credenza,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  CredenzaBody,
} from '@uikit'
import { toast } from 'sonner'
import type { RoleListItem } from '@/types'

interface DeleteRoleDialogProps {
  role: RoleListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (id: string) => Promise<{ success: boolean; error?: string }>
}

export const DeleteRoleDialog = ({ role, open, onOpenChange, onConfirm }: DeleteRoleDialogProps) => {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)

  const handleConfirm = async () => {
    if (!role) return

    setLoading(true)
    try {
      const result = await onConfirm(role.id)

      if (result.success) {
        toast.success(t('roles.deleteSuccess', 'Role deleted'))
        onOpenChange(false)
      } else {
        toast.error(result.error || t('roles.deleteError', 'Failed to delete role'))
      }
    } finally {
      setLoading(false)
    }
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="border-destructive/30">
        <CredenzaHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <AlertTriangle className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <CredenzaTitle>{t('roles.deleteTitle', 'Delete Role')}</CredenzaTitle>
              <CredenzaDescription>
                {t('roles.deleteDescription', 'Are you sure you want to delete this role? This action cannot be undone.')}
              </CredenzaDescription>
            </div>
          </div>
        </CredenzaHeader>

        <CredenzaBody>
          {role && (
            <div className="my-4 p-4 bg-muted rounded-lg">
              <div className="flex items-center gap-3">
                <div
                  className="w-10 h-10 rounded-full flex items-center justify-center"
                  style={{ backgroundColor: role.color || '#6b7280' }}
                >
                  <Shield className="h-5 w-5 text-white" />
                </div>
                <div>
                  <p className="font-medium">{role.name}</p>
                  {role.description && (
                    <p className="text-sm text-muted-foreground">{role.description}</p>
                  )}
                </div>
              </div>
              {role.userCount > 0 && (
                <div className="mt-3 p-2 bg-destructive/10 rounded text-sm text-destructive">
                  {t('roles.deleteWarning', '{{count}} users are assigned to this role and will need to be reassigned.', { count: role.userCount })}
                </div>
              )}
            </div>
          )}
        </CredenzaBody>

        <CredenzaFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={loading} className="cursor-pointer">
            {t('buttons.cancel', 'Cancel')}
          </Button>
          <Button
            variant="destructive"
            onClick={handleConfirm}
            disabled={loading}
            className="cursor-pointer"
          >
            {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {loading ? t('labels.deleting', 'Deleting...') : t('buttons.delete', 'Delete')}
          </Button>
        </CredenzaFooter>
      </CredenzaContent>
    </Credenza>
  )
}
