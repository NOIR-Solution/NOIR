import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { AlertTriangle, Loader2 } from 'lucide-react'
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog'
import { Button } from '@/components/ui/button'
import { toast } from 'sonner'
import type { UserListItem } from '@/types'

interface DeleteUserDialogProps {
  user: UserListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onConfirm: (id: string) => Promise<{ success: boolean; error?: string }>
}

export function DeleteUserDialog({ user, open, onOpenChange, onConfirm }: DeleteUserDialogProps) {
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
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle className="flex items-center gap-2">
            <AlertTriangle className="h-5 w-5 text-amber-500" />
            {user.isLocked
              ? t('users.unlockTitle', 'Unlock User')
              : t('users.lockTitle', 'Lock User')}
          </AlertDialogTitle>
          <AlertDialogDescription>
            {user.isLocked
              ? t('users.unlockDescription', 'Are you sure you want to unlock "{{email}}"? They will be able to sign in again.', { email: user.email })
              : t('users.lockDescription', 'Are you sure you want to lock "{{email}}"? They will not be able to sign in until unlocked.', { email: user.email })}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <Button
            variant="outline"
            onClick={() => onOpenChange(false)}
            disabled={loading}
          >
            {t('buttons.cancel', 'Cancel')}
          </Button>
          <Button
            variant={user.isLocked ? 'default' : 'destructive'}
            onClick={handleConfirm}
            disabled={loading}
          >
            {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            {user.isLocked
              ? t('users.unlock', 'Unlock')
              : t('users.lock', 'Lock')}
          </Button>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  )
}
