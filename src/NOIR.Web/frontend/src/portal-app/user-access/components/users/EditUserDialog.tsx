import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { Loader2 } from 'lucide-react'
import {
  Button,
  Checkbox,
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  Input,
  Label,
} from '@uikit'

import { toast } from 'sonner'
import { getUserById, updateUser } from '@/services/users'
import type { UserListItem, UserProfile } from '@/types'

interface EditUserDialogProps {
  user: UserListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess: () => void
}

export const EditUserDialog = ({ user, open, onOpenChange, onSuccess }: EditUserDialogProps) => {
  const { t } = useTranslation('common')
  const [loading, setLoading] = useState(false)
  const [loadingProfile, setLoadingProfile] = useState(false)
  const [profile, setProfile] = useState<UserProfile | null>(null)

  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName] = useState('')
  const [displayName, setDisplayName] = useState('')
  const [lockoutEnabled, setLockoutEnabled] = useState(false)

  // Load full user profile when dialog opens
  useEffect(() => {
    if (user && open) {
      setLoadingProfile(true)
      getUserById(user.id)
        .then((data) => {
          setProfile(data)
          setFirstName(data.firstName || '')
          setLastName(data.lastName || '')
          setDisplayName(data.displayName || '')
          setLockoutEnabled(!data.isActive)
        })
        .catch(() => {
          toast.error(t('users.loadError', 'Failed to load user details'))
        })
        .finally(() => {
          setLoadingProfile(false)
        })
    }
  }, [user, open, t])

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!user) return

    setLoading(true)
    try {
      await updateUser({
        userId: user.id,
        firstName: firstName.trim() || null,
        lastName: lastName.trim() || null,
        displayName: displayName.trim(), // Send empty string to clear, non-empty to update
        lockoutEnabled,
      })
      toast.success(t('messages.updateSuccess', 'Updated successfully'))
      onSuccess()
      onOpenChange(false)
    } catch {
      toast.error(t('messages.operationFailed', 'Operation failed. Please try again.'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-[425px]">
        <DialogHeader>
          <DialogTitle>{t('users.editTitle', 'Edit User')}</DialogTitle>
          <DialogDescription>
            {t('users.editDescription', 'Update user details and account status')}
          </DialogDescription>
        </DialogHeader>

        {loadingProfile ? (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="h-8 w-8 animate-spin text-muted-foreground" />
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <div className="grid gap-4 py-4">
              <div className="grid gap-2">
                <Label htmlFor="email">{t('labels.email', 'Email')}</Label>
                <Input
                  id="email"
                  value={profile?.email || user?.email || ''}
                  disabled
                  className="bg-muted"
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div className="grid gap-2">
                  <Label htmlFor="firstName">{t('users.form.firstName', 'First Name')}</Label>
                  <Input
                    id="firstName"
                    value={firstName}
                    onChange={(e) => setFirstName(e.target.value)}
                    placeholder={t('users.form.firstNamePlaceholder', 'John')}
                  />
                </div>
                <div className="grid gap-2">
                  <Label htmlFor="lastName">{t('users.form.lastName', 'Last Name')}</Label>
                  <Input
                    id="lastName"
                    value={lastName}
                    onChange={(e) => setLastName(e.target.value)}
                    placeholder={t('users.form.lastNamePlaceholder', 'Doe')}
                  />
                </div>
              </div>

              <div className="grid gap-2">
                <Label htmlFor="displayName">{t('users.form.displayName', 'Display Name')}</Label>
                <Input
                  id="displayName"
                  value={displayName}
                  onChange={(e) => setDisplayName(e.target.value)}
                  placeholder={t('users.form.displayNamePlaceholder', 'John Doe')}
                />
                <p className="text-xs text-muted-foreground">
                  {t('users.form.displayNameHint', 'Optional. Overrides first/last name display.')}
                </p>
              </div>

              <div className="flex items-start space-x-3 rounded-lg border p-3">
                <Checkbox
                  id="lockout"
                  checked={lockoutEnabled}
                  onCheckedChange={(checked) => setLockoutEnabled(checked as boolean)}
                  className="mt-0.5"
                />
                <div className="space-y-0.5">
                  <Label htmlFor="lockout" className="cursor-pointer">
                    {t('users.form.lockAccount', 'Lock Account')}
                  </Label>
                  <p className="text-xs text-muted-foreground">
                    {t('users.form.lockAccountHint', 'Prevent user from signing in')}
                  </p>
                </div>
              </div>
            </div>

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => onOpenChange(false)}
                disabled={loading}
              >
                {t('buttons.cancel', 'Cancel')}
              </Button>
              <Button type="submit" disabled={loading}>
                {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                {t('buttons.save', 'Save')}
              </Button>
            </DialogFooter>
          </form>
        )}
      </DialogContent>
    </Dialog>
  )
}
