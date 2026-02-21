import { useState, useEffect } from 'react'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { KeyRound, Eye, EyeOff, Loader2 } from 'lucide-react'
import {
  Button,
  Credenza,
  CredenzaBody,
  CredenzaContent,
  CredenzaDescription,
  CredenzaFooter,
  CredenzaHeader,
  CredenzaTitle,
  Input,
  Label,
} from '@uikit'

import { resetTenantAdminPassword } from '@/services/tenants'
import type { TenantListItem } from '@/types'

interface ResetAdminPasswordDialogProps {
  tenant: TenantListItem | null
  open: boolean
  onOpenChange: (open: boolean) => void
  onSuccess?: () => void
}

export const ResetAdminPasswordDialog = ({
  tenant,
  open,
  onOpenChange,
  onSuccess,
}: ResetAdminPasswordDialogProps) => {
  const { t } = useTranslation('common')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Reset form when dialog opens (not on close — avoids content flash during close animation)
  useEffect(() => {
    if (open) {
      setNewPassword('')
      setConfirmPassword('')
      setShowPassword(false)
      setShowConfirmPassword(false)
      setError(null)
    }
  }, [open])

  const validatePassword = (): string | null => {
    if (!newPassword) {
      return t('validation.required', { field: t('auth.newPassword') })
    }
    if (newPassword.length < 6) {
      return t('validation.minLength', { field: t('auth.newPassword'), min: 6 })
    }
    if (newPassword !== confirmPassword) {
      return t('validation.passwordMismatch')
    }
    return null
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!tenant) return

    const validationError = validatePassword()
    if (validationError) {
      setError(validationError)
      return
    }

    setLoading(true)
    setError(null)

    try {
      const result = await resetTenantAdminPassword(tenant.id, newPassword)

      if (result.success) {
        toast.success(
          t('tenants.resetPasswordSuccess', {
            email: result.adminEmail || tenant.name
          })
        )
        onOpenChange(false)
        onSuccess?.()
      } else {
        setError(result.message || t('messages.operationFailed'))
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : t('messages.operationFailed'))
    } finally {
      setLoading(false)
    }
  }

  return (
    <Credenza open={open} onOpenChange={onOpenChange}>
      <CredenzaContent className="sm:max-w-[425px]">
        <form onSubmit={handleSubmit}>
          <CredenzaHeader>
            <CredenzaTitle className="flex items-center gap-2">
              <KeyRound className="h-5 w-5" />
              {t('tenants.resetAdminPasswordTitle')}
            </CredenzaTitle>
            <CredenzaDescription>
              {t('tenants.resetAdminPasswordDescription', {
                name: tenant?.name || tenant?.identifier
              })}
            </CredenzaDescription>
          </CredenzaHeader>

          <CredenzaBody>
            <div className="grid gap-4 py-4">
              {error && (
                <div className="p-3 text-sm text-destructive bg-destructive/10 rounded-md">
                  {error}
                </div>
              )}

              <div className="grid gap-2">
                <Label htmlFor="newPassword">{t('auth.newPassword')}</Label>
                <div className="relative">
                  <Input
                    id="newPassword"
                    type={showPassword ? 'text' : 'password'}
                    value={newPassword}
                    onChange={(e) => setNewPassword(e.target.value)}
                    placeholder={t('auth.enterNewPassword')}
                    disabled={loading}
                    className="pr-10"
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="absolute right-0 top-0 h-full px-3 hover:bg-transparent"
                    onClick={() => setShowPassword(!showPassword)}
                    tabIndex={-1}
                    aria-label={showPassword ? t('buttons.hidePassword') : t('buttons.showPassword')}
                  >
                    {showPassword ? (
                      <EyeOff className="h-4 w-4 text-muted-foreground" />
                    ) : (
                      <Eye className="h-4 w-4 text-muted-foreground" />
                    )}
                  </Button>
                </div>
              </div>

              <div className="grid gap-2">
                <Label htmlFor="confirmPassword">{t('auth.confirmPassword')}</Label>
                <div className="relative">
                  <Input
                    id="confirmPassword"
                    type={showConfirmPassword ? 'text' : 'password'}
                    value={confirmPassword}
                    onChange={(e) => setConfirmPassword(e.target.value)}
                    placeholder={t('auth.confirmNewPassword')}
                    disabled={loading}
                    className="pr-10"
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="absolute right-0 top-0 h-full px-3 hover:bg-transparent"
                    onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                    tabIndex={-1}
                    aria-label={showConfirmPassword ? t('buttons.hidePassword') : t('buttons.showPassword')}
                  >
                    {showConfirmPassword ? (
                      <EyeOff className="h-4 w-4 text-muted-foreground" />
                    ) : (
                      <Eye className="h-4 w-4 text-muted-foreground" />
                    )}
                  </Button>
                </div>
              </div>
            </div>
          </CredenzaBody>

          <CredenzaFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={loading}
              className="cursor-pointer"
            >
              {t('buttons.cancel')}
            </Button>
            <Button type="submit" disabled={loading} className="cursor-pointer">
              {loading && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              {loading ? t('labels.loading') : t('buttons.resetPassword')}
            </Button>
          </CredenzaFooter>
        </form>
      </CredenzaContent>
    </Credenza>
  )
}
