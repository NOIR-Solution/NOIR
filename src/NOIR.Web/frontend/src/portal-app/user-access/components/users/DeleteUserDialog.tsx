import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle, Loader2 } from 'lucide-react'
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
import type { UserListItem } from '@/types'

interface DeleteUserDialogProps {
  user: UserListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (id: string) => Promise<{ success: boolean; error?: string }>
}

export const DeleteUserDialog = ({ user, open, onOpenChange, onConfirm }: DeleteUserDialogProps) => {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)

  const handleConfirm = async () => {
    if (!user) return

    setLoading(true)
    try {
      const result = await onConfirm(user.id)

      if (result.success) {
        toast.success(
          user.isLocked
            ? t('users.unlockSuccess', 'User unlocked successfully')
            : t('users.lockSuccess', 'User locked successfully')
        )
        onOpenChange(false)
      } else {
        toast.error(result.error || t('messages.operationFailed', 'Operation failed'))
      }
    } finally {
      setLoading(false)
    }
  }

  if (!user) return null

  return (
    <AlertDialog open={open} onOpenChange={onOpenChange}>
      <AlertDialogContent className="border-destructive/30">
        <AlertDialogHeader>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-destructive/10 border border-destructive/20">
              <AlertTriangle className="h-5 w-5 text-destructive" />
            </div>
            <div>
              <AlertDialogTitle>
                {user.isLocked
                  ? t('users.unlockTitle', 'Unlock User')
                  : t('users.lockTitle', 'Lock User')}
              </AlertDialogTitle>
              <AlertDialogDescription>
                {user.isLocked
                  ? t('users.unlockDescription', 'Are you sure you want to unlock "{{email}}"? They will be able to sign in again.', { email: user.email })
                  : t('users.lockDescription', 'Are you sure you want to lock "{{email}}"? They will not be able to sign in until unlocked.', { email: user.email })}
              </AlertDialogDescription>
            </div>
          </div>
        </AlertDialogHeader>
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
            {user.isLocked
              ? t('users.unlock', 'Unlock')
              : t('users.lock', 'Lock')}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
