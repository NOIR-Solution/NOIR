import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle, Loader2, Shield } from 'lucide-react'
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
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
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent className="border-destructive/30">
        <AlertDialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <AlertTriangle className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <AlertDialogTitle>{t('roles.deleteTitle', 'Delete Role')}</AlertDialogTitle>
              <AlertDialogDescription>
                {t('roles.deleteDescription', 'Are you sure you want to delete this role? This action cannot be undone.')}
              </AlertDialogDescription>
            </div>
          </div>
        </AlertDialogHeader>

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

        <AlertDialogFooter>
          <AlertDialogCancel disabled={loading} className="cursor-pointer">
            {t('buttons.cancel', 'Cancel')}
          </AlertDialogCancel>
          <AlertDialogAction
            onClick={handleConfirm}
            disabled={loading}
            className="bg-destructive/10 text-destructive border border-destructive/30 hover:bg-destructive hover:text-destructive-foreground transition-colors cursor-pointer"
          >
            {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {loading ? t('labels.deleting', 'Deleting...') : t('buttons.delete', 'Delete')}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
