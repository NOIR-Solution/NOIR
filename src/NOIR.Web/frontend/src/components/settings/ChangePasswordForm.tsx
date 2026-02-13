import { useState, useMemo } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { Lock, Eye, EyeOff, Shield } from 'lucide-react'
import { Button, Card, CardContent, CardDescription, CardHeader, CardTitle, Input, Label } from '@uikit'

import { changePassword, ApiError } from '@/services/settings'
import { useAuthContext } from '@/contexts/AuthContext'
import { useValidatedForm } from '@/hooks/useValidatedForm'
import { changePasswordSchema } from '@/validation/schemas.generated'
import { createValidationTranslator } from '@/lib/validation-i18n'
import { z } from 'zod'

// Extended schema factory with confirm password (client-side only validation)
const createChangePasswordFormSchema = (t: (key: string, options?: Record<string, unknown>) => string) =>
  changePasswordSchema.extend({
    confirmPassword: z.string().min(1, { message: t('validation.confirmNewPassword') }),
  }).refine((data) => data.newPassword === data.confirmPassword, {
    message: t('validation.passwordsMismatch'),
    path: ["confirmPassword"],
  }).refine((data) => data.currentPassword !== data.newPassword, {
    message: t('validation.passwordSameAsCurrent'),
    path: ["newPassword"],
  })

type ChangePasswordFormData = z.infer<ReturnType<typeof createChangePasswordFormSchema>>

export function ChangePasswordForm() {
  const { t } = useTranslation('auth')
  const { t: tCommon } = useTranslation('common')
  const navigate = useNavigate()
  const { logout } = useAuthContext()

  // Memoized translation function for validation errors
  const translateError = useMemo(() => createValidationTranslator(tCommon), [tCommon])

  const [showCurrentPassword, setShowCurrentPassword] = useState(false)
  const [showNewPassword, setShowNewPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)

  // Use validated form with Zod schema
  const { form, handleSubmit, isSubmitting, serverError } = useValidatedForm<ChangePasswordFormData>({
    schema: createChangePasswordFormSchema(tCommon),
    defaultValues: {
      currentPassword: '',
      newPassword: '',
      confirmPassword: '',
    },
    onSubmit: async (data) => {
      await changePassword({
        currentPassword: data.currentPassword,
        newPassword: data.newPassword,
      })

      toast.success(t('changePassword.success'))

      // Log out user since all sessions were revoked
      await logout()
      navigate('/login')
    },
    onError: (error) => {
      if (error instanceof ApiError) {
        // Error handled by useValidatedForm
      }
    },
  })

  const newPassword = form.watch('newPassword')
  const confirmPassword = form.watch('confirmPassword')

  return (
    <Card className="max-w-xl">
      <CardHeader>
        <div className="flex items-center gap-3">
          <div className="flex items-center justify-center w-10 h-10 rounded-lg bg-blue-600/10">
            <Shield className="h-5 w-5 text-blue-600" />
          </div>
          <div>
            <CardTitle>{t('changePassword.title')}</CardTitle>
            <CardDescription>{t('changePassword.description')}</CardDescription>
          </div>
        </div>
      </CardHeader>
      <CardContent>
        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Current Password */}
          <div className="space-y-2">
            <Label htmlFor="currentPassword">
              {t('changePassword.currentPassword')}
            </Label>
            <div className="relative group">
              <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground group-focus-within:text-blue-600 transition-colors" />
              <Input
                id="currentPassword"
                type={showCurrentPassword ? 'text' : 'password'}
                {...form.register('currentPassword')}
                className="pl-10 pr-10 focus:border-blue-600 focus:ring-blue-600/20"
                autoComplete="current-password"
                aria-invalid={!!form.formState.errors.currentPassword}
              />
              <button
                type="button"
                onClick={() => setShowCurrentPassword(!showCurrentPassword)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                aria-label={showCurrentPassword ? t('login.hidePassword') : t('login.showPassword')}
              >
                {showCurrentPassword ? (
                  <EyeOff className="h-4 w-4" />
                ) : (
                  <Eye className="h-4 w-4" />
                )}
              </button>
            </div>
            {form.formState.errors.currentPassword && (
              <p className="text-sm font-medium text-destructive">{translateError(form.formState.errors.currentPassword.message)}</p>
            )}
          </div>

          {/* New Password */}
          <div className="space-y-2">
            <Label htmlFor="newPassword">{t('changePassword.newPassword')}</Label>
            <div className="relative group">
              <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground group-focus-within:text-blue-600 transition-colors" />
              <Input
                id="newPassword"
                type={showNewPassword ? 'text' : 'password'}
                {...form.register('newPassword')}
                className="pl-10 pr-10 focus:border-blue-600 focus:ring-blue-600/20"
                autoComplete="new-password"
                aria-invalid={!!form.formState.errors.newPassword}
              />
              <button
                type="button"
                onClick={() => setShowNewPassword(!showNewPassword)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                aria-label={showNewPassword ? t('login.hidePassword') : t('login.showPassword')}
              >
                {showNewPassword ? (
                  <EyeOff className="h-4 w-4" />
                ) : (
                  <Eye className="h-4 w-4" />
                )}
              </button>
            </div>
            {form.formState.errors.newPassword && (
              <p className="text-sm font-medium text-destructive">{translateError(form.formState.errors.newPassword.message)}</p>
            )}
          </div>

          {/* Confirm Password */}
          <div className="space-y-2">
            <Label htmlFor="confirmPassword">
              {t('changePassword.confirmPassword')}
            </Label>
            <div className="relative group">
              <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground group-focus-within:text-blue-600 transition-colors" />
              <Input
                id="confirmPassword"
                type={showConfirmPassword ? 'text' : 'password'}
                {...form.register('confirmPassword')}
                className="pl-10 pr-10 focus:border-blue-600 focus:ring-blue-600/20"
                autoComplete="new-password"
                aria-invalid={!!form.formState.errors.confirmPassword}
              />
              <button
                type="button"
                onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                className="absolute right-3 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground transition-colors"
                aria-label={showConfirmPassword ? t('login.hidePassword') : t('login.showPassword')}
              >
                {showConfirmPassword ? (
                  <EyeOff className="h-4 w-4" />
                ) : (
                  <Eye className="h-4 w-4" />
                )}
              </button>
            </div>
            {form.formState.errors.confirmPassword && (
              <p className="text-sm font-medium text-destructive">{translateError(form.formState.errors.confirmPassword.message)}</p>
            )}
            {confirmPassword && !form.formState.errors.confirmPassword && newPassword === confirmPassword && (
              <p className="text-xs text-green-600">
                {t('forgotPassword.reset.passwordsMatch')}
              </p>
            )}
          </div>

          {/* Server Error */}
          {serverError && (
            <div className="p-4 rounded-lg bg-destructive/10 border border-destructive/20">
              <p className="text-sm text-destructive font-medium">{serverError}</p>
            </div>
          )}

          {/* Submit */}
          <Button
            type="submit"
            disabled={isSubmitting}
            className="w-full bg-gradient-to-r from-blue-700 to-cyan-700 hover:from-blue-800 hover:to-cyan-800 text-white shadow-lg"
          >
            {isSubmitting ? t('changePassword.submitting') : t('changePassword.submit')}
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}
