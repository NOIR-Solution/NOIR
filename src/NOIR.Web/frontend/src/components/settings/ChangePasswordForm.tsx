import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { Lock, Eye, EyeOff, Shield } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { changePassword, ApiError } from '@/services/settings'
import { useAuthContext } from '@/contexts/AuthContext'

// Minimum password length - matches backend (6 chars in dev)
const MIN_PASSWORD_LENGTH = 6

export function ChangePasswordForm() {
  const { t } = useTranslation('auth')
  const navigate = useNavigate()
  const { logout } = useAuthContext()

  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [showCurrentPassword, setShowCurrentPassword] = useState(false)
  const [showNewPassword, setShowNewPassword] = useState(false)
  const [showConfirmPassword, setShowConfirmPassword] = useState(false)
  const [error, setError] = useState('')
  const [isLoading, setIsLoading] = useState(false)

  const isPasswordValid = newPassword.length >= MIN_PASSWORD_LENGTH

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError('')

    // Validate new password meets minimum length
    if (!isPasswordValid) {
      setError(t('changePassword.tooShort', { count: MIN_PASSWORD_LENGTH }))
      return
    }

    // Check passwords match
    if (newPassword !== confirmPassword) {
      setError(t('forgotPassword.reset.passwordsDoNotMatch'))
      return
    }

    // Check new is different from current
    if (currentPassword === newPassword) {
      setError(t('changePassword.mustBeDifferent'))
      return
    }

    setIsLoading(true)

    try {
      await changePassword({ currentPassword, newPassword })

      toast.success(t('changePassword.success'))

      // Log out user since all sessions were revoked
      await logout()
      navigate('/login')
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message)
      } else {
        setError(t('changePassword.failed'))
      }
    } finally {
      setIsLoading(false)
    }
  }

  const isFormValid =
    currentPassword.length > 0 &&
    isPasswordValid &&
    newPassword === confirmPassword &&
    currentPassword !== newPassword

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
                value={currentPassword}
                onChange={(e) => setCurrentPassword(e.target.value)}
                className="pl-10 pr-10 focus:border-blue-600 focus:ring-blue-600/20"
                required
                autoComplete="current-password"
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
          </div>

          {/* New Password */}
          <div className="space-y-2">
            <Label htmlFor="newPassword">{t('changePassword.newPassword')}</Label>
            <div className="relative group">
              <Lock className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground group-focus-within:text-blue-600 transition-colors" />
              <Input
                id="newPassword"
                type={showNewPassword ? 'text' : 'password'}
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                className="pl-10 pr-10 focus:border-blue-600 focus:ring-blue-600/20"
                required
                autoComplete="new-password"
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
            {newPassword.length > 0 && newPassword.length < MIN_PASSWORD_LENGTH && (
              <p className="text-sm text-amber-600 dark:text-amber-500 font-medium">
                {t('changePassword.minLength', { count: MIN_PASSWORD_LENGTH })}
              </p>
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
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                className="pl-10 pr-10 focus:border-blue-600 focus:ring-blue-600/20"
                required
                autoComplete="new-password"
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
            {confirmPassword && (
              <p
                className={`text-xs ${
                  newPassword === confirmPassword
                    ? 'text-green-600'
                    : 'text-destructive'
                }`}
              >
                {newPassword === confirmPassword
                  ? t('forgotPassword.reset.passwordsMatch')
                  : t('forgotPassword.reset.passwordsDoNotMatch')}
              </p>
            )}
          </div>

          {/* Error */}
          {error && (
            <div className="p-4 rounded-lg bg-destructive/10 border border-destructive/20">
              <p className="text-sm text-destructive font-medium">{error}</p>
            </div>
          )}

          {/* Submit */}
          <Button
            type="submit"
            disabled={isLoading || !isFormValid}
            className="w-full bg-gradient-to-r from-blue-700 to-cyan-700 hover:from-blue-800 hover:to-cyan-800 text-white shadow-lg"
          >
            {isLoading ? t('changePassword.submitting') : t('changePassword.submit')}
          </Button>
        </form>
      </CardContent>
    </Card>
  )
}
